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
        private UnityEngine.Object journalDetailTarget;
        private int journalDetailFrame;
        private Label journalEnemyCount, journalResourceCount;
        private int journalKnown = -1;
        private bool journalAccessible;
        private VisualElement journalNavigation, journalHeading;
        private bool journalIndexOpen = true;

        private string JournalMapKey(int index) => index < 0 ? "overall" : definition.maps[index].config.name;
        private string JournalMapName(string key)
        {
            var index = Array.FindIndex(definition.maps, m => m.config && m.config.name == key);
            if (index < 0) return ToolkitLocalization.Text("statistics.overall", "Overall");
            var map = definition.maps[index];
            return map.IsAccessible ? ToolkitLocalization.Text("map." + map.config.name, map.label.Trim()) : "???";
        }

        private void ShowJournalIndex()
        {
            journalIndexOpen = true;
            RefreshJournalNavigation();
        }
        private void RefreshJournalNavigation()
        {
            if (journalNavigation == null || journalContent == null) return;
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            journalNavigation.style.display = !portrait || journalIndexOpen ? DisplayStyle.Flex : DisplayStyle.None;
            journalContent.parent.style.display = portrait && journalIndexOpen ? DisplayStyle.None : DisplayStyle.Flex;
            journalContent.style.display = portrait && journalIndexOpen ? DisplayStyle.None : DisplayStyle.Flex;
            journalHeading.style.display = portrait && !journalIndexOpen ? DisplayStyle.Flex : DisplayStyle.None;
            journalHeading.Q<Label>().text = JournalMapName(JournalMapKey(journalMap));
            for (var i = 0; i < journalSelectors.Count; i++)
                StyleSelection(journalSelectors[i], (!portrait || !journalIndexOpen) && i == journalMap + 1);
        }

        private void BuildMapJournal()
        {
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            journalSelectors.Clear();
            var layout = ToolkitGameplay.E(body, "journal-layout");
            var navigation = ToolkitControls.RecessedScroll(layout, "journal-map-index", theme, definition.inset);
            journalNavigation = navigation.parent; journalNavigation.AddToClassList("journal-navigation");
            var selectors = ToolkitGameplay.E(navigation.contentContainer, "journal-selectors");
            for (var i = -1; i < definition.maps.Length; i++)
            {
                var index = i;
                var button = ToolkitGameplay.B(selectors, "", () => SelectJournalMap(index, true));
                button.name = "journal-map-" + (i + 1); button.AddToClassList("journal-selector");
                JournalArt(button, i < 0 ? definition.portrait :
                    definition.maps[i].IsAccessible ? definition.maps[i].icon : definition.maps[i].unknownIcon);
                Text(button, JournalMapName(JournalMapKey(i)), 7).name = "journal-map-label";
                if (i == definition.maps.Length - 1) button.AddToClassList("journal-selector-last");
                journalSelectors.Add(button);
            }
            var page = ToolkitGameplay.E(layout, "journal-page");
            journalHeading = ToolkitGameplay.E(page, "journal-page-heading");
            ToolkitGameplay.L(journalHeading, "", "journal-page-title");
            var back = ToolkitGameplay.B(journalHeading, "", ShowJournalIndex); back.name = "journal-back";
            back.AddToClassList("journal-back");
            var backIcon = ToolkitGameplay.E(back, "journal-back-icon"); backIcon.pickingMode = PickingMode.Ignore;
            ToolkitTheme.Background(backIcon, Resources.Load<Sprite>("UI/Icons/ArrowLeft"));
            journalContent = ToolkitGameplay.E(page, "journal-content");
            var indexOpen = journalIndexOpen;
            SelectJournalMap(Mathf.Clamp(journalMap, -1, definition.maps.Length - 1));
            journalIndexOpen = indexOpen;
            RefreshJournalNavigation();
            RefreshJournalAccess();
        }
        public void SelectJournalMap(int index, bool userInitiated = false)
        {
            if (index < -1 || index >= definition.maps.Length || journalContent == null) return;
            if (userInitiated && MapStatisticsGuidance.Active &&
                (MapStatisticsGuidance.Stage != 4 || index < 0 || definition.maps[index].config.name != MapStatisticsGuidance.TargetMap)) return;
            journalMap = index; journalRunKey = -1; journalKnown = -1;
            journalIndexOpen = false;
            RefreshJournalNavigation();
            journalContent.Clear(); summaries.Clear(); journalDiscoveries.Clear();
            for (var i = 0; i < journalSelectors.Count; i++) StyleSelection(journalSelectors[i], i == index + 1);
            journalAccessible = index < 0 || definition.maps[index].IsAccessible;
            journalRecent = journalCollection = null; journalEnemyCount = journalResourceCount = null;
            if (!journalAccessible)
            {
                var map = definition.maps[index];
                Text(journalContent, "???", 8).AddToClassList("journal-heading");
                var hint = Text(journalContent, ToolkitLocalization.Text("journal.locked." + map.config.name, map.lockedHint), 8);
                hint.name = "journal-locked-hint"; hint.AddToClassList("journal-note");
                return;
            }
            if (userInitiated && index >= 0) MapStatisticsGuidance.Viewed(definition.maps[index].config.name);
            var columns = ToolkitGameplay.E(journalContent, "journal-columns");
            var mainScroll = ToolkitControls.RecessedScroll(columns, "general-statistics", theme, definition.inset);
            mainScroll.parent.AddToClassList("journal-main");
            var main = mainScroll.contentContainer;
            var side = ToolkitGameplay.E(columns, "journal-side");
            Text(side, ToolkitLocalization.Text("journal.recent-runs", "Recent runs"), 8).AddToClassList("journal-section-title");
            journalRecent = ToolkitResponsiveLayout.IsPortrait ? ToolkitGameplay.E(side, "journal-recent-content") :
                ToolkitControls.RecessedScroll(side, "journal-run-scroll", theme, definition.inset).contentContainer;
            Text(main, ToolkitLocalization.Text("journal.lifetime", "Lifetime"), 8).AddToClassList("journal-section-title");
            AddSummary(main, index < 0 ? (ToolkitStatisticsDefinition.MapEntry?)null : definition.maps[index]);
            var summary = main.Q(className: "eov-stat-summary");
            summary.AddToClassList("journal-lifetime");
            summary.Q(className: "general-identity").style.display = DisplayStyle.None;
            journalCollection = ToolkitGameplay.E(main, "journal-collection");
            BuildJournalCollection();
            if (ToolkitResponsiveLayout.IsPortrait) main.Add(side);
            RefreshMapJournal();
        }
        private void RefreshJournalAccess()
        {
            if (selectedTab != 0 || journalContent == null || journalSelectors.Count != definition.maps.Length + 1) return;
            for (var i = 0; i < definition.maps.Length; i++)
            {
                var accessible = definition.maps[i].IsAccessible;
                var button = journalSelectors[i + 1];
                button.EnableInClassList("map-locked", !accessible);
                var label = button.Q<Label>("journal-map-label");
                var name = JournalMapName(JournalMapKey(i));
                if (label.text != name) label.text = name;
                var image = button.Q<Image>();
                var sprite = accessible ? definition.maps[i].icon : definition.maps[i].unknownIcon;
                if (image.sprite != sprite) image.sprite = sprite;
            }
            RefreshJournalNavigation();
            if (journalAccessible != (journalMap < 0 || definition.maps[journalMap].IsAccessible)) SelectJournalMap(journalMap);
        }
        private void RefreshMapJournal()
        {
            RefreshJournalAccess();
            if (summaries.Count == 0) return;
            var data = journalMap < 0
                ? StatisticsPresentation.General(tracker, GameManager.Instance && GameManager.Instance.IsKillScalingMode)
                : StatisticsPresentation.Map(tracker.GetMapStats(definition.maps[journalMap].config) ?? new GameData.MapStatistics(), definition.maps[journalMap].killScaling);
            UpdateSummary(summaries[0], data);
            var key = tracker.RecentRuns.Count > 0 ? tracker.RecentRuns[tracker.RecentRuns.Count - 1].RunNumber : 0;
            if (key != journalRunKey) { journalRunKey = key; BuildJournalRecent(); }
            var known = journalDiscoveries.Sum(d => d.enemy ? (killTracker ? killTracker.GetRevealLevel(d.enemy) : 0) + (CatalogueKnown(d.enemy) ? 1 : 0) : JournalDiscovered(d.resource, null) ? 1 : 0);
            if (known != journalKnown)
            {
                journalKnown = known;
                foreach (var d in journalDiscoveries)
                {
                    var discovered = JournalDiscovered(d.resource, d.enemy);
                    d.button.EnableInClassList("known", discovered);
                    d.button.style.display = discovered ? DisplayStyle.Flex : DisplayStyle.None;
                    if (d.enemy) d.button.Q<Image>().sprite = CatalogueArtwork(d.enemy).icon;
                    d.button.tooltip = discovered ? (d.resource ? ToolkitLocalization.Name(d.resource) : CatalogueName(d.enemy)) : "";
                }
                foreach (var group in journalDiscoveries.GroupBy(d => d.button.parent))
                {
                    var ordered = group.OrderByDescending(d => JournalDiscovered(d.resource, d.enemy))
                        .ThenBy(d => d.resource ? d.resource.resourceID : d.enemy.displayOrder)
                        .ThenBy(d => d.resource ? d.resource.name : d.enemy.name, StringComparer.Ordinal).ToList();
                    for (var i = 0; i < ordered.Count; i++)
                        if (group.Key.IndexOf(ordered[i].button) != i) group.Key.Insert(i, ordered[i].button);
                    var unknown = group.Key.Q<Label>("journal-undiscovered-count");
                    var remaining = ordered.Count(d => !JournalDiscovered(d.resource, d.enemy));
                    unknown.text = LocaleNumbers.Integer(remaining);
                    unknown.parent.style.display = remaining > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    unknown.parent.tooltip = ToolkitLocalization.Text("statistics.undiscovered-count", "Undiscovered ({0})", remaining);
                }
                void Count(Label label, bool enemies)
                {
                    var group = journalDiscoveries.Where(d => enemies ? d.enemy != null : d.resource != null).ToList();
                    label.text = LocaleNumbers.Integer(group.Count(d => JournalDiscovered(d.resource, d.enemy))) + " / " + LocaleNumbers.Integer(group.Count);
                }
                Count(journalEnemyCount, true); Count(journalResourceCount, false);
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
                Text(journalRecent, ToolkitLocalization.Text("journal.no-runs", "No saved runs here."), 7).AddToClassList("journal-empty");
                return;
            }
            foreach (var run in runs.AsEnumerable().Reverse())
            {
                var button = ToolkitGameplay.B(journalRecent, "", () => ShowJournalRun(run));
                button.AddToClassList("journal-run"); button.userData = run;
                var heading = journalMap < 0 ? ToolkitGameplay.E(button, "journal-run-line") : button;
                if (journalMap < 0)
                {
                    button.AddToClassList("journal-run-overall");
                    Text(heading, JournalMapName(run.MapType), 7).AddToClassList("journal-run-map");
                }
                var duration = Text(heading, JournalDuration(run.Duration), 7);
                duration.AddToClassList("journal-run-duration");
                duration.tooltip = CalcUtils.FormatTime(run.Duration);
                var totals = journalMap < 0 ? ToolkitGameplay.E(button, "journal-run-line") : button;
                Text(totals, ToolkitLocalization.Text("journal.run-resources", "Resources: {0}", ItemStatisticsPresentation.Number(run.ResourcesCollected)), 7).AddToClassList("journal-run-rewards");
                Text(totals, run.Abandoned ? ToolkitLocalization.Text("run.outcome-abandoned", "Abandoned") : run.Reaped ? ToolkitLocalization.Text("run.outcome-reaped", "Reaped") : run.Died ? ToolkitLocalization.Text("run.outcome-died", "Died") : ToolkitLocalization.Text("run.outcome-retreated", "Retreated"), 6).AddToClassList("journal-run-outcome");
            }
        }
        private static string JournalDuration(double duration)
        {
            if (!LocaleNumbers.IsFinite(duration) || duration < 0 || duration >= TimeSpan.MaxValue.TotalSeconds)
                return CalcUtils.FormatTime(duration);
            var elapsed = TimeSpan.FromSeconds(duration);
            var hours = (long)elapsed.TotalHours;
            return hours > 0
                ? $"{hours.ToString(LocaleNumbers.Culture)}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes}:{elapsed.Seconds:00}";
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
        private static void JournalArt(VisualElement parent, Sprite sprite, bool enemyArt = false)
        {
            var mask = ToolkitGameplay.E(parent, "journal-art");
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            if (enemyArt && sprite)
            {
                var size = sprite.rect.size * (16f / sprite.pixelsPerUnit);
                if (Mathf.Max(size.x, size.y) > 24f) size *= 24f / Mathf.Max(size.x, size.y);
                image.style.width = size.x; image.style.height = size.y;
            }
            else image.style.width = image.style.height = Length.Percent(100);
            image.style.flexShrink = 0; mask.Add(image);
        }
        private void BuildJournalCollection()
        {
            journalEnemyCount = journalResourceCount = null;
            var resources = new HashSet<Resource>();
            var enemies = new List<EnemyData>();
            void Drops(IEnumerable<ResourceDrop> drops) { if (drops == null) return; foreach (var drop in drops) if (drop != null && drop.resource && drop.weight > 0) resources.Add(drop.resource); }
            if (journalMap < 0)
            {
                resources.UnionWith(AssetCache.GetAll<Resource>("Resource Items").Where(r => r));
                enemies.AddRange(AssetCache.GetAll<EnemyData>("").Where(e => e));
            }
            else
            {
                var settings = definition.maps[journalMap].config.taskGeneratorSettings;
                foreach (var category in new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting })
                    if (category != null && category.weight > 0 && category.tasks != null)
                        foreach (var task in category.tasks) if (task) Drops(task.resourceDrops);
                if (settings.enemies != null) enemies.AddRange(settings.enemies.Where(e => e && e.weight > 0).Distinct());
                foreach (var enemy in enemies) Drops(enemy.resourceDrops);
            }
            var groups = ToolkitGameplay.E(journalCollection, "journal-collection-groups");
            var foes = ToolkitGameplay.E(groups, "journal-foes");
            Label Heading(VisualElement parent, string key, string fallback)
            {
                var heading = ToolkitGameplay.E(parent, "journal-collection-heading");
                Text(heading, ToolkitLocalization.Text(key, fallback), 8).AddToClassList("journal-section-title");
                var count = Text(heading, "", 7); count.AddToClassList("journal-note"); return count;
            }
            journalEnemyCount = Heading(foes, "journal.enemies", "Enemies");
            var enemyGrid = ToolkitGameplay.E(foes, "journal-discoveries");
            foreach (var enemy in CatalogueEnemies(enemies).OrderByDescending(e => JournalDiscovered(null, e)).ThenBy(e => e.displayOrder).ThenBy(e => e.name, StringComparer.Ordinal)) AddDiscovery(enemyGrid, null, enemy, CatalogueArtwork(enemy).icon);
            AddUndiscoveredCount(enemyGrid);
            var loot = ToolkitGameplay.E(groups, "journal-loot");
            journalResourceCount = Heading(loot, "journal.resources", "Resources");
            var resourceGrid = ToolkitGameplay.E(loot, "journal-discoveries");
            foreach (var resource in resources.OrderByDescending(r => JournalDiscovered(r, null)).ThenBy(r => r.resourceID).ThenBy(r => r.name, StringComparer.Ordinal)) AddDiscovery(resourceGrid, resource, null, resource.icon);
            AddUndiscoveredCount(resourceGrid);
        }
        private void AddUndiscoveredCount(VisualElement parent)
        {
            var tile = ToolkitGameplay.E(parent, "journal-discovery");
            tile.AddToClassList("journal-undiscovered");
            tile.style.display = DisplayStyle.None;
            var count = Text(tile, "", 7);
            count.name = "journal-undiscovered-count";
            count.AddToClassList("journal-unknown");
        }
        private void AddDiscovery(VisualElement parent, Resource resource, EnemyData enemy, Sprite sprite)
        {
            var button = ToolkitGameplay.B(parent, "", () => OpenJournalDiscovery(resource, enemy));
            button.name = enemy ? "journal-enemy-" + enemy.name : "journal-resource-" + resource.name;
            button.AddToClassList("journal-discovery"); JournalArt(button, sprite, enemy != null);
            if (enemy && EnemyFamily(enemy) != null && sprite)
            {
                var image = button.Q<Image>();
                image.style.width = sprite.rect.width * 16 / sprite.pixelsPerUnit;
                image.style.height = sprite.rect.height * 16 / sprite.pixelsPerUnit;
            }
            journalDiscoveries.Add((resource, enemy, button));
        }
        private bool JournalDiscovered(Resource resource, EnemyData enemy) => resource
            ? resource.totalReceived > 0 : enemy && CatalogueKnown(enemy);

        private void OpenJournalDiscovery(Resource resource, EnemyData enemy)
        {
            SelectTab(enemy ? 3 : 5);
            if (enemy) enemy = CatalogueArtwork(enemy);
            journalDetailTarget = enemy ? (UnityEngine.Object)enemy : resource;
            journalDetailFrame = Time.frameCount + 2;
        }

        private void RevealJournalDiscovery()
        {
            if (!journalDetailTarget || Time.frameCount < journalDetailFrame) return;
            VisualElement target = null;
            if (selectedTab == 3 && journalDetailTarget is EnemyData enemy) target = RevealEnemyEntry(enemy);
            else if (selectedTab == 5 && journalDetailTarget is Resource resource && itemRows.TryGetValue(resource, out var itemRow)) target = itemRow.root;
            if (target == null) { journalDetailTarget = null; return; }
            var scroll = target.GetFirstAncestorOfType<ScrollView>();
            if (target.panel == null || target.layout.height <= 0 || scroll == null || scroll.contentViewport.layout.height <= 0) return;
            scroll.ScrollTo(target);
            target.AddToClassList("statistics-entry-highlight");
            target.schedule.Execute(() => target.RemoveFromClassList("statistics-entry-highlight")).ExecuteLater(2000);
            journalDetailTarget = null;
        }
    }
}
