using Blindsided.Utilities;
using System.Collections.Generic;
using TimelessEchoes.Gear;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitForgeScreen
    {
        private VisualElement oddsRows;
        private readonly List<(VisualElement swatch, Label name, Label value)> oddsLabels = new();
        private void BuildOddsLegend(VisualElement parent)
        {
            oddsLabels.Clear();
            var legend = new VisualElement { name = "forge-odds-legend" };
            legend.AddToClassList("forge-odds-legend"); parent.Add(legend);
            LocalizedLabel(legend, "forge.rarity-odds", "Rarity odds", ToolkitControls.TextRole.Heading);
            oddsRows = new VisualElement(); oddsRows.AddToClassList("forge-odds-rows"); legend.Add(oddsRows);
        }
        private static float ValidWeight(float weight) => float.IsNaN(weight) || float.IsInfinity(weight) ? 0 : Mathf.Max(0, weight);
        private bool IsRarityKnown(RaritySO rarity)
        {
            if (!rarity) return false;
            if (session.HasResult && session.Pending?.rarity == rarity) return true;
            foreach (var slot in equipment.Slots)
                if (equipment.GetEquipped(slot)?.rarity == rarity) return true;
            foreach (var binding in definition.catalog.cores)
                if (binding.core && binding.core.tierIndex == rarity.tierIndex) {
                    var ingot = binding.ingotResource ? binding.ingotResource : binding.core.requiredIngot;
                    return ingot && resources.IsUnlocked(ingot);
                }
            return false;
        }
        private Color RarityColour(RaritySO rarity) => IsRarityKnown(rarity) ? rarity.color : new Color32(126, 112, 108, 255);
        private void RefreshOddsLegend(List<(RaritySO r, float w)> weights)
        {
            while (oddsLabels.Count < weights.Count) {
                var row = new VisualElement(); row.AddToClassList("forge-odds-row"); oddsRows.Add(row);
                var swatch = new VisualElement(); swatch.AddToClassList("forge-odds-swatch"); row.Add(swatch);
                var name = Label(row, ""); name.AddToClassList("forge-odds-name");
                var value = Label(row, ""); value.AddToClassList("forge-odds-value");
                oddsLabels.Add((swatch, name, value));
            }
            float total = 0; foreach (var entry in weights) total += ValidWeight(entry.w);
            for (var i = 0; i < oddsLabels.Count; i++) {
                var labels = oddsLabels[i]; labels.name.parent.style.display = i < weights.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (i >= weights.Count) continue;
                var entry = weights[i]; labels.swatch.style.backgroundColor = RarityColour(entry.r);
                labels.name.text = IsRarityKnown(entry.r) ? entry.r.GetName() : "???";
                var percent = total > 0 ? ValidWeight(entry.w) / total * 100 : 0;
                labels.name.parent.EnableInClassList("forge-odds-zero", percent <= 0);
                labels.value.text = percent > 0 && percent < .001f ? "<" + LocaleNumbers.PercentPoints(.001, "0.###") : percent.ToString("0.###", LocaleNumbers.Culture) + LocaleNumbers.PercentSuffix;
            }
        }
    }
}
