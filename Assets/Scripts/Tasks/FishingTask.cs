using UnityEngine;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Tasks
{
    public class FishingTask : ContinuousTask
    {
        [SerializeField] private Transform fishingPoint;
        public override Transform Target => fishingPoint != null ? fishingPoint : transform;

        protected override string AnimationName => "Fishing";
        // Disable interrupt trigger usage for fishing; we'll force Idle instead on interrupt
        protected override string InterruptTriggerName => string.Empty;
        protected override string CompletionTriggerName => "CatchFish";

        public override void OnInterrupt(HeroController hero)
        {
            // Hide progress bar via base (no trigger will be set because InterruptTriggerName is empty)
            base.OnInterrupt(hero);

            if (hero != null)
            {
                if (hero.Animator != null)
                    hero.Animator.Play("Idle");

                if (hero.AutoBuffAnimator != null && hero.AutoBuffAnimator.isActiveAndEnabled)
                    hero.AutoBuffAnimator.Play("Idle");
            }
        }
    }
}