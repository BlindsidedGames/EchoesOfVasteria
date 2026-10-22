using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    /// <summary>Small, synchronous lifetime records captured before a crafted item can be pooled.</summary>
    public static class ForgeAnalyticsService
    {
        public static void RecordCraft(GameData.ForgeStats forge, GearItem item)
        {
            if (forge == null || item == null) return;
            if (forge.TotalCrafts < int.MaxValue) forge.TotalCrafts++;
            if (item.rarity) forge.HighestCraftedRarityTier = Math.Max(forge.HighestCraftedRarityTier, item.rarity.tierIndex);
            forge.HighestRollByStat ??= new Dictionary<string, float>();
            if (item.affixes == null) return;
            foreach (var affix in item.affixes)
            {
                if (affix?.stat == null || float.IsNaN(affix.value) || float.IsInfinity(affix.value)) continue;
                var id = string.IsNullOrWhiteSpace(affix.stat.id) ? affix.stat.name : affix.stat.id;
                if (!forge.HighestRollByStat.TryGetValue(id, out var best) || affix.value > best)
                    forge.HighestRollByStat[id] = affix.value;
            }
        }
    }

    public enum ConversionType { Ingot, Crystal, Chunk, Core }
}
