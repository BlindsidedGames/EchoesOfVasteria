using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using UnityEngine;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    public class TaskStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        private GameplayStatTracker statTracker;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        private readonly Dictionary<TaskData, TaskStatEntryUIReferences> entries = new();
        private readonly Dictionary<TaskData, (int completed, float time, float xp)> lastDisplayedByTask = new();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(128);
        private List<TaskData> defaultOrder = new();

        public enum SortMode
        {
            Default,
            Completions,
            TaskTime,
            Unknown
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

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
        }

        private void RefreshTick()
        {
            if (!IsPanelVisible()) return;
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

            foreach (var data in sorted)
            {
                var obj = Instantiate(references.taskEntryPrefab.gameObject, references.taskEntryParent);
                var ui = obj.GetComponent<TaskStatEntryUIReferences>();
                if (ui == null) continue;
                entries[data] = ui;
            }

            SortEntries();
        }

        private void UpdateEntries()
        {
            foreach (var pair in entries)
                UpdateEntry(pair.Key, pair.Value);
        }

        private void UpdateEntry(TaskData data, TaskStatEntryUIReferences ui)
        {
            if (data == null || ui == null) return;

            var record = statTracker ? statTracker.GetTaskRecord(data) : null;
            var completed = record?.TotalCompleted ?? 0;
            var time = record?.TimeSpent ?? 0f;
            var xp = record?.XpGained ?? 0f;

            // Early-out if values did not change (prevents string building & TMP updates)
            if (lastDisplayedByTask.TryGetValue(data, out var last))
            {
                if (last.completed == completed && Mathf.Approximately(last.time, time) && Mathf.Approximately(last.xp, xp))
                {
                    // Still update icon/name only if earned state changed; otherwise skip entirely
                    // Earned state changes only when completed crosses zero which would change 'completed'
                    return;
                }
            }
            lastDisplayedByTask[data] = (completed, time, xp);

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
                if (completed > 0)
                {
                    var minStr = CalcUtils.FormatNumber(data.minX, true);
                    var maxStr = CalcUtils.FormatNumber(data.maxX, true);
                    ui.entrySpawnDistanceText.text =
                        $"Minimum Spawn Distance: {minStr}\nMaximum Spawn Distance: {maxStr}";
                }
                else
                {
                    ui.entrySpawnDistanceText.text =
                        "Minimum Spawn Distance: ???\nMaximum Spawn Distance: ???";
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