using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TMPro;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;
using TimelessEchoes.MapGeneration;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    public class TaskStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        [Header("Distance Preview")]
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private TMP_Text distanceText;
        private GameplayStatTracker statTracker;
        private EnemyStatsPanelUI enemyPanel;
        private float lastKnownMaxDistance;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

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

        private void OnEnable()
        {
            TaskWeightService.ToggleChanged += OnTaskWeightToggleChanged;
            if (statTracker == null)
                statTracker = GameplayStatTracker.Instance;
            if (statTracker != null)
                statTracker.OnMaxRunDistanceChanged += OnMaxRunDistanceChanged;
            SetupDistanceSlider();
            UpdateEntries();
            SortEntries();
            nextUpdateTime = Time.unscaledTime + updateInterval;
            UITicker.Instance?.Subscribe(RefreshTick, updateInterval);
        }

        private void Update()
        {
            if (UITicker.Instance != null) return;
            if (Time.unscaledTime >= nextUpdateTime)
            {
                UpdateEntries();
                SortEntries();
                nextUpdateTime = Time.unscaledTime + updateInterval;
            }
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
            if (distanceSlider != null)
                distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
            if (statTracker != null)
                statTracker.OnMaxRunDistanceChanged -= OnMaxRunDistanceChanged;
            TaskWeightService.ToggleChanged -= OnTaskWeightToggleChanged;
        }

        private void RefreshTick()
        {
            if (!IsPanelVisible()) return;
            UpdateEntries();
            SortEntries();
        }

        private void EnsureDistanceControls()
        {
            if (distanceSlider != null && distanceText != null)
                return;

            if (enemyPanel == null)
                enemyPanel = FindFirstObjectByType<EnemyStatsPanelUI>(FindObjectsInactive.Include);

            if (enemyPanel != null)
            {
                if (distanceSlider == null)
                    distanceSlider = enemyPanel.DistanceSlider;
                if (distanceText == null)
                    distanceText = enemyPanel.DistanceText;
            }
        }

        private void SetupDistanceSlider()
        {
            EnsureDistanceControls();
            if (distanceSlider == null)
                return;

            var tracker = GameplayStatTracker.Instance;
            lastKnownMaxDistance = tracker != null ? tracker.MaxRunDistance : 0f;
            if (distanceSlider != null)
            {
                distanceSlider.minValue = 0f;
                distanceSlider.maxValue = lastKnownMaxDistance;
                distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
                distanceSlider.onValueChanged.AddListener(OnDistanceSliderChanged);
            }
            UpdateDistanceLabel();
        }

        private void OnMaxRunDistanceChanged(float newMax)
        {
            if (distanceSlider != null)
            {
                bool wasAtMax = Mathf.Approximately(distanceSlider.value, lastKnownMaxDistance);
                distanceSlider.maxValue = newMax;
                if (wasAtMax || distanceSlider.value > newMax)
                    distanceSlider.SetValueWithoutNotify(newMax);
            }
            lastKnownMaxDistance = newMax;
            UpdateDistanceLabel();
            UpdateEntries();
        }

        private void OnDistanceSliderChanged(float _)
        {
            UpdateDistanceLabel();
            cachedWeightsWorldX = float.NaN;
            UpdateEntries();
        }

        private void UpdateDistanceLabel()
        {
            if (distanceText == null)
                return;
            var distance = GetPreviewDistance();
            distanceText.text = $"Distance: {CalcUtils.FormatNumber(distance, true)}";
        }

        private float GetPreviewDistance()
        {
            if (distanceSlider != null)
                return Mathf.Clamp(distanceSlider.value, distanceSlider.minValue, distanceSlider.maxValue);
            var tracker = GameplayStatTracker.Instance;
            return tracker != null ? tracker.MaxRunDistance : 0f;
        }

        private void OnTaskWeightToggleChanged(TaskData task, bool _)
        {
            cachedWeightsWorldX = float.NaN;
            if (!isActiveAndEnabled)
                return;
            UpdateEntries();
            SortEntries();
        }

        private bool IsPanelVisible()
        {
            if (references != null && references.taskEntryParent != null)
                return references.taskEntryParent.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        public void SetSortMode(SortMode mode)
        {
            sortMode = mode;
            SortEntries();
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

            SortEntries();
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
            var skill = data.associatedSkill;
            if (!tasksBySkill.TryGetValue(skill, out var list) || list == null || list.Count == 0)
                return false;

            float total = 0f;
            for (int i = 0; i < list.Count; i++)
                total += TaskWeightService.GetEffectiveWeight(list[i], worldX);

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

            if (lastDisplayedByTask.TryGetValue(data, out var cached) &&
                cached.Matches(completed, time, xp, spawnChance, effectiveWeight, nextImprovement, toggleEnabled))
                return;

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
                    string minStr = CalcUtils.FormatNumber(data.minX, true);
                    string maxStr = float.IsInfinity(data.maxX)
                        ? "Infinity"
                        : CalcUtils.FormatNumber(data.maxX, true);
                    ui.entrySpawnDistanceText.text =
                        $"Spawn chance: {chancePercent}% ({weightText})\n{nextLine}\nMin Distance: {minStr}, Max Distance: {maxStr}";
                }
                else
                {
                    string minStr = CalcUtils.FormatNumber(data.minX, true);
                    string maxStr = float.IsInfinity(data.maxX)
                        ? "Infinity"
                        : CalcUtils.FormatNumber(data.maxX, true);
                    ui.entrySpawnDistanceText.text =
                        $"Spawn chance: -- ({weightText})\nNext improvement: --\nMin Distance: {minStr}, Max Distance: {maxStr}";
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

            IEnumerable<TaskData> known = defaultOrder;
            IEnumerable<TaskData> unknown = Enumerable.Empty<TaskData>();
            if (statTracker != null)
            {
                known = defaultOrder.Where(t => (statTracker.GetTaskRecord(t)?.TotalCompleted ?? 0) > 0);
                unknown = defaultOrder.Where(t => (statTracker.GetTaskRecord(t)?.TotalCompleted ?? 0) == 0);
            }

            if (sortMode == SortMode.Default)
            {
                var sortedKnownDefault = known
                    .OrderBy(t => t.taskID)
                    .ThenBy(t => t.taskName)
                    .ToList();
                var sortedUnknownDefault = unknown
                    .OrderBy(t => t.taskID)
                    .ThenBy(t => t.taskName)
                    .ToList();
                var finalDefault = sortedKnownDefault.Concat(sortedUnknownDefault).ToList();
                ApplyOrder(finalDefault);
                return;
            }

            if (sortMode == SortMode.Unknown)
            {
                var sortedUnknownUnknown = unknown
                    .OrderBy(t => t.taskID)
                    .ThenBy(t => t.taskName)
                    .ToList();
                var sortedKnownUnknown = known
                    .OrderBy(t => t.taskID)
                    .ThenBy(t => t.taskName)
                    .ToList();
                var finalUnknown = sortedUnknownUnknown.Concat(sortedKnownUnknown).ToList();
                ApplyOrder(finalUnknown);
                return;
            }

            float GetValue(TaskData t) => sortMode == SortMode.Completions
                ? statTracker?.GetTaskRecord(t)?.TotalCompleted ?? 0
                : statTracker?.GetTaskRecord(t)?.TimeSpent ?? 0f;

            var sortedKnownByValue = known
                .OrderByDescending(GetValue)
                .ThenBy(t => t.taskID)
                .ThenBy(t => t.taskName)
                .ToList();

            var finalOrder = sortedKnownByValue.Concat(unknown).ToList();
            ApplyOrder(finalOrder);
        }

        private void ApplyOrder(IList<TaskData> order)
        {
            var index = 0;
            foreach (var data in order)
                if (entries.TryGetValue(data, out var ui))
                    ui.transform.SetSiblingIndex(index++);
        }
    }
}
