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
        public static readonly List<EchoController> CombatEchoes = new();

        public System.Collections.Generic.List<Skill> capableSkills = new();
        public float lifetime = 10f;
        public bool disableSkills;
        public bool combatEnabled;

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
            if (combatEnabled && !CombatEchoes.Contains(this))
                CombatEchoes.Add(this);

            if (hero != null && taskController != null && !disableSkills)
                AssignTask();
        }

        private void OnDisable()
        {
            CombatEchoes.Remove(this);
        }

        /// <summary>
        /// Configure the echo after it is spawned.
        /// </summary>
        public void Init(System.Collections.Generic.IEnumerable<Skill> skills, float duration, bool disable, bool combat)
        {
            capableSkills = skills != null ? new System.Collections.Generic.List<Skill>(skills) : new System.Collections.Generic.List<Skill>();
            lifetime = duration;
            remaining = duration;
            disableSkills = disable;
            combatEnabled = combat;

            if (hero != null)
            {
                bool combatOnly = combatEnabled && (disableSkills || capableSkills == null || capableSkills.Count == 0);
                hero.UnlimitedAggroRange = combatOnly;
            }

            if (combatEnabled && isActiveAndEnabled && !CombatEchoes.Contains(this))
                CombatEchoes.Add(this);

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
                    if (combatEnabled && hero != null && hero.AllowAttacks)
                        return; // stay alive for combat
                    Destroy(gameObject);
                }
            }
            else if (disableSkills)
            {
                if (combatEnabled && hero != null && hero.AllowAttacks)
                    return; // stay alive for combat
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            CombatEchoes.Remove(this);
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
