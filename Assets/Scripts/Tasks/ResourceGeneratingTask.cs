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
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private ResourceManager resourceManager;

        protected void GenerateDrops()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();

            if (resourceManager == null) return;

            foreach (var drop in resourceDrops)
            {
                if (drop.resource == null || Random.value > drop.dropChance) continue;

                var min = drop.dropRange.x;
                var max = drop.dropRange.y;
                if (max < min) max = min;

                var t = Random.value * Random.value; // Bias towards lower numbers
                var count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);

                if (count > 0)
                {
                    resourceManager.Add(drop.resource, count);
                    FloatingText.Spawn($"{drop.resource.name} x{count}",
                        transform.position + Vector3.up,
                        Color.blue);
                }
            }
        }
    }
}