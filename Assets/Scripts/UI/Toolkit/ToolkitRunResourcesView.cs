using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;

namespace TimelessEchoes.UI.Toolkit
{
    internal sealed class ToolkitRunResourcesView : VisualElement
    {
        private readonly Label heading, elapsed, killBonus, rates, empty;
        private readonly ToolkitRunDefinition definition;
        private readonly Image resourceIcon, timeIcon;
        private readonly ScrollView scroll;
        private readonly Dictionary<Resource, Entry> entries = new();
        private sealed class Entry { public VisualElement root; public Label name, amount, rate, total; }
        public ToolkitRunResourcesView(ToolkitRunDefinition definition)
        {
            this.definition = definition;
            AddToClassList("run-resources-view");
            var header = E(this, "row between run-resources-header");
            var title = E(header, "run-resources-title");
            var total = E(title, "row run-resources-total");
            resourceIcon = ToolkitGameplay.Icon(total, definition.ResourceIcon(0), 16);
            heading = L(total, "", "heading");
            ToolkitLocalization.BindTooltip(total, "run.summary.resources", "Resources gathered");
            rates = L(title, "", "small muted run-resources-rates"); rates.name = "run-resources-rates";
            var metadata = E(header, "run-resources-metadata");
            var time = E(metadata, "row run-resources-meta-line");
            timeIcon = ToolkitGameplay.Icon(time, definition.TimeIcon(0), 16);
            elapsed = L(time, "", "run-resources-time"); elapsed.name = "run-resources-time";
            ToolkitLocalization.BindTooltip(time, "run.summary.label.time", "Time");
            var bonus = E(metadata, "row run-resources-meta-line run-resources-bonus-line");
            ToolkitGameplay.Icon(bonus, definition.bonusIcon, 16);
            killBonus = L(bonus, "", "run-resources-bonus"); killBonus.name = "run-resources-bonus";
            ToolkitLocalization.BindTooltip(bonus, "run.summary.label.bonus", "Bonus");
            scroll = ToolkitGameplay.Scroll(this, "run-resource-list");
            scroll.contentContainer.AddToClassList("run-resource-grid");
            empty = L(scroll, "", "muted run-resources-empty");
        }
        private static string Amount(double value) => ToolkitControls.WithoutInlineTextSize(CalcUtils.FormatNumber(value, true));
        public void SetColumns(int columns)
        {
            EnableInClassList("run-resources-two", columns == 2);
            EnableInClassList("run-resources-three", columns == 3);
        }
        public void SetBottomClearance(float clearance) => scroll.contentContainer.style.paddingBottom = clearance;
        public void Refresh(IReadOnlyDictionary<Resource, double> resources, double seconds, double bonusPercent, double distance, double damage, int kills)
        {
            var multiplier = 1 + bonusPercent * .01;
            var total = resources.Values.Sum() * multiplier;
            resourceIcon.sprite = definition.ResourceIcon(total);
            timeIcon.sprite = definition.TimeIcon(seconds);
            heading.text = ToolkitLocalization.Text("run.total-label", "Total") + " " + Amount(total);
            elapsed.text = ToolkitControls.WithoutInlineTextSize(CalcUtils.FormatTime(seconds, mspace:false, shortForm:true));
            killBonus.text = "×" + multiplier.ToString("0.##", LocaleNumbers.Culture);
            var safeSeconds = Math.Max(.0001, seconds);
            rates.text = ToolkitLocalization.Text("run.compact-rates", "Dist/m: {0:N0} · DPS: {1:N0} · Kills/m: {2:N0}", distance * 60 / safeSeconds, damage / safeSeconds, kills * 60 / safeSeconds);
            foreach (var key in entries.Keys.Where(key => !resources.ContainsKey(key)).ToArray())
            { entries[key].root.RemoveFromHierarchy(); entries.Remove(key); }
            var position = 1;
            foreach (var pair in resources.OrderBy(pair => pair.Key.resourceID))
            {
                if (!entries.TryGetValue(pair.Key, out var entry))
                {
                    var row = E(scroll, "row run-resource-entry"); row.name = "run-resource-" + pair.Key.name;
                    ToolkitGameplay.Icon(row, pair.Key.icon, 20);
                    var body = E(row, "run-resource-body");
                    var identity = E(body, "row between run-resource-line");
                    var name = L(identity, "", "run-resource-name");
                    var amount = L(identity, "", "strong run-resource-amount");
                    var details = E(body, "row between run-resource-line");
                    entry = new Entry { root=row, name=name, amount=amount, rate=L(details, "", "small muted"), total=L(details, "", "small run-resource-return") };
                    entries.Add(pair.Key, entry);
                }
                if (scroll.contentContainer.IndexOf(entry.root) != position) scroll.contentContainer.Insert(position, entry.root);
                position++;
                entry.name.text = ToolkitLocalization.Name(pair.Key);
                entry.amount.text = Amount(pair.Value);
                entry.rate.text = ToolkitLocalization.Text("run.resource-per-minute", "{0} / min", Amount(pair.Value * 60 / safeSeconds));
                entry.total.text = bonusPercent > 0 ? ToolkitLocalization.Text("run.resource-return", "On return: {0}", Amount(pair.Value * (1 + bonusPercent * .01))) : "";
                entry.total.style.display = bonusPercent > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            empty.text = ToolkitLocalization.Text("run.portrait.empty", "Nothing gathered yet");
            empty.style.display = resources.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

