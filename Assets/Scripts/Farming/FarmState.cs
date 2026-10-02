using System;
using System.Collections.Generic;

namespace TimelessEchoes.Farming
{
    /// <summary>Save-owned state. Unknown dictionary entries survive all prototype commands.</summary>
    public sealed class FarmState
    {
        public int FormatVersion = 2;
        public string JournalLineage = Guid.NewGuid().ToString("N");
        public long LastIssuedSequence;
        public long CommittedThroughSequence;
        // Only committed operations beyond an unresolved gap. Settled history is a watermark.
        public Dictionary<long, FarmOperationReceipt> CompletedSequences = new Dictionary<long, FarmOperationReceipt>();
        public bool OriginalBedsPrepared;
        public string OriginalBedsBuildOperationId;
        public Dictionary<string, FarmBedState> Beds = new Dictionary<string, FarmBedState>();
        public Dictionary<string, FarmSeedState> Seeds = new Dictionary<string, FarmSeedState>();
        public Dictionary<string, FarmOperationReceipt> Operations = new Dictionary<string, FarmOperationReceipt>();
        // Selected completion results waiting for a successful durable credit. A routine
        // snapshot can preserve these without revealing/granting an uncommitted pack.
        public Dictionary<string, FarmPendingCredit> PendingCredits = new Dictionary<string, FarmPendingCredit>();
        public HashSet<string> HarvestedBatchIds = new HashSet<string>();

        public FarmState DeepClone()
        {
            var copy = new FarmState
            {
                FormatVersion = FormatVersion,
                JournalLineage = JournalLineage,
                LastIssuedSequence = LastIssuedSequence,
                CommittedThroughSequence = CommittedThroughSequence,
                OriginalBedsPrepared = OriginalBedsPrepared,
                OriginalBedsBuildOperationId = OriginalBedsBuildOperationId,
                HarvestedBatchIds = HarvestedBatchIds == null ? new HashSet<string>() : new HashSet<string>(HarvestedBatchIds)
            };
            if (Beds != null)
                foreach (var pair in Beds) copy.Beds[pair.Key] = pair.Value?.DeepClone();
            if (Seeds != null)
                foreach (var pair in Seeds) copy.Seeds[pair.Key] = pair.Value?.DeepClone();
            if (Operations != null)
                foreach (var pair in Operations) copy.Operations[pair.Key] = pair.Value?.DeepClone();
            if (PendingCredits != null)
                foreach (var pair in PendingCredits) copy.PendingCredits[pair.Key] = pair.Value?.DeepClone();
            if (CompletedSequences != null)
                foreach (var pair in CompletedSequences) copy.CompletedSequences[pair.Key] = pair.Value?.DeepClone();
            return copy;
        }
    }

    public sealed class FarmBedState
    {
        public bool Unlocked;
        public string BatchId;
        public string RecipeId;
        public double DurationSeconds;
        public double ElapsedSeconds;
        public int HarvestYield;
        public long LastGrowthUtcTicks;
        // Bounded replay protection for a paid batch reintroduced into this bed.
        public long LastHarvestedPlantSequence;
        public string LegacyBatchId;

        public bool IsPlanted => !string.IsNullOrEmpty(BatchId);
        public bool IsReady => IsPlanted && DurationSeconds > 0 && ElapsedSeconds >= DurationSeconds;
        public FarmBedState DeepClone() => (FarmBedState)MemberwiseClone();
    }

    public sealed class FarmSeedState
    {
        public long Quantity;
        public long LifetimeAcquired;
        public bool IsDiscovered => LifetimeAcquired > 0;
        public FarmSeedState DeepClone() => (FarmSeedState)MemberwiseClone();
    }

    public sealed class FarmOperationReceipt
    {
        public string Fingerprint;
        public long CommittedAtUtcTicks;
        public List<string> BatchIds = new List<string>();

        public FarmOperationReceipt DeepClone() => new FarmOperationReceipt
        {
            Fingerprint = Fingerprint,
            CommittedAtUtcTicks = CommittedAtUtcTicks,
            BatchIds = BatchIds == null ? null : new List<string>(BatchIds)
        };
    }

    public sealed class FarmPendingCredit
    {
        public bool Rolled;
        public long CompletedAtUtcTicks;
        public FarmPendingCredit DeepClone() => (FarmPendingCredit)MemberwiseClone();
    }

    /// <summary>Development recipe values, not approved release balancing.</summary>
    [Serializable]
    public sealed class FarmTuning
    {
        public double RadishGrowthSeconds = 30 * 60;
        public int RadishYield = 10;
        public int OriginalBedsLogCost = 10;
        public int OriginalBedsStickCost = 20;
    }
}
