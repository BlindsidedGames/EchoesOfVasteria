using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     A base class for tasks that generate resources upon completion.
    /// </summary>
    public abstract class ResourceGeneratingTask : BaseTask
    {
        // Resource drops are now configured via TaskData

        private ResourceManager resourceManager;

        protected void GenerateDrops()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();

            var skillController = FindFirstObjectByType<TimelessEchoes.Skills.SkillController>();

            if (resourceManager == null) return;

            if (taskData == null)
                return;

            foreach (var drop in taskData.resourceDrops)
            {
                if (drop.resource == null || Random.value > drop.dropChance) continue;

                var min = drop.dropRange.x;
                var max = drop.dropRange.y;
                if (max < min) max = min;

                var t = Random.value * Random.value; // Bias towards lower numbers
                var count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);

                if (count > 0)
                {
                    if (skillController)
                    {
                        int mult = skillController.GetEffectMultiplier(associatedSkill, TimelessEchoes.Skills.MilestoneType.DoubleResources);
                        count *= mult;
                    }
                    resourceManager.Add(drop.resource, count);
                }
            }
        }
    }
}