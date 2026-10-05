using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TimelessEchoes.Farming
{
    /// <summary>
    /// Closed legacy namespace plus a per-bank sequence journal. Pending results and committed
    /// gaps survive snapshots; only a contiguous durable prefix can lose its individual receipts.
    /// No clock/age cap can discard outstanding work. All economic changes remain proposals.
    /// </summary>
    public static class FarmJournal
    {
        public const int CurrentFormat = 2;
        private const string Prefix = "farm2/";

        public static void UpgradeLegacy(FarmState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.FormatVersion == CurrentFormat)
            {
                if (!ValidState(state)) throw new InvalidOperationException("Invalid farm journal bounds or lineage.");
                return;
            }
            if (state.FormatVersion != 1) throw new InvalidOperationException("Unsupported farm journal format.");
            if (state.LastIssuedSequence != 0 || state.CommittedThroughSequence != 0 || state.CompletedSequences?.Count > 0)
                throw new InvalidOperationException("Ambiguous legacy sequence state; original records preserved.");
            state.Operations ??= new Dictionary<string, FarmOperationReceipt>();
            state.PendingCredits ??= new Dictionary<string, FarmPendingCredit>();
            state.HarvestedBatchIds ??= new HashSet<string>();
            state.CompletedSequences ??= new Dictionary<long, FarmOperationReceipt>();
            // Preserve unknown history and markers belonging to any live/unknown bed or operation.
            var liveBatches = new HashSet<string>();
            var knownBatches = new HashSet<string>();
            var opaqueBatches = new HashSet<string>();
            if (state.Beds != null)
                foreach (var pair in state.Beds)
                    if (pair.Value?.IsPlanted == true)
                    {
                        liveBatches.Add(pair.Value.BatchId);
                        if (FarmCommands.OriginalBedIds.Contains(pair.Key)) pair.Value.LegacyBatchId = pair.Value.BatchId;
                    }
            foreach (var pair in state.Operations)
            {
                var known = IsKnownFingerprint(pair.Value?.Fingerprint);
                if (pair.Value?.BatchIds != null)
                    foreach (var id in pair.Value.BatchIds)
                        if (id != null) (known ? knownBatches : opaqueBatches).Add(id);
            }
            foreach (var id in knownBatches)
                if (!liveBatches.Contains(id) && !opaqueBatches.Contains(id)) state.HarvestedBatchIds.Remove(id);
            foreach (var id in state.Operations.Keys.ToArray())
                if (IsKnownFingerprint(state.Operations[id]?.Fingerprint))
                {
                    if (state.PendingCredits.TryGetValue(id, out var pending))
                    {
                        // A retained matching receipt is durable proof this stale intent already paid.
                        // Conflicting or opaque intents remain available for explicit recovery.
                        if (pending == null || state.Operations[id].Fingerprint != CreditFingerprint(pending)) continue;
                        state.PendingCredits.Remove(id);
                    }
                    state.Operations.Remove(id);
                }
            // Retained legacy pending IDs are the complete whitelist. No new UUID may enter it.
            state.JournalLineage = Guid.NewGuid().ToString("N");
            state.LastIssuedSequence = state.CommittedThroughSequence = 0;
            state.FormatVersion = CurrentFormat;
        }

        public static bool IsKnownFingerprint(string value)
        {
            if (value == "prepare-original" || value == "adventure-radish:0" || value == "adventure-radish:1" ||
                value == "harvest-ready" || value == "harvest-fields" || value == "harvest-auto" ||
                value == "plant-radish:" + FarmCommands.WestBedId || value == "plant-radish:" + FarmCommands.EastBedId) return true;
            if (value == null) return false;
            if (value.StartsWith("build:", StringComparison.Ordinal)) return FarmCommands.KnownBuild(value.Substring(6));
            if (value.StartsWith("water:", StringComparison.Ordinal)) return FarmCommands.KnownBed(value.Substring(6));
            var parts = value.Split(':');
            if (parts.Length != 3) return false;
            return parts[0] == "plant" ? FarmCommands.KnownBed(parts[1]) && FarmCommands.KnownRecipe(parts[2]) :
                parts[0] == "repeat" ? FarmCommands.KnownBed(parts[1]) && (parts[2] == "0" || parts[2] == "1") :
                parts[0] == "seed" && FarmCommands.KnownSeed(parts[1]) && (parts[2] == "0" || parts[2] == "1");
        }

        /// <summary>Pure UI reservation: a failed validation/write consumes no sequence or gap.</summary>
        public static string NextOperation(FarmState state, string fingerprint)
        {
            if (!ValidState(state) || state.LastIssuedSequence == long.MaxValue || !IsKnownFingerprint(fingerprint))
                return null;
            return Token(state.JournalLineage, state.LastIssuedSequence + 1, fingerprint);
        }

        /// <summary>Mutable completion intent, captured with its fixed selection by ordinary saves.</summary>
        public static string StageCredit(FarmState state, bool rolled, DateTime utcNow)
        {
            var id = NextOperation(state, "adventure-radish:" + (rolled ? "1" : "0"));
            if (id == null) return null;
            state.PendingCredits ??= new Dictionary<string, FarmPendingCredit>();
            state.PendingCredits.Add(id, new FarmPendingCredit { Rolled = rolled, CompletedAtUtcTicks = utcNow.Ticks });
            state.LastIssuedSequence++;
            return id;
        }

        public static string CreditFingerprint(FarmPendingCredit pending) => string.IsNullOrEmpty(pending.SeedId) ? "adventure-radish:" + (pending.Rolled ? "1" : "0") : "seed:" + pending.SeedId + ":" + (pending.Rolled ? "1" : "0");
        public static string StageSeed(FarmState state, string seedId, bool rolled, DateTime now)
        {
            var pending = new FarmPendingCredit { SeedId = seedId, Rolled = rolled, CompletedAtUtcTicks = now.Ticks };
            var id = NextOperation(state, CreditFingerprint(pending));
            if (id == null) return null;
            state.PendingCredits.Add(id, pending); state.LastIssuedSequence++; return id;
        }

        public static bool TrySequence(FarmState state, string id, out long sequence, out string fingerprint)
        {
            sequence = 0; fingerprint = null;
            if (!ValidState(state) || id == null || !id.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            var parts = id.Split('/');
            if (parts.Length != 4 || parts[1] != state.JournalLineage ||
                !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out sequence) || sequence <= 0)
                return false;
            fingerprint = Uri.UnescapeDataString(parts[3]);
            return IsKnownFingerprint(fingerprint) && id == Token(state.JournalLineage, sequence, fingerprint);
        }

        public static FarmCommandStatus Inspect(FarmState state, string id, string fingerprint, out string reason)
        {
            reason = null;
            if (!ValidState(state)) { reason = "InvalidFarmJournal"; return FarmCommandStatus.Rejected; }
            if (TrySequence(state, id, out var sequence, out var encoded))
            {
                if (encoded != fingerprint) { reason = "OperationIdConflict"; return FarmCommandStatus.Rejected; }
                if (sequence <= state.CommittedThroughSequence) return FarmCommandStatus.AlreadyApplied;
                if (state.CompletedSequences?.TryGetValue(sequence, out var receipt) == true)
                {
                    if (receipt?.Fingerprint == fingerprint) return FarmCommandStatus.AlreadyApplied;
                    reason = "OperationIdConflict"; return FarmCommandStatus.Rejected;
                }
                if (state.PendingCredits?.TryGetValue(id, out var pending) == true)
                {
                    if (sequence <= state.LastIssuedSequence && pending != null &&
                        fingerprint == CreditFingerprint(pending))
                        return FarmCommandStatus.Accepted;
                    reason = "PendingRollConflict"; return FarmCommandStatus.Rejected;
                }
                // Only a new UI command can issue a sequence inside its successful candidate.
                if (sequence == state.LastIssuedSequence + 1 && state.LastIssuedSequence < long.MaxValue &&
                    !fingerprint.StartsWith("adventure-radish:", StringComparison.Ordinal) && !fingerprint.StartsWith("seed:", StringComparison.Ordinal)) return FarmCommandStatus.Accepted;
                reason = "UnreservedOperation"; return FarmCommandStatus.Rejected;
            }
            // A migrated legacy completion can settle only an existing fixed intent.
            if (id != null && !id.StartsWith(Prefix, StringComparison.Ordinal) &&
                state.PendingCredits?.TryGetValue(id, out var legacy) == true && legacy != null &&
                fingerprint == CreditFingerprint(legacy))
            {
                if (state.Operations?.TryGetValue(id, out var receipt) == true)
                {
                    if (receipt?.Fingerprint == fingerprint) return FarmCommandStatus.AlreadyApplied;
                    reason = "OperationIdConflict"; return FarmCommandStatus.Rejected;
                }
                return FarmCommandStatus.Accepted;
            }
            reason = id?.StartsWith(Prefix, StringComparison.Ordinal) == true ? "ForeignOrInvalidOperation" : "LegacyOperationSealed";
            return FarmCommandStatus.Rejected;
        }

        internal static FarmOperationReceipt AddReceipt(FarmState candidate, string id, string fingerprint, long ticks)
        {
            var receipt = new FarmOperationReceipt { Fingerprint = fingerprint, CommittedAtUtcTicks = ticks };
            if (TrySequence(candidate, id, out var sequence, out _))
            {
                candidate.LastIssuedSequence = Math.Max(candidate.LastIssuedSequence, sequence);
                candidate.CompletedSequences ??= new Dictionary<long, FarmOperationReceipt>();
                candidate.CompletedSequences.Add(sequence, receipt);
            }
            // Legacy pending IDs are sealed immediately after their intent is consumed; no receipt grows.
            return receipt;
        }

        internal static void CompactCommitted(FarmState candidate)
        {
            while (candidate.CommittedThroughSequence < candidate.LastIssuedSequence &&
                candidate.CompletedSequences.TryGetValue(candidate.CommittedThroughSequence + 1, out var receipt) &&
                receipt != null && IsKnownFingerprint(receipt.Fingerprint))
            {
                candidate.CompletedSequences.Remove(candidate.CommittedThroughSequence + 1);
                candidate.CommittedThroughSequence++;
            }
        }

        public static bool IsCommittedCredit(FarmState state, string id) =>
            TrySequence(state, id, out _, out var fingerprint) && (fingerprint.StartsWith("adventure-radish:", StringComparison.Ordinal) || fingerprint.StartsWith("seed:", StringComparison.Ordinal)) &&
            Inspect(state, id, fingerprint, out _) == FarmCommandStatus.AlreadyApplied;

        public static bool IsCommittedPlant(FarmState state, string id, string bedId) =>
            TrySequence(state, id, out var sequence, out var fingerprint) && sequence <= state.LastIssuedSequence &&
            fingerprint == "plant-radish:" + bedId &&
            Inspect(state, id, fingerprint, out _) == FarmCommandStatus.AlreadyApplied;

        public static bool HasValidBounds(FarmState state) => ValidState(state);

        private static bool ValidState(FarmState state) => state?.FormatVersion == CurrentFormat &&
            Guid.TryParseExact(state.JournalLineage, "N", out _) && state.CommittedThroughSequence >= 0 &&
            state.LastIssuedSequence >= state.CommittedThroughSequence;

        private static string Token(string lineage, long sequence, string fingerprint) =>
            Prefix + lineage + "/" + sequence.ToString(CultureInfo.InvariantCulture) + "/" + Uri.EscapeDataString(fingerprint);
    }
}
