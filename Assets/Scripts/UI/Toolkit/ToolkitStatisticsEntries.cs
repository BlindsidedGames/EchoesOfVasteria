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
                enemyDistanceLabel = ToolkitControls.Text("", ToolkitControls.TextRole.Subheading); enemyDistanceLabel.style.unityTextAlign = TextAnchor.MiddleCenter; distanceControls.Add(enemyDistanceLabel);
                enemyDistance = ToolkitControls.Slider("enemy-distance", 0, tracker.MaxRunDistance, _ => RefreshEnemies(), definition.sliderTrack, definition.sliderFill, definition.sliderHandle);
                ToolkitGameplay.StyleSlider(enemyDistance);ToolkitControls.SetSliderValue(enemyDistance, tracker.MaxRunDistance); distanceControls.Add(enemyDistance);
                foreach (EnemyStatsPanelUI.SortMode mode in Enum.GetValues(typeof(EnemyStatsPanelUI.SortMode)))
                    statSortButtons.Add(Action(footer, "enemy-sort-" + mode, mode switch { EnemyStatsPanelUI.SortMode.AttackRate => "Attack Rate", EnemyStatsPanelUI.SortMode.MoveSpeed => "Movement", _ => mode.ToString() }, () => SetEnemySort(mode)));
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
                    statSortButtons.Add(Action(footer, "task-sort-" + mode, mode == TaskStatsPanelUI.SortMode.TaskTime ? "Task Time" : mode.ToString(), () => SetTaskSort(mode)));
            }
        }
        public void SetTaskSort(TaskStatsPanelUI.SortMode mode) { taskSort = mode; if (IsOpen && selectedTab == 4) RefreshTasks(); }
        public void SetEnemySort(EnemyStatsPanelUI.SortMode mode) { enemySort = mode; if (IsOpen && selectedTab == 3) RefreshEnemies(); }
        private void RefreshTasks()
        {
            taskPresentation.Refresh(tracker);
            foreach (var task in taskPresentation.Ordered(taskSort, tracker))
            {
                var row = taskRows[task]; row.Root.BringToFront(); var data = taskPresentation.Describe(task, tracker);
                row.Title.text = data.title; row.Fields[0].text = data.totals; row.Fields[1].text = data.detail; row.Icon.sprite = data.icon;
                row.Toggle.style.display = data.toggleVisible ? DisplayStyle.Flex : DisplayStyle.None;
                ToolkitGameplay.SetToggle(row.ToggleIcon,data.toggle);
            }
            for (var i = 0; i < statSortButtons.Count; i++) StyleSelection(statSortButtons[i], i == (int)taskSort);
        }
        private void RefreshEnemies()
        {
            var distance = enemyDistance?.value ?? tracker.MaxRunDistance;
            if (enemyDistanceLabel != null) enemyDistanceLabel.text = $"Distance | {distance:N0}";
            foreach (var enemy in enemyPresentation.Ordered(enemySort, killTracker))
            {
                var row = enemyRows[enemy]; row.Root.BringToFront(); var data = enemyPresentation.Describe(enemy, killTracker, distance);
                row.Title.text = data.title; row.Fields[0].text = data.health; row.Fields[1].text = data.defense; row.Fields[2].text = data.movement; row.Fields[3].text = data.kills; row.Icon.sprite = data.icon;
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
