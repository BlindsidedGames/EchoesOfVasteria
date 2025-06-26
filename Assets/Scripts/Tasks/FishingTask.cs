using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Task for fishing at a single location.
    ///     The hero handles movement and timing while this task tracks
    ///     completion status.
    /// </summary>
    public class FishingTask : BaseTask
    {
        [SerializeField] private float fishTime = 2f;
        [SerializeField] private SlicedFilledImage progressBar;
        [SerializeField] private Transform fishingPoint;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private bool complete;
        private ResourceManager resourceManager;

        public float FishTime => fishTime;
        public SlicedFilledImage ProgressBar => progressBar;
        public IList<ResourceDrop> Drops => resourceDrops;

        public override Transform Target => fishingPoint != null ? fishingPoint : transform;

        public override void StartTask()
        {
            complete = false;
            if (progressBar != null)
                progressBar.fillAmount = 1f;
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        public override bool IsComplete()
        {
            return complete;
        }

        public void CompleteTask()
        {
            if (complete) return;
            complete = true;
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager != null)
                foreach (var drop in resourceDrops)
                {
                    if (drop.resource == null) continue;
                    if (Random.value > drop.dropChance) continue;

                    var min = drop.dropRange.x;
                    var max = drop.dropRange.y;
                    if (max < min) max = min;
                    var t = Random.value;
                    t *= t;
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
