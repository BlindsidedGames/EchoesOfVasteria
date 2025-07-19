using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Buffs;
using UnityEngine;
using System.Collections.Generic;

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

        private HeroController claimedBy;
        public HeroController ClaimedBy => claimedBy;

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
                            var config = ms.echoSpawnConfig;
                            int count = config != null ? Mathf.Max(1, config.echoCount) : 1;
                            var skills = config != null && config.capableSkills != null && config.capableSkills.Count > 0
                                ? config.capableSkills
                                : new System.Collections.Generic.List<Skill> { associatedSkill };
                            bool disable = config != null && config.disableSkills;
                            for (int c = 0; c < count; c++)
                            {
                                var skill = skills[Mathf.Min(c, skills.Count - 1)];
                                bool combat = config != null && config.combatEnabled &&
                                              SkillController.Instance != null &&
                                              SkillController.Instance.CombatSkill == skill;
                                EchoManager.SpawnEcho(new System.Collections.Generic.List<Skill> { skill }, ms.echoDuration, combat, disable);
                            }
                        }
                    }
                }
            }

            return amount;
        }
    }}