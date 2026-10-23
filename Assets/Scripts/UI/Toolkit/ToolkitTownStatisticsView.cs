using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>One statistics page for all town production, with no duplicated progression counters.</summary>
    public sealed class ToolkitTownStatisticsView
    {
        private readonly VisualElement content;
        private readonly ToolkitTheme theme;
        private readonly Dictionary<string, bool> expanded = new();
        private string lastSignature;
        private readonly StatDefSO[] stats = AssetCache.GetAll<StatDefSO>("").Where(s => s).OrderBy(s => StatSortOrder.GetIndex(s.heroMapping)).ToArray();
        private readonly RaritySO[] rarities = AssetCache.GetAll<RaritySO>("").Where(r => r).ToArray();

        public ToolkitTownStatisticsView(VisualElement parent, ToolkitTheme theme)
        {
            this.theme = theme;
            var scroll = ToolkitControls.RecessedScroll(parent, "town-statistics", theme);
            scroll.style.flexGrow = 1;
            content = scroll.contentContainer;
            content.AddToClassList("town-statistics");
        }

        public void Refresh()
        {
            var data = Oracle.oracle?.saveData;
            if (data == null) return;
            var forge = data.Forge; var cauldron = data.CauldronTotals; var farm = data.Farm;
            var signature = ToolkitLocalization.Revision.ToString();
            if (lastSignature == signature) { RefreshValues(); return; }
            lastSignature = signature;
            content.Clear();
            var section = Section("forge", "Forge");
            Row(section, "crafted", "Items crafted", Number(forge?.TotalCrafts ?? 0));
            Row(section, "salvaged", "Items salvaged", Number(forge?.TotalSalvaged ?? 0));
            var rarity = rarities.FirstOrDefault(r => r.tierIndex == (forge?.HighestCraftedRarityTier ?? -1));
            Row(section, "highest-rarity", "Highest rarity crafted", rarity ? rarity.GetName() : ToolkitLocalization.Text("town-stats.none", "None"));
            var title = ToolkitControls.Text(ToolkitLocalization.Text("town-stats.best-rolls", "Best stat rolls"), ToolkitControls.TextRole.Body);
            title.AddToClassList("town-statistics-roll-title"); section.Add(title);
            var rolls = new VisualElement(); rolls.AddToClassList("town-statistics-rolls"); section.Add(rolls);
            foreach (var stat in stats)
            {
                float? value = null;
                if (forge?.HighestRollByStat != null)
                    foreach (var pair in forge.HighestRollByStat)
                        if (StatDefUtils.MatchesStatId(pair.Key, stat) && (!value.HasValue || pair.Value > value)) value = pair.Value;
                StatIconLookup.TryGetIcon(stat.heroMapping, out var sprite);
                var row = AddRow(rolls, stat.GetName(), value.HasValue ? Number(value.Value) + (stat.isPercent ? LocaleNumbers.PercentSuffix : "") : "—", sprite);
                row.name = "town-stat-roll-" + stat.id;
                row.AddToClassList("town-statistics-roll");
            }
            section = Section("cauldron", "Cauldron");
            Row(section, "tastings", "Tastings", Number(cauldron?.TotalTastings ?? 0));
            Row(section, "cards", "Cards gained", Number(cauldron?.TotalCards ?? 0));
            section = Section("fields", "Fields");
            if (farm?.StatisticsSinceUpdate == true)
            {
                var since = ToolkitControls.Text(ToolkitLocalization.Text("town-stats.since-update", "Since this update"), ToolkitControls.TextRole.Caption);
                since.AddToClassList("town-statistics-since-update"); section.Add(since);
            }
            Row(section, "harvests", "Harvests", Number(farm?.TotalHarvests ?? 0));
            Row(section, "crops", "Crops gathered", Number(farm?.CropsGathered ?? 0));
            Row(section, "logs", "Logs gathered", Number(farm?.LogsGathered ?? 0));
        }

        // Lifetime counters may tick while this page is open. Keep controls, focus and scroll position.
        private void RefreshValues()
        {
            var data = Oracle.oracle?.saveData;
            void Set(string id, string text)
            {
                var label = content.Q(id)?.Q<Label>(className: "town-statistics-value");
                if (label != null && label.text != text) label.text = text;
            }
            Set("town-statistics-crafted", Number(data?.Forge?.TotalCrafts ?? 0));
            Set("town-statistics-salvaged", Number(data?.Forge?.TotalSalvaged ?? 0));
            var rarity = rarities.FirstOrDefault(r => r.tierIndex == (data?.Forge?.HighestCraftedRarityTier ?? -1));
            Set("town-statistics-highest-rarity", rarity ? rarity.GetName() : ToolkitLocalization.Text("town-stats.none", "None"));
            Set("town-statistics-tastings", Number(data?.CauldronTotals?.TotalTastings ?? 0));
            Set("town-statistics-cards", Number(data?.CauldronTotals?.TotalCards ?? 0));
            Set("town-statistics-harvests", Number(data?.Farm?.TotalHarvests ?? 0));
            Set("town-statistics-crops", Number(data?.Farm?.CropsGathered ?? 0));
            Set("town-statistics-logs", Number(data?.Farm?.LogsGathered ?? 0));
            foreach (var stat in stats)
            {
                float? best = null;
                if (data?.Forge?.HighestRollByStat != null)
                    foreach (var pair in data.Forge.HighestRollByStat)
                        if (StatDefUtils.MatchesStatId(pair.Key, stat) && (!best.HasValue || pair.Value > best)) best = pair.Value;
                Set("town-stat-roll-" + stat.id, best.HasValue ? Number(best.Value) + (stat.isPercent ? LocaleNumbers.PercentSuffix : "") : "—");
            }
        }

        private VisualElement Section(string id, string fallback)
        {
            var section = new ToolkitDisclosure(ToolkitLocalization.Text("town-stats." + id, fallback), theme, !expanded.TryGetValue(id, out var open) || open);
            section.name = "town-statistics-" + id;
            section.AddToClassList("town-statistics-section");
            section.Changed += value => expanded[id] = value;
            content.Add(section); return section.Content;
        }
        private void Row(VisualElement parent, string id, string fallback, string value) =>
            AddRow(parent, ToolkitLocalization.Text("town-stats." + id, fallback), value).name = "town-statistics-" + id;
        private static string Number(double value) => value.ToString("N2", LocaleNumbers.Culture).TrimEnd('0').TrimEnd(Blindsided.Utilities.LocaleNumbers.Culture.NumberFormat.NumberDecimalSeparator.ToCharArray());
        private static VisualElement AddRow(VisualElement parent, string label, string value, Sprite sprite = null)
        {
            var row = new VisualElement(); row.AddToClassList("town-statistics-row"); parent.Add(row);
            if (sprite)
            {
                var icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("town-statistics-icon");
                icon.style.width = sprite.rect.width * 16 / sprite.pixelsPerUnit;
                icon.style.height = sprite.rect.height * 16 / sprite.pixelsPerUnit;
                row.Add(icon);
            }
            var name = ToolkitControls.Text(label, ToolkitControls.TextRole.Body); name.AddToClassList("town-statistics-name"); row.Add(name);
            var number = ToolkitControls.Text(value, ToolkitControls.TextRole.Body); number.AddToClassList("town-statistics-value"); row.Add(number);
            return row;
        }
    }
}
