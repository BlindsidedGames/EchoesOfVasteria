using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using UnityEngine;

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

        private float lastGrantedXp;

        public float LastGrantedXp => lastGrantedXp;

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

        protected bool ShouldInstantComplete()
        {
            var controller = SkillController.Instance ?? FindFirstObjectByType<SkillController>();
            return controller && controller.RollForEffect(associatedSkill, MilestoneType.InstantTask);
        }

        protected float GrantCompletionXP()
        {
            lastGrantedXp = 0f;
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
            return amount;
        }
    }}