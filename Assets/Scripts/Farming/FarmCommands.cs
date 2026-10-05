using System;
using System.Collections.Generic;

namespace TimelessEchoes.Farming
{
    public enum FarmCommandStatus { Accepted, AlreadyApplied, NoChange, Rejected }

    /// <summary>
    /// A proposal only. The save coordinator must validate/apply inventory deltas and commit
    /// Candidate in the same snapshot, then publish it. Never publish a failed-write candidate.
    /// </summary>
    public sealed class FarmCommandResult
    {
        public FarmCommandStatus Status;
        public string Reason;
        public FarmState Candidate;
        public Dictionary<string, double> ResourceDeltas = new Dictionary<string, double>();
        public List<string> HarvestedBeds = new List<string>();
        public List<string> CompletedQuests = new List<string>();
        internal FarmOperationReceipt OperationReceipt;
        public bool Accepted => Status == FarmCommandStatus.Accepted;
    }

    public static partial class FarmCommands
    {
        public const string WestBedId = "farm.original.west";
        public const string EastBedId = "farm.original.east";
        public const string RadishSeedId = "seed.radish";
        public const string RadishRecipeId = "recipe.radish.v1";
        public static readonly IReadOnlyList<string> OriginalBedIds = Array.AsReadOnly(new[] { WestBedId, EastBedId });

        public static FarmCommandResult PrepareBeds(FarmState state, string operationId, DateTime utcNow, FarmTuning tuning)
        {
            var result = Begin(state, operationId, "prepare-original", utcNow);
            if (!result.Accepted) return result;
            if (tuning == null || tuning.OriginalBedsLogCost < 0 || tuning.OriginalBedsStickCost < 0)
                return Reject("InvalidBuildTuning");
            if (result.Candidate.OriginalBedsPrepared) return Unchanged("OriginalBedsAlreadyPrepared");
            foreach (var id in OriginalBedIds)
            {
                if (!result.Candidate.Beds.TryGetValue(id, out var bed) || bed == null)
                    result.Candidate.Beds[id] = bed = new FarmBedState();
                bed.Unlocked = true;
            }
            result.Candidate.OriginalBedsPrepared = true;
            result.Candidate.OriginalBedsBuildOperationId = operationId;
            result.ResourceDeltas["Log"] = -tuning.OriginalBedsLogCost;
            result.ResourceDeltas["Stick"] = -tuning.OriginalBedsStickCost;
            return result;
        }

        /// <summary>
        /// Adapter supplies an already-selected eligible primary Radish completion and its stable
        /// ID. Record misses too, so retry cannot reroll. This method performs no random roll,
        /// grants no starter/pity packs, and is never called for harvest or abandoned work.
        /// </summary>
        public static FarmCommandResult RecordRadishAdventureCompletion(FarmState state, string operationId,
            DateTime utcNow, bool eligiblePrimaryRadishCompletion, bool packRolled)
        {
            if (!eligiblePrimaryRadishCompletion) return Reject("IneligibleAdventureCompletion");
            if (string.IsNullOrWhiteSpace(operationId)) return Reject("MissingOperationId");
            FarmPendingCredit selected = null;
            if (state?.PendingCredits?.TryGetValue(operationId, out selected) == true &&
                selected != null && selected.Rolled != packRolled) return Reject("PendingRollConflict");
            if (state?.FormatVersion == FarmJournal.CurrentFormat && selected != null &&
                FarmJournal.TrySequence(state, operationId, out _, out var encoded) &&
                encoded == "adventure-radish:" + (packRolled ? "1" : "0") &&
                FarmJournal.IsCommittedCredit(state, operationId))
            {
                // Defensive acknowledgement of a stale intent next to durable commit proof.
                // Persist cleanup in the same lane, without another seed or a live-only removal.
                var acknowledged = state.DeepClone();
                acknowledged.PendingCredits.Remove(operationId);
                return new FarmCommandResult { Status = FarmCommandStatus.Accepted, Candidate = acknowledged,
                    Reason = "CommittedIntentAcknowledged" };
            }
            var result = Begin(state, operationId, "adventure-radish:" + (packRolled ? "1" : "0"), utcNow);
            if (!result.Accepted) return result;
            if (result.Candidate.PendingCredits.TryGetValue(operationId, out var pending) &&
                pending != null && pending.Rolled != packRolled)
                return Reject("PendingRollConflict");
            result.Candidate.PendingCredits.Remove(operationId);
            if (!packRolled) return result;
            result.ResourceDeltas[SeedResourceName(RadishSeedId)] = 1;
            return result;
        }

        public static FarmCommandResult Plant(FarmState state, string bedId, string operationId, DateTime utcNow, FarmTuning tuning)
        {
            var result = Begin(state, operationId, "plant-radish:" + bedId, utcNow);
            if (!result.Accepted) return result;
            if (!IsOriginalBed(bedId)) return Reject("UnknownBed");
            if (tuning == null || !Finite(tuning.RadishGrowthSeconds) || tuning.RadishGrowthSeconds <= 0 || tuning.RadishYield <= 0)
                return Reject("InvalidRecipeTuning");
            if (!result.Candidate.Beds.TryGetValue(bedId, out var bed) || bed == null || !bed.Unlocked)
                return Reject("BedLocked");
            if (bed.IsPlanted) return Reject("BedOccupied");
            // Inventory validation belongs to the durable transaction, including legacy commands.
            result.ResourceDeltas[SeedResourceName(RadishSeedId)] = -1;
            bed.BatchId = "radish:" + operationId;
            bed.RecipeId = RadishRecipeId;
            bed.DurationSeconds = tuning.RadishGrowthSeconds;
            bed.HarvestYield = tuning.RadishYield;
            bed.ElapsedSeconds = 0;
            bed.LastGrowthUtcTicks = UtcTicks(utcNow);
            result.OperationReceipt.BatchIds.Add(bed.BatchId);
            return result;
        }

        /// <summary>
        /// Supply explicitly measured seconds during active play. Missing elapsed time means zero;
        /// persisted UTC timestamps are legacy metadata and never accrue closed-game growth.
        /// Each paid batch is finite; elapsed time is never banked for a later planting.
        /// </summary>
        public static FarmCommandResult AdvanceGrowth(FarmState state, DateTime utcNow, double? activeElapsedSeconds = null)
        {
            if (activeElapsedSeconds.HasValue && (!Finite(activeElapsedSeconds.Value) || activeElapsedSeconds.Value < 0))
                return Reject("InvalidActiveElapsed");
            if (!HasGrowingOriginalBeds(state)) return Unchanged("NoGrowthChange");
            var candidate = (state ?? new FarmState()).DeepClone();
            bool changed = Settle(candidate, UtcTicks(utcNow), activeElapsedSeconds);
            return changed ? new FarmCommandResult { Status = FarmCommandStatus.Accepted, Candidate = candidate }
                : Unchanged("NoGrowthChange");
        }

        public static bool HasGrowingOriginalBeds(FarmState state)
        {
            if (state?.Beds == null) return false;
            foreach (var id in OriginalBedIds)
                if (state.Beds.TryGetValue(id, out var bed) && bed != null && bed.Unlocked && bed.IsPlanted &&
                    bed.RecipeId == RadishRecipeId && Finite(bed.DurationSeconds) && bed.DurationSeconds > 0 &&
                    Finite(bed.ElapsedSeconds) && bed.ElapsedSeconds >= 0 && bed.ElapsedSeconds < bed.DurationSeconds)
                    return true;
            return false;
        }

        /// <summary>
        /// Mutable save-contributor clock capture only. Economic candidates still deep-clone and
        /// commit immutable snapshots. This touches no inventory, intent, receipt or unknown bed.
        /// </summary>
        public static bool CaptureGrowthInPlace(FarmState state, DateTime utcNow, double? activeElapsedSeconds = null)
        {
            if (activeElapsedSeconds.HasValue && (!Finite(activeElapsedSeconds.Value) || activeElapsedSeconds.Value < 0))
                throw new ArgumentOutOfRangeException(nameof(activeElapsedSeconds));
            return HasGrowingOriginalBeds(state) && Settle(state, UtcTicks(utcNow), activeElapsedSeconds);
        }

        /// <summary>All currently ready original beds, their receipts and the yield form one proposal.</summary>
        public static FarmCommandResult HarvestReady(FarmState state, string operationId, DateTime utcNow,
            double? activeElapsedSeconds = null, double harvestYieldBonusPercent = 0)
        {
            var result = Begin(state, operationId, "harvest-ready", utcNow);
            if (!result.Accepted) return result;
            if (activeElapsedSeconds.HasValue && (!Finite(activeElapsedSeconds.Value) || activeElapsedSeconds.Value < 0))
                return Reject("InvalidActiveElapsed");
            Settle(result.Candidate, UtcTicks(utcNow), activeElapsedSeconds);
            double total = 0;
            foreach (var id in OriginalBedIds)
            {
                if (!result.Candidate.Beds.TryGetValue(id, out var bed) || bed == null || !bed.Unlocked ||
                    bed.RecipeId != RadishRecipeId || !bed.IsReady) continue;
                if (bed.HarvestYield <= 0 || result.Candidate.HarvestedBatchIds.Contains(bed.BatchId))
                    return Reject("InvalidOrPreviouslyHarvestedBatch");
                if (result.Candidate.FormatVersion == FarmJournal.CurrentFormat)
                {
                    var plantId = bed.BatchId.StartsWith("radish:", StringComparison.Ordinal) ? bed.BatchId.Substring(7) : null;
                    if (FarmJournal.TrySequence(result.Candidate, plantId, out var plantSequence, out var fingerprint))
                    {
                        if (fingerprint != "plant-radish:" + id || !FarmJournal.IsCommittedPlant(state, plantId, id) ||
                            plantSequence <= bed.LastHarvestedPlantSequence)
                            return Reject("InvalidOrPreviouslyHarvestedBatch");
                        bed.LastHarvestedPlantSequence = plantSequence;
                    }
                    else if (bed.BatchId != bed.LegacyBatchId) return Reject("InvalidOrPreviouslyHarvestedBatch");
                    bed.LegacyBatchId = null;
                    result.Candidate.HarvestedBatchIds.Remove(bed.BatchId);
                }
                else result.Candidate.HarvestedBatchIds.Add(bed.BatchId);
                result.OperationReceipt.BatchIds.Add(bed.BatchId);
                result.HarvestedBeds.Add(id);
                total += bed.HarvestYield;
                bed.BatchId = null;
                bed.RecipeId = null;
                bed.DurationSeconds = 0;
                bed.ElapsedSeconds = 0;
                bed.HarvestYield = 0;
                bed.LastGrowthUtcTicks = UtcTicks(utcNow);
            }
            if (result.HarvestedBeds.Count == 0) return Reject("NoReadyBeds");
            if (!Finite(harvestYieldBonusPercent) || harvestYieldBonusPercent < 0 || harvestYieldBonusPercent > 70)
                return Reject("InvalidHarvestYieldBonus");
            // Final payout lives in the committed proposal; replay cannot re-evaluate card ownership.
            result.ResourceDeltas["Radish"] = total * (1 + harvestYieldBonusPercent / 100d);
            return result;
        }

        private static FarmCommandResult Begin(FarmState state, string operationId, string fingerprint, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(operationId)) return Reject("MissingOperationId");
            if (state?.FormatVersion != 1)
            {
                var status = FarmJournal.Inspect(state, operationId, fingerprint, out var reason);
                if (status != FarmCommandStatus.Accepted)
                    return new FarmCommandResult { Status = status, Reason = reason };
                var proposal = state.DeepClone();
                var journalReceipt = FarmJournal.AddReceipt(proposal, operationId, fingerprint, UtcTicks(utcNow));
                FarmJournal.CompactCommitted(proposal);
                return new FarmCommandResult { Status = FarmCommandStatus.Accepted, Candidate = proposal, OperationReceipt = journalReceipt };
            }
            if (state?.Operations != null && state.Operations.TryGetValue(operationId, out var receipt))
                return receipt != null && receipt.Fingerprint == fingerprint
                    ? new FarmCommandResult { Status = FarmCommandStatus.AlreadyApplied, Reason = "OperationAlreadyCommitted" }
                    : Reject("OperationIdConflict");
            var candidate = (state ?? new FarmState()).DeepClone();
            candidate.Operations[operationId] = new FarmOperationReceipt { Fingerprint = fingerprint, CommittedAtUtcTicks = UtcTicks(utcNow) };
            return new FarmCommandResult { Status = FarmCommandStatus.Accepted, Candidate = candidate, OperationReceipt = candidate.Operations[operationId] };
        }

        private static bool Settle(FarmState state, long nowTicks, double? activeSeconds)
        {
            bool changed = false;
            foreach (var id in OriginalBedIds)
            {
                if (!state.Beds.TryGetValue(id, out var bed) || bed == null || !bed.Unlocked || !bed.IsPlanted ||
                    bed.RecipeId != RadishRecipeId || !Finite(bed.DurationSeconds) || bed.DurationSeconds <= 0 ||
                    !Finite(bed.ElapsedSeconds) || bed.ElapsedSeconds < 0 || bed.IsReady) continue;
                double seconds = activeSeconds ?? 0;
                double elapsed = Math.Min(bed.DurationSeconds, bed.ElapsedSeconds + seconds);
                changed |= elapsed != bed.ElapsedSeconds || nowTicks != bed.LastGrowthUtcTicks;
                bed.ElapsedSeconds = elapsed;
                bed.LastGrowthUtcTicks = nowTicks;
            }
            return changed;
        }

        private static long UtcTicks(DateTime time) => time.Kind == DateTimeKind.Local ? time.ToUniversalTime().Ticks : time.Ticks;
        private static bool IsOriginalBed(string id) => id == WestBedId || id == EastBedId;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static FarmCommandResult Reject(string reason) => new FarmCommandResult { Status = FarmCommandStatus.Rejected, Reason = reason };
        private static FarmCommandResult Unchanged(string reason) => new FarmCommandResult { Status = FarmCommandStatus.NoChange, Reason = reason };
    }
}
