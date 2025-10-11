using System;
using System.Collections.Generic;
using System.Globalization;
using Sirenix.OdinInspector;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "TaskUnlockEffect", menuName = "SO/Milestones/Effects/Task Unlock")]
    public sealed class MilestoneTaskUnlockEffectDefinition : MilestoneEffectDefinition
    {
        [Serializable]
        private class TaskEntry
        {
            [SerializeField]
            [Tooltip("Task unlocked by this effect.")]
            private TaskData task;

            [SerializeField]
            [MinValue(0f)]
            [Tooltip("Multiplier applied to the milestone magnitude when determining the spawn weight.")]
            private float weightMultiplier = 1f;

            public TaskData Task => task;

            public float GetWeight(float magnitude)
            {
                if (task == null)
                    return 0f;

                return Mathf.Max(0f, magnitude * weightMultiplier);
            }
        }

        [SerializeField]
        [Tooltip("Tasks that become available when this milestone tier is reached.")]
        private List<TaskEntry> tasks = new();

        [SerializeField]
        [Tooltip("Passive description template. {0} => task names, {1} => formatted weight.")]
        private string passiveDescriptionTemplate = "Unlocks {0} tasks (weight {1}).";

        [SerializeField]
        [Tooltip("Active description template. {0} => task names, {1} => formatted weight.")]
        private string activeDescriptionTemplate = "Sets spawn weight for {0} tasks to {1} while active.";

        [SerializeField]
        [Tooltip("Numeric format used when displaying weights.")]
        private string weightFormat = "0.##";

        public IEnumerable<TaskData> EnumerateTasks()
        {
            if (tasks == null)
                yield break;

            foreach (var entry in tasks)
            {
                if (entry == null)
                    continue;

                var task = entry.Task;
                if (task == null)
                    continue;

                yield return task;
            }
        }

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            if (tasks == null || tasks.Count == 0)
                return;

            foreach (var entry in tasks)
            {
                if (entry == null)
                    continue;

                var task = entry.Task;
                if (task == null)
                    continue;

                var unlockSkill = task.UnlockSkill;
                if (unlockSkill != null && context.Controller != null)
                {
                    int level = context.Controller.GetLevel(unlockSkill);
                    if (level < task.UnlockSkillLevel)
                        continue;
                }

                float weight = entry.GetWeight(magnitude);
                if (weight <= 0f)
                    continue;

                context.Aggregator.RegisterTaskUnlock(task, weight, context.Source);
            }
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            using var enumerator = EnumerateTasks().GetEnumerator();
            if (!enumerator.MoveNext())
                return string.Empty;

            var taskNames = new List<string> { enumerator.Current.taskName };
            while (enumerator.MoveNext())
                taskNames.Add(enumerator.Current.taskName);

            string joined = string.Join(", ", taskNames);
            float weight = Mathf.Max(0f, magnitude);
            string formattedWeight = weight.ToString(weightFormat, CultureInfo.InvariantCulture);
            string template = isActive ? activeDescriptionTemplate : passiveDescriptionTemplate;

            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            return string.Format(template, joined, formattedWeight);
        }
    }
}
