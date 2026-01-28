using System.Collections.Generic;
using System.Text;
using TMPro;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.UI.Cauldron
{
    /// <summary>
    /// Handles weights preview text and tooltip display.
    /// </summary>
    public class CauldronWeightsPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text firstPercentText;
        [SerializeField] private TMP_Text spriteColText;
        [SerializeField] private TMP_Text nextPercentText;
        [SerializeField] private TMP_Text nameColText;
        [SerializeField] private GameObject tooltipObject;

        // Reusable StringBuilders - DRY optimization
        private readonly StringBuilder _colFirst = new(256);
        private readonly StringBuilder _colSprite = new(256);
        private readonly StringBuilder _colNext = new(256);
        private readonly StringBuilder _colName = new(256);

        /// <summary>
        /// Refreshes the weights text display for current and next level.
        /// </summary>
        /// <param name="currentLevel">Current Eva level.</param>
        /// <param name="cauldron">CauldronManager instance.</param>
        /// <param name="config">CauldronConfig for colors.</param>
        public void Refresh(int currentLevel, CauldronManager cauldron, CauldronConfig config)
        {
            if (cauldron == null || config == null) return;
            if (firstPercentText == null && spriteColText == null && nextPercentText == null && nameColText == null) return;

            var lvl = Mathf.Max(1, currentLevel);
            var next = lvl + 1;

            var cur = cauldron.GetEffectiveWeightsAtLevel(lvl);
            var nxt = cauldron.GetEffectiveWeightsAtLevel(next);

            var tCurrent = ComputeTotal(cur);
            if (tCurrent <= 0f)
            {
                ClearAllText();
                return;
            }

            var tNext = ComputeTotal(nxt);
            if (tNext <= 0f) tNext = 1f;

            var rows = BuildRows(cur, nxt, config);
            FormatColumns(rows, tCurrent, tNext);
        }

        private float ComputeTotal(CauldronManager.EffectiveWeightsSnapshot w)
        {
            return w.wNothing + w.wAEFarming + w.wAEFishing + w.wAEMining
                   + w.wAEWoodcutting + w.wAELooting + w.wAECombat
                   + w.wBuff + w.wLow + w.wX2 + w.wX10 + w.wInfinity;
        }

        private void ClearAllText()
        {
            if (firstPercentText != null) firstPercentText.text = string.Empty;
            if (spriteColText != null) spriteColText.text = string.Empty;
            if (nextPercentText != null) nextPercentText.text = string.Empty;
            if (nameColText != null) nameColText.text = string.Empty;
        }

        private List<(string label, float current, float next, Color color)> BuildRows(
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

        private void FormatColumns(List<(string label, float current, float next, Color color)> rows,
            float tCurrent, float tNext)
        {
            _colFirst.Clear();
            _colSprite.Clear();
            _colNext.Clear();
            _colName.Clear();

            // Headers
            _colFirst.Append("<b>Current</b>\n");
            _colSprite.Append("<sprite=9>\n");
            _colNext.Append("<b>Next</b>\n");
            _colName.Append("\n");

            for (int i = 0; i < rows.Count; i++)
            {
                var hex = ColorUtility.ToHtmlStringRGB(rows[i].color);
                var cp = Mathf.Clamp01(rows[i].current / tCurrent) * 100f;
                var np = Mathf.Clamp01(rows[i].next / tNext) * 100f;

                if (i > 0)
                {
                    _colFirst.Append('\n');
                    _colSprite.Append('\n');
                    _colNext.Append('\n');
                    _colName.Append('\n');
                }

                _colFirst.Append($"<b>{cp:F2}%</b>");
                _colSprite.Append($"<sprite=9 color=#{hex}>");
                _colNext.Append($"{np:F2}%");
                _colName.Append($"* {rows[i].label}");
            }

            if (firstPercentText != null) firstPercentText.SetText(_colFirst);
            if (spriteColText != null) spriteColText.SetText(_colSprite);
            if (nextPercentText != null) nextPercentText.SetText(_colNext);
            if (nameColText != null) nameColText.SetText(_colName);
        }

        /// <summary>
        /// Shows the weights tooltip.
        /// </summary>
        public void ShowTooltip()
        {
            if (tooltipObject != null)
                tooltipObject.SetActive(true);
        }

        /// <summary>
        /// Hides the weights tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipObject != null)
                tooltipObject.SetActive(false);
        }
    }
}
