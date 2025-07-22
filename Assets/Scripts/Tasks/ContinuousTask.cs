using Blindsided.Utilities;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     A base class for tasks that require the hero to wait for a duration,
    ///     showing a progress bar.
    /// </summary>
    public abstract class ContinuousTask : ResourceGeneratingTask
    {
        // Duration for the task is defined on the TaskData
        [SerializeField] private GameObject progressBarObject;
        [SerializeField] private SlicedFilledImage progressBar;
        private bool isComplete;

        private float timer;

        protected float TaskDuration => taskData != null ? taskData.taskDuration : 0f;

        protected abstract string AnimationName { get; }
        protected abstract string InterruptTriggerName { get; }

        public override bool BlocksMovement => true;

        private void OnEnable()
        {
            HideProgressBar();
        }

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
                AnimatorUtils.SetTriggerAndReset(hero, hero.Animator, InterruptTriggerName);
                isComplete = true;
                HideProgressBar();
                GenerateDrops();
                GrantCompletionXP();
                return;
            }


            hero.Animator.Play(AnimationName);
            if (hero.AutoBuffAnimator != null && hero.AutoBuffAnimator.isActiveAndEnabled)
                hero.AutoBuffAnimator.Play(AnimationName);
            ShowProgressBar();
        }

        public override void Tick(HeroController hero)
        {
            var delta = Time.deltaTime;
            var controller = SkillController.Instance ?? FindFirstObjectByType<SkillController>();
            if (controller != null && associatedSkill != null)
                delta *= controller.GetTaskSpeedMultiplier(associatedSkill);

            var buffManager = BuffManager.Instance ?? FindFirstObjectByType<BuffManager>();
            if (buffManager != null)
                delta *= buffManager.TaskSpeedMultiplier;
            timer += delta;
            if (!isComplete)
            {
                // No audio playback here; sounds are triggered via animation events.
            }

            UpdateProgressBar();

            if (timer >= TaskDuration)
            {
                AnimatorUtils.SetTriggerAndReset(hero, hero.Animator, InterruptTriggerName);
                isComplete = true;
                HideProgressBar();
                GenerateDrops();
                GrantCompletionXP();
                // The hero will get a new task automatically now
            }
        }

        public override void OnInterrupt(HeroController hero)
        {
            AnimatorUtils.SetTriggerAndReset(hero, hero.Animator, InterruptTriggerName);
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
            if (progressBar != null && TaskDuration > 0f)
                progressBar.fillAmount = Mathf.Clamp01((TaskDuration - timer) / TaskDuration);
        }
    }
}