using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Tasks;
using TimelessEchoes.Stats;
using TimelessEchoes.Skills;
using TimelessEchoes.MapGeneration;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class TaskStatisticsPresentation
    {
        private readonly List<TaskData> tasks = AssetCache.GetAll<TaskData>("Tasks").OrderBy(t => t.taskID).ThenBy(t => t.taskName).ToList();
        private readonly Dictionary<Skill, List<TaskData>> bySkill = new();
        private readonly Dictionary<TaskData, ProceduralTaskGenerator.WeightedTaskCategory> categories = new();
        private readonly Dictionary<ProceduralTaskGenerator.WeightedTaskCategory, float> categoryTotals = new();
        private float categoryWeightSum, worldX;
        private bool inRun;
        private MapGenerationConfig config;
        public TaskStatisticsPresentation()
        {
            foreach (var task in tasks)
            {
                if (!task.associatedSkill) continue;
                if (!bySkill.TryGetValue(task.associatedSkill, out var list)) bySkill[task.associatedSkill] = list = new();
                list.Add(task);
            }
        }
        public void Refresh(GameplayStatTracker tracker)
        {
            worldX = tracker && tracker.MaxRunDistance > 0 ? tracker.MaxRunDistance : 100;
            inRun = tracker && tracker.RunInProgress; config = inRun ? GameManager.CurrentGenerationConfig : null;
            categories.Clear(); categoryTotals.Clear(); categoryWeightSum = 0;
            if (!config) return;
            var settings = config.taskGeneratorSettings;
            foreach (var category in new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting })
            {
                if (category == null || categoryTotals.ContainsKey(category)) continue;
                categoryWeightSum += Mathf.Max(0, category.weight); float sum = 0;
                if (category.tasks != null) foreach (var task in category.tasks)
                {
                    if (!task) continue;
                    categories[task] = category; sum += TaskWeightService.GetEffectiveWeight(task, worldX);
                }
                categoryTotals[category] = sum;
            }
        }
        public List<TaskData> Ordered(TaskStatsPanelUI.SortMode mode, GameplayStatTracker tracker)
        {
            bool Known(TaskData t) => tracker && (tracker.GetTaskRecord(t)?.TotalCompleted ?? 0) > 0;
            var known = tasks.Where(Known).ToList(); var unknown = tasks.Where(t => !Known(t)).ToList();
            int Tie(TaskData a, TaskData b) { var c = a.taskID.CompareTo(b.taskID); return c != 0 ? c : string.Compare(a.taskName, b.taskName, StringComparison.Ordinal); }
            if (mode == TaskStatsPanelUI.SortMode.Completions || mode == TaskStatsPanelUI.SortMode.TaskTime)
                known.Sort((a, b) => { var ra = tracker.GetTaskRecord(a); var rb = tracker.GetTaskRecord(b); float va = mode == TaskStatsPanelUI.SortMode.Completions ? ra.TotalCompleted : ra.TimeSpent; float vb = mode == TaskStatsPanelUI.SortMode.Completions ? rb.TotalCompleted : rb.TimeSpent; var c = vb.CompareTo(va); return c != 0 ? c : Tie(a, b); });
            else { known.Sort(Tie); unknown.Sort(Tie); }
            if (mode == TaskStatsPanelUI.SortMode.Unknown) { unknown.AddRange(known); return unknown; }
            known.AddRange(unknown); return known;
        }
        private bool Chance(TaskData task, out float chance)
        {
            chance = 0;
            if (inRun)
            {
                if (!config || !categories.TryGetValue(task, out var category) || category == null) return true;
                var categoryWeight = Mathf.Max(0, category.weight);
                if (categoryWeightSum > 0 && categoryWeight > 0 && categoryTotals.TryGetValue(category, out var total) && total > 0)
                    chance = categoryWeight / categoryWeightSum * (TaskWeightService.GetEffectiveWeight(task, worldX) / total);
                return true;
            }
            if (!task.associatedSkill || !bySkill.TryGetValue(task.associatedSkill, out var list) || list.Count == 0) return false;
            float sum = 0; foreach (var candidate in list) if (TaskWeightService.IsTaskUnlocked(candidate)) sum += TaskWeightService.GetEffectiveWeight(candidate, worldX);
            if (TaskWeightService.IsTaskUnlocked(task) && sum > 0) chance = TaskWeightService.GetEffectiveWeight(task, worldX) / sum;
            return true;
        }
        public (string title, string totals, string detail, Sprite icon, bool toggleVisible, bool toggle) Describe(TaskData task, GameplayStatTracker tracker)
        {
            var record = tracker ? tracker.GetTaskRecord(task) : null;
            var completed = record?.TotalCompleted ?? 0; var known = completed > 0;
            var totals = ToolkitLocalization.Text("statistics.task-totals", "Completions: {0}\nTime on Task: {1}\nXP Gained: {2}", CalcUtils.FormatNumber(completed, true), CalcUtils.FormatTime(record?.TimeSpent ?? 0), CalcUtils.FormatNumber(record?.XpGained ?? 0, true));
            var detail = string.Empty;
            if (known)
            {
                var weight = TaskWeightService.GetEffectiveWeight(task, worldX); if (inRun && (!config || !categories.ContainsKey(task))) weight = 0;
                var weightText = CalcUtils.FormatNumber(Mathf.Max(0, weight), true);
                var distance = ToolkitLocalization.Text("statistics.task-min-distance", "Min Distance: {0}", CalcUtils.FormatNumber(task.GetEffectiveMinX(), true));
                if (task.enforceMaxDistance) distance += ToolkitLocalization.Text("statistics.task-max-distance", ", Max Distance: {0}", float.IsInfinity(task.maxX) ? ToolkitLocalization.Text("common.infinity", "Infinity") : CalcUtils.FormatNumber(task.maxX, true));
                if (Chance(task, out var chance))
                {
                    chance = Mathf.Clamp01(float.IsFinite(chance) ? chance : 0);
                    var next = TaskWeightService.TryGetNextThreshold(task, out var remaining) ? remaining : 0;
                    var improvement = next > 0 ? ToolkitLocalization.Text("statistics.task-improvement", "Next improvement: {0} Tasks", CalcUtils.FormatNumber(next, true)) : ToolkitLocalization.Text("statistics.task-improvement-maxed", "Next improvement: Maxed");
                    detail = ToolkitLocalization.Text("statistics.task-chance", "Spawn chance: {0:0.##}% ({1})\n{2}\n{3}", chance * 100, weightText, improvement, distance);
                }
                else detail = ToolkitLocalization.Text("statistics.task-no-chance", "Spawn chance: -- ({0})\nNext improvement: --\n{1}", weightText, distance);
            }
            return (known ? ToolkitLocalization.Text("task." + task.name, task.taskName) : "???", totals, detail, known ? task.taskIcon : null, known, TaskWeightService.IsToggleEnabled(task));
        }
    }
}
