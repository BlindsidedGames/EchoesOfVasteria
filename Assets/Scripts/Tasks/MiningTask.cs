using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Task for mining ore. On completion the ore sprite is replaced with
    ///     a depleted rock sprite and the object remains in the scene.
    /// </summary>
    public class MiningTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite depletedSprite;
        [SerializeField] private Transform miningPoint;

        private Sprite originalSprite;
        private bool isDepleted;

        protected override string AnimationName => "Mining";
        protected override string InterruptTriggerName => "StopMining";

        public override Transform Target => miningPoint != null ? miningPoint : transform;

        public override void StartTask()
        {
            base.StartTask();
            isDepleted = false;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (originalSprite == null)
                    originalSprite = spriteRenderer.sprite;
                spriteRenderer.sprite = originalSprite;
            }
        }

        public override void Tick(HeroController hero)
        {
            base.Tick(hero);
            if (!isDepleted && IsComplete())
            {
                isDepleted = true;
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && depletedSprite != null)
                    spriteRenderer.sprite = depletedSprite;
            }
        }
    }
}
