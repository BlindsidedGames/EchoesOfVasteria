using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;

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
            var dropTotals = new Dictionary<Resource, double>();
            var dropOrder = new List<Resource>();

            foreach (var drop in taskData.resourceDrops)
            {
                if (drop.resource == null || Random.value > drop.dropChance) continue;
                if (drop.requiredQuest != null && !QuestCompleted(drop.requiredQuest.questId)) continue;
                if (worldX < drop.minX || worldX > drop.maxX) continue;

                var min = drop.dropRange.x;
                var max = drop.dropRange.y;
                if (max < min) max = min;

                var t = Random.value * Random.value; // Bias towards lower numbers
                var count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);

                if (count > 0)
                {
                    double final = count;
                    if (skillController)
                    {
                        int mult = skillController.GetEffectMultiplier(associatedSkill, TimelessEchoes.Skills.MilestoneType.DoubleResources);
                        float resourceMult = skillController.GetResourceGainMultiplier();
                        final = count * mult * resourceMult;
                    }

                    resourceManager.Add(drop.resource, final);
                    if (dropTotals.ContainsKey(drop.resource))
                        dropTotals[drop.resource] += final;
                    else
                    {
                        dropTotals[drop.resource] = final;
                        dropOrder.Add(drop.resource);
                    }
                }
            }

            if (dropTotals.Count > 0)
            {
                var parts = new List<string>();
                foreach (var res in dropOrder)
                    parts.Add($"{Blindsided.Utilities.TextStrings.ResourceIcon(res.resourceID)}{Mathf.FloorToInt((float)dropTotals[res])}");
                var lines = new List<string>();
                var line = string.Empty;
                for (var i = 0; i < parts.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(line);
                            line = string.Empty;
                        }
                        line = parts[i];
                    }
                    else
                    {
                        line += ", " + parts[i];
                    }
                }
                if (line.Length > 0)
                    lines.Add(line);

                if (Blindsided.SaveData.StaticReferences.ItemDropFloatingText)
                    FloatingText.Spawn(string.Join("\n", lines), transform.position + Vector3.up,
                        FloatingText.DefaultColor, 8f, null,
                        Blindsided.SaveData.StaticReferences.DropFloatingTextDuration);
            }
        }

    }
}