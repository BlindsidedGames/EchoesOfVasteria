using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TMPro;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.UI.Core;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Event-driven task stats panel. Subscribes to TaskWeightService and GameplayStatTracker
    /// events instead of polling via UITicker.
    /// </summary>
    public class TaskStatsPanelUI : EventDrivenStatsPanelUI
    {
        [SerializeField] private StatPanelReferences references;
        private GameplayStatTracker statTracker;
        private float lastKnownMaxDistance;

        private readonly Dictionary<TaskData, TaskStatEntryUIReferences> entries = new();
        private readonly Dictionary<TaskData, EntryDisplayState> lastDisplayedByTask = new();
        private readonly Dictionary<TaskData, ProceduralTaskGenerator.WeightedTaskCategory> categoryByTask = new();
        private readonly List<ProceduralTaskGenerator.WeightedTaskCategory> activeCategories = new();
        private readonly Dictionary<ProceduralTaskGenerator.WeightedTaskCategory, float> categoryTaskWeights = new();
        private readonly Dictionary<Skill, List<TaskData>> tasksBySkill = new();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(128);
        private MapGenerationConfig cachedConfig;
        private float cachedCategoryWeightSum;
        private float cachedWeightsWorldX = float.NaN;
        private List<TaskData> defaultOrder = new();

        // Scratch lists for allocation-free sorting
        private readonly List<TaskData> _scratchKnown = new();
        private readonly List<TaskData> _scratchUnknown = new();
        private readonly List<TaskData> _scratchFinal = new();
        private bool _sortDirty = true;
        private SortMode _lastAppliedSortMode;

        // Static comparison delegate to avoid closure allocations
        private static readonly System.Comparison<TaskData> CompareByIdThenName =
            (a, b) =>
            {
                int cmp = a.taskID.CompareTo(b.taskID);
                return cmp != 0 ? cmp : string.Compare(a.taskName, b.taskName, System.StringComparison.Ordinal);
            };

        public enum SortMode
        {
            Default,
            Completions,
            TaskTime,
            Unknown
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

        private struct EntryDisplayState
        {
            public int completed;
            public float time;
            public float xp;
            public float spawnChance;
            public float weight;
            public int nextImprovement;
            public bool toggleOn;

            public bool Matches(int nextCompleted, float nextTime, float nextXp, float nextSpawnChance, float nextWeight, int nextNextImprovement, bool nextToggleOn)
            {
                return completed == nextCompleted
                       && Mathf.Approximately(time, nextTime)
                       && Mathf.Approximately(xp, nextXp)
                       && Mathf.Approximately(spawnChance, nextSpawnChance)
                       && Mathf.Approximately(weight, nextWeight)
                       && nextImprovement == nextNextImprovement
                       && toggleOn == nextToggleOn;
            }

            public void Set(int newCompleted, float newTime, float newXp, float newSpawnChance, float newWeight, int newNextImprovement, bool newToggle)
            {
                completed = newCompleted;
                time = newTime;
                xp = newXp;
                spawnChance = newSpawnChance;
                weight = newWeight;
                nextImprovement = newNextImprovement;
                toggleOn = newToggle;
            }
        }

        private void Awake()
        {
            if (references == null)
                references = GetComponent<StatPanelReferences>();
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
            BuildEntries();
        }

        protected override bool IsPanelVisible()
        {
            if (references != null && references.taskEntryParent != null)
                return references.taskEntryParent.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        protected override void SubscribeToEvents()
        {
            // Re-acquire tracker in case it wasn't ready at Awake
            if (statTracker == null)
                statTracker = GameplayStatTracker.Instance;
            
            // Subscribe to task toggle changes
            TaskWeightService.ToggleChanged += OnTaskWeightToggleChanged;
            
            if (statTracker != null)
            {
                statTracker.OnMaxRunDistanceChanged += OnMaxRunDistanceChanged;
                statTracker.OnTaskCompletedEvent += OnTaskCompleted;
            }
            
            SetupDistanceSlider();
        }

        protected override void UnsubscribeFromEvents()
        {
            TaskWeightService.ToggleChanged -= OnTaskWeightToggleChanged;
            
            if (statTracker != null)
            {
                statTracker.OnMaxRunDistanceChanged -= OnMaxRunDistanceChanged;
                statTracker.OnTaskCompletedEvent -= OnTaskCompleted;
            }
        }

        // Event handlers
        private void OnTaskWeightToggleChanged(TaskData task, bool _)
        {
            cachedWeightsWorldX = float.NaN;
            _sortDirty = true; // Toggle change may affect spawn weights and sort order
            OnDataChanged();
        }

        private void OnMaxRunDistanceChanged(float newMax)
        {
            lastKnownMaxDistance = newMax > 0f ? newMax : 100f;
            OnDataChanged();
        }

        private void OnTaskCompleted()
        {
            _sortDirty = true; // Completion may change known/unknown status
            OnDataChanged();
        }

        private void SetupDistanceSlider()
        {
            // Distance slider is no longer used for tasks - spawning uses skill-based unlocks instead.
            // Just track max distance for display purposes.
            lastKnownMaxDistance = GetPreviewDistance();
        }

        private float GetPreviewDistance()
        {
            // Use max distance if available, otherwise use a reasonable default for preview calculations.
            // The default of 100f ensures spawn weights work correctly when no run has been started yet.
            var tracker = GameplayStatTracker.Instance;
            var maxDist = tracker != null ? tracker.MaxRunDistance : 0f;
            return maxDist > 0f ? maxDist : 100f;
        }

        public void SetSortMode(SortMode mode)
        {
            if (sortMode == mode) return;
            sortMode = mode;
            _sortDirty = true;
            if (IsPanelVisible())
                RefreshUI();
        }

        private void BuildEntries()
        {
            if (references == null || references.taskEntryParent == null || references.taskEntryPrefab == null)
                return;

            UIUtils.ClearChildren(references.taskEntryParent);

            var allTasks = Blindsided.Utilities.AssetCache.GetAll<TaskData>("Tasks");
            var sorted = allTasks
                .OrderBy(t => t.taskID)
                .ThenBy(t => t.taskName)
                .ToList();
            defaultOrder = sorted;
            entries.Clear();
            tasksBySkill.Clear();
            _sortDirty = true; // Ensure initial sort happens

            foreach (var data in sorted)
            {
                var obj = Instantiate(references.taskEntryPrefab.gameObject, references.taskEntryParent);
                var ui = obj.GetComponent<TaskStatEntryUIReferences>();
                if (ui == null) continue;
                entries[data] = ui;
                ConfigureEntry(data, ui);

                var skill = data.associatedSkill;
                if (!tasksBySkill.TryGetValue(skill, out var list))
                {
                    list = new List<TaskData>();
                    tasksBySkill[skill] = list;
                }
                list.Add(data);
            }
        }

        private void ConfigureEntry(TaskData data, TaskStatEntryUIReferences ui)
        {
            if (ui == null || ui.toggleButton == null)
                return;

            ui.toggleButton.onClick.RemoveAllListeners();
            var captured = data;
            ui.toggleButton.onClick.AddListener(() => OnToggleClicked(captured));
            if (ui.toggleImage != null)
            {
                ui.toggleImage.sprite = null;
                ui.toggleImage.enabled = false;
            }
            ui.toggleButton.gameObject.SetActive(false);
        }

        private void OnToggleClicked(TaskData data)
        {
            if (data == null) return;
            var current = TaskWeightService.IsToggleEnabled(data);
            TaskWeightService.SetToggle(data, !current);
        }

        protected override void RefreshUI()
        {
            UpdateEntries();
            SortEntries();
            isDirty = false;
        }

        private void UpdateEntries()
        {
            var runActive = statTracker != null && statTracker.RunInProgress;
            RefreshCategoryMappings(runActive);
            var worldX = GetPreviewDistance();
            if (runActive)
                RefreshCategoryWeightCache(worldX);
            else
            {
                cachedWeightsWorldX = float.NaN;
                cachedCategoryWeightSum = 0f;
            }

            foreach (var pair in entries)
                UpdateEntry(pair.Key, pair.Value, worldX, runActive);
        }

        private void RefreshCategoryMappings(bool runActive)
        {
            if (!runActive)
            {
                cachedConfig = null;
                categoryByTask.Clear();
                activeCategories.Clear();
                categoryTaskWeights.Clear();
                return;
            }

            var config = TimelessEchoes.GameManager.CurrentGenerationConfig;
            if (cachedConfig == config)
                return;

            cachedConfig = config;
            categoryByTask.Clear();
            activeCategories.Clear();
            categoryTaskWeights.Clear();

            if (config == null)
                return;

            var settings = config.taskGeneratorSettings;
            RegisterCategory(settings.woodcutting);
            RegisterCategory(settings.mining);
            RegisterCategory(settings.farming);
            RegisterCategory(settings.fishing);
            RegisterCategory(settings.looting);
        }

        private void RegisterCategory(ProceduralTaskGenerator.WeightedTaskCategory category)
        {
            if (category == null)
                return;
            if (!activeCategories.Contains(category))
                activeCategories.Add(category);
            if (category.tasks == null)
                return;
            foreach (var task in category.tasks)
            {
                if (task == null) continue;
                categoryByTask[task] = category;
            }
        }

        private void RefreshCategoryWeightCache(float worldX)
        {
            if (!float.IsNaN(cachedWeightsWorldX) && Mathf.Approximately(cachedWeightsWorldX, worldX))
                return;

            cachedWeightsWorldX = worldX;
            cachedCategoryWeightSum = 0f;
            categoryTaskWeights.Clear();

            foreach (var category in activeCategories)
            {
                if (category == null)
                    continue;

                var categoryWeight = Mathf.Max(0f, category.weight);
                cachedCategoryWeightSum += categoryWeight;

                float taskSum = 0f;
                if (category.tasks != null)
                {
                    foreach (var task in category.tasks)
                    {
                        if (task == null) continue;
                        taskSum += TaskWeightService.GetEffectiveWeight(task, worldX);
                    }
                }
                categoryTaskWeights[category] = taskSum;
            }
        }

        private bool TryGetSpawnChance(TaskData data, float worldX, bool runActive, out float chance)
        {
            chance = 0f;
            if (runActive)
            {
                if (cachedConfig == null)
                {
                    chance = 0f;
                    return true;
                }

                if (categoryByTask.TryGetValue(data, out var category) && category != null)
                {
                    var categoryWeight = Mathf.Max(0f, category.weight);
                    if (categoryWeight <= 0f || cachedCategoryWeightSum <= 0f)
                    {
                        chance = 0f;
                        return true;
                    }

                    if (!categoryTaskWeights.TryGetValue(category, out var totalTaskWeight))
                        totalTaskWeight = 0f;

                    var taskWeight = TaskWeightService.GetEffectiveWeight(data, worldX);
                    var categoryShare = categoryWeight / cachedCategoryWeightSum;
                    var taskShare = totalTaskWeight > 0f ? taskWeight / totalTaskWeight : 0f;

                    chance = categoryShare * taskShare;
                    return true;
                }

                chance = 0f;
                return true;
            }

            // Town/default view: compare only within the associated skill (category) independent of map weights.
            // Only include unlocked tasks in the calculation.
            var skill = data.associatedSkill;
            if (!tasksBySkill.TryGetValue(skill, out var list) || list == null || list.Count == 0)
                return false;

            float total = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                if (IsTaskUnlocked(list[i]))
                    total += TaskWeightService.GetEffectiveWeight(list[i], worldX);
            }

            // If task is locked, spawn chance is 0
            if (!IsTaskUnlocked(data))
            {
                chance = 0f;
                return true;
            }

            var weight = TaskWeightService.GetEffectiveWeight(data, worldX);
            chance = total > 0f ? weight / total : 0f;
            return true;
        }

        private void UpdateEntry(TaskData data, TaskStatEntryUIReferences ui, float worldX, bool runActive)
        {
            if (data == null || ui == null) return;

            var record = statTracker ? statTracker.GetTaskRecord(data) : null;
            var completed = record?.TotalCompleted ?? 0;
            var time = record?.TimeSpent ?? 0f;
            var xp = record?.XpGained ?? 0f;
            var toggleEnabled = TaskWeightService.IsToggleEnabled(data);
            var effectiveWeight = TaskWeightService.GetEffectiveWeight(data, worldX);
            if (runActive)
            {
                if (cachedConfig == null || !categoryByTask.ContainsKey(data))
                    effectiveWeight = 0f;
            }

            var hasCompletions = completed > 0;
            float spawnChance = -1f;
            bool hasChanceInfo = false;
            if (hasCompletions)
            {
                hasChanceInfo = TryGetSpawnChance(data, worldX, runActive, out var chance);
                if (hasChanceInfo)
                    spawnChance = Mathf.Clamp01(float.IsFinite(chance) ? chance : 0f);
            }

            int nextImprovement = hasCompletions && TaskWeightService.TryGetNextThreshold(data, out var remaining)
                ? remaining
                  : (hasCompletions ? 0 : -1);

            if (lastDisplayedByTask.TryGetValue(data, out var cached))
            {
                // Detect transition from unknown to known - requires re-sort
                if (cached.completed == 0 && completed > 0)
                    _sortDirty = true;

                if (cached.Matches(completed, time, xp, spawnChance, effectiveWeight, nextImprovement, toggleEnabled))
                    return;
            }

            var updatedState = new EntryDisplayState();
            updatedState.Set(completed, time, xp, spawnChance, effectiveWeight, nextImprovement, toggleEnabled);
            lastDisplayedByTask[data] = updatedState;

            if (ui.entryIconImage != null)
            {
                ui.entryIconImage.sprite = completed > 0 ? data.taskIcon : null;
                if (completed > 0 && data.taskIcon != null)
                    ui.entryIconImage.SetNativeSize();
                ui.entryIconImage.enabled = completed > 0 && data.taskIcon != null;
            }

            if (ui.entryIDText != null)
                ui.entryIDText.text = $"#{data.taskID}";

            if (ui.entryNameText != null)
                ui.entryNameText.text = completed > 0 ? data.taskName : "???";

            if (ui.entryCompletionsTimeOnTaskExperienceText != null)
            {
                _sb.Clear();
                _sb.Append("Completions: "); _sb.Append(CalcUtils.FormatNumber(completed, true)); _sb.Append('\n');
                _sb.Append("Time on Task: "); _sb.Append(CalcUtils.FormatTime(time)); _sb.Append('\n');
                _sb.Append("XP Gained: "); _sb.Append(CalcUtils.FormatNumber(xp, true));
                ui.entryCompletionsTimeOnTaskExperienceText.SetText(_sb);
            }

            if (ui.entrySpawnDistanceText != null)
            {
                var weightText = CalcUtils.FormatNumber(Mathf.Max(0f, effectiveWeight), true);
                string minStr = CalcUtils.FormatNumber(data.GetEffectiveMinX(), true);
                bool showMaxDistance = data != null && data.enforceMaxDistance;
                string maxStr = float.IsInfinity(data.maxX)
                    ? "Infinity"
                    : CalcUtils.FormatNumber(data.maxX, true);
                string distanceLine = showMaxDistance
                    ? $"Min Distance: {minStr}, Max Distance: {maxStr}"
                    : $"Min Distance: {minStr}";

                if (!hasCompletions)
                {
                    ui.entrySpawnDistanceText.text = string.Empty;
                }
                else if (hasChanceInfo)
                {
                    var chancePercent = (spawnChance * 100f).ToString("0.##");
                    string nextLine = nextImprovement > 0
                        ? $"Next improvement: {CalcUtils.FormatNumber(nextImprovement, true)} Tasks"
                        : "Next improvement: Maxed";
                    ui.entrySpawnDistanceText.text =
                        $"Spawn chance: {chancePercent}% ({weightText})\n{nextLine}\n{distanceLine}";
                }
                else
                {
                    ui.entrySpawnDistanceText.text =
                        $"Spawn chance: -- ({weightText})\nNext improvement: --\n{distanceLine}";
                }
            }

            if (ui.toggleButton != null)
            {
                ui.toggleButton.gameObject.SetActive(hasCompletions);
                if (hasCompletions)
                {
                    ui.toggleButton.interactable = true;
                    if (ui.toggleImage != null)
                    {
                        ui.toggleImage.enabled = true;
                        ui.toggleImage.sprite = toggleEnabled ? ui.toggleOnSprite : ui.toggleOffSprite;
                    }
                }
                else
                {
                    if (ui.toggleImage != null)
                    {
                        ui.toggleImage.sprite = null;
                        ui.toggleImage.enabled = false;
                    }
                }
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0)
                return;

            // Early exit if no re-sort needed
            if (!_sortDirty && sortMode == _lastAppliedSortMode)
                return;

            _sortDirty = false;
            _lastAppliedSortMode = sortMode;

            // Clear scratch lists for reuse
            _scratchKnown.Clear();
            _scratchUnknown.Clear();
            _scratchFinal.Clear();

            // Partition into known/unknown
            for (int i = 0; i < defaultOrder.Count; i++)
            {
                var task = defaultOrder[i];
                if (statTracker != null && (statTracker.GetTaskRecord(task)?.TotalCompleted ?? 0) > 0)
                    _scratchKnown.Add(task);
                else
                    _scratchUnknown.Add(task);
            }

            if (sortMode == SortMode.Default)
            {
                // Sort each partition by ID then name
                _scratchKnown.Sort(CompareByIdThenName);
                _scratchUnknown.Sort(CompareByIdThenName);

                // Combine: known first, then unknown
                _scratchFinal.AddRange(_scratchKnown);
                _scratchFinal.AddRange(_scratchUnknown);
                ApplyOrderScratch();
                return;
            }

            if (sortMode == SortMode.Unknown)
            {
                // Sort each partition by ID then name
                _scratchKnown.Sort(CompareByIdThenName);
                _scratchUnknown.Sort(CompareByIdThenName);

                // Combine: unknown first, then known
                _scratchFinal.AddRange(_scratchUnknown);
                _scratchFinal.AddRange(_scratchKnown);
                ApplyOrderScratch();
                return;
            }

            // Completions or TaskTime mode - sort known by value descending
            var currentMode = sortMode;
            var tracker = statTracker;
            _scratchKnown.Sort((a, b) =>
            {
                float valA = GetStatValue(a, currentMode, tracker);
                float valB = GetStatValue(b, currentMode, tracker);
                int cmp = valB.CompareTo(valA); // Descending
                if (cmp != 0) return cmp;
                cmp = a.taskID.CompareTo(b.taskID);
                return cmp != 0 ? cmp : string.Compare(a.taskName, b.taskName, System.StringComparison.Ordinal);
            });

            // Combine: sorted known first, then unknown (keeps default order)
            _scratchFinal.AddRange(_scratchKnown);
            _scratchFinal.AddRange(_scratchUnknown);
            ApplyOrderScratch();
        }

        private static float GetStatValue(TaskData t, SortMode mode, GameplayStatTracker tracker)
        {
            if (tracker == null) return 0f;
            var record = tracker.GetTaskRecord(t);
            if (record == null) return 0f;
            return mode == SortMode.Completions ? record.TotalCompleted : record.TimeSpent;
        }

        private void ApplyOrderScratch()
        {
            for (int i = 0; i < _scratchFinal.Count; i++)
            {
                if (entries.TryGetValue(_scratchFinal[i], out var ui))
                    ui.transform.SetSiblingIndex(i);
            }
        }

        private bool IsTaskUnlocked(TaskData task)
        {
            return TaskWeightService.IsTaskUnlocked(task);
        }
    }
}
