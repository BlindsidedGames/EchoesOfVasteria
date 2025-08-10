using System.Collections;
using TimelessEchoes.Hero;
using UnityEngine;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Tasks
{
    public class OpenChestTask : ResourceGeneratingTask
    {
        [SerializeField] private Animator chestAnimator;
        [SerializeField] private Transform openPoint;
        [SerializeField] private float openDuration = 0.5f; // Duration of the chest's opening animation
        private bool isWaiting;

        private bool opened;

        public override Transform Target => openPoint != null ? openPoint : transform;

        // The hero should not move while the chest is opening.
        public override bool BlocksMovement => isWaiting;

        public override bool IsComplete()
        {
            return opened;
        }

        public override void StartTask()
        {
            // Do not reset 'opened' so completed chests remain complete if re-selected
            isWaiting = false;
        }

        public override void OnArrival(HeroController hero)
        {
            if (opened || isWaiting) return;

            if (ShouldInstantComplete())
            {
                if (chestAnimator != null)
                    AnimatorUtils.SetTriggerAndReset(this, chestAnimator, "Open");
                GenerateDrops();
                GrantCompletionXP();
                opened = true;
                isWaiting = false;
                NotifyCompleted();
                return;
            }

            // Trigger the chest's own animation (e.g., lid opening)
            if (chestAnimator != null)
                AnimatorUtils.SetTriggerAndReset(this, chestAnimator, "Open");

            // Start the delay coroutine
            hero.StartCoroutine(DelayedLoot());
        }

        private IEnumerator DelayedLoot()
        {
            isWaiting = true;
            yield return new WaitForSeconds(openDuration);

            GenerateDrops();
            GrantCompletionXP();
            opened = true;
            isWaiting = false;
            NotifyCompleted();
        }
    }
}