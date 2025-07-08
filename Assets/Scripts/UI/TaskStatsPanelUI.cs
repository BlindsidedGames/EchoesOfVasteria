using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using UnityEngine;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.UI
{
    public class TaskStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        private GameplayStatTracker statTracker;

        private readonly Dictionary<TaskData, TaskStatEntryUIReferences> entries = new();
        private List<TaskData> defaultOrder = new();

        public enum SortMode
        {
            Default,
            Completions,
            TaskTime
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
        }

        private void Update()
        {
            UpdateEntries();
            SortEntries();
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

            foreach (Transform child in references.taskEntryParent)
                Destroy(child.gameObject);

            var allTasks = Resources.LoadAll<TaskData>("Tasks");
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
                var comp = CalcUtils.FormatNumber(completed, true);
                var timeStr = CalcUtils.FormatTime(time);
                var xpStr = CalcUtils.FormatNumber(xp, true);
                ui.entryCompletionsTimeOnTaskExperienceText.text =
                    $"Completions: {comp}\nTime on Task: {timeStr}\nXP Gained: {xpStr}";
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0)
                return;

            if (sortMode == SortMode.Default)
            {
                ApplyOrder(defaultOrder);
                return;
            }

            IOrderedEnumerable<TaskData> ordered;
            if (sortMode == SortMode.Completions)
                ordered = entries.Keys.OrderByDescending(t => statTracker?.GetTaskRecord(t)?.TotalCompleted ?? 0);
            else
                ordered = entries.Keys.OrderByDescending(t => statTracker?.GetTaskRecord(t)?.TimeSpent ?? 0f);

            ApplyOrder(ordered.ToList());
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