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
        private readonly Dictionary<TaskData, ProceduralTaskGenerator.WeightedTaskCategory> categories = new();
        private readonly Dictionary<ProceduralTaskGenerator.WeightedTaskCategory, float> categoryTotals = new();
        private float categoryWeightSum, worldX;
        private bool inRun;
        private MapGenerationConfig config;
        public void Refresh(GameplayStatTracker tracker)
        {
            worldX = Hero.HeroController.Instance ? Hero.HeroController.Instance.transform.position.x : 0;
            inRun = tracker && tracker.RunInProgress; config = inRun ? GameManager.CurrentGenerationConfig : null;
            categories.Clear(); categoryTotals.Clear(); categoryWeightSum = 0;
            if (!config) return;
            var settings = config.taskGeneratorSettings;
            foreach (var category in new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting })
            {
                if (category == null || categoryTotals.ContainsKey(category) || !CategoryAllowed(category)) continue;
                categoryWeightSum += category.GetWeight(worldX); float sum = 0;
                if (category.tasks != null) foreach (var task in category.tasks)
                {
                    if (!task) continue;
                    categories[task] = category; sum += SelectionWeight(task);
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
        public float MapWeight(Skill skill)
        {
            var settings=GameManager.CurrentGenerationConfig?.taskGeneratorSettings;
            if(settings==null||!skill)return 0;
            // Keep map relevance stable when task preferences or skill levels change.
            return new[]{settings.woodcutting,settings.mining,settings.farming,settings.fishing,settings.looting}
                .Where(category=>category?.tasks!=null&&category.tasks.Any(task=>task&&task.associatedSkill==skill))
                .Select(category=>Mathf.Max(0,category.weight)).DefaultIfEmpty(0).Max();
        }
        private static bool CategoryAllowed(ProceduralTaskGenerator.WeightedTaskCategory category) =>
            TimelessEchoes.Quests.QuestUtils.QuestCompleted(category.requiredQuest) &&
            (string.IsNullOrEmpty(category.requiredNpcId) || Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains(category.requiredNpcId));

        private float SelectionWeight(TaskData task, bool? favoured = null)
        {
            if (!config || !task) return 0;
            var terrain = config.tilemapChunkSettings;
            var available = new[] { terrain.bottomTerrain, terrain.middleTerrain, terrain.topTerrain };
            if (task.spawnTerrains?.Count > 0 && !available.Any(t => t && task.spawnTerrains.Contains(t))) return 0;
            return TaskWeightService.GetEffectiveWeight(task, worldX, favoured) * task.GetTerrainMultiplier(available);
        }

        private bool Chance(TaskData task, out float chance, bool? favoured = null)
        {
            chance = 0;
            if (inRun)
            {
                if (!config || !categories.TryGetValue(task, out var category) || category == null) return true;
                var categoryWeight = favoured.HasValue ? category.GetWeight(worldX, task, favoured.Value) : category.GetWeight(worldX);
                var allCategories = favoured.HasValue ? categoryWeightSum - category.GetWeight(worldX) + categoryWeight : categoryWeightSum;
                if (categoryTotals.TryGetValue(category, out var total))
                {
                    if (favoured.HasValue) total += SelectionWeight(task, favoured) - SelectionWeight(task);
                    if (allCategories > 0 && categoryWeight > 0 && total > 0)
                        chance = categoryWeight / allCategories * SelectionWeight(task, favoured) / total;
                }
                return true;
            }
            return false;
        }
        public bool TryChance(TaskData task, out float chance) => Chance(task, out chance);
        public (string title, string totals, string detail, Sprite icon, bool toggleVisible, bool toggle) Describe(TaskData task, GameplayStatTracker tracker)
        {
            var record = tracker ? tracker.GetTaskRecord(task) : null;
            var completed = record?.TotalCompleted ?? 0; var known = completed > 0;
            var totals = ToolkitLocalization.Text("statistics.task-totals", "Completions: {0}\nTime on Task: {1}\nXP Gained: {2}", CalcUtils.FormatNumber(completed, true), CalcUtils.FormatTime(record?.TimeSpent ?? 0), CalcUtils.FormatNumber(record?.XpGained ?? 0, true));
            if (ToolkitResponsiveLayout.IsPortrait)
                totals = ToolkitLocalization.Text("statistics.task-totals-compact", "Done: {0}\nTime: {1}\nXP: {2}", CalcUtils.FormatNumber(completed, true), CalcUtils.FormatTime(record?.TimeSpent ?? 0), CalcUtils.FormatNumber(record?.XpGained ?? 0, true));
            var detail = string.Empty;
            if (known)
            {
                var next = TaskWeightService.TryGetNextThreshold(task, out var remaining) ? remaining : 0;
                var improvement = next > 0
                    ? ToolkitLocalization.Text("statistics.favour-next", "Next boost: {0} completions", CalcUtils.FormatNumber(next, true))
                    : ToolkitLocalization.Text("statistics.favour-max", "Boost maxed");
                detail = improvement;
                if (Chance(task, out var chance))
                {
                    chance = Mathf.Clamp01(chance);
                    var line = ToolkitLocalization.Text("statistics.favour-chance", "Chance: {0:0.##}%", chance * 100);
                    if (!TaskWeightService.IsToggleEnabled(task) && Chance(task, out var preview, true) && !Mathf.Approximately(chance, preview))
                        line = ToolkitLocalization.Text("statistics.favour-preview", "Chance: {0:0.##}% → {1:0.##}%", chance * 100, Mathf.Clamp01(preview) * 100);
                    detail = line + "\n" + improvement;
                }
            }
            return (known ? ToolkitLocalization.Text("task." + task.name, task.taskName) : "???", totals, detail, known ? task.taskIcon : null, known, TaskWeightService.IsToggleEnabled(task));
        }
    }
}
