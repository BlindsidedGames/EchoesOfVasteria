using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;

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
            // Builds a weighted list of eligible drops. The first slot is always rolled,
            // then additionalLootChances are processed sequentially for extra slots.
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

            var results = DropResolver.RollDrops(taskData.resourceDrops, taskData.additionalLootChances, worldX);
            foreach (var res in results)
            {
                double final = res.count;
                if (skillController)
                {
                    int mult = skillController.GetEffectMultiplier(associatedSkill, TimelessEchoes.Skills.MilestoneType.DoubleResources);
                    float resourceMult = skillController.GetResourceGainMultiplier();
                    final = res.count * mult * resourceMult;
                }

                resourceManager.Add(res.resource, final);
                if (dropTotals.ContainsKey(res.resource))
                    dropTotals[res.resource] += final;
                else
                {
                    dropTotals[res.resource] = final;
                    dropOrder.Add(res.resource);
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
                    FloatingText.SpawnResourceText(string.Join("\n", lines), transform.position + Vector3.up,
                        FloatingText.DefaultColor, 8f, null,
                        Blindsided.SaveData.StaticReferences.DropFloatingTextDuration);
            }
        }

    }
}