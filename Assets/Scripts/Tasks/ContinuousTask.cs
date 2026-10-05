using Blindsided.Utilities;
using TimelessEchoes.Buffs;
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
        private TimelessEchoes.Farming.FarmService.StagedTaskCredit farmCredit;

        private float timer;
        public float ProgressRemaining => TaskDuration > 0 ? Mathf.Clamp01((TaskDuration - timer) / TaskDuration) : 1;

        protected float TaskDuration => taskData != null ? taskData.taskDuration : 0f;

        protected abstract string AnimationName { get; }
        protected abstract string InterruptTriggerName { get; }
        protected virtual string CompletionTriggerName => InterruptTriggerName;

        public override bool BlocksMovement => true;

        protected override void OnEnable()
        {
            base.OnEnable();
            // Reset progress and completion state on reuse (e.g., after pooling)
            isComplete = false;
            farmCredit = null;
            timer = 0f;
            HideProgressBar();
        }

        public override void StartTask()
        {
            isComplete = false;
            farmCredit = null;
            timer = 0f;
            HideProgressBar();
        }

        public override bool IsComplete()
        {
            return isComplete;
        }

        public override void OnArrival(HeroBase hero)
        {
            if (isComplete) return;
            if (ShouldInstantComplete())
            {
                AnimatorUtils.SetTriggerAndReset(hero, hero.Animator, CompletionTriggerName);
                isComplete = true;
                CompleteWork(hero);
                return;
            }

            
            hero.Animator.Play(AnimationName);
            hero.PlaySecondaryAnimation(AnimationName);
            ShowProgressBar();
        }

        public override void Tick(HeroBase hero)
        {
            if (isComplete) return;
            var delta = Time.deltaTime;
            var controller = SkillController.Instance;
            if (controller != null && associatedSkill != null)
                delta *= controller.GetTaskSpeedMultiplier(associatedSkill);

            var buffManager = BuffManager.Instance;
            if (buffManager != null)
                delta *= buffManager.TaskSpeedMultiplier;
            timer += delta;


            UpdateProgressBar();

            if (timer >= TaskDuration)
            {
                AnimatorUtils.SetTriggerAndReset(hero, hero.Animator, CompletionTriggerName);
                isComplete = true;
                CompleteWork(hero);
                // The hero will get a new task automatically now
            }
        }

        private void CompleteWork(HeroBase hero)
        {
            var farm = TimelessEchoes.Farming.FarmService.Instance;
            var credit = farm?.StageCompletedTask(farmCredit, taskData, hero);
            farmCredit = credit;
            try
            {
                HideProgressBar();
                var xpGranted = GrantCompletionXP();
                GenerateDrops(xpGranted);
                OnTaskCompleted(hero);
                NotifyCompleted();
            }
            finally
            {
                // Ordinary rewards/notifications may throw or return the hero to town. Work
                // already completed; preserve its selected intent and then capture contributors.
                if (farm) farm.CommitStagedTask(credit);
            }
        }

        public override void OnInterrupt(HeroBase hero)
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
            if (progressBar != null && progressBar.enabled && TaskDuration > 0f)
                progressBar.fillAmount = Mathf.Clamp01((TaskDuration - timer) / TaskDuration);
        }

        /// <summary>
        ///     Optional hook invoked when the task reaches completion (either instantly on arrival
        ///     or after the normal duration). Subclasses can override to perform completion-side
        ///     effects such as swapping sprites or playing VFX before the task component is removed.
        /// </summary>
        /// <param name="hero">The hero performing the task.</param>
        protected virtual void OnTaskCompleted(HeroBase hero)
        {
        }
    }
}
