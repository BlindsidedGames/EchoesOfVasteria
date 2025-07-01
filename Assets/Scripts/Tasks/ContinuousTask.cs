using Blindsided.Utilities;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     A base class for tasks that require the hero to wait for a duration,
    ///     showing a progress bar.
    /// </summary>
    public abstract class ContinuousTask : ResourceGeneratingTask
    {
        [SerializeField] private float taskDuration = 2f;
        [SerializeField] private GameObject progressBarObject;
        [SerializeField] private SlicedFilledImage progressBar;
        private bool isComplete;

        private float timer;

        protected abstract string AnimationName { get; }
        protected abstract string InterruptTriggerName { get; }

        public override bool BlocksMovement => true;

        public override void StartTask()
        {
            isComplete = false;
            timer = 0f;
            HideProgressBar();
        }

        public override bool IsComplete()
        {
            return isComplete;
        }

        public override void OnArrival(HeroController hero)
        {
            if (ShouldInstantComplete())
            {
                hero.Animator.SetTrigger(InterruptTriggerName);
                isComplete = true;
                HideProgressBar();
                GenerateDrops();
                GrantCompletionXP();
                return;
            }

            hero.Animator.Play(AnimationName);
            ShowProgressBar();
        }

        public override void Tick(HeroController hero)
        {
            timer += Time.deltaTime;
            UpdateProgressBar();

            if (timer >= taskDuration)
            {
                hero.Animator.SetTrigger(InterruptTriggerName);
                isComplete = true;
                HideProgressBar();
                GenerateDrops();
                GrantCompletionXP();
                // The hero will get a new task automatically now
            }
        }

        public override void OnInterrupt(HeroController hero)
        {
            hero.Animator.SetTrigger(InterruptTriggerName);
            HideProgressBar();
        }

        private void ShowProgressBar()
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = 1f;
                var obj = progressBarObject != null ? progressBarObject : progressBar.gameObject;
                obj.SetActive(true);
            }
        }

        private void HideProgressBar()
        {
            if (progressBar != null)
            {
                var obj = progressBarObject != null ? progressBarObject : progressBar.gameObject;
                obj.SetActive(false);
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar != null) progressBar.fillAmount = Mathf.Clamp01((taskDuration - timer) / taskDuration);
        }
    }
}