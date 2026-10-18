using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Blindsided;
using TimelessEchoes.Gear.UI;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Lifetime forge history belongs beside the other statistics, away from crafting controls.</summary>
    public sealed class ToolkitForgingStatisticsView
    {
        private readonly ForgeStatisticsPresentation presentation = new();
        private readonly Dictionary<string, ToolkitDisclosure> sections = new();
        private readonly VisualElement content;
        private readonly ToolkitTheme theme;
        private string lastText;

        public ToolkitForgingStatisticsView(VisualElement parent, ToolkitTheme theme)
        {
            this.theme = theme;
            var scroll = ToolkitControls.RecessedScroll(parent, "forging-statistics", theme);
            scroll.style.flexGrow = 1;
            content = scroll.contentContainer;
            content.AddToClassList("forging-statistics");
        }

        public void Refresh()
        {
            var text = presentation.BuildStatsText(Oracle.oracle?.saveData?.Forge, preserveSectionIds: true);
            if (text == lastText) return;
            lastText = text;
            var groups = Regex.Split(text, @"<size=105%><b>(.*?)</b></size>");
            for (var i = 1; i + 1 < groups.Length; i += 2)
            {
                var key = groups[i];
                if (!sections.TryGetValue(key, out var section))
                {
                    var title = ToolkitLocalization.Text("forge.stats.section-" + key.ToLowerInvariant().Replace(" ", "-"), key);
                    section = new ToolkitDisclosure(title, theme, key == "Totals");
                    section.AddToClassList("forging-statistics-section");
                    sections.Add(key, section); content.Add(section);
                }
                section.Content.Clear();
                var body = Regex.Replace(groups[i + 1], @"</?(?:size|b|mspace)(?:=[^>]*)?>", "");
                foreach (var line in body.Split('\n'))
                foreach (var part in line.Split('•'))
                {
                    var value = part.Trim(); if (value.Length == 0) continue;
                    var split = value.IndexOf(": ", StringComparison.Ordinal);
                    var row = new VisualElement(); row.AddToClassList("forging-statistics-row"); section.Content.Add(row);
                    var label = ToolkitControls.Text(split > 0 ? value.Substring(0, split) : value.TrimEnd(':'), ToolkitControls.TextRole.Body);
                    label.AddToClassList("forging-statistics-name"); row.Add(label);
                    if (split <= 0) continue;
                    var number = ToolkitControls.Text(value.Substring(split + 2), ToolkitControls.TextRole.Body);
                    number.AddToClassList("forging-statistics-value"); row.Add(number);
                }
            }
        }
    }
}
