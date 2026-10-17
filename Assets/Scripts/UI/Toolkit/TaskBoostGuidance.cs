using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Tasks;
using TimelessEchoes.Stats;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    public static class TaskBoostGuidance
    {
        // Town statistics compares tasks within each skill. Wait for an actual choice,
        // and point at a discovered task so its Boost control is available.
        public static TaskData FindTask()
        {
            var tracker = GameplayStatTracker.Instance;
            if (!tracker) return null;
            var distance = tracker.MaxRunDistance > 0 ? tracker.MaxRunDistance : 50;
            return AssetCache.GetAll<TaskData>("Tasks")
                .Where(t => t && t.associatedSkill && TaskWeightService.GetEffectiveWeight(t, distance) > 0)
                .GroupBy(t => t.associatedSkill).Where(g => g.Count() > 1)
                .SelectMany(g => g).Where(t => TaskWeightService.GetTotalCompletions(t) > 0)
                .OrderBy(t => t.taskID).FirstOrDefault();
        }
    }
}
