using System.Collections.Generic;
using MPUIKIT;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI.Cauldron
{
    /// <summary>
    /// Handles pie chart rendering for Cauldron odds display.
    /// </summary>
    public class CauldronPieChartPresenter : MonoBehaviour
    {
        [SerializeField] private List<MPImageBasic> pieSlices;
        private float[] _fractionsBuffer;

        /// <summary>
        /// Refreshes the pie chart display based on effective weights.
        /// </summary>
        /// <param name="weights">Current effective weights snapshot.</param>
        /// <param name="config">Cauldron configuration for slice colors.</param>
        public void Refresh(CauldronManager.EffectiveWeightsSnapshot weights, CauldronConfig config)
        {
            if (pieSlices == null || pieSlices.Count == 0) return;
            if (config == null) return;

            var sliceData = BuildSliceData(weights, config);
            RenderSlices(sliceData);
        }

        private List<(Color color, float weight)> BuildSliceData(
            CauldronManager.EffectiveWeightsSnapshot weights,
            CauldronConfig config)
        {
            // Check if subcategories are active (sum of AE weights > 0)
            var hasSub = (weights.wAEFarming
                          + weights.wAEFishing
                          + weights.wAEMining
                          + weights.wAEWoodcutting
                          + weights.wAELooting
                          + weights.wAECombat) > 0f;

            var slices = new List<(Color color, float weight)>();

            if (hasSub)
            {
                // Include: Nothing, AE subcategories, Buff, Lowest, Evas, Vast, Infinity
                slices.Add((config.sliceNothing, weights.wNothing));
                slices.Add((config.sliceAEFarming, weights.wAEFarming));
                slices.Add((config.sliceAEFishing, weights.wAEFishing));
                slices.Add((config.sliceAEMining, weights.wAEMining));
                slices.Add((config.sliceAEWoodcutting, weights.wAEWoodcutting));
                slices.Add((config.sliceAELooting, weights.wAELooting));
                slices.Add((config.sliceAECombat, weights.wAECombat));
                slices.Add((config.sliceBuff, weights.wBuff));
                slices.Add((config.sliceLowest, weights.wLow));
                slices.Add((config.sliceEvas, weights.wX2));
                slices.Add((config.sliceVast, weights.wX10));
                slices.Add((config.sliceInfinity, weights.wInfinity));
            }
            else
            {
                // Fall back to legacy single Alter-Echo slice (no subcategories)
                slices.Add((config.sliceNothing, weights.wNothing));
                slices.Add((config.sliceBuff, weights.wBuff));
                slices.Add((config.sliceLowest, weights.wLow));
                slices.Add((config.sliceEvas, weights.wX2));
                slices.Add((config.sliceVast, weights.wX10));
                slices.Add((config.sliceInfinity, weights.wInfinity));
            }

            return slices;
        }

        private void RenderSlices(List<(Color color, float weight)> slices)
        {
            // Calculate total weight
            var total = 0f;
            for (var i = 0; i < slices.Count; i++)
                total += Mathf.Max(0f, slices[i].weight);

            if (total <= 0f)
            {
                // No weights, disable all slices
                for (var i = 0; i < pieSlices.Count; i++)
                    if (pieSlices[i] != null) pieSlices[i].enabled = false;
                return;
            }

            // Background is at index 0; overlays are 1..N using the same layering logic as forge
            var overlayCapacity = Mathf.Max(0, pieSlices.Count - 1);
            var sliceCount = Mathf.Min(overlayCapacity, slices.Count);

            // Set sibling indices for proper layering
            for (var i = 0; i < sliceCount; i++)
            {
                var img = pieSlices[i + 1];
                if (img != null) img.transform.SetSiblingIndex(i + 1);
            }

            // Allocate fractions buffer if needed
            if (_fractionsBuffer == null || _fractionsBuffer.Length < sliceCount)
                _fractionsBuffer = new float[Mathf.NextPowerOfTwo(sliceCount)];

            var fractions = _fractionsBuffer;
            for (var i = 0; i < sliceCount; i++)
                fractions[i] = Mathf.Max(0f, slices[i].weight) / total;

            // Render slices in reverse order (layering)
            var used = 0f;
            for (var layer = 0; layer < sliceCount; layer++)
            {
                var weightIndex = sliceCount - 1 - layer; // reverse like forge
                var img = pieSlices[layer + 1];
                if (img == null)
                {
                    used += fractions[weightIndex];
                    continue;
                }

                var fill = layer == 0 ? 1f : Mathf.Clamp01(1f - used);
                used += fractions[weightIndex];

                img.enabled = fill > 0f;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = 2;
                img.fillClockwise = true;
                img.fillAmount = fill;
                img.color = slices[weightIndex].color;
                var rt = img.rectTransform;
                if (rt != null) rt.localEulerAngles = Vector3.zero;
            }

            // Hide unused slices
            for (var i = sliceCount + 1; i < pieSlices.Count; i++)
                if (pieSlices[i] != null) pieSlices[i].enabled = false;
        }
    }
}
