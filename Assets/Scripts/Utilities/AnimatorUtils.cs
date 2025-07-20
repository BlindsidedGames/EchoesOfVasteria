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

            if (runner is TimelessEchoes.Hero.HeroController hero &&
                hero.AutoBuffAnimator != null && hero.AutoBuffAnimator.isActiveAndEnabled)
            {
                var autoAnim = hero.AutoBuffAnimator;
                autoAnim.SetTrigger(triggerName);
                runner.StartCoroutine(ResetNextFrame(autoAnim, triggerName));
            }
        }

        private static IEnumerator ResetNextFrame(Animator animator, string triggerName)
        {
            yield return null;
            animator.ResetTrigger(triggerName);
        }
    }
}
