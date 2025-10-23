using System;
using Blindsided.SaveData;
using TimelessEchoes.Stats;
using UnityEngine;
using static Blindsided.Oracle;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Centralised helper for task spawn weights that applies persistent toggles
    ///     and completion-based tier bonuses.
    /// </summary>
    public static class TaskWeightService
    {
        private const float ToggleMultiplier = 2f;
        private const float OutOfRangeMultiplier = 0.1f;
        private const float TierBonusPerThreshold = 0.1f;
        private const string ToggleKeyPrefix = "TaskWeightToggle_";

        private static readonly int[] CompletionThresholds = { 10, 100, 1_000, 10_000, 100_000 };

        public static event Action<TaskData, bool> ToggleChanged;

        public static float GetEffectiveWeight(TaskData task, float worldX)
        {
            if (task == null)
                return 0f;

            if (worldX < task.minX)
                return 0f;

            var baseWeight = Mathf.Max(0f, task.weight);
            if (baseWeight <= 0f)
                return 0f;

            var multiplier = GetProgressMultiplier(task);
            var weight = baseWeight * multiplier;

            if (worldX > task.maxX)
                weight *= OutOfRangeMultiplier;

            return weight;
        }

        public static bool IsToggleEnabled(TaskData task)
        {
            if (task == null) return false;
            return PlayerPrefs.GetInt(GetToggleKey(task), 0) == 1;
        }

        public static void SetToggle(TaskData task, bool enabled)
        {
            if (task == null) return;
            var key = GetToggleKey(task);
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ToggleChanged?.Invoke(task, enabled);
        }

        public static int GetTotalCompletions(TaskData task)
        {
            if (task == null) return 0;

            var tracker = GameplayStatTracker.Instance;
            var record = tracker != null ? tracker.GetTaskRecord(task) : null;
            if (record != null)
                return Mathf.Max(0, record.TotalCompleted);

            if (oracle?.saveData?.TaskRecords != null
                && oracle.saveData.TaskRecords.TryGetValue(task.taskID, out var savedRecord)
                && savedRecord != null)
                return Mathf.Max(0, savedRecord.TotalCompleted);

            return 0;
        }

        public static int GetCompletedTierCount(TaskData task)
        {
            var total = GetTotalCompletions(task);
            var count = 0;
            for (var i = 0; i < CompletionThresholds.Length; i++)
            {
                if (total >= CompletionThresholds[i])
                    count++;
                else
                    break;
            }
            return count;
        }

        public static bool TryGetNextThreshold(TaskData task, out int remaining)
        {
            var total = GetTotalCompletions(task);
            foreach (var threshold in CompletionThresholds)
            {
                if (total < threshold)
                {
                    remaining = threshold - total;
                    return true;
                }
            }

            remaining = 0;
            return false;
        }

        public static float GetProgressMultiplier(TaskData task)
        {
            var multiplier = 1f + TierBonusPerThreshold * GetCompletedTierCount(task);
            if (IsToggleEnabled(task))
                multiplier *= ToggleMultiplier;
            return multiplier;
        }

        private static string GetToggleKey(TaskData task)
        {
            return $"{ToggleKeyPrefix}{task.taskID}";
        }
    }
}
