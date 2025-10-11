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

        private HeroBase claimedBy;
        public HeroBase ClaimedBy => claimedBy;

        private TaskController cachedTaskController;
        private bool autoRemovalScheduled;
        private const float EchoXpFraction = 0.25f;

        // When true, this task should no longer be considered for selection until reset (e.g., pooled & reused)
        public virtual bool IsExhausted { get; protected set; }

        public bool Claim(HeroBase hero)
        {
            if (hero == null) return false;
            if (claimedBy == null || claimedBy == hero)
            {
                claimedBy = hero;
                return true;
            }

            return false;
        }

        public void ReleaseClaim(HeroBase hero)
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
        public virtual void OnArrival(HeroBase hero)
        {
        }

        /// <summary>
        ///     Called every frame while the hero is at the task location and performing the task.
        /// </summary>
        public virtual void Tick(HeroBase hero)
        {
        }

        /// <summary>
        ///     Called by the HeroController when the task is interrupted (e.g., by combat).
        /// </summary>
        public virtual void OnInterrupt(HeroBase hero)
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

        protected virtual void OnEnable()
        {
            // Schedule a light periodic check to self-remove when hero is far ahead.
            if (enableAutoRemovalWhenBehind && !autoRemovalScheduled)
            {
                // Slight jitter to avoid synchronized spikes
                var initialDelay = UnityEngine.Random.Range(0f, 0.25f);
                InvokeRepeating(nameof(CheckAutoRemovalBehindHero), initialDelay, Mathf.Max(0.1f, autoRemovalIntervalSeconds));
                autoRemovalScheduled = true;
            }

            // Reset per-spawn availability
            IsExhausted = false;
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
                cachedTaskController = TaskController.Instance ?? hero.GetComponentInParent<TaskController>();

            cachedTaskController?.RemoveTask(this);
        }

        protected bool ShouldInstantComplete()
        {
            var controller = SkillController.Instance;
            return controller && controller.RollForProc(associatedSkill, MilestoneProcType.InstantTask);
        }

        protected float GrantCompletionXP()
        {
            lastGrantedXp = 0f;
            bool echoPerformer = claimedBy != null && claimedBy.IsEcho;
            if (associatedSkill == null || taskData == null || taskData.xpForCompletion <= 0f)
                return 0f;

            var controller = SkillController.Instance;
            var amount = taskData.xpForCompletion;
            if (controller)
            {
                int mult = controller.GetStackingMultiplier(associatedSkill, MilestoneProcType.DoubleXP);
                amount *= mult;
            }

            if (echoPerformer)
                amount *= EchoXpFraction;

            float appliedXp = controller != null ? controller.AddExperience(associatedSkill, amount) : amount;
            lastGrantedXp = appliedXp;

            if (controller != null && associatedSkill != null)
            {
                var spawnEntries = controller.GetSpawnEntries(associatedSkill);
                if (spawnEntries != null)
                {
                    foreach (var entry in spawnEntries)
                    {
                        if (UnityEngine.Random.value <= entry.Chance)
                        {
                            var fallback = entry.UseAssociatedSkillFallback && associatedSkill != null
                                ? new List<Skill> { associatedSkill }
                                : null;
                            EchoManager.SpawnEchoes(entry.Config, entry.Duration, fallback, true, entry.Count);
                        }
                    }
                }
            }
            return appliedXp;
        }

#if UNITY_EDITOR
        private static readonly Color unclaimedGizmoColor = new Color(0.65f, 0.72f, 0.82f, 0.85f);
        private static readonly Color claimedGizmoColor = new Color(0.45f, 0.85f, 0.5f, 1f);
        private static readonly Color claimLineColor = new Color(0.62f, 0.16f, 0.85f, 1f);

        private void OnDrawGizmos()
        {
            DrawClaimGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawClaimGizmos(true);
        }

        private void DrawClaimGizmos(bool emphasize)
        {
            var anchor = Target != null ? Target : transform;
            if (anchor == null)
                return;

            var originalColor = Gizmos.color;
            var originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.identity;
            var position = anchor.position;

            bool isClaimed = claimedBy != null;
            Gizmos.color = isClaimed ? claimedGizmoColor : unclaimedGizmoColor;

            float radius = emphasize ? 0.45f : 0.35f;
            Gizmos.DrawWireSphere(position, radius);

            if (isClaimed)
            {
                var hero = claimedBy;
                if (hero != null)
                {
                    var heroTransform = hero.transform;
                    if (heroTransform != null)
                    {
                        Gizmos.color = claimLineColor;
                        Gizmos.DrawLine(position, heroTransform.position);

                        float markerRadius = emphasize ? 0.18f : 0.12f;
                        Gizmos.DrawSphere(position, markerRadius);
                        Gizmos.DrawSphere(heroTransform.position, markerRadius * 0.75f);
                    }
                }
            }

            Gizmos.matrix = originalMatrix;
            Gizmos.color = originalColor;
        }
#endif
    }
}

