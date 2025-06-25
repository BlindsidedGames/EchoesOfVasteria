using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for mining a rock and collecting resources.
    /// </summary>
    public class MiningTask : MonoBehaviour, ITask
    {
        [System.Serializable]
        public class ResourceDrop
        {
            public Resource resource;
            public Vector2Int dropRange = new Vector2Int(1, 1);
            [Range(0f, 1f)] public float dropChance = 1f;
        }

        [SerializeField] private float mineTime = 2f;
        [SerializeField] private SlicedFilledImage progressBar;
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;
        [SerializeField] private Transform upPoint;
        [SerializeField] private Transform downPoint;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private ResourceManager resourceManager;
        private Hero.HeroController hero;
        private AIPath heroAI;
        private AIDestinationSetter heroSetter;
        private float timer;
        private bool mining;
        private bool complete;
        private Transform currentPoint;

        public Transform Target
        {
            get
            {
                if (hero == null)
                    hero = FindFirstObjectByType<Hero.HeroController>();
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
            mining = false;
            timer = 0f;
            if (progressBar != null)
            {
                progressBar.fillAmount = 1f;
                progressBar.gameObject.SetActive(false);
            }
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (hero == null)
                hero = FindFirstObjectByType<Hero.HeroController>();
            if (hero != null)
            {
                heroAI = hero.GetComponent<AIPath>();
                heroSetter = hero.GetComponent<AIDestinationSetter>();
                currentPoint = GetNearestPoint(hero.transform);
                hero.SetDestination(currentPoint);
            }
        }

        private void Update()
        {
            if (complete || hero == null)
                return;

            if (!mining)
            {
                if (currentPoint == null)
                    currentPoint = GetNearestPoint(hero.transform);
                float dist = Vector2.Distance(hero.transform.position, currentPoint.position);
                if (dist <= 0.1f)
                {
                    BeginMining();
                }
            }
            else
            {
                timer += Time.deltaTime;
                if (progressBar != null)
                    progressBar.fillAmount = Mathf.Clamp01(1f - timer / mineTime);
                if (timer >= mineTime)
                {
                    FinishMining();
                }
            }
        }

        private void BeginMining()
        {
            mining = true;
            if (heroAI != null)
                heroAI.canMove = false;
            if (heroSetter != null)
                heroSetter.target = transform;
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.fillAmount = 1f;
            }
            var anim = hero != null ? hero.GetComponentInChildren<Animator>() : null;
            anim?.Play("Mining");
        }

        private void FinishMining()
        {
            mining = false;
            complete = true;
            if (heroAI != null)
                heroAI.canMove = true;
            if (heroSetter != null)
                heroSetter.target = null;
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
            var anim = hero != null ? hero.GetComponentInChildren<Animator>() : null;
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
        }

        public bool IsComplete()
        {
            return complete;
        }
    }
}
