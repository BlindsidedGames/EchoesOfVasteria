using System.Collections.Generic;
using Blindsided.Utilities;
using UnityEngine;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for mining a rock and dropping a resource when complete.
    /// </summary>
    public class MiningTask : MonoBehaviour, ITask
    {
        [SerializeField] private Transform rock;
        [SerializeField] private float timeToMine = 2f;
        [SerializeField] private SlicedFilledImage progressBar;
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;
        [SerializeField] private Transform upPoint;
        [SerializeField] private Transform downPoint;
        [SerializeField] private List<GameObject> possibleDrops = new();

        private Transform targetPoint;
        private HeroController hero;
        private float timer;
        private bool mining;
        private bool complete;

        public Transform Target => targetPoint;
        public Transform Rock => rock;
        public bool InProgress => mining && !complete;

        public void StartTask()
        {
            hero = Object.FindFirstObjectByType<HeroController>();

            targetPoint = PickNearestPoint();
            if (targetPoint == null)
                targetPoint = rock;

            if (hero != null)
                hero.SetDestination(targetPoint);

            timer = 0f;
            mining = false;
            complete = false;
            if (progressBar != null)
                progressBar.fillAmount = 1f;
        }

        private Transform PickNearestPoint()
        {
            if (hero == null)
                return leftPoint ?? rightPoint ?? upPoint ?? downPoint;

            var points = new[] { leftPoint, rightPoint, upPoint, downPoint };
            var bestDist = float.MaxValue;
            Transform best = null;
            foreach (var p in points)
            {
                if (p == null) continue;
                var d = Vector2.Distance(hero.transform.position, p.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }
            return best;
        }

        private void Update()
        {
            if (complete)
                return;

            if (!mining)
            {
                if (hero == null || targetPoint == null)
                    return;

                if (Vector2.Distance(hero.transform.position, targetPoint.position) <= 0.1f)
                {
                    mining = true;
                    var anim = hero.GetComponent<Animator>();
                    if (anim != null)
                        anim.Play("Mining");
                }
            }
            else
            {
                timer += Time.deltaTime;
                if (progressBar != null)
                    progressBar.fillAmount = Mathf.Clamp01(1f - timer / timeToMine);
                if (timer >= timeToMine)
                {
                    CompleteMining();
                }
            }
        }

        private void CompleteMining()
        {
            if (complete)
                return;
            complete = true;
            mining = false;
            if (hero != null)
            {
                var anim = hero.GetComponent<Animator>();
                if (anim != null)
                    anim.SetTrigger("StopMining");
            }

            if (possibleDrops.Count > 0 && rock != null)
            {
                var drop = possibleDrops[Random.Range(0, possibleDrops.Count)];
                if (drop != null)
                    Instantiate(drop, rock.position, Quaternion.identity);
            }
        }

        public bool IsComplete()
        {
            return complete;
        }
    }
}
