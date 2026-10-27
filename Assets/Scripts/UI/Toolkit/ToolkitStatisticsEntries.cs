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
        private EnemyStatsPanelUI.SortMode enemySort;
        private EnemyKillTracker killTracker;
        private VisualElement statContent, distanceControls;
        private Slider enemyDistance;
        private Label enemyDistanceLabel;
        private ToolkitTaskPreferences taskPreferences;
        private readonly Dictionary<EnemyData, ToolkitStatRow> enemyRows = new();
        private readonly List<Button> statSortButtons = new();

        private void BuildStatList(bool enemies)
        {
            
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            if (!enemies)
            {
                taskPresentation ??= new TaskStatisticsPresentation();
                taskPreferences = new ToolkitTaskPreferences(body, root, theme, definition.inset, taskPresentation, tracker);
                return;
            }
            var scroll = ToolkitControls.RecessedScroll(body, enemies ? "enemy-statistics" : "task-statistics", theme, definition.inset);
            statContent = scroll.contentContainer; enemyRows.Clear(); statSortButtons.Clear();
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
        public void SetEnemySort(EnemyStatsPanelUI.SortMode mode) { enemySort = mode; if (IsOpen && selectedTab == 3) RefreshEnemies(); }
        private void RefreshTasks()
        {
            taskPreferences?.Refresh();
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
        private void TaskToggleChanged(TaskData task, bool enabled)
        {
            if (selectedTab == 4) dirty = true;
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            if (IsOpen && selectedTab == 4 && general?.TaskBoostTutorialStage == 1 && task == TaskBoostGuidance.FindTask())
                general.TaskBoostTutorialStage = 2;
        }
        private void TaskWeightsChanged() { if (selectedTab == 4) dirty = true; }
        private void KillChanged(EnemyData _) { if (selectedTab == 0 || selectedTab == 3) dirty = true; }
        private void TaskCompleted() { if (selectedTab == 0 || selectedTab == 4) dirty = true; }
        private void InventoryChanged() { if (selectedTab == 5) dirty = true; }
    }
}
