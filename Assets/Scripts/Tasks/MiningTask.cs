using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;

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

        private ResourceManager resourceManager;
        private bool complete;

        public float MineTime => mineTime;
        public IList<ResourceDrop> Drops => resourceDrops;
        public SlicedFilledImage ProgressBar => progressBar;

        public Transform Target
        {
            get
            {
                if (cachedTarget != null)
                    return cachedTarget;

                var hero = FindFirstObjectByType<Hero.HeroController>();
                cachedTarget = hero == null
                    ? (leftPoint != null ? leftPoint : transform)
                    : GetNearestPoint(hero.transform);
                return cachedTarget;
            }
        }

        private Transform GetNearestPoint(Transform tr)
        {
            Transform[] points = { leftPoint, rightPoint, upPoint, downPoint };
            var best = transform;
            float bestDist = float.PositiveInfinity;
            foreach (var p in points)
            {
                if (p == null) continue;
                float d = Vector2.Distance(tr.position, p.position);
                if (d < bestDist)
                {
                    best = p;
                    bestDist = d;
                }
            }
            return best;
        }

        public void StartTask()
        {
            complete = false;
            if (cachedTarget == null)
                _ = Target;
            if (progressBar != null)
            {
                progressBar.fillAmount = 1f;
                progressBar.gameObject.SetActive(false);
            }
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
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
            {
                foreach (var drop in resourceDrops)
                {
                    if (drop.resource == null) continue;
                    if (Random.value > drop.dropChance) continue;

                    int min = drop.dropRange.x;
                    int max = drop.dropRange.y;
                    if (max < min) max = min;
                    float t = Random.value;
                    t *= t;
                    int count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
                    if (count > 0)
                    {
                        resourceManager.Add(drop.resource, count);
                        TimelessEchoes.FloatingText.Spawn($"{drop.resource.name} x{count}", transform.position + Vector3.up, Color.yellow);
                    }
                }
            }
            Destroy(gameObject);
        }

        public bool IsComplete()
        {
            return complete;
        }
    }
}
