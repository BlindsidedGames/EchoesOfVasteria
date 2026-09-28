using System;
using System.Collections.Generic;
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
        private VisualElement root, body, footer, graph, averageLine;
        private Label maximumLabel, averageLabel, oldestLabel, middleLabel, newestLabel, graphTitle, tooltip;
        private readonly List<(Label left, Label right)> summaries = new();
        private readonly List<(Button hit, VisualElement fill, VisualElement bonus)> bars = new();
        private readonly List<Button> tabButtons = new(), modeButtons = new();
        private GameplayStatTracker tracker;
        private bool dirty;
        private ToolkitLeaderboardView leaderboard;
        private int selectedTab;
        private RunStatsPanelUI.GraphMode graphMode;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;
        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(tracker = GameplayStatTracker.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "statistics" }; root.AddToClassList("eov-statistics"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var tabs = new VisualElement(); tabs.AddToClassList("eov-stat-tabs"); root.Add(tabs);
            string[] titles = { "General", "Rank", "Graphs", "Enemies", "Tasks", "Items" };
            for (var i = 0; i < titles.Length; i++)
            {
                var index = i; var button = Action(tabs, "stats-tab-" + i, titles[i], () => SelectTab(index)); tabButtons.Add(button);
            }
            body = new VisualElement { name = "stats-body" }; body.AddToClassList("eov-stat-body"); root.Add(body);
            footer = new VisualElement(); footer.AddToClassList("eov-stat-tabs"); root.Add(footer);
            tooltip = Text(root, "", 6); tooltip.name = "run-tooltip"; tooltip.AddToClassList("eov-stat-tooltip"); tooltip.style.display = DisplayStyle.None;
            tracker.OnRunEnded += RunEnded; tracker.OnDistanceAdded += DistanceChanged; tracker.OnTaskCompletedEvent += TaskCompleted; tracker.OnMaxRunDistanceChanged += MaximumDistanceChanged;
            killTracker = EnemyKillTracker.Instance; if (killTracker) killTracker.OnKillRegistered += KillChanged;
            TimelessEchoes.Tasks.TaskWeightService.ToggleChanged += TaskToggleChanged; TimelessEchoes.Tasks.TaskWeightService.WeightsChanged += TaskWeightsChanged;
            Blindsided.EventHandler.OnLoadData += Changed;
            resourceManager = TimelessEchoes.Upgrades.ResourceManager.Instance;
            if (resourceManager) { resourceManager.OnInventoryChanged += InventoryChanged; resourceManager.OnResourceAdded += ResourceChanged; resourceManager.OnResourceTierUpgraded += ResourceTierChanged; }
            SelectTab(selectedTab); Layout(); return true;
        }
        public void SelectTab(int index)
        {
            if (!IsOpen || (index < 0 || index > 5)) return;
            leaderboard?.Dispose(); leaderboard = null;
            selectedTab = index; distanceControls?.RemoveFromHierarchy(); distanceControls = null; enemyDistance = null; body.Clear(); footer.Clear(); modeButtons.Clear(); summaries.Clear(); bars.Clear(); tooltip.style.display = DisplayStyle.None;
            for (var i = 0; i < tabButtons.Count; i++) StyleSelection(tabButtons[i], i == index);
            footer.style.display = index != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (index == 0) BuildGeneral(); else if (index == 1) leaderboard = new ToolkitLeaderboardView(body, footer, theme, definition); else if (index == 2) BuildGraph(); else if (index == 5) BuildItems(); else BuildStatList(index == 3); Refresh();
        }
        private void BuildGeneral()
        {
             body.style.paddingTop = 0; body.style.paddingBottom = 0; body.style.paddingLeft = 0; body.style.paddingRight = 0;
            var inset = new VisualElement(); inset.style.flexGrow = 1; inset.style.minHeight = 0; inset.style.paddingLeft = 0; inset.style.paddingRight = 0; inset.style.paddingTop = 0; inset.style.paddingBottom = 0;  body.Add(inset);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "general-statistics", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-scroll"); scroll.style.flexGrow = 1; scroll.style.minHeight = 0; theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); inset.Add(scroll);
            AddSummary(scroll, null);
            foreach (var map in definition.maps) AddSummary(scroll, map);
        }
        private void AddSummary(VisualElement parent, ToolkitStatisticsDefinition.MapEntry? map)
        {
            var row = new VisualElement(); row.AddToClassList("eov-stat-summary");  row.style.minHeight = 32; parent.Add(row);
            var iconFrame = new VisualElement(); iconFrame.AddToClassList("eov-stat-summary-icon");  row.Add(iconFrame);
            if (map.HasValue)
            {
                var label = Text(iconFrame, map.Value.label, map.Value.labelSize); label.style.unityTextAlign = TextAnchor.MiddleCenter; label.style.whiteSpace = WhiteSpace.Normal; label.style.flexGrow = 1; label.style.marginLeft = 2; label.style.marginRight = 2;
            }
            else
            {
                var mask = new VisualElement(); mask.AddToClassList("eov-stat-portrait");  iconFrame.Add(mask);
                var icon = new Image { sprite = definition.portrait, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; icon.style.position = Position.Absolute; icon.style.width = 32; icon.style.height = 32; icon.style.left = -4; icon.style.top = .96f; mask.Add(icon);
            }
            var left = Text(row, "", 6); var right = Text(row, "", 6); left.style.flexGrow = right.style.flexGrow = 1; left.style.flexBasis = right.style.flexBasis = 0;
            summaries.Add((left, right));
        }
        private void BuildGraph()
        {
             body.style.paddingTop = 0; body.style.paddingBottom = 0; body.style.paddingLeft = 0; body.style.paddingRight = 0;
            graphTitle = Text(body, "", 7); graphTitle.style.height = 9.86f; graphTitle.style.unityTextAlign = TextAnchor.MiddleCenter; graphTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            var line = new VisualElement(); line.style.flexDirection = FlexDirection.Row; line.style.flexGrow = 1; line.style.marginTop = 1; body.Add(line);
            var axis = new VisualElement(); axis.style.width = 36; axis.style.flexShrink = 0; line.Add(axis);
            maximumLabel = Text(axis, "", 5); maximumLabel.style.unityTextAlign = TextAnchor.UpperRight;
            var zero = Text(axis, "0", 5); zero.style.position = Position.Absolute; zero.style.bottom = 5.33f; zero.style.right = 0;
            var column = new VisualElement(); column.style.flexGrow = 1; line.Add(column);
            graph = new VisualElement { name = "run-graph" }; graph.style.flexGrow = 1; graph.style.flexDirection = FlexDirection.Row; graph.style.paddingLeft = 2; graph.style.paddingRight = 2; graph.style.paddingTop = 2; graph.style.paddingBottom = 1;  column.Add(graph);
            for (var i = 0; i < 50; i++)
            {
                var index = i; var hit = new Button { name = "run-bar-" + i }; hit.AddToClassList("eov-stat-bar"); graph.Add(hit);
                var fill = new VisualElement { pickingMode = PickingMode.Ignore }; fill.AddToClassList("eov-stat-bar-fill");  hit.Add(fill);
                var bonus = new VisualElement { pickingMode = PickingMode.Ignore }; bonus.AddToClassList("eov-stat-bar-fill");  hit.Add(bonus);
                hit.RegisterCallback<PointerEnterEvent>(e => ShowRun(index, e.position)); hit.RegisterCallback<PointerLeaveEvent>(_ => tooltip.style.display = DisplayStyle.None);
                hit.clicked += () => ShowRun(index, hit.worldBound.center); hit.RegisterCallback<FocusInEvent>(_ => ShowRun(index, hit.worldBound.center)); hit.RegisterCallback<FocusOutEvent>(_ => tooltip.style.display = DisplayStyle.None);
                bars.Add((hit, fill, bonus));
            }
            averageLine = new VisualElement { name = "run-average", pickingMode = PickingMode.Ignore }; averageLine.AddToClassList("eov-stat-average"); ToolkitTheme.Background(averageLine, definition.averageLine); averageLine.style.unityBackgroundImageTintColor = definition.averageColor; graph.Add(averageLine);
            averageLabel = Text(averageLine, "", 3.9f); averageLabel.style.position = Position.Absolute; averageLabel.style.right = Length.Percent(100); averageLabel.style.top = -3; averageLabel.style.width = 34;averageLabel.style.whiteSpace=WhiteSpace.NoWrap; averageLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            var numbers = new VisualElement(); numbers.style.height = 5.33f; numbers.style.flexDirection = FlexDirection.Row; column.Add(numbers);
            oldestLabel = Text(numbers, "", 3.9f); middleLabel = Text(numbers, "", 3.9f); newestLabel = Text(numbers, "", 3.9f);
            foreach (var label in new[] { oldestLabel, middleLabel, newestLabel }) { label.style.flexGrow = 1; label.style.flexBasis = 0; }
            middleLabel.style.unityTextAlign = TextAnchor.MiddleCenter; newestLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            foreach (RunStatsPanelUI.GraphMode mode in Enum.GetValues(typeof(RunStatsPanelUI.GraphMode)))
                modeButtons.Add(Action(footer, "graph-" + mode, mode.ToString(), () => SetGraphMode(mode)));
        }
        public void SetGraphMode(RunStatsPanelUI.GraphMode mode) { graphMode = mode; if (IsOpen && selectedTab == 2) RefreshGraph(); }
        private void Refresh()
        {
            dirty = false;
            if (selectedTab == 1) return;
            if (selectedTab == 2) { RefreshGraph(); return; }
            if (selectedTab == 5) { RefreshItems(); return; }
            if (selectedTab == 3) { RefreshEnemies(); return; }
            if (selectedTab == 4) { RefreshTasks(); return; }
            var value = StatisticsPresentation.General(tracker, GameManager.Instance && GameManager.Instance.IsKillScalingMode);
            summaries[0].left.text = value.left; summaries[0].right.text = value.right;
            for (var i = 0; i < definition.maps.Length; i++)
            {
                var map = definition.maps[i]; value = StatisticsPresentation.Map(tracker.GetMapStats(map.config) ?? new GameData.MapStatistics(), map.killScaling);
                summaries[i + 1].left.text = value.left; summaries[i + 1].right.text = value.right;
            }
        }
        private void RefreshGraph()
        {
            var runs = tracker.RecentRuns; var (maximum, average) = StatisticsPresentation.Range(runs, graphMode);
            string Format(double n) => graphMode == RunStatsPanelUI.GraphMode.Duration ? CalcUtils.FormatTime(n) : CalcUtils.FormatNumber(n, true);
            maximumLabel.text = Format(maximum); averageLabel.text = Format(average);
            var ratio = maximum > 0 ? Mathf.Clamp01((float)(average / maximum)) : 0;
            averageLine.style.display = ratio >= .05f ? DisplayStyle.Flex : DisplayStyle.None; averageLine.style.bottom = Length.Percent(ratio * 100);
            graphTitle.text = "<smallcaps>" + (graphMode switch { RunStatsPanelUI.GraphMode.Duration => "Run Time", RunStatsPanelUI.GraphMode.Resources => "Resources Gathered", RunStatsPanelUI.GraphMode.Kills => "Enemies Killed", _ => "Distance from Town" }) + "</smallcaps>";
            oldestLabel.text = runs.Count > 0 ? runs[0].RunNumber.ToString() : ""; newestLabel.text = runs.Count > 0 ? runs[runs.Count - 1].RunNumber.ToString() : "";
            middleLabel.text = runs.Count > 0 && runs[runs.Count - 1].RunNumber >= 50 ? (Mathf.FloorToInt((runs[0].RunNumber + runs[runs.Count - 1].RunNumber) * .5f) + 1).ToString() : "";
            var colors = graphMode == RunStatsPanelUI.GraphMode.Resources ? definition.resourceColors : graphMode == RunStatsPanelUI.GraphMode.Kills ? definition.killColors : definition.distanceColors;
            for (var i = 0; i < bars.Count; i++)
            {
                var index = runs.Count - bars.Count + i; var bar = bars[i]; bool valid = index >= 0 && index < runs.Count;
                var run = valid ? runs[index] : null;
                bar.hit.userData = index;
                bar.fill.style.height = Length.Percent(valid && maximum > 0 ? Mathf.Clamp01((float)(StatisticsPresentation.Value(run, graphMode) / maximum)) * 100 : 0);
                bar.fill.style.backgroundColor = !valid ? Color.clear : run.Abandoned ? new Color(.57f,.55f,.54f) : run.Reaped ? new Color(.68f,.54f,.8f) : run.Died ? new Color(.85f,.39f,.38f) : new Color(.94f,.73f,.52f);
                bar.bonus.style.height = Length.Percent(valid && maximum > 0 && graphMode == RunStatsPanelUI.GraphMode.Resources ? Mathf.Clamp01((float)(run.BonusResourcesCollected / maximum)) * 100 : 0);
                bar.bonus.style.backgroundColor = new Color(.44f,.72f,.49f);
            }
            for (var i = 0; i < modeButtons.Count; i++) StyleSelection(modeButtons[i], i == (int)graphMode);
        }
        private void ShowRun(int bar, Vector2 position)
        {
            var index = tracker.RecentRuns.Count - bars.Count + bar; if (index < 0 || index >= tracker.RecentRuns.Count) return;
            tooltip.text = StatisticsPresentation.RunDetails(tracker.RecentRuns[index]); tooltip.style.display = DisplayStyle.Flex;
            var local = root.WorldToLocal(position); bool left = position.x < root.worldBound.center.x;
            tooltip.AddToClassList("surface");
            tooltip.style.left = Mathf.Clamp(local.x - (left ? .12f : .78f) * 160, 0, root.resolvedStyle.width - 160); tooltip.style.top = Mathf.Clamp(local.y - 35, 0, root.resolvedStyle.height - 100);
        }
        private Label Text(VisualElement parent, string value, float size)
        {
            var role = size >= 8 ? ToolkitControls.TextRole.Heading : size >= 7 ? ToolkitControls.TextRole.Subheading : size >= 6 ? ToolkitControls.TextRole.Body : ToolkitControls.TextRole.Caption;
            var label = ToolkitControls.Text(value, role); label.AddToClassList("eov-stat-text");
            if (size < 5) label.style.fontSize = Mathf.Max(7,size);
            parent.Add(label); return label;
        }
        private Button Action(VisualElement parent, string name, string title, Action clicked)
        {
            var button = ToolkitGameplay.B(parent,title,clicked);button.name=name; button.AddToClassList("eov-control--tab"); button.text = "<b>" + title + "</b>"; button.AddToClassList("eov-stat-tab"); if (parent.childCount > 0) parent.ElementAt(parent.childCount - 1).style.marginRight = 2; button.style.marginRight = 0; parent.Add(button); return button;
        }
        private void StyleSelection(Button button, bool selected)
        {
            button.EnableInClassList("active",selected);
        }
        private void Changed() { dirty = true; leaderboard?.Refresh(); }
        private void RunEnded(bool _) => Changed();
        private void DistanceChanged(float _) { if (selectedTab == 0) dirty = true; }
        private void Update() { if (!IsOpen) return; Layout(); if (dirty) Refresh(); }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() => windowLayout.Centered(root, theme);
        public void Hide()
        {
            leaderboard?.Dispose(); leaderboard = null;
            if (tracker) { tracker.OnRunEnded -= RunEnded; tracker.OnDistanceAdded -= DistanceChanged; tracker.OnTaskCompletedEvent -= TaskCompleted; tracker.OnMaxRunDistanceChanged -= MaximumDistanceChanged; }
            if (resourceManager) { resourceManager.OnInventoryChanged -= InventoryChanged; resourceManager.OnResourceAdded -= ResourceChanged; resourceManager.OnResourceTierUpgraded -= ResourceTierChanged; }
            if (killTracker) killTracker.OnKillRegistered -= KillChanged;
            TimelessEchoes.Tasks.TaskWeightService.ToggleChanged -= TaskToggleChanged; TimelessEchoes.Tasks.TaskWeightService.WeightsChanged -= TaskWeightsChanged;
            taskRows.Clear(); enemyRows.Clear(); statSortButtons.Clear(); distanceControls = null; enemyDistance = null;
            itemRows.Clear(); itemSortButtons.Clear(); itemContent = null;
            Blindsided.EventHandler.OnLoadData -= Changed; root?.RemoveFromHierarchy(); root = null; tracker = null; tabButtons.Clear(); modeButtons.Clear(); bars.Clear(); summaries.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
