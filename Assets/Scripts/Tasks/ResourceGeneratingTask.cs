using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static TimelessEchoes.TELogger;

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
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }

            var skillController = TimelessEchoes.Skills.SkillController.Instance;
            if (skillController == null)
                TELogger.Log("SkillController missing", TELogCategory.Resource, this);

            if (resourceManager == null) return;

            if (taskData == null)
                return;

            var worldX = transform.position.x;

            foreach (var drop in taskData.resourceDrops)
            {
                if (drop.resource == null || Random.value > drop.dropChance) continue;
                if (worldX < drop.minX || worldX > drop.maxX) continue;

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
                        float resourceMult = skillController.GetResourceGainMultiplier();
                        double amount = count * mult * resourceMult;
                        resourceManager.Add(drop.resource, amount);
                    }
                    else
                    {
                        resourceManager.Add(drop.resource, count);
                    }
                }
            }
        }
    }}