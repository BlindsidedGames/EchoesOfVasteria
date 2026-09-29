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
        private VisualElement root, body, footer;
        private Label tooltip;
        private sealed class SummaryRow { public readonly List<(Label name, Label value)> fields = new(); }
        private readonly List<SummaryRow> summaries = new();
        private readonly List<Button> tabButtons = new();
        private GameplayStatTracker tracker;
        private bool dirty;
        private ToolkitLeaderboardView leaderboard;
        private int selectedTab;
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
            runGraph = null; selectedTab = index; distanceControls?.RemoveFromHierarchy(); distanceControls = null; enemyDistance = null; body.Clear(); footer.Clear(); summaries.Clear(); tooltip.style.display = DisplayStyle.None;
            for (var i = 0; i < tabButtons.Count; i++) StyleSelection(tabButtons[i], i == index);
            footer.style.display = index != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (index == 0) BuildGeneral(); else if (index == 1) leaderboard = new ToolkitLeaderboardView(body, footer, theme, definition); else if (index == 2) BuildGraph(); else if (index == 5) BuildItems(); else BuildStatList(index == 3); Refresh();
        }
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
                var size = sprite.rect.size * (16f / sprite.pixelsPerUnit);
                image.style.width = size.x; image.style.height = size.y; image.style.flexShrink = 0; mask.Add(image);
            }
            Text(identity, map.HasValue ? map.Value.label.Trim() : "Overall", 8).AddToClassList("general-title");
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
        private void BuildGraph() => runGraph = new ToolkitRunGraphView(body, footer, definition, () => tracker.RecentRuns);
        public void SetGraphMode(RunStatsPanelUI.GraphMode mode)
        {
            if (IsOpen && selectedTab == 2) runGraph?.SetMetric((ToolkitRunGraphView.Metric)Enum.Parse(typeof(ToolkitRunGraphView.Metric), mode.ToString()));
        }
        private void Refresh()
        {
            dirty = false;
            if (selectedTab == 1) return;
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
            journalSelectors.Clear(); journalDiscoveries.Clear(); journalContent = journalRecent = journalCollection = null; journalDiscoveryCount = null;
            Blindsided.EventHandler.OnLoadData -= Changed; root?.RemoveFromHierarchy(); root = null; tracker = null; runGraph = null; tabButtons.Clear(); summaries.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
