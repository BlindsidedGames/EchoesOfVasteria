using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Upgrades;
namespace TimelessEchoes.UI.Cauldron { public static class CauldronWeightsPresentation {
        public static float ComputeTotal(CauldronManager.EffectiveWeightsSnapshot w)
        {
            return w.wNothing + w.wAEFarming + w.wAEFishing + w.wAEMining
                   + w.wAEWoodcutting + w.wAELooting + w.wAECombat
                   + w.wBuff + w.wLow + w.wX2 + w.wX10 + w.wInfinity;
        }

        public static List<(string label, float current, float next, Color color)> BuildRows(
            CauldronManager.EffectiveWeightsSnapshot cur,
            CauldronManager.EffectiveWeightsSnapshot nxt,
            CauldronConfig config)
        {
            // Determine if AE subcategories are present
            bool hasAE = (cur.wAEFarming + cur.wAEFishing + cur.wAEMining
                          + cur.wAEWoodcutting + cur.wAELooting + cur.wAECombat) > 0f;

            var rows = new List<(string label, float current, float next, Color color)>();
            rows.Add(("Nothing", cur.wNothing, nxt.wNothing, config.sliceNothing));

            if (hasAE)
            {
                rows.Add(("Farming", cur.wAEFarming, nxt.wAEFarming, config.sliceAEFarming));
                rows.Add(("Fishing", cur.wAEFishing, nxt.wAEFishing, config.sliceAEFishing));
                rows.Add(("Mining", cur.wAEMining, nxt.wAEMining, config.sliceAEMining));
                rows.Add(("Logging", cur.wAEWoodcutting, nxt.wAEWoodcutting, config.sliceAEWoodcutting));
                rows.Add(("Looting", cur.wAELooting, nxt.wAELooting, config.sliceAELooting));
                rows.Add(("Combat", cur.wAECombat, nxt.wAECombat, config.sliceAECombat));
            }

            rows.Add(("Buffs", cur.wBuff, nxt.wBuff, config.sliceBuff));
            rows.Add(("Lowest", cur.wLow, nxt.wLow, config.sliceLowest));
            rows.Add(("Blessing", cur.wX2, nxt.wX2, config.sliceEvas));
            rows.Add(("Surge", cur.wX10, nxt.wX10, config.sliceVast));
            rows.Add(("Eternal", cur.wInfinity, nxt.wInfinity, config.sliceInfinity));

            return rows;
        }


}}
