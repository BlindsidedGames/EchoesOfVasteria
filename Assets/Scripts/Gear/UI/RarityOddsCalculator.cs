using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.Utilities;
using UnityEngine;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Gear.UI
{
    public static class RarityOddsCalculator
    {
        /// <summary>
        /// Computes rarity weight lines and raw weights for a given core, applying level scaling and pity rules.
        /// </summary>
        public static (List<string> lines, List<(RaritySO r, float w)> weights) BuildRarityWeightInfo(CoreSO core)
        {
            var rarities = AssetCache.GetAll<RaritySO>().OrderBy(r => r.tierIndex).ToList();
            var svc = CraftingService.Instance ?? Object.FindFirstObjectByType<CraftingService>();
            var conf = svc != null ? svc.Config : null;
            var o = Oracle.oracle;
            var level = o != null && o.saveData != null ? Mathf.Max(0, o.saveData.CraftingMasteryLevel) : 0;
            var craftsSince = o != null && o.saveData != null ? Mathf.Max(0, o.saveData.PityCraftsSinceLast) : 0;
            var pityMinTier = 0;
            if (conf != null && !UpgradeFeatureToggle.DisableCraftingPity)
            {
                if (craftsSince >= conf.pityMythicWithin) pityMinTier = 5;
                else if (craftsSince >= conf.pityLegendaryWithin) pityMinTier = 4;
                else if (craftsSince >= conf.pityEpicWithin) pityMinTier = 3;
                else if (craftsSince >= conf.pityRareWithin) pityMinTier = 2;
            }

            var weights = new List<(RaritySO r, float w)>();
            foreach (var r in rarities)
            {
                var baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
                var bonus = r != null && conf != null && conf.enableLevelScaling
                    ? core.GetRarityWeightPerLevel(r) * level
                    : 0f;
                var w = Mathf.Max(0f, baseW + bonus);
                if (r != null && r.tierIndex < pityMinTier)
                    w = 0f;
                weights.Add((r, w));
            }

            var total = weights.Sum(t => t.w);
            var lines = new List<string>(rarities.Count);
            foreach (var (r, w) in weights)
            {
                var p = total > 0f ? w / total : 0f;
                var name = r != null ? r.GetName() : "(null)";
                lines.Add($"{name}: {p * 100f:0.000}%");
            }

            return (lines, weights);
        }
    }
}


