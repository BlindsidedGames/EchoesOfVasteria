using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for mining a rock and collecting resources. All logic is driven by
    /// the hero rather than this component.
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

        private ResourceManager resourceManager;
        private bool complete;

        public float MineTime => mineTime;
        public SlicedFilledImage ProgressBar => progressBar;

        public Transform Target
        {
            get
            {
                var hero = FindFirstObjectByType<Hero.HeroController>();
                if (hero == null)
                    return leftPoint != null ? leftPoint : transform;
                return GetNearestPoint(hero.transform);
            }
        }

        private Transform GetNearestPoint(Transform tr)
        {
            Transform[] points = { leftPoint, rightPoint, upPoint, downPoint };
            Transform best = transform;
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
            if (progressBar != null)
            {
                progressBar.fillAmount = 1f;
                progressBar.gameObject.SetActive(false);
            }
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        /// <summary>
        /// Called by the hero to perform mining over time.
        /// </summary>
        public IEnumerator MineCoroutine(Hero.HeroController hero)
        {
            if (hero == null) yield break;

            var ai = hero.GetComponent<AIPath>();
            var setter = hero.GetComponent<AIDestinationSetter>();
            var anim = hero.GetComponentInChildren<Animator>();

            if (ai != null)
                ai.canMove = false;
            if (setter != null)
                setter.target = transform;
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.fillAmount = 1f;
            }
            anim?.Play("Mining");

            float timer = 0f;
            while (timer < mineTime)
            {
                timer += Time.deltaTime;
                if (progressBar != null)
                    progressBar.fillAmount = Mathf.Clamp01((mineTime - timer) / mineTime);
                yield return null;
            }

            if (ai != null)
                ai.canMove = true;
            if (setter != null)
                setter.target = null;
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
            anim?.SetTrigger("StopMining");

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

            complete = true;
            Destroy(gameObject);
        }

        public bool IsComplete()
        {
            return complete;
        }
    }
}
