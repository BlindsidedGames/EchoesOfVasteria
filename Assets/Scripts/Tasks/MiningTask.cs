using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Hero;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Data container for a mining objective. The hero now handles
    ///     movement and timing logic when assigned this task.
    /// </summary>
    public class MiningTask : MonoBehaviour, ITask
    {
        [SerializeField] private float mineTime = 2f;
        [SerializeField] private SlicedFilledImage progressBar;
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;
        [SerializeField] private Transform upPoint;
        [SerializeField] private Transform downPoint;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private Transform cachedTarget;
        private bool complete;

        private ResourceManager resourceManager;

        public float MineTime => mineTime;
        public IList<ResourceDrop> Drops => resourceDrops;
        public SlicedFilledImage ProgressBar => progressBar;

        public Transform Target
        {
            get
            {
                if (cachedTarget != null)
                    return cachedTarget;

                var hero = FindFirstObjectByType<HeroController>();
                cachedTarget = hero == null
                    ? leftPoint != null ? leftPoint : transform
                    : GetNearestPoint(hero.transform);
                return cachedTarget;
            }
        }

        public void StartTask()
        {
            complete = false;
            if (cachedTarget == null)
                _ = Target;
            if (progressBar != null) progressBar.fillAmount = 1f;
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        public bool IsComplete()
        {
            return complete;
        }

        private Transform GetNearestPoint(Transform tr)
        {
            Transform[] points = { leftPoint, rightPoint, upPoint, downPoint };
            var best = transform;
            var bestDist = float.PositiveInfinity;
            foreach (var p in points)
            {
                if (p == null) continue;
                var d = Vector2.Distance(tr.position, p.position);
                if (d < bestDist)
                {
                    best = p;
                    bestDist = d;
                }
            }

            return best;
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
                        FloatingText.Spawn($"{drop.resource.name} x{count}", transform.position + Vector3.up,
                            Color.yellow);
                    }
                }

            Destroy(gameObject);
        }
    }
}