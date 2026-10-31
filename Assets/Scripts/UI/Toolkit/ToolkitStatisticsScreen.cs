using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class ToolkitStatisticsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitStatisticsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, body, footer;
        private Label tooltip;
        private sealed class SummaryRow { public readonly List<(Label name, Label value)> fields = new(); }
        private readonly List<SummaryRow> summaries = new();
        private readonly Dictionary<int, Button> tabButtons = new();
        private GameplayStatTracker tracker;
        private bool dirty, localizationDirty;
        private Image boostHand;
        private bool boostScrolled;
        private int selectedTab;
        private DropdownField portraitSort;
        private bool syncingPortraitSelectors;
        private bool? previousPortraitLayout;
        private readonly List<(string title, System.Action action)> portraitSortActions = new();
        public static readonly IReadOnlyList<int> Sections = Array.AsReadOnly(new[] { 0, 2, 3, 4, 5, 6 });
        private ToolkitTownStatisticsView townView;
        private float nextTownRefresh;
        public bool IsOpen => root != null;
        public int SelectedTab => selectedTab;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;
        public bool Show()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if (IsOpen) return true;
            if (!IsConfigured || !(tracker = GameplayStatTracker.Instance)) return false;
            if ((Blindsided.Oracle.oracle?.saveData?.General.MapStatisticsTutorialStage ?? 0) >= 8)
            {
                selectedTab = 0;
                var map = GameManager.Instance && GameManager.Instance.CurrentMap
                    ? GameManager.CurrentGenerationConfig?.name
                    : tracker.RecentRuns.LastOrDefault()?.MapType;
                journalMap = Array.FindIndex(definition.maps, entry =>
                    entry.config && entry.config.name == map && entry.IsAccessible);
            }
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "statistics" }; root.AddToClassList("eov-statistics"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var tabs = new VisualElement(); tabs.AddToClassList("eov-stat-tabs"); root.Add(tabs);
            foreach (var index in Sections)
            {
                var tab = index;
                tabButtons.Add(tab, Action(tabs, "stats-tab-" + tab, TabTitle(tab), () => SelectTab(tab)));
            }
            BuildMapTutorial();
            body = new VisualElement { name = "stats-body" }; body.AddToClassList("eov-stat-body"); root.Add(body);
            portraitSort = new DropdownField("Sort", new List<string> { "Default" }, 0) { name = "portrait-statistics-sort" };
            ToolkitLocalization.Bind(portraitSort.labelElement, "statistics.sort-label", "Sort");
            portraitSort.AddToClassList("portrait-picker"); root.Add(portraitSort);
            portraitSort.RegisterValueChangedCallback(evt => { var i = portraitSortActions.FindIndex(a => a.title == evt.newValue); if (!syncingPortraitSelectors && i >= 0 && i < portraitSortActions.Count) portraitSortActions[i].action(); });
            footer = new VisualElement(); footer.AddToClassList("eov-stat-tabs"); root.Add(footer);
            tooltip = Text(root, "", 6); tooltip.name = "run-tooltip"; tooltip.AddToClassList("eov-stat-tooltip"); tooltip.style.display = DisplayStyle.None;
            tracker.OnRunEnded += RunEnded; tracker.OnDistanceAdded += DistanceChanged; tracker.OnTaskCompletedEvent += TaskCompleted; tracker.OnMaxRunDistanceChanged += MaximumDistanceChanged;
            killTracker = EnemyKillTracker.Instance; if (killTracker) killTracker.OnKillRegistered += KillChanged;
            TimelessEchoes.Tasks.TaskWeightService.ToggleChanged += TaskToggleChanged; TimelessEchoes.Tasks.TaskWeightService.WeightsChanged += TaskWeightsChanged;
            ToolkitLocalization.Changed += LocalizationChanged;
            Blindsided.EventHandler.OnLoadData += Changed;
            resourceManager = TimelessEchoes.Upgrades.ResourceManager.Instance;
            if (resourceManager) { resourceManager.OnInventoryChanged += InventoryChanged; resourceManager.OnResourceAdded += ResourceChanged; resourceManager.OnResourceTierUpgraded += ResourceTierChanged; }
            SelectTab(selectedTab); Layout(); return true;
        }
        public void SelectTab(int index, bool preserveJournalSelection = false)
        {
            if (!IsOpen || !tabButtons.ContainsKey(index)) return;
            if (index != selectedTab) journalDetailTarget = null;
            if (index == 0 && !preserveJournalSelection) journalIndexOpen = true;
            taskPreferences?.CloseDetails(); taskPreferences = null;
            runGraph = null; townView = null; selectedTab = index; distanceControls?.RemoveFromHierarchy(); distanceControls = null; enemyDistance = null; body.Clear(); footer.Clear(); portraitSortActions.Clear(); summaries.Clear(); tooltip.style.display = DisplayStyle.None;
            foreach (var pair in tabButtons) StyleSelection(pair.Value, pair.Key == index);
            footer.style.display = index != 0 && index != 6 ? DisplayStyle.Flex : DisplayStyle.None;
            boostScrolled = false;
            if (index == 0) BuildGeneral(); else if (index == 2) BuildGraph(); else if (index == 5) BuildItems(); else if (index == 6) townView = new ToolkitTownStatisticsView(body, theme); else BuildStatList(index == 3); Refresh();
        }
        public static string TabTitle(int index) => index switch
        {
            0 => ToolkitLocalization.Text("statistics.tab-general", "General"),
            2 => ToolkitLocalization.Text("statistics.tab-graphs", "Graphs"),
            3 => ToolkitLocalization.Text("statistics.tab-enemies", "Enemies"),
            4 => ToolkitLocalization.Text("statistics.tab-tasks", "Tasks"),
            5 => ToolkitLocalization.Text("statistics.tab-items", "Items"),
            6 => ToolkitLocalization.Text("statistics.tab-town", "Town"),
            _ => ""
        };

        private void BuildGeneral() => BuildMapJournal();
        private void AddSummary(VisualElement parent, ToolkitStatisticsDefinition.MapEntry? map)
        {
            var row = ToolkitGameplay.E(parent, "eov-stat-summary");
            var identity = ToolkitGameplay.E(row, "general-identity");
            var frame = ToolkitGameplay.E(identity, "general-art-frame");
            var mask = ToolkitGameplay.E(frame, "general-art-mask");
            var sprite = map.HasValue ? map.Value.icon : definition.portrait;
            if (sprite)
            {
                var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                image.style.width = image.style.height = Length.Percent(100); image.style.flexShrink = 0; mask.Add(image);
            }
            Text(identity, map.HasValue ? ToolkitLocalization.Text("map." + map.Value.config.name, map.Value.label.Trim()) : ToolkitLocalization.Text("statistics.overall", "Overall"), 8).AddToClassList("general-title");
            var summary = new SummaryRow();
            var metrics = ToolkitGameplay.E(row, "general-metrics");
            for (var columnIndex = 0; columnIndex < 2; columnIndex++)
            {
                var column = ToolkitGameplay.E(metrics, "general-column");
                column.AddToClassList(columnIndex == 0 ? "general-column-first" : "general-column-last");
                for (var i = 0; i < (map.HasValue ? 4 : 5); i++)
                {
                    var metric = ToolkitGameplay.E(column, "general-metric");
                    var name = Text(metric, "", 7); name.AddToClassList("general-metric-name");
                    var value = Text(metric, "", 7); value.AddToClassList("general-metric-value");
                    summary.fields.Add((name, value));
                }
            }
            summaries.Add(summary);
        }
        private static void UpdateSummary(SummaryRow row, (string left, string right) data)
        {
            var lines = (data.left + "\n" + data.right).Split('\n');
            for (var i = 0; i < row.fields.Count; i++)
            {
                var split = lines[i].IndexOf(": ", StringComparison.Ordinal);
                row.fields[i].name.text = lines[i].Substring(0, split);
                row.fields[i].value.text = lines[i].Substring(split + 2);
            }
        }
        private ToolkitRunGraphView runGraph;
        private void BuildGraph()
        {
            runGraph = new ToolkitRunGraphView(body, footer, definition, () => tracker.RecentRuns,
                () => tracker.PreviousRuns, () => tracker.EarlierRunDetailsUnavailable);
            GraphStatisticsGuidance.MarkViewed();
        }
        public void SetGraphMode(RunStatsPanelUI.GraphMode mode)
        {
            if (IsOpen && selectedTab == 2) runGraph?.SetMetric((ToolkitRunGraphView.Metric)Enum.Parse(typeof(ToolkitRunGraphView.Metric), mode.ToString()));
        }
        private void Refresh()
        {
            dirty = false;
            if (selectedTab == 6) { townView?.Refresh(); nextTownRefresh = Time.unscaledTime + .75f; return; }
            if (selectedTab == 2) { RefreshGraph(); return; }
            if (selectedTab == 5) { RefreshItems(); return; }
            if (selectedTab == 3) { RefreshEnemies(); return; }
            if (selectedTab == 4) { RefreshTasks(); return; }
            RefreshMapJournal();
        }
        private void RefreshGraph() => runGraph?.Refresh();
        private Label Text(VisualElement parent, string value, float size)
        {
            var role = size >= 8 ? ToolkitControls.TextRole.Heading : size >= 7 ? ToolkitControls.TextRole.Subheading : size >= 6 ? ToolkitControls.TextRole.Body : ToolkitControls.TextRole.Caption;
            var label = ToolkitControls.Text(value, role); label.AddToClassList("eov-stat-text");
            parent.Add(label); return label;
        }
        private Button Action(VisualElement parent, string name, string title, Action clicked)
        {
            if (parent == footer) portraitSortActions.Add((title, clicked));
            var button = ToolkitGameplay.B(parent,title,clicked);button.name=name; button.AddToClassList("eov-control--tab"); button.text = "<b>" + title + "</b>"; button.AddToClassList("eov-stat-tab"); if (parent.childCount > 0) parent.ElementAt(parent.childCount - 1).style.marginRight = 2; button.style.marginRight = 0; parent.Add(button); return button;
        }
        private void StyleSelection(Button button, bool selected)
        {
            button.EnableInClassList("active",selected);
        }
        private void LocalizationChanged() => localizationDirty = true;
        private void RefreshLocalization()
        {
            foreach (var pair in tabButtons) pair.Value.text = "<b>" + TabTitle(pair.Key) + "</b>";
            if (selectedTab == 2 && runGraph != null)
            {
                var state = runGraph.ViewState; SelectTab(selectedTab); runGraph?.RestoreViewState(state);
            }
            else
            {
                var distance = enemyDistance?.value;
                SelectTab(selectedTab, true);
                if (distance.HasValue && enemyDistance != null)
                { ToolkitControls.SetSliderValue(enemyDistance, distance.Value); RefreshEnemies(); }
            }
        }
        private void Changed() { dirty = true; journalKnown = -1; }
        private void RunEnded(bool _) => Changed();
        private void DistanceChanged(float _) { if (selectedTab == 0 || selectedTab == 4) dirty = true; }
        private void Update() { if (!IsOpen) return; Layout(); RefreshJournalAccess(); RefreshMapTutorial(); RefreshGraphGuidance(); RefreshBoostGuidance(); if (localizationDirty) { localizationDirty = false; RefreshLocalization(); } if (dirty || selectedTab == 6 && Time.unscaledTime >= nextTownRefresh) Refresh(); RevealJournalDiscovery(); }
        // Portrait section guidance belongs to the global Statistics popup.
        private VisualElement StatisticsSectionTarget(int tab) => ToolkitResponsiveLayout.IsPortrait ? null : tabButtons[tab];
        private void PositionStatisticsHand(Image hand, VisualElement target, string option = null)
        {
            if (target == null || target.resolvedStyle.display == DisplayStyle.None || target.worldBound.width <= 0)
            { hand.style.display = DisplayStyle.None; return; }
            if (target is DropdownField && !string.IsNullOrEmpty(option))
            {
                var overlay = root.panel?.visualTree;
                var item = overlay?.Query(className: "unity-base-dropdown__item").ToList().Find(row =>
                    row.worldBound.height > 0 && row.resolvedStyle.display != DisplayStyle.None &&
                    row.Q<Label>(className: "unity-base-dropdown__label")?.text == option);
                if (item != null)
                {
                    // Native dropdowns are above the screen tree. Keep the guide in
                    // that overlay too, with space beside the option's label.
                    item.style.paddingRight = 36;
                    if (hand.parent != overlay) overlay.Add(hand);
                    var choicePoint = overlay.WorldToLocal(new Vector2(item.worldBound.xMax - 30, item.worldBound.center.y));
                    hand.style.left = Mathf.Round(choicePoint.x + 1.5f * (1 + Mathf.Cos(Time.unscaledTime * 4)));
                    hand.style.top = Mathf.Round(choicePoint.y - 12);
                    hand.style.rotate = new Rotate(Angle.Degrees(270));
                    if (overlay.IndexOf(hand) != overlay.childCount - 1) hand.BringToFront();
                    return;
                }
            }
            if (hand.parent != root) root.Add(hand);
            if (target is Button && target.name.StartsWith("toggle-task-stat-", StringComparison.Ordinal))
            {
                var viewport = target.GetFirstAncestorOfType<ScrollView>()?.contentViewport;
                if (viewport != null && !viewport.worldBound.Contains(target.worldBound.center))
                { hand.style.display = DisplayStyle.None; return; }
                var beside = root.WorldToLocal(new Vector2(target.worldBound.xMin, target.worldBound.center.y));
                hand.style.left = Mathf.Round(beside.x - 27 - 1.5f * (1 + Mathf.Cos(Time.unscaledTime * 4)));
                hand.style.top = Mathf.Round(beside.y - 12);
                hand.style.rotate = new Rotate(Angle.Degrees(90));
                hand.BringToFront();
                return;
            }
            // A labelled dropdown includes its caption. Point at its actual input.
            var input = target is DropdownField ? target.Q(className: "unity-base-popup-field__input") : null;
            var rect = (input ?? target).worldBound;
            var anchorX = ToolkitResponsiveLayout.IsPortrait && target is DropdownField ? rect.xMax - 12 : rect.center.x;
            var below = root.WorldToLocal(new Vector2(anchorX, rect.yMax));
            var top = root.WorldToLocal(new Vector2(anchorX, rect.yMin));
            var above = below.y + 38 > root.contentRect.height ||
                ToolkitResponsiveLayout.IsPortrait && target is DropdownField && top.y >= 34;
            var point = above ? top : below;
            var bounce = Mathf.Round(1.5f * (1 + Mathf.Cos(Time.unscaledTime * 4)));
            hand.style.left = Mathf.Clamp(Mathf.Round(point.x - 12), 0, Mathf.Max(0, root.contentRect.width - 24));
            hand.style.top = Mathf.Round(above ? point.y - 26 - bounce : point.y + 2 + bounce);
            hand.style.rotate = new Rotate(Angle.Degrees(above ? 180 : 0));
            if (root.IndexOf(hand) != root.childCount - 1) hand.BringToFront();
        }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout()
        {
            if (selectedTab == 4)
            {
                var area = ToolkitWindowLayout.SafeArea;
                var width = Mathf.Min(900, area.width - 24);
                windowLayout.Apply(root, new Rect(area.center.x - width / 2, area.y + ToolkitResponsiveLayout.WindowTop,
                    width, Mathf.Max(0, area.height - ToolkitResponsiveLayout.WindowTop - ToolkitResponsiveLayout.WindowBottom)));
            }
            else windowLayout.Centered(root, theme);
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            if (previousPortraitLayout.HasValue && previousPortraitLayout.Value != portrait)
            {
                if (selectedTab == 0) SelectTab(0, true);
                else if (selectedTab == 3) RefreshEnemies();
                else runGraph?.Refresh();
            }
            previousPortraitLayout = portrait;
            taskPreferences?.Layout();
            syncingPortraitSelectors = true;
            footer.style.display = !portrait && selectedTab != 0 && selectedTab != 2 && selectedTab != 4 && selectedTab != 6 ? DisplayStyle.Flex : DisplayStyle.None;
            portraitSort.style.display = portrait && portraitSortActions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (portraitSortActions.Count > 0)
            {
                var labels = portraitSortActions.Select(a => a.title).ToList();
                if (!portraitSort.choices.SequenceEqual(labels)) portraitSort.choices = labels;
                var buttons = footer.Query<Button>().ToList();
                var selected = buttons.FindIndex(b => b.ClassListContains("active"));
                var sortTitle = labels[Mathf.Clamp(selected, 0, labels.Count - 1)];
                if (portraitSort.value != sortTitle) portraitSort.SetValueWithoutNotify(sortTitle);
                if (distanceControls != null && root.IndexOf(distanceControls) != root.IndexOf(portraitSort) - 1)
                    distanceControls.PlaceBehind(portraitSort);
            }
            syncingPortraitSelectors = false;
        }
        public void Hide()
        {
            if (IsOpen && MapStatisticsGuidance.Stage == 7)
                Blindsided.Oracle.oracle.saveData.General.MapStatisticsTutorialStage = 8;
            ToolkitLocalization.Changed -= LocalizationChanged;
            if (tracker) { tracker.OnRunEnded -= RunEnded; tracker.OnDistanceAdded -= DistanceChanged; tracker.OnTaskCompletedEvent -= TaskCompleted; tracker.OnMaxRunDistanceChanged -= MaximumDistanceChanged; }
            if (resourceManager) { resourceManager.OnInventoryChanged -= InventoryChanged; resourceManager.OnResourceAdded -= ResourceChanged; resourceManager.OnResourceTierUpgraded -= ResourceTierChanged; }
            if (killTracker) killTracker.OnKillRegistered -= KillChanged;
            TimelessEchoes.Tasks.TaskWeightService.ToggleChanged -= TaskToggleChanged; TimelessEchoes.Tasks.TaskWeightService.WeightsChanged -= TaskWeightsChanged;
            boostHand?.RemoveFromHierarchy(); graphHand?.RemoveFromHierarchy(); mapTutorialHand?.RemoveFromHierarchy();
            boostHand = null; graphHand = null; boostScrolled = false; mapTutorial = null; mapTutorialText = null; mapTutorialHand = null;
            taskPreferences?.CloseDetails(); taskPreferences = null;
            enemyRows.Clear(); enemyFamilyRows.Clear(); statSortButtons.Clear(); distanceControls = null; enemyDistance = null;
            itemRows.Clear(); itemSortButtons.Clear(); itemContent = null;
            journalSelectors.Clear(); journalDiscoveries.Clear(); journalDetailTarget = null; journalContent = journalRecent = journalCollection = null; journalEnemyCount = journalResourceCount = null;
            Blindsided.EventHandler.OnLoadData -= Changed; root?.RemoveFromHierarchy(); root = null; tracker = null; runGraph = null; townView = null; tabButtons.Clear(); summaries.Clear();
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
