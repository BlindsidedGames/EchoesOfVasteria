using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Buffs;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Base component for task behaviours.
    ///     Provides default implementations of <see cref="ITask" /> members.
    /// </summary>
    public abstract class BaseTask : MonoBehaviour, ITask
    {
        [SerializeField] public Skill associatedSkill;
        [SerializeField] public TaskData taskData;

        [Header("Auto Removal When Behind")]
        [SerializeField] private bool enableAutoRemovalWhenBehind = true;
        [SerializeField] private float autoRemovalDistanceX = 20f;
        [SerializeField] private float autoRemovalIntervalSeconds = 2f;

        /// <summary>
        ///     Fired when the task completes. Subscribers should
        ///     remove the task from any tracking collections.
        /// </summary>
        public event System.Action<ITask> TaskCompleted;

        private bool completionNotified;

        private float lastGrantedXp;

        public float LastGrantedXp => lastGrantedXp;

        private HeroController claimedBy;
        public HeroController ClaimedBy => claimedBy;

        private TaskController cachedTaskController;
        private bool autoRemovalScheduled;

        public bool Claim(HeroController hero)
        {
            if (hero == null) return false;
            if (claimedBy == null || claimedBy == hero)
            {
                claimedBy = hero;
                return true;
            }

            return false;
        }

        public void ReleaseClaim(HeroController hero)
        {
            if (claimedBy == hero || hero == null)
                claimedBy = null;
        }

        public void ClearClaim()
        {
            claimedBy = null;
        }

        /// <summary>
        ///     A property to indicate if this task should prevent the hero from moving.
        /// </summary>
        public virtual bool BlocksMovement => false;

        /// <summary>
        ///     The world location relevant to this task.
        /// </summary>
        public abstract Transform Target { get; }

        /// <summary>
        ///     Begin the task's logic. Override to perform initialization.
        /// </summary>
        public virtual void StartTask()
        {
        }

        /// <summary>
        ///     Determine if the task has completed.
        /// </summary>
        public abstract bool IsComplete();

        /// <summary>
        ///     Called by the HeroController once it reaches the task's target destination.
        /// </summary>
        public virtual void OnArrival(HeroController hero)
        {
        }

        /// <summary>
        ///     Called every frame while the hero is at the task location and performing the task.
        /// </summary>
        public virtual void Tick(HeroController hero)
        {
        }

        /// <summary>
        ///     Called by the HeroController when the task is interrupted (e.g., by combat).
        /// </summary>
        public virtual void OnInterrupt(HeroController hero)
        {
        }

        /// <summary>
        ///     Notify listeners that this task has completed. Ensures
        ///     the event is only invoked once.
        /// </summary>
        protected void NotifyCompleted()
        {
            if (completionNotified)
                return;
            completionNotified = true;
            TaskCompleted?.Invoke(this);
        }

        private void OnEnable()
        {
            // Schedule a light periodic check to self-remove when hero is far ahead.
            if (enableAutoRemovalWhenBehind && !autoRemovalScheduled)
            {
                // Slight jitter to avoid synchronized spikes
                var initialDelay = UnityEngine.Random.Range(0f, 0.25f);
                InvokeRepeating(nameof(CheckAutoRemovalBehindHero), initialDelay, Mathf.Max(0.1f, autoRemovalIntervalSeconds));
                autoRemovalScheduled = true;
            }
        }

        private void OnDisable()
        {
            if (autoRemovalScheduled)
            {
                CancelInvoke(nameof(CheckAutoRemovalBehindHero));
                autoRemovalScheduled = false;
            }
        }

        private void CheckAutoRemovalBehindHero()
        {
            // Only main hero position is considered
            var hero = HeroController.Instance;
            if (hero == null)
                return;

            // If this task is already complete, let normal completion flow remove it
            if (IsComplete())
                return;

            // Compare along X axis using the task's Target if available
            var target = Target != null ? Target : transform;
            if (target == null)
                return;

            var deltaX = hero.transform.position.x - target.position.x;
            if (deltaX <= autoRemovalDistanceX)
                return;

            // Remove from the active TaskController safely
            if (cachedTaskController == null)
                cachedTaskController = hero.GetComponentInParent<TaskController>();

            cachedTaskController?.RemoveTask(this);
        }

        protected bool ShouldInstantComplete()
        {
            var controller = SkillController.Instance ?? FindFirstObjectByType<SkillController>();
            bool milestone = controller && controller.RollForEffect(associatedSkill, MilestoneType.InstantTask);
            bool buff = TimelessEchoes.Buffs.BuffManager.Instance != null && TimelessEchoes.Buffs.BuffManager.Instance.InstantTaskBuffActive;
            return milestone || buff;
        }

        protected float GrantCompletionXP()
        {
            lastGrantedXp = 0f;
            if (claimedBy != null && claimedBy.IsEcho)
                return 0f;
            if (associatedSkill == null || taskData == null || taskData.xpForCompletion <= 0f)
                return 0f;

            var controller = SkillController.Instance ?? FindFirstObjectByType<SkillController>();
            var amount = taskData.xpForCompletion;
            if (controller)
            {
                int mult = controller.GetEffectMultiplier(associatedSkill, MilestoneType.DoubleXP);
                amount *= mult;
            }

            controller?.AddExperience(associatedSkill, amount);
            lastGrantedXp = amount;

            if (controller != null && associatedSkill != null)
            {
                var progress = controller.GetProgress(associatedSkill);
                if (progress != null)
                {
                    foreach (var id in progress.Milestones)
                    {
                        var ms = associatedSkill.milestones.Find(m => m.bonusID == id);
                        if (ms != null && ms.type == MilestoneType.SpawnEcho && UnityEngine.Random.value <= ms.chance)
                        {
                            EchoManager.SpawnEchoes(ms.echoSpawnConfig, ms.echoDuration,
                                new List<Skill> { associatedSkill }, true);
                        }
                    }
                }
            }

            return amount;
        }
    }}