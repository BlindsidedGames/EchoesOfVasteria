using System.Collections.Generic;
using System.Linq;
using MPUIKIT;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
        private void SelectCore(CoreSO core)
        {
            var changed = core != selectedCore;
            // Stop auto-crafting if the player changes the selected core
            if (isAutoCrafting && changed)
                StopAutoCrafting();
            selectedCore = core;
            // Persist last selected core per save
            var o = Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.SavedPreferences != null)
                o.saveData.SavedPreferences.LastSelectedForgeCore = selectedCore != null ? selectedCore.name : null;
            // update visual selections using mapped cores
            foreach (var slot in coreSlots)
            {
                if (slot == null) continue;
                var mappedCore = coreSlotCoreByRef.TryGetValue(slot, out var mc) ? mc : slot.Core;
                slot.SetSelected(mappedCore == selectedCore);
            }

            // update selected core/ingot previews
            var previewSlot = GetSlotForCore(selectedCore);
            UpdateSelectedCorePreview(previewSlot);
            UpdateIngotPreview(selectedCore);
            UpdateIngotCraftPreview(selectedCore);
            UpdateCrystalCraftPreview(selectedCore);
            UpdateChunkCraftPreview(selectedCore);
            UpdateCoreCraftPreview(selectedCore);
            UpdateMaxCraftsText();
            ThrottledRefreshOdds();
            // Clear any pending result text when the player changes core
            if (changed)
            {
                if (resultText != null) resultText.text = string.Empty;
                ClearResultTierText();
            }
            RefreshActionButtons();
        }

        private void OnCoreSlotClicked(CoreSlotUIReferences slot, CoreSO core)
        {
            Debug.Log(core != null
                ? $"ForgeWindowUI: Core clicked -> {core.name}"
                : "ForgeWindowUI: Core clicked -> (null)");
            SelectCore(core);
        }

        private void RefreshOdds()
        {
            var core = selectedCore ?? (cores != null && cores.Count > 0 ? cores[0] : null);
            if (core == null)
            {
                RefreshOddsPieChart(null);
                return;
            }

            var info = RarityOddsCalculator.BuildRarityWeightInfo(core);
            RefreshOddsPieChart(info.weights);
        }

        private void ThrottledRefreshOdds()
        {
            // refresh at most 5 times per second
            if (Time.unscaledTime < nextOddsRefreshTime)
                return;
            nextOddsRefreshTime = Time.unscaledTime + 0.2f;
            RefreshOdds();
        }

        private void RefreshOddsPieChart(List<(RaritySO r, float w)> weights)
        {
            if (oddsPieSlices == null || oddsPieSlices.Count == 0)
                return;

            if (weights == null || weights.Count == 0)
            {
                for (var i = 0; i < oddsPieSlices.Count; i++)
                    if (oddsPieSlices[i] != null)
                        oddsPieSlices[i].enabled = false;
                return;
            }

            var total = 0f;
            for (var i = 0; i < weights.Count; i++) total += Mathf.Max(0f, weights[i].w);
            if (total <= 0f)
            {
                for (var i = 0; i < oddsPieSlices.Count; i++)
                    if (oddsPieSlices[i] != null)
                        oddsPieSlices[i].enabled = false;
                return;
            }

            // Layered approach with background at index 0, reversed order
            var overlayCapacity = Mathf.Max(0, oddsPieSlices.Count - 1);
            var sliceCount = Mathf.Min(overlayCapacity, weights.Count);

            // Keep background (index 0) where it is; ensure overlays are in indices [1..sliceCount]
            for (var i = 0; i < sliceCount; i++)
            {
                var img = oddsPieSlices[i + 1];
                if (img != null)
                    img.transform.SetSiblingIndex(i + 1);
            }

            // Precompute normalized fractions of weights we will use
            var fractions = new float[sliceCount];
            for (var i = 0; i < sliceCount; i++)
                fractions[i] = Mathf.Max(0f, weights[i].w) / total;

            var used = 0f;
            for (var layer = 0; layer < sliceCount; layer++)
            {
                // Reverse the mapping so visual order is reversed
                var weightIndex = sliceCount - 1 - layer;
                var img = oddsPieSlices[layer + 1];
                if (img == null) { used += fractions[weightIndex]; continue; }

                var fill = layer == 0 ? 1f : Mathf.Clamp01(1f - used);
                used += fractions[weightIndex];

                img.enabled = fill > 0f;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = 2;
                img.fillClockwise = true;
                img.fillAmount = fill;
                img.color = weights[weightIndex].r != null ? weights[weightIndex].r.color : Color.white;
                var rt = img.rectTransform;
                if (rt != null)
                    rt.localEulerAngles = Vector3.zero;
            }

            // Disable any extra overlay slices beyond available weights (leave background alone)
            for (var i = sliceCount + 1; i < oddsPieSlices.Count; i++)
                if (oddsPieSlices[i] != null)
                    oddsPieSlices[i].enabled = false;
        }

        private void UpdateCoreWeightTooltipText()
        {
            if (coreWeightHoverText == null)
                return;

            var core = selectedCore ?? (cores != null && cores.Count > 0 ? cores[0] : null);
            if (core == null)
            {
                coreWeightHoverText.text = string.Empty;
                return;
            }

            var info = RarityOddsCalculator.BuildRarityWeightInfo(core);

            // Ensure TMP can render color tags
            coreWeightHoverText.richText = true;

            // Build colored lines: color the rarity name and trailing ':' using the rarity color
            float total = 0f;
            for (int i = 0; i < info.weights.Count; i++)
                total += Mathf.Max(0f, info.weights[i].w);

            System.Text.StringBuilder sb = new System.Text.StringBuilder(info.weights.Count * 16);
            for (int i = 0; i < info.weights.Count; i++)
            {
                var (r, wRaw) = info.weights[i];
                float w = Mathf.Max(0f, wRaw);
                float p = total > 0f ? w / total : 0f;
                string name = r != null ? r.GetName() : "(null)";

                if (r != null)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(r.color);
                    sb.Append("<color=#").Append(hex).Append(">").Append(name).Append(":</color> ");
                }
                else
                {
                    sb.Append(name).Append(": ");
                }

                sb.Append((p * 100f).ToString("0.000")).Append('%');

                if (i < info.weights.Count - 1)
                    sb.Append('\n');
            }

            coreWeightHoverText.text = sb.ToString();
        }

        private void ShowCoreWeightTooltip()
        {
            UpdateCoreWeightTooltipText();
            if (coreWeightHoverObject != null)
                coreWeightHoverObject.SetActive(true);
        }

        private void HideCoreWeightTooltip()
        {
            if (coreWeightHoverObject != null)
                coreWeightHoverObject.SetActive(false);
        }

        private void ForceRefreshAllCoreSlots()
        {
            // Force refresh all core slots to ensure UI consistency
            foreach (var slot in coreSlots)
                if (slot != null)
                    slot.Refresh();
        }

        private CoreSlotUIReferences GetSlotForCore(CoreSO core)
        {
            if (core == null) return null;
            foreach (var s in coreSlots)
            {
                if (s == null) continue;
                var mapped = coreSlotCoreByRef.TryGetValue(s, out var mc) ? mc : s.Core;
                if (mapped == core) return s;
            }

            return null;
        }
    }
}
