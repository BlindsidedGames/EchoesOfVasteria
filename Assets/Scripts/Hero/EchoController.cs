using System.Linq;
using System.Collections.Generic;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Controls Echo behaviour and lifetime.
    /// </summary>
    public class EchoController : MonoBehaviour
    {
        public System.Collections.Generic.List<Skill> capableSkills = new();
        public float lifetime = 10f;
        public bool disableSkills;

        private HeroController hero;
        private TaskController taskController;
        private float remaining;

        private void Awake()
        {
            hero = GetComponent<HeroController>();
            taskController = GetComponentInParent<TaskController>();
            remaining = lifetime;
        }

        private void OnEnable()
        {
            if (hero != null && taskController != null && !disableSkills)
                AssignTask();
        }

        /// <summary>
        /// Configure the echo after it is spawned.
        /// </summary>
        public void Init(System.Collections.Generic.IEnumerable<Skill> skills, float duration, bool disable)
        {
            capableSkills = skills != null ? new System.Collections.Generic.List<Skill>(skills) : new System.Collections.Generic.List<Skill>();
            lifetime = duration;
            remaining = duration;
            disableSkills = disable;

            if (hero != null)
            {
                var combatSkill = SkillController.Instance?.CombatSkill;
                bool combatOnly = disableSkills || (capableSkills != null && capableSkills.Count == 1 && combatSkill != null && capableSkills.Contains(combatSkill));
                hero.UnlimitedAggroRange = combatOnly;
            }

            if (!disableSkills && isActiveAndEnabled && hero != null && taskController != null)
                AssignTask();
        }

    private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (!disableSkills && taskController != null)
            {
                bool hasTask = false;
                if (capableSkills == null || capableSkills.Count == 0)
                {
                    hasTask = taskController.tasks.Any(t => t is BaseTask b && !t.IsComplete());
                }
                else
                {
                    foreach (var s in capableSkills)
                    {
                        if (s == null) continue;
                        if (taskController.tasks.Any(t => t is BaseTask b && b.associatedSkill == s && !t.IsComplete()))
                        {
                            hasTask = true;
                            break;
                        }
                    }
                }

                if (!hasTask)
                {
                    var combatSkill = SkillController.Instance?.CombatSkill;
                    if (capableSkills != null && combatSkill != null && capableSkills.Contains(combatSkill) && hero != null && hero.AllowAttacks)
                        return; // stay alive for combat
                    Destroy(gameObject);
                }
            }
            else if (disableSkills)
            {
                var combatSkill = SkillController.Instance?.CombatSkill;
                if (capableSkills != null && combatSkill != null && capableSkills.Contains(combatSkill) && hero != null && hero.AllowAttacks)
                    return; // stay alive for combat
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (hero != null)
                hero.UnlimitedAggroRange = false;
        }

        private void AssignTask()
        {
            if (hero == null || taskController == null)
                return;

            if (disableSkills)
                return;

            if (capableSkills == null || capableSkills.Count == 0)
            {
                taskController.SelectEarliestTask(hero);
                return;
            }
            if (capableSkills.Count == 1)
            {
                var s = capableSkills[0];
                if (s != null)
                    taskController.SelectEarliestTask(hero, s);
                return;
            }

            taskController.SelectEarliestTask(hero, capableSkills);
        }
    }
}
