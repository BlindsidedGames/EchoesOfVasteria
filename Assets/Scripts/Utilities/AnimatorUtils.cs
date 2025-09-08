using System.Collections;
using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Utility helpers for working with Animator triggers.
    /// </summary>
    public static class AnimatorUtils
    {
        /// <summary>
        /// Sets a trigger and resets it on the next frame to ensure it does not remain active.
        /// </summary>
        public static void SetTriggerAndReset(MonoBehaviour runner, Animator animator, string triggerName)
        {
            if (runner == null || animator == null || string.IsNullOrEmpty(triggerName))
                return;

            animator.SetTrigger(triggerName);
            runner.StartCoroutine(ResetNextFrame(animator, triggerName));

            if (runner is TimelessEchoes.Hero.HeroBase hb)
            {
                hb.SetSecondaryTrigger(triggerName);
                runner.StartCoroutine(ResetNextFrameSecondary(hb, triggerName));
            }
        }

        private static IEnumerator ResetNextFrame(Animator animator, string triggerName)
        {
            yield return null;
            animator.ResetTrigger(triggerName);
        }

        private static IEnumerator ResetNextFrameSecondary(TimelessEchoes.Hero.HeroBase hero, string triggerName)
        {
            yield return null;
            hero.ResetSecondaryTrigger(triggerName);
        }
    }
}
