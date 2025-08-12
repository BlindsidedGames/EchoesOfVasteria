using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Chest looting implemented as a continuous task. The chest swaps through 5
    /// sprites only during the final 0.5 seconds of the task duration, ending on
    /// the final (open) frame. No chest Animator is used.
    /// </summary>
    public class OpenChestTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Sprites for chest opening from closed to fully open (5 frames)")]
        [SerializeField] private Sprite[] openStages = new Sprite[5];
        [SerializeField] private Transform openPoint;
        [Tooltip("Time window at the end of the task during which opening sprites play")]
        [SerializeField] private float playbackWindowSeconds = 0.5f;
        [Tooltip("Reference to the hero's audio component. If not set, it will be fetched from the arriving hero.")]
        [SerializeField] private TimelessEchoes.Hero.HeroAudio heroAudio;

        private float localTimer;
        private float duration;
        private int currentFrameIndex = -1;
        private Sprite initialClosedSprite;
        private bool playedOpenSfx;

        // Drive the hero's animation
        protected override string AnimationName => "Loot";
        protected override string InterruptTriggerName => "StopLooting";

        public override Transform Target => openPoint != null ? openPoint : transform;

        public override void StartTask()
        {
            base.StartTask();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            localTimer = 0f;
            duration = TaskDuration;
            currentFrameIndex = -1;
            playedOpenSfx = false;

            if (spriteRenderer != null)
            {
                if (initialClosedSprite == null)
                    initialClosedSprite = spriteRenderer.sprite;
                // Ensure we start closed visually
                spriteRenderer.sprite = initialClosedSprite;
            }
        }

        public override void OnArrival(HeroController hero)
        {
            // Base handles instant-complete path and hero animation/progress bar
            base.OnArrival(hero);
            if (heroAudio == null && hero != null)
                heroAudio = hero.GetComponent<TimelessEchoes.Hero.HeroAudio>();
        }

        public override void Tick(HeroController hero)
        {
            // Progress the base task (timer, progress bar, completion)
            base.Tick(hero);

            // Mirror the same time scaling used by base to keep visuals in sync
            var delta = Time.deltaTime;
            var controller = TimelessEchoes.Skills.SkillController.Instance ?? FindFirstObjectByType<TimelessEchoes.Skills.SkillController>();
            if (controller != null && associatedSkill != null)
                delta *= controller.GetTaskSpeedMultiplier(associatedSkill);

            var buffManager = TimelessEchoes.Buffs.BuffManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Buffs.BuffManager>();
            if (buffManager != null)
                delta *= buffManager.TaskSpeedMultiplier;

            localTimer += delta;

            if (spriteRenderer == null || openStages == null || openStages.Length == 0 || duration <= 0f)
                return;

            // Only animate during the final playbackWindowSeconds of the task
            var window = Mathf.Min(playbackWindowSeconds, duration);
            var windowStart = duration - window;

            if (localTimer < windowStart)
            {
                // Before the window: ensure closed sprite
                if (currentFrameIndex != -1 && initialClosedSprite != null)
                {
                    currentFrameIndex = -1;
                    spriteRenderer.sprite = initialClosedSprite;
                }
                return;
            }

            if (!playedOpenSfx)
            {
                if (heroAudio == null && hero != null)
                    heroAudio = hero.GetComponent<TimelessEchoes.Hero.HeroAudio>();
                heroAudio?.PlayChestOpen();
                playedOpenSfx = true;
            }

            var timeIntoWindow = Mathf.Clamp(localTimer - windowStart, 0f, window);
            var normalized = window > 0f ? Mathf.Clamp01(timeIntoWindow / window) : 1f;

            // Map normalized [0,1] to frame indices [0, stages-1]
            int stages = openStages.Length;
            int newFrame = Mathf.Clamp(Mathf.FloorToInt(normalized * stages), 0, stages - 1);

            if (newFrame != currentFrameIndex)
            {
                currentFrameIndex = newFrame;
                var s = openStages[currentFrameIndex];
                if (s != null)
                    spriteRenderer.sprite = s;
            }
        }

        public override void OnInterrupt(HeroController hero)
        {
            base.OnInterrupt(hero);
            // Revert visuals to closed if interrupted
            if (spriteRenderer != null && initialClosedSprite != null)
                spriteRenderer.sprite = initialClosedSprite;
        }

        protected override void OnTaskCompleted(HeroController hero)
        {
            // Ensure final open sprite persists on completion, including instant-complete
            if (spriteRenderer != null && openStages != null && openStages.Length > 0)
            {
                var last = openStages[openStages.Length - 1];
                if (last != null)
                    spriteRenderer.sprite = last;
            }
        }
    }
}