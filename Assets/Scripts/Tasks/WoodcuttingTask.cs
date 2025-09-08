using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Task for chopping down a tree. Spawns a stump when completed and
    ///     does not destroy the original tree GameObject.
    /// </summary>
    public class WoodcuttingTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite stumpSprite;
        [Tooltip("Optional extra stump sprites chosen at random")] [SerializeField]
        private Sprite[] stumpSpriteOptions;

        [SerializeField] private Transform cuttingPoint;

        private Sprite originalSprite;

        private bool spawnedStump;

        protected override string AnimationName => "Chopping";
        protected override string InterruptTriggerName => "StopChopping";
        public override Transform Target => cuttingPoint != null ? cuttingPoint : transform;

        public override void StartTask()
        {
            base.StartTask();
            spawnedStump = false;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (originalSprite == null)
                    originalSprite = spriteRenderer.sprite;
                spriteRenderer.sprite = originalSprite;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Reset visual state when reused from pool
            spawnedStump = false;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && originalSprite != null)
                spriteRenderer.sprite = originalSprite;
            IsExhausted = false;
        }

        public override void Tick(HeroBase hero)
        {
            base.Tick(hero);
            if (!spawnedStump && IsComplete())
            {
                spawnedStump = true;
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    var s = ChooseStumpSprite();
                    if (s != null)
                        spriteRenderer.sprite = s;
                }
            }
        }

        protected override void OnTaskCompleted(HeroBase hero)
        {
            if (spawnedStump)
                return;
            spawnedStump = true;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var s = ChooseStumpSprite();
                if (s != null)
                    spriteRenderer.sprite = s;
            }
            IsExhausted = true;
        }

        private Sprite ChooseStumpSprite()
        {
            if (stumpSpriteOptions == null || stumpSpriteOptions.Length == 0)
                return stumpSprite;

            var count = stumpSpriteOptions.Length + (stumpSprite != null ? 1 : 0);
            var index = Random.Range(0, count);
            if (index == 0 && stumpSprite != null)
                return stumpSprite;
            var optionIndex = stumpSprite != null ? index - 1 : index;
            return stumpSpriteOptions[optionIndex];
        }
    }
}
