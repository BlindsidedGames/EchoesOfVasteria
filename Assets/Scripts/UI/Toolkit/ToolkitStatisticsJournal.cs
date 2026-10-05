using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Enemies;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitStatisticsScreen
    {
        private int journalMap = -1, journalRunKey = -1;
        private VisualElement journalContent, journalRecent, journalCollection;
        private readonly List<Button> journalSelectors = new();
        private readonly List<(Resource resource, EnemyData enemy, Button button)> journalDiscoveries = new();
        private Label journalDiscoveryCount;
        private int journalKnown = -1;

        private void BuildMapJournal()
        {
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            journalSelectors.Clear();
            var layout = ToolkitGameplay.E(body, "journal-layout");
            var selectors = ToolkitGameplay.E(layout, "journal-selectors");
            for (var i = -1; i < definition.maps.Length; i++)
            {
                var index = i;
                var button = ToolkitGameplay.B(selectors, "", () => SelectJournalMap(index));
                button.name = "journal-map-" + (i + 1); button.AddToClassList("journal-selector");
                JournalArt(button, i < 0 ? definition.portrait : definition.maps[i].icon);
                Text(button, i < 0 ? ToolkitLocalization.Text("statistics.overall", "Overall") : ToolkitLocalization.Text("map." + definition.maps[i].config.name, definition.maps[i].label.Trim()), 7);
                if (i == definition.maps.Length - 1) button.AddToClassList("journal-selector-last");
                journalSelectors.Add(button);
            }
            journalContent = ToolkitGameplay.E(layout, "journal-content");
            SelectJournalMap(Mathf.Clamp(journalMap, -1, definition.maps.Length - 1));
        }
        public void SelectJournalMap(int index)
        {
            if (index < -1 || index >= definition.maps.Length || journalContent == null) return;
            journalMap = index; journalRunKey = -1; journalKnown = -1;
            journalContent.Clear(); summaries.Clear(); journalDiscoveries.Clear();
            for (var i = 0; i < journalSelectors.Count; i++) StyleSelection(journalSelectors[i], i == index + 1);
            var columns = ToolkitGameplay.E(journalContent, "journal-columns");
            var mainScroll = ToolkitControls.RecessedScroll(columns, "general-statistics", theme, definition.inset);
            mainScroll.parent.AddToClassList("journal-main");
            var main = mainScroll.contentContainer;
            var side = ToolkitGameplay.E(columns, "journal-side");
            Text(side, ToolkitLocalization.Text("journal.recent-runs", "Recent runs"), 8).AddToClassList("journal-section-title");
            journalRecent = ToolkitControls.RecessedScroll(side, "journal-run-scroll", theme, definition.inset).contentContainer;
            Text(main, ToolkitLocalization.Text("journal.lifetime", "Lifetime"), 8).AddToClassList("journal-section-title");
            AddSummary(main, index < 0 ? (ToolkitStatisticsDefinition.MapEntry?)null : definition.maps[index]);
            var summary = main.Q(className: "eov-stat-summary");
            summary.AddToClassList("journal-lifetime");
            summary.Q(className: "general-identity").style.display = DisplayStyle.None;
            journalCollection = ToolkitGameplay.E(main, "journal-collection");
            BuildJournalCollection();
            RefreshMapJournal();
        }
        private void RefreshMapJournal()
        {
            if (summaries.Count == 0) return;
            var data = journalMap < 0
                ? StatisticsPresentation.General(tracker, GameManager.Instance && GameManager.Instance.IsKillScalingMode)
                : StatisticsPresentation.Map(tracker.GetMapStats(definition.maps[journalMap].config) ?? new GameData.MapStatistics(), definition.maps[journalMap].killScaling);
            UpdateSummary(summaries[0], data);
            var key = tracker.RecentRuns.Count > 0 ? tracker.RecentRuns[tracker.RecentRuns.Count - 1].RunNumber : 0;
            if (key != journalRunKey) { journalRunKey = key; BuildJournalRecent(); }
            var known = journalDiscoveries.Count(d => d.resource ? d.resource.totalReceived > 0 : killTracker && killTracker.GetKills(d.enemy) > 0);
            if (known != journalKnown)
            {
                journalKnown = known;
                foreach (var d in journalDiscoveries)
                {
                    var discovered = d.resource ? d.resource.totalReceived > 0 : killTracker && killTracker.GetKills(d.enemy) > 0;
                    d.button.EnableInClassList("known", discovered);
                    d.button.Q<Image>().style.display = discovered ? DisplayStyle.Flex : DisplayStyle.None;
                    d.button.Q<Label>().text = discovered ? "" : "?";
                    d.button.tooltip = discovered ? (d.resource ? ToolkitLocalization.Name(d.resource) : ToolkitLocalization.Text("enemy." + d.enemy.name, d.enemy.enemyName)) : ToolkitLocalization.Text("common.undiscovered", "Undiscovered");
                }
                if (journalDiscoveryCount != null) journalDiscoveryCount.text = ToolkitLocalization.Text("journal.discovery-count", "{0} / {1} discovered", known, journalDiscoveries.Count);
            }
        }
        private void BuildJournalRecent()
        {
            journalRecent.Clear();
            var key = journalMap < 0 ? null : definition.maps[journalMap].config.name;
            var runs = tracker.RecentRuns.Where(r => key == null || r.MapType == key).ToList();
            Text(journalRecent, ToolkitLocalization.Text("journal.recent-count", "{0} of the last {1} saved runs", runs.Count, tracker.RecentRuns.Count), 6).AddToClassList("journal-note");
            if (runs.Count == 0)
            {
                Text(journalRecent, ToolkitLocalization.Text("journal.no-runs", "No recent runs here yet. Your lifetime records are still shown."), 7).AddToClassList("journal-empty");
                return;
            }
            foreach (var run in runs.AsEnumerable().Reverse())
            {
                var button = ToolkitGameplay.B(journalRecent, "", () => ShowJournalRun(run));
                button.AddToClassList("journal-run"); button.userData = run;
                Text(button, "#" + run.RunNumber, 7).AddToClassList("journal-run-number");
                Text(button, CalcUtils.FormatTime(run.Duration), 7).AddToClassList("journal-run-duration");
                Text(button, ToolkitLocalization.Text("journal.run-resources", "{0} resources", ItemStatisticsPresentation.Number(run.ResourcesCollected)), 7).AddToClassList("journal-run-rewards");
                Text(button, run.Abandoned ? ToolkitLocalization.Text("run.outcome-abandoned", "Abandoned") : run.Reaped ? ToolkitLocalization.Text("run.outcome-reaped", "Reaped") : run.Died ? ToolkitLocalization.Text("run.outcome-died", "Died") : ToolkitLocalization.Text("run.outcome-retreated", "Retreated"), 6).AddToClassList("journal-run-outcome");
            }
        }
        private void ShowJournalRun(GameData.RunRecord run)
        {
            // A second activation dismisses the in-page detail; no hover-only information.
            var existing = journalRecent.Q<Label>("journal-run-detail");
            if (existing != null) { var same = existing.userData == run; existing.RemoveFromHierarchy(); if (same) return; }
            var detail = Text(journalRecent, StatisticsPresentation.RunDetails(run), 7);
            detail.name = "journal-run-detail"; detail.userData = run; detail.AddToClassList("journal-run-detail");
            var runButton = journalRecent.Children().FirstOrDefault(e => e is Button && e.userData == run);
            if (runButton != null) journalRecent.Insert(journalRecent.IndexOf(runButton) + 1, detail);
        }
        private static void JournalArt(VisualElement parent, Sprite sprite)
        {
            var mask = ToolkitGameplay.E(parent, "journal-art");
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            if (sprite) { var size = sprite.rect.size * (16f / sprite.pixelsPerUnit); image.style.width = size.x; image.style.height = size.y; }
            image.style.flexShrink = 0; mask.Add(image);
        }
        private void BuildJournalCollection()
        {
            journalDiscoveryCount = null;
            if (journalMap < 0)
            {
                Text(journalCollection, ToolkitLocalization.Text("journal.explore-map", "Explore a map"), 8).AddToClassList("journal-section-title");
                Text(journalCollection, ToolkitLocalization.Text("journal.choose-map", "Choose a map to browse its resources and enemies alongside your records."), 7).AddToClassList("journal-note");
                return;
            }
            var settings = definition.maps[journalMap].config.taskGeneratorSettings;
            var resources = new HashSet<Resource>();
            void Drops(IEnumerable<ResourceDrop> drops) { if (drops == null) return; foreach (var drop in drops) if (drop != null && drop.resource && drop.weight > 0) resources.Add(drop.resource); }
            foreach (var category in new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting })
                if (category != null && category.weight > 0 && category.tasks != null)
                    foreach (var task in category.tasks) if (task) Drops(task.resourceDrops);
            var enemies = settings.enemies == null ? new List<EnemyData>() : settings.enemies.Where(e => e && e.weight > 0).Distinct().OrderBy(e => e.displayOrder).ToList();
            foreach (var enemy in enemies) Drops(enemy.resourceDrops);
            var heading = ToolkitGameplay.E(journalCollection, "journal-collection-heading");
            Text(heading, ToolkitLocalization.Text("journal.found-here", "Found here"), 8).AddToClassList("journal-section-title");
            journalDiscoveryCount = Text(heading, "", 6); journalDiscoveryCount.AddToClassList("journal-note");
            journalDiscoveryCount.tooltip = ToolkitLocalization.Text("journal.discoveries-help", "Discoveries are shared across all maps. These are configured drop pools; availability depends on progression.");
            var groups = ToolkitGameplay.E(journalCollection, "journal-collection-groups");
            var foes = ToolkitGameplay.E(groups, "journal-foes");
            Text(foes, ToolkitLocalization.Text("journal.enemies", "Enemies"), 7).AddToClassList("journal-collection-label");
            var enemyGrid = ToolkitGameplay.E(foes, "journal-discoveries");
            foreach (var enemy in enemies) AddDiscovery(enemyGrid, null, enemy, enemy.icon);
            LimitDiscoveryPreview(foes, enemyGrid);
            var loot = ToolkitGameplay.E(groups, "journal-loot");
            Text(loot, ToolkitLocalization.Text("journal.resources", "Resources"), 7).AddToClassList("journal-collection-label");
            var resourceGrid = ToolkitGameplay.E(loot, "journal-discoveries");
            foreach (var resource in resources.OrderByDescending(r => r.totalReceived > 0).ThenBy(r => r.resourceID)) AddDiscovery(resourceGrid, resource, null, resource.icon);
            LimitDiscoveryPreview(loot, resourceGrid);
        }
        private void LimitDiscoveryPreview(VisualElement parent, VisualElement grid)
        {
            const int previewCount = 12;
            if (grid.childCount <= previewCount) return;
            bool expanded = false;
            Button toggle = null;
            void Apply()
            {
                for (int i = previewCount; i < grid.childCount; i++) grid[i].style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                toggle.text = expanded ? ToolkitLocalization.Text("journal.show-fewer", "Show fewer") : ToolkitLocalization.Text("journal.show-all", "Show all {0}", grid.childCount);
            }
            toggle = ToolkitGameplay.B(parent, "", () => { expanded = !expanded; Apply(); });
            toggle.AddToClassList("journal-show-all"); Apply();
        }
        private void AddDiscovery(VisualElement parent, Resource resource, EnemyData enemy, Sprite sprite)
        {
            Button button = null;
            button = ToolkitGameplay.B(parent, "", () =>
            {
                var label = journalCollection.Q<Label>("journal-discovery-detail");
                if (label == null) { label = Text(journalCollection, "", 7); label.name = "journal-discovery-detail"; }
                label.text = button.tooltip;
            });
            button.AddToClassList("journal-discovery"); JournalArt(button, sprite);
            var unknown = Text(button, "?", 7); unknown.AddToClassList("journal-unknown");
            journalDiscoveries.Add((resource, enemy, button));
        }
    }
}
