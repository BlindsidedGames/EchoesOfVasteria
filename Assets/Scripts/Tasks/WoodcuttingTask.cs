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

        private Sprite originalSprite;

        private bool spawnedStump;

        protected override string AnimationName => "Chopping";
        protected override string InterruptTriggerName => "StopChopping";

        public override Transform Target => transform;

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

        public override void Tick(TimelessEchoes.Hero.HeroController hero)
        {
            base.Tick(hero);
            if (!spawnedStump && IsComplete())
            {
                spawnedStump = true;
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && stumpSprite != null)
                    spriteRenderer.sprite = stumpSprite;
            }
        }
    }
}
