using TimelessEchoes.UI.Toolkit;
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
            rows.Add((ToolkitLocalization.Text("cauldron.reward-nothing", "Nothing"), cur.wNothing, nxt.wNothing, config.sliceNothing));

            if (hasAE)
            {
                rows.Add((ToolkitLocalization.Text("cauldron.reward-farming", "Farming"), cur.wAEFarming, nxt.wAEFarming, config.sliceAEFarming));
                rows.Add((ToolkitLocalization.Text("cauldron.reward-fishing", "Fishing"), cur.wAEFishing, nxt.wAEFishing, config.sliceAEFishing));
                rows.Add((ToolkitLocalization.Text("cauldron.reward-mining", "Mining"), cur.wAEMining, nxt.wAEMining, config.sliceAEMining));
                rows.Add((ToolkitLocalization.Text("cauldron.reward-logging", "Logging"), cur.wAEWoodcutting, nxt.wAEWoodcutting, config.sliceAEWoodcutting));
                rows.Add((ToolkitLocalization.Text("cauldron.reward-looting", "Looting"), cur.wAELooting, nxt.wAELooting, config.sliceAELooting));
                rows.Add((ToolkitLocalization.Text("cauldron.reward-combat", "Combat"), cur.wAECombat, nxt.wAECombat, config.sliceAECombat));
            }

            rows.Add((ToolkitLocalization.Text("cauldron.reward-buffs", "Buffs"), cur.wBuff, nxt.wBuff, config.sliceBuff));
            rows.Add((ToolkitLocalization.Text("cauldron.reward-lowest", "Lowest"), cur.wLow, nxt.wLow, config.sliceLowest));
            rows.Add((ToolkitLocalization.Text("cauldron.reward-blessing", "Blessing"), cur.wX2, nxt.wX2, config.sliceEvas));
            rows.Add((ToolkitLocalization.Text("cauldron.reward-surge", "Surge"), cur.wX10, nxt.wX10, config.sliceVast));
            rows.Add((ToolkitLocalization.Text("cauldron.reward-eternal", "Eternal"), cur.wInfinity, nxt.wInfinity, config.sliceInfinity));

            return rows;
        }


}}
