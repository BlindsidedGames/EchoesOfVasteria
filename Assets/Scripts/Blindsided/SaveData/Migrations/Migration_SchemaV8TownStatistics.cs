using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Gear;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>Project lifetime history after older migrations have consumed legacy telemetry.</summary>
    internal sealed class Migration_SchemaV8TownStatistics : IPostVersionSaveMigration
    {
        public int? TargetSchema => 8;
        public string TargetVersion => null;
        public string Id => "SchemaV8TownStatistics";
        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var old = data.Forge ?? new GameData.ForgeStats();
            // Earlier wire formats do not contain this field; their default zero is not evidence of a craft.
            var best = data.SchemaVersion < 8 ? -1 : old.HighestCraftedRarityTier;
            foreach (var rarity in AssetCache.GetAll<RaritySO>(""))
                if (rarity && old.CraftsByRarity != null && old.CraftsByRarity.TryGetValue(rarity.name, out var count) && count > 0)
                    best = Math.Max(best, rarity.tierIndex);
            var rolls = new Dictionary<string, float>(old.HighestRollByStat ?? new Dictionary<string, float>());
            if (old.StatRolls != null)
                foreach (var pair in old.StatRolls)
                    if (pair.Value != null && !float.IsNaN(pair.Value.max) && !float.IsInfinity(pair.Value.max) &&
                        (!rolls.TryGetValue(pair.Key, out var value) || pair.Value.max > value)) rolls[pair.Key] = pair.Value.max;
            data.Forge = new GameData.ForgeStats
            {
                TotalCrafts = old.TotalCrafts, TotalSalvaged = old.TotalSalvaged,
                HighestCraftedRarityTier = best, HighestRollByStat = rolls
            };
            var cauldron = data.CauldronTotals;
            data.CauldronTotals = new GameData.CauldronTotalsRecord
            {
                TotalTastings = cauldron?.TotalTastings ?? 0, TotalCards = cauldron?.TotalCards ?? 0
            };
            // Inventory and XP cannot reconstruct past plot completions or exact payouts.
            data.Farm ??= new TimelessEchoes.Farming.FarmState();
            if (data.SchemaVersion < 8) data.Farm.StatisticsSinceUpdate = true;
        }
    }
}
