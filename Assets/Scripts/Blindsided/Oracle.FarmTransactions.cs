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
            => TryCommitFarmCommand(command, out _);

        // Commit a validated action to live state. The next interval, town or lifecycle
        // checkpoint persists its complete resource changes and replay receipts.
        public bool TryCommitFarmCommand(Func<FarmState, FarmCommandResult> command, out string error)
        {
            error = null;
            if (!CanBeginSaveMutation || !CanSave())
            { error = "Save is busy or requires recovery."; return false; }
            _economicTransactionActive = true;
            var publishing = false;
            try
            {
                ResourceManager.Instance?.SaveState();
                return TryPublishFarmProposal(command(saveData.Farm), out error, ref publishing);
            }
            catch (Exception ex)
            {
                if (publishing) RequireRuntimeReloadRecovery("A farm action failed during publication", ex,
                    checkpointCommitted: false, phase: "farm-apply");
                error = ex.Message;
                return false;
            }
            finally
            {
                _economicTransactionActive = false;
                // A callback can return to town. Capture only after publication completes.
                if (_savePending && CanSave()) { _savePending = false; RequestSave(); }
            }
        }

        // The shared background transaction owns the checkpoint for all of these harvests.
        internal void ApplyBackgroundFarmProposal(FarmCommandResult proposal)
        {
            if (!_economicTransactionActive)
                throw new InvalidOperationException("Background harvest requires an active transaction.");
            var publishing = false;
            if (!TryPublishFarmProposal(proposal, out var error, ref publishing))
                throw new InvalidOperationException(error ?? "Background harvest was rejected.");
        }

        private bool TryPublishFarmProposal(FarmCommandResult proposal, out string error, ref bool publishing)
        {
            error = null;
            if (proposal.Status == FarmCommandStatus.AlreadyApplied) return true;
            var owner = saveData;
            if (!FarmTransaction.TryPrepareProgress(owner, proposal, out var candidate, out error)) return false;
            publishing = true;
            var preparedNow = candidate.Farm.OriginalBedsBuildOperationId != owner.Farm.OriginalBedsBuildOperationId;
            owner.Farm = candidate.Farm;
            owner.SchemaVersion = candidate.SchemaVersion;
            owner.AppliedMigrationIds = candidate.AppliedMigrationIds;
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
    }
}
