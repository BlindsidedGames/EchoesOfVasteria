using System.Reflection;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for watering crops. The sprite swaps through growth stages
    /// and is disabled once complete, leaving the object intact.
    /// </summary>
    public class FarmingTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] growthStages = new Sprite[3];
        [SerializeField] private Transform wateringPoint;

        private float localTimer;
        private float duration;
        private int currentStage;

        protected override string AnimationName => "Water";
        protected override string InterruptTriggerName => "StopWatering";

        public override Transform Target => wateringPoint != null ? wateringPoint : transform;

        public override void StartTask()
        {
            base.StartTask();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            localTimer = 0f;
            currentStage = 0;
            duration = TaskDuration;
            if (spriteRenderer != null && spriteRenderer.enabled == false)
                spriteRenderer.enabled = true;
        }

        public override void Tick(HeroController hero)
        {
            base.Tick(hero);
            localTimer += Time.deltaTime;

            if (spriteRenderer != null && growthStages.Length >= 3)
            {
                float quarter = duration > 0f ? duration / 4f : 0f;
                int newStage = 0;
                if (localTimer >= 3f * quarter)
                    newStage = 3;
                else if (localTimer >= 2f * quarter)
                    newStage = 2;
                else if (localTimer >= quarter)
                    newStage = 1;

                if (newStage != currentStage)
                {
                    currentStage = newStage;
                    if (newStage > 0 && newStage - 1 < growthStages.Length)
                    {
                        var s = growthStages[newStage - 1];
                        if (s != null)
                            spriteRenderer.sprite = s;
                    }
                }
            }

            if (IsComplete() && spriteRenderer != null && spriteRenderer.enabled)
            {
                spriteRenderer.enabled = false;
            }
        }

        // TaskDuration property from ContinuousTask provides the duration
    }
}
