using System.Reflection;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Buffs;
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
        [Tooltip("Optional overlay sprite used to show watered soil")]
        [SerializeField] private SpriteRenderer wetSpriteRenderer;
        [Tooltip("Growth sprites with index 0 as the starting sprite, and 1/2/3 as the growth stages.")]
        [SerializeField] private Sprite[] growthStages = new Sprite[4];
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

        protected override void OnEnable()
        {
            base.OnEnable();
            // Reset for reuse from pool
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            if (wetSpriteRenderer != null)
                wetSpriteRenderer.enabled = false;
            // Set starting sprite (stage 0)
            if (spriteRenderer != null && growthStages != null && growthStages.Length > 0)
            {
                var startSprite = growthStages[0];
                if (startSprite != null)
                    spriteRenderer.sprite = startSprite;
            }
            currentStage = 0;
            IsExhausted = false;
        }

        public override void OnArrival(HeroBase hero)
        {
            // Ensure base handles instant-complete first; if it does, it will early-return
            base.OnArrival(hero);
            if (IsComplete())
                return;
            if (wetSpriteRenderer != null)
                wetSpriteRenderer.enabled = true;
        }

        public override void Tick(HeroBase hero)
        {
            base.Tick(hero);
            var delta = Time.deltaTime;
            var controller = SkillController.Instance ?? FindFirstObjectByType<SkillController>();
            if (controller != null && associatedSkill != null)
                delta *= controller.GetTaskSpeedMultiplier(associatedSkill);

            var buffManager = BuffManager.Instance ?? FindFirstObjectByType<BuffManager>();
            if (buffManager != null)
                delta *= buffManager.TaskSpeedMultiplier;
            localTimer += delta;

            if (spriteRenderer != null && growthStages != null && growthStages.Length >= 4)
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
                    // Map stages 1/2/3 directly to indices 1/2/3 (index 0 is the starting sprite)
                    if (newStage > 0 && newStage < growthStages.Length)
                    {
                        var s = growthStages[newStage];
                        if (s != null)
                            spriteRenderer.sprite = s;
                    }
                }
            }

            if (IsComplete() && spriteRenderer != null && spriteRenderer.enabled)
            {
                spriteRenderer.enabled = false;
                if (wetSpriteRenderer != null)
                    wetSpriteRenderer.enabled = false;
            }
        }

        public override void OnInterrupt(HeroBase hero)
        {
            base.OnInterrupt(hero);
            if (wetSpriteRenderer != null)
                wetSpriteRenderer.enabled = false;
        }

        protected override void OnTaskCompleted(HeroBase hero)
        {
            // Mirror the visual cleanup that happens in Tick() when complete
            if (spriteRenderer != null && spriteRenderer.enabled)
                spriteRenderer.enabled = false;
            if (wetSpriteRenderer != null)
                wetSpriteRenderer.enabled = false;
            IsExhausted = true;
        }

        // TaskDuration property from ContinuousTask provides the duration
    }
}
