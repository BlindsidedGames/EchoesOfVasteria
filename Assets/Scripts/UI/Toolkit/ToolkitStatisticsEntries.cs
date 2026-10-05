using System;
using System.Collections.Generic;
using TimelessEchoes.Tasks;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitStatisticsScreen
    {
        private TaskStatisticsPresentation taskPresentation;
        private EnemyStatisticsPresentation enemyPresentation;
        private TaskStatsPanelUI.SortMode taskSort;
        private EnemyStatsPanelUI.SortMode enemySort;
        private EnemyKillTracker killTracker;
        private VisualElement statContent, distanceControls;
        private Slider enemyDistance;
        private Label enemyDistanceLabel;
        private readonly Dictionary<TaskData, ToolkitStatRow> taskRows = new();
        private readonly Dictionary<EnemyData, ToolkitStatRow> enemyRows = new();
        private readonly List<Button> statSortButtons = new();

        private void BuildStatList(bool enemies)
        {
            
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            var scroll = ToolkitControls.RecessedScroll(body, enemies ? "enemy-statistics" : "task-statistics", theme, definition.inset);
            statContent = scroll.contentContainer; taskRows.Clear(); enemyRows.Clear(); statSortButtons.Clear();
            if (enemies)
            {
                enemyPresentation ??= new EnemyStatisticsPresentation();
                foreach (var data in enemyPresentation.Ordered(enemySort, killTracker))
                {
                    var row = new ToolkitStatRow("enemy-stat-" + data.name, "#" + data.displayOrder, 4, theme, definition.row, definition.iconFrame, definition.progressTrack, definition.progressFill);
                    enemyRows.Add(data, row); statContent.Add(row.Root);
                }
                distanceControls = new VisualElement { name = "enemy-distance-controls" }; distanceControls.AddToClassList("eov-stat-distance");  root.Insert(root.IndexOf(footer), distanceControls);
                enemyDistanceLabel = ToolkitControls.Text("", ToolkitControls.TextRole.Subheading); enemyDistanceLabel.style.unityTextAlign = TextAnchor.MiddleLeft; distanceControls.Add(enemyDistanceLabel);
                enemyDistance = ToolkitControls.Slider("enemy-distance", 0, tracker.MaxRunDistance, _ => RefreshEnemies(), definition.sliderTrack, definition.sliderFill, definition.sliderHandle);
                ToolkitGameplay.StyleSlider(enemyDistance);ToolkitControls.SetSliderValue(enemyDistance, tracker.MaxRunDistance); distanceControls.Add(enemyDistance);
                foreach (EnemyStatsPanelUI.SortMode mode in Enum.GetValues(typeof(EnemyStatsPanelUI.SortMode)))
                    statSortButtons.Add(Action(footer, "enemy-sort-" + mode, EnemySortLabel(mode), () => SetEnemySort(mode)));
            }
            else
            {
                taskPresentation ??= new TaskStatisticsPresentation();
                foreach (var data in taskPresentation.Ordered(taskSort, tracker))
                {
                    var task = data;
                    var row = new ToolkitStatRow("task-stat-" + data.name, "#" + data.taskID, 2, theme, definition.row, definition.iconFrame, null, null, () => TaskWeightService.SetToggle(task, !TaskWeightService.IsToggleEnabled(task)));
                    taskRows.Add(data, row); statContent.Add(row.Root);
                }
                foreach (TaskStatsPanelUI.SortMode mode in Enum.GetValues(typeof(TaskStatsPanelUI.SortMode)))
                    statSortButtons.Add(Action(footer, "task-sort-" + mode, TaskSortLabel(mode), () => SetTaskSort(mode)));
            }
        }
        private static string EnemySortLabel(EnemyStatsPanelUI.SortMode mode) => mode switch
        {
            EnemyStatsPanelUI.SortMode.Damage => ToolkitLocalization.Text("statistics.sort-damage", "Damage"),
            EnemyStatsPanelUI.SortMode.Health => ToolkitLocalization.Text("statistics.sort-health", "Health"),
            EnemyStatsPanelUI.SortMode.Defense => ToolkitLocalization.Text("statistics.sort-defense", "Defense"),
            EnemyStatsPanelUI.SortMode.AttackRate => ToolkitLocalization.Text("statistics.sort-attackrate", "Attack Rate"),
            EnemyStatsPanelUI.SortMode.MoveSpeed => ToolkitLocalization.Text("statistics.sort-movespeed", "Movement"),
            EnemyStatsPanelUI.SortMode.Vision => ToolkitLocalization.Text("statistics.sort-vision", "Vision"),
            _ => ToolkitLocalization.Text("statistics.sort-default", "Default")
        };
        private static string TaskSortLabel(TaskStatsPanelUI.SortMode mode) => mode switch
        {
            TaskStatsPanelUI.SortMode.Completions => ToolkitLocalization.Text("statistics.sort-completions", "Completions"),
            TaskStatsPanelUI.SortMode.TaskTime => ToolkitLocalization.Text("statistics.sort-tasktime", "Task Time"),
            TaskStatsPanelUI.SortMode.Unknown => ToolkitLocalization.Text("statistics.sort-unknown", "Unknown"),
            _ => ToolkitLocalization.Text("statistics.sort-default", "Default")
        };
        public void SetTaskSort(TaskStatsPanelUI.SortMode mode) { taskSort = mode; if (IsOpen && selectedTab == 4) RefreshTasks(); }
        public void SetEnemySort(EnemyStatsPanelUI.SortMode mode) { enemySort = mode; if (IsOpen && selectedTab == 3) RefreshEnemies(); }
        private void RefreshTasks()
        {
            taskPresentation.Refresh(tracker);
            var ordered = taskPresentation.Ordered(taskSort, tracker);
            for (var index = 0; index < ordered.Count; index++)
            {
                var task = ordered[index]; var row = taskRows[task];
                if (statContent.IndexOf(row.Root) != index) statContent.Insert(index, row.Root);
                var data = taskPresentation.Describe(task, tracker);
                row.Title.text = data.title; row.Fields[0].text = data.totals; row.Fields[1].text = data.detail; row.SetEntryIcon(data.icon, data.toggleVisible, data.toggle);
                row.Toggle.style.display = data.toggleVisible ? DisplayStyle.Flex : DisplayStyle.None;
                ToolkitGameplay.SetToggle(row.ToggleIcon,data.toggle);
                row.Toggle.tooltip = data.toggle ? ToolkitLocalization.Text("statistics.task-boost-enabled", "Spawn boost enabled. Click to remove the task weight bonus.") : ToolkitLocalization.Text("statistics.task-boost-hint", "Boost this task's spawn weight. Completion milestones increase the bonus.");
            }
            for (var i = 0; i < statSortButtons.Count; i++) StyleSelection(statSortButtons[i], i == (int)taskSort);
        }
        private void RefreshEnemies()
        {
            var distance = enemyDistance?.value ?? tracker.MaxRunDistance;
            if (enemyDistanceLabel != null) enemyDistanceLabel.text = ToolkitLocalization.Text("statistics.preview-distance", "Preview distance: {0:N0}", distance);
            var ordered = enemyPresentation.Ordered(enemySort, killTracker);
            for (var index = 0; index < ordered.Count; index++)
            {
                var enemy = ordered[index]; var row = enemyRows[enemy];
                if (statContent.IndexOf(row.Root) != index) statContent.Insert(index, row.Root);
                var data = enemyPresentation.Describe(enemy, killTracker, distance);
                row.Title.text = data.title;
                for (var i = 0; i < row.EnemyValues.Length; i++) row.EnemyValues[i].text = data.values[i];
                row.SetEntryIcon(data.icon, killTracker && killTracker.GetKills(enemy) > 0);
                row.SetProgress(data.showProgress, data.progress);
            }
            for (var i = 0; i < statSortButtons.Count; i++) StyleSelection(statSortButtons[i], i == (int)enemySort);
        }
        private void MaximumDistanceChanged(float value)
        {
            if (selectedTab == 3 && enemyDistance != null)
            {
                bool atMaximum = Mathf.Approximately(enemyDistance.value, enemyDistance.highValue);
                var current = enemyDistance.value; enemyDistance.highValue = value;
                ToolkitControls.SetSliderValue(enemyDistance, atMaximum || current > value ? value : current);
            }
            if (selectedTab == 0 || selectedTab == 3 || selectedTab == 4) dirty = true;
        }
        private void TaskToggleChanged(TaskData _, bool __) { if (selectedTab == 4) dirty = true; }
        private void TaskWeightsChanged() { if (selectedTab == 4) dirty = true; }
        private void KillChanged(EnemyData _) { if (selectedTab == 0 || selectedTab == 3) dirty = true; }
        private void TaskCompleted() { if (selectedTab == 0 || selectedTab == 4) dirty = true; }
        private void InventoryChanged() { if (selectedTab == 5) dirty = true; }
    }
}
