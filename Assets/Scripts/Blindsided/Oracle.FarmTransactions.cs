using System;
using Blindsided.SaveData;
using TimelessEchoes.Farming;
using TimelessEchoes.Upgrades;

namespace Blindsided
{
    public partial class Oracle
    {
        private bool _economicTransactionActive;

        // Task rewards follow the normal checkpoint policy. Validate and publish the
        // complete credit in memory; interval/town/lifecycle saves persist its receipt.
        public bool TryApplyFarmCredit(string operationId)
        {
            if (!CanBeginSaveMutation || !CanSave() || operationId == null) return false;
            var state = saveData.Farm;
            if (state?.PendingCredits?.TryGetValue(operationId, out var pending) != true || pending == null)
                return state != null && FarmJournal.IsCommittedCredit(state, operationId);
            var now = new DateTime(pending.CompletedAtUtcTicks, DateTimeKind.Utc);
            return TryApplyFarmProgress(current => !string.IsNullOrEmpty(pending.SeedId)
                ? FarmCommands.CreditSeed(current, operationId, now, pending.SeedId, pending.Rolled)
                : FarmCommands.RecordRadishAdventureCompletion(current, operationId, now, true, pending.Rolled));
        }

        internal bool TryApplyFarmProgress(Func<FarmState, FarmCommandResult> command)
        {
            if (!CanBeginSaveMutation || !CanSave()) return false;
            _economicTransactionActive = true;
            try
            {
                // ResourceManager owns live amounts between checkpoints. Copy only its
                // records, not every save contributor, before validating a harvest/credit.
                ResourceManager.Instance?.SaveState();
                var proposal = command(saveData.Farm);
                if (proposal.Status == FarmCommandStatus.AlreadyApplied) return true;
                if (!FarmTransaction.TryPrepareProgress(saveData, proposal, out var candidate, out _)) return false;
                saveData.Farm = candidate.Farm;
                saveData.SchemaVersion = candidate.SchemaVersion;
                saveData.AppliedMigrationIds = candidate.AppliedMigrationIds;
                var manager = ResourceManager.Instance;
                manager?.BeginBatch();
                try
                {
                    foreach (var delta in proposal.ResourceDeltas)
                    {
                        saveData.Resources[delta.Key] = candidate.Resources[delta.Key];
                        saveData.ResourceStats[delta.Key] = candidate.ResourceStats[delta.Key];
                        manager?.PublishCommittedResource(delta.Key, saveData.Resources[delta.Key], saveData.ResourceStats[delta.Key]);
                    }
                }
                finally { manager?.EndBatch(); }
                return true;
            }
            finally
            {
                _economicTransactionActive = false;
                // A reward callback can return to town and request a checkpoint.
                // Honor that request after publication, without saving every task.
                if (_savePending && CanSave()) { _savePending = false; RequestSave(); }
            }
        }

        /// <summary>
        /// First-slice main-thread lane: drain older saves, capture contributors, synchronously
        /// commit, then patch touched fields/caches before releasing the barrier. The disk payload
        /// is immutable. A process exit after commit reloads its receipts, never pays twice.
        /// </summary>
        public bool TryCommitFarmCommand(Func<FarmState, FarmCommandResult> command, out string error)
        {
            error = null;
            if (_economicTransactionActive || !CanBeginSaveMutation || !CanSave())
            { error = "Save is busy or requires recovery."; return false; }
            _economicTransactionActive = true;
            try
            {
                StopSaveCoordinatorMonitoring();
                CompleteActiveSaveBlocking();
                _savePending = false;
                if (!TryPrepareSnapshot(out error)) return false;
                var owner = saveData;
                var slot = CurrentSlot;
                owner.Farm ??= new FarmState();
                var proposal = command(owner.Farm);
                if (proposal.Status == FarmCommandStatus.AlreadyApplied) return true;
                if (!FarmTransaction.TryPrepare(owner, proposal, out var candidate, out error)) return false;
                var result = GetSaveResult(SaveManager.Instance.SaveDetailedAsync(candidate,
                    GetSlotDirectoryName(slot)), slot, waitForCompletion: true);
                HandleSaveResult(result, slot);
                if (!result.Succeeded) { error = result.Error; return false; }
                // No user callbacks run while the synchronous commit owns this frame. Validate
                // ownership anyway; never publish another slot's candidate after a reentrant load.
                if (!ReferenceEquals(owner, saveData) || CurrentSlot != slot)
                { RequireRuntimeReloadRecovery("The farm transaction committed", new InvalidOperationException("Save ownership changed before publication")); error = "Committed farm state requires reload."; return false; }
                try
                {
                    var preparedNow = candidate.Farm.OriginalBedsBuildOperationId != owner.Farm.OriginalBedsBuildOperationId;
                    owner.Farm = candidate.Farm;
                    if (preparedNow && candidate.Quests.TryGetValue("Farm.PrepareBeds", out var quest))
                        owner.Quests["Farm.PrepareBeds"] = quest;
                    foreach (var id in proposal.CompletedQuests) owner.Quests[id] = candidate.Quests[id];
                    var manager = ResourceManager.Instance;
                    manager?.BeginBatch();
                    try
                    {
                        foreach (var delta in proposal.ResourceDeltas)
                        {
                            owner.Resources[delta.Key] = candidate.Resources[delta.Key];
                            owner.ResourceStats[delta.Key] = candidate.ResourceStats[delta.Key];
                            manager?.PublishCommittedResource(delta.Key, owner.Resources[delta.Key], owner.ResourceStats[delta.Key]);
                        }
                    }
                    finally { manager?.EndBatch(); }
                    return true;
                }
                catch (Exception ex)
                {
                    // Authority already advanced. Block every subsequent write until reload;
                    // stale runtime contributors must not overwrite the committed receipt.
                    RequireRuntimeReloadRecovery("The farm transaction committed", ex);
                    error = "Farm committed; runtime publication requires reload: " + ex.Message;
                    return false;
                }
            }
            catch (Exception ex) { error = ex.Message; return false; }
            finally
            {
                _economicTransactionActive = false;
                if (_savePending && CanSave()) { _savePending = false; RequestSave(); }
            }
        }
    }
}
