using System.Linq;
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
        public Skill targetSkill;
        public float lifetime = 10f;

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
            if (hero != null && taskController != null && targetSkill != null)
                taskController.SelectEarliestTask(hero, targetSkill);
        }

        /// <summary>
        /// Configure the echo after it is spawned.
        /// </summary>
        public void Init(Skill skill, float duration)
        {
            targetSkill = skill;
            lifetime = duration;
            remaining = duration;

            if (isActiveAndEnabled && hero != null && taskController != null)
                taskController.SelectEarliestTask(hero, targetSkill);
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (taskController != null && targetSkill != null)
            {
                bool hasTask = taskController.tasks.Any(t => t is BaseTask b &&
                                                            b.associatedSkill == targetSkill &&
                                                            !t.IsComplete());
                if (!hasTask)
                    Destroy(gameObject);
            }
        }
    }
}
