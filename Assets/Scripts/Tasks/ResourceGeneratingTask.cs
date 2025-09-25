using TimelessEchoes.Skills;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Buffs;
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

        protected void GenerateDrops(float xpAwarded = 0f)
        {
            // Builds a weighted list of eligible drops. The first slot is always rolled,
            // then additionalLootChances are processed sequentially for extra slots.
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }

            var skillController = SkillController.Instance;
            if (skillController == null)
                TELogger.Log("SkillController missing", TELogCategory.Resource, this);

            if (resourceManager == null || taskData == null)
                return;

            var worldX = transform.position.x;
            var dropTotals = new Dictionary<Resource, double>();
            var dropOrder = new List<Resource>();

            var results = DropResolver.RollDrops(taskData.resourceDrops, taskData.additionalLootChances, worldX);
            // Batch ResourceManager notifications to coalesce UI refreshes into a single update
            resourceManager.BeginBatch();
            try
            {
                foreach (var res in results)
                {
                    double final = res.count;
                    if (skillController)
                    {
                        int mult = skillController.GetStackingMultiplier(associatedSkill, MilestoneProcType.DoubleResources);
                        float resourceMult = skillController.GetResourceGainMultiplier();
                        final = res.count * mult * resourceMult;
                    }

                    var buff = BuffManager.Instance;
                    if (buff != null)
                        final *= buff.ResourceGainMultiplier;

                    resourceManager.Add(res.resource, final);
                    if (dropTotals.ContainsKey(res.resource))
                        dropTotals[res.resource] += final;
                    else
                    {
                        dropTotals[res.resource] = final;
                        dropOrder.Add(res.resource);
                    }
                }
            }
            finally
            {
                resourceManager.EndBatch();
            }

            if (dropTotals.Count == 0)
                return;

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
            const float floatingTextFontSize = 8f;

            if (lines.Count > 0 && xpAwarded > 0f && associatedSkill != null)
            {
                var xpLine = SkillIconLookup.FormatXpLine(associatedSkill, xpAwarded, floatingTextFontSize);
                if (!string.IsNullOrEmpty(xpLine))
                {
                    int lastIndex = lines.Count - 1;
                    lines[lastIndex] = $"{lines[lastIndex]} {xpLine}";
                }
            }

            if (lines.Count > 0 && StaticReferences.ItemDropFloatingText)
            {
                FloatingText.SpawnResourceText(string.Join("\n", lines), transform.position + Vector3.up,
                    FloatingText.DefaultColor, floatingTextFontSize, null,
                    StaticReferences.DropFloatingTextDuration);
            }
        }
    }
}
