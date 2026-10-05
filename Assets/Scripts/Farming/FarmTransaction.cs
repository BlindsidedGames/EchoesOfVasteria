using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;

namespace TimelessEchoes.Farming
{
    /// <summary>Inventory and farm proposal form one immutable durable snapshot.</summary>
    public static class FarmTransaction
    {
        public static bool TryPrepare(GameData captured, FarmCommandResult proposal, out GameData candidate, out string error)
        {
            candidate = null; error = null;
            if (proposal == null || !proposal.Accepted || proposal.Candidate == null)
            { error = proposal?.Reason ?? "No farm proposal."; return false; }
            candidate = CurrentSaveCodec.Clone(captured);
            candidate.Farm = proposal.Candidate.DeepClone();
            HashSet<string> migrated;
            try { migrated = FarmSeedResources.Upgrade(candidate); }
            catch (Exception ex) { candidate = null; error = ex.Message; return false; }
            foreach (var delta in proposal.ResourceDeltas)
            {
                candidate.Resources.TryGetValue(delta.Key, out var entry);
                entry ??= new GameData.ResourceEntry { Tier = 1 };
                if (double.IsNaN(delta.Value) || double.IsInfinity(delta.Value) ||
                    double.IsNaN(entry.Amount) || double.IsInfinity(entry.Amount) || entry.Amount < 0 ||
                    entry.Amount + delta.Value < 0 || double.IsInfinity(entry.Amount + delta.Value) ||
                    FarmCommands.CropNames.Any(crop => delta.Key == FarmCommands.SeedResourceName("seed." + crop)) &&
                    entry.Amount + delta.Value - entry.Amount != delta.Value)
                { candidate = null; error = "Insufficient or invalid " + delta.Key + "."; return false; }
                entry.Amount += delta.Value;
                if (delta.Value > 0) entry.Earned = true;
                candidate.Resources[delta.Key] = entry;
                candidate.ResourceStats.TryGetValue(delta.Key, out var stats);
                stats ??= new GameData.ResourceRecord();
                if (double.IsNaN(stats.TotalReceived) || double.IsInfinity(stats.TotalReceived) || stats.TotalReceived < 0 ||
                    double.IsNaN(stats.TotalSpent) || double.IsInfinity(stats.TotalSpent) || stats.TotalSpent < 0)
                { candidate = null; error = "Invalid resource ledger."; return false; }
                var seedResource = FarmCommands.CropNames.Any(crop => delta.Key == FarmCommands.SeedResourceName("seed." + crop));
                var oldReceived = stats.TotalReceived; var oldSpent = stats.TotalSpent;
                if (delta.Value > 0) stats.TotalReceived += delta.Value;
                else stats.TotalSpent -= delta.Value;
                if (double.IsInfinity(stats.TotalReceived) || double.IsInfinity(stats.TotalSpent) ||
                    seedResource && (delta.Value > 0 ? stats.TotalReceived - oldReceived != delta.Value : stats.TotalSpent - oldSpent != -delta.Value))
                { candidate = null; error = "Resource ledger overflow."; return false; }
                candidate.ResourceStats[delta.Key] = stats;
            }
            if (captured.Farm?.OriginalBedsPrepared != true && candidate.Farm.OriginalBedsPrepared)
                candidate.Quests["Farm.PrepareBeds"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = DateTime.UtcNow.Ticks };
            foreach (var id in proposal.CompletedQuests)
                candidate.Quests[id] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = DateTime.UtcNow.Ticks };
            // Existing Oracle publication patches every touched resource in one inventory batch.
            // Zero deltas publish migration transfers without applying them or their stats again.
            foreach (var name in migrated)
                if (!proposal.ResourceDeltas.ContainsKey(name)) proposal.ResourceDeltas[name] = 0;
            return true;
        }
    }

    /// <summary>Converts only recognized historical seed stock on a detached save candidate.</summary>
    public static class FarmSeedResources
    {
        public const string MigrationId = "SchemaV7SeedResources";
        public static bool NeedsRepair(GameData data) => data?.Farm == null || data.Farm.SeedResourceRevision != 1 ||
            data.Farm.MigratedSeedIds == null || FarmCommands.CropNames.Any(crop => !data.Farm.MigratedSeedIds.Contains("seed." + crop) ||
                data.Farm.Seeds?.TryGetValue("seed." + crop, out var seed) == true && seed != null && seed.Quantity != 0);

        public static HashSet<string> Upgrade(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Farm?.SeedResourceRevision > 1) throw new InvalidOperationException("Unsupported seed resource revision.");
            var farm = data.Farm ?? new FarmState();
            var receipts = farm.MigratedSeedIds ?? new HashSet<string>();
            var converted = new Dictionary<string, (GameData.ResourceEntry entry, GameData.ResourceRecord stats)>();
            // Validate the whole known ledger before changing any balance or receipt.
            foreach (var crop in FarmCommands.CropNames)
            {
                var id = "seed." + crop;
                FarmSeedState seed = null;
                farm.Seeds?.TryGetValue(id, out seed);
                if (receipts.Contains(id))
                {
                    if (seed != null && seed.Quantity != 0) throw new InvalidOperationException("Seed migration receipt conflicts with residual stock: " + id);
                    continue;
                }
                if (seed == null) continue;
                if (seed.Quantity < 0 || seed.LifetimeAcquired < 0 || seed.Quantity > 9007199254740991L || seed.LifetimeAcquired > 9007199254740991L)
                    throw new InvalidOperationException("Invalid or unrepresentable legacy seed balance: " + id);
                var name = FarmCommands.SeedResourceName(id);
                data.Resources.TryGetValue(name, out var old);
                data.ResourceStats.TryGetValue(name, out var stats);
                var received = Math.Max(seed.Quantity, seed.LifetimeAcquired);
                converted[name] = (new GameData.ResourceEntry
                {
                    Amount = AddExact(old?.Amount ?? 0, seed.Quantity), Earned = old?.Earned == true || received > 0,
                    Tier = old?.Tier ?? 1, BestPerMinute = old?.BestPerMinute ?? 0
                }, new GameData.ResourceRecord
                {
                    TotalReceived = AddExact(stats?.TotalReceived ?? 0, received),
                    TotalSpent = AddExact(stats?.TotalSpent ?? 0, received - seed.Quantity)
                });
            }
            data.Farm = farm;
            farm.MigratedSeedIds = receipts;
            foreach (var pair in converted)
            {
                data.Resources[pair.Key] = pair.Value.entry;
                data.ResourceStats[pair.Key] = pair.Value.stats;
            }
            foreach (var crop in FarmCommands.CropNames)
            {
                var id = "seed." + crop;
                if (farm.Seeds?.TryGetValue(id, out var seed) == true && seed != null) seed.Quantity = 0;
                receipts.Add(id);
            }
            farm.SeedResourceRevision = 1;
            // The receipt is persisted with balances; unknown ledger entries stay opaque.
            data.SchemaVersion = Math.Max(data.SchemaVersion, 7);
            data.AppliedMigrationIds ??= new HashSet<string>();
            data.AppliedMigrationIds.Add(MigrationId);
            return new HashSet<string>(converted.Keys);
        }

        private static double AddExact(double value, long amount)
        {
            var result = value + amount;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || double.IsInfinity(result) || result - value != amount)
                throw new InvalidOperationException("Seed resource conversion would lose inventory precision.");
            return result;
        }
    }
}
