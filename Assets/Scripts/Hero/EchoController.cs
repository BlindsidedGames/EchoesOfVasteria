using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Controls Echo behaviour and lifetime.
    /// </summary>
    public class EchoController : MonoBehaviour
    {
        public static readonly List<EchoController> CombatEchoes = new();
        public static readonly List<EchoController> AllEchoes = new();

        public List<Skill> capableSkills = new();
        public float lifetime = 10f;
        public bool disableSkills;
        public bool combatEnabled;
        public EchoType Type { get; private set; } = EchoType.All;

        // Skill indicator references are stored on the hero controller
        // so they can be configured on the main hero prefab.

        private HeroController hero;
        private TaskController taskController;
        private float remaining;
        private float defaultAggroRange;
        private GameObject durationBarParent;
        private SlicedFilledImage durationFill;

        /// <summary>
        ///     Returns true once <see cref="Init" /> has completed.
        /// </summary>
        public bool Initialized { get; private set; }

        private void Awake()
        {
            hero = GetComponent<HeroController>();
            taskController = GetComponentInParent<TaskController>();
            remaining = lifetime;
            if (!AllEchoes.Contains(this))
                AllEchoes.Add(this);
            if (hero != null)
                defaultAggroRange = hero.CombatAggroRange;
        }

        private void OnEnable()
        {
            if (!Initialized)
                return;

            if (!AllEchoes.Contains(this))
                AllEchoes.Add(this);

            if ((Type == EchoType.Combat || Type == EchoType.All) && !CombatEchoes.Contains(this))
                CombatEchoes.Add(this);

            UpdateIndicators();

            if (hero != null && taskController != null && Type != EchoType.Combat)
                AssignTask();
        }

        private void OnDisable()
        {
            CombatEchoes.Remove(this);
            AllEchoes.Remove(this);
            if (durationBarParent != null)
                durationBarParent.SetActive(false);
            if (hero != null)
            {
                hero.UnlimitedAggroRange = false;
                hero.CombatAggroRange = defaultAggroRange;
            }
        }

        /// <summary>
        ///     Configure the echo after it is spawned.
        /// </summary>
        public void Init(IEnumerable<Skill> skills, float duration, EchoType type)
        {
            capableSkills = skills != null ? new List<Skill>(skills) : new List<Skill>();
            lifetime = duration;
            remaining = duration;
            Type = type;
            disableSkills = type == EchoType.Combat;
            combatEnabled = type == EchoType.Combat || type == EchoType.All;

            if (hero != null)
            {
                // When skills are not restricted, echoes should still focus on tasks
                // rather than roaming across the map for combat. Treat an empty
                // skill list as "all skills" instead of "combat only".
                var combatOnly = combatEnabled && disableSkills;
                hero.UnlimitedAggroRange = combatOnly;
                if (combatOnly)
                    hero.CombatAggroRange = defaultAggroRange;
                durationBarParent = hero.EchoDurationBar;
                durationFill = hero.EchoDurationFill;
                if (durationBarParent != null)
                    durationBarParent.SetActive(!float.IsPositiveInfinity(duration));
                if (durationFill != null && !float.IsPositiveInfinity(duration))
                    durationFill.fillAmount = 1f;
            }

            Initialized = true;

            UpdateIndicators();

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

            if (durationBarParent != null && durationBarParent.activeSelf && durationFill != null &&
                !float.IsPositiveInfinity(lifetime))
                durationFill.fillAmount = remaining / lifetime;

            if (!disableSkills && taskController != null)
            {
                var hasTask = false;
                if (capableSkills == null || capableSkills.Count == 0)
                    hasTask = taskController.tasks.Any(t => t is BaseTask b && !t.IsComplete());
                else
                    foreach (var s in capableSkills)
                    {
                        if (s == null) continue;
                        if (taskController.tasks.Any(t => t is BaseTask b && b.associatedSkill == s && !t.IsComplete()))
                        {
                            hasTask = true;
                            break;
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
            AllEchoes.Remove(this);
            if (durationBarParent != null)
                durationBarParent.SetActive(false);
            if (hero != null)
            {
                hero.UnlimitedAggroRange = false;
                hero.CombatAggroRange = defaultAggroRange;
            }
        }

        private void UpdateIndicators()
        {
            if (hero == null)
                return;

            void SetActive(GameObject obj, bool state)
            {
                if (obj != null)
                    obj.SetActive(state);
            }

            SetActive(hero.CombatIndicator, Type == EchoType.Combat || Type == EchoType.All);
            SetActive(hero.MiningIndicator, false);
            SetActive(hero.WoodcuttingIndicator, false);
            SetActive(hero.FishingIndicator, false);
            SetActive(hero.FarmingIndicator, false);
            SetActive(hero.LootingIndicator, false);

            var hasSkills = capableSkills != null && capableSkills.Count > 0;

            if (!hasSkills && Type != EchoType.Combat && Type != EchoType.Selective)
            {
                SetActive(hero.MiningIndicator, true);
                SetActive(hero.WoodcuttingIndicator, true);
                SetActive(hero.FishingIndicator, true);
                SetActive(hero.FarmingIndicator, true);
                SetActive(hero.LootingIndicator, true);
                return;
            }

            if (capableSkills == null)
                return;

            foreach (var s in capableSkills)
            {
                if (s == null) continue;
                switch (s.skillName)
                {
                    case "Mining":
                        SetActive(hero.MiningIndicator, true);
                        break;
                    case "Woodcutting":
                        SetActive(hero.WoodcuttingIndicator, true);
                        break;
                    case "Fishing":
                        SetActive(hero.FishingIndicator, true);
                        break;
                    case "Farming":
                        SetActive(hero.FarmingIndicator, true);
                        break;
                    case "Looting":
                        SetActive(hero.LootingIndicator, true);
                        break;
                }
            }
        }

        private void AssignTask()
        {
            if (hero == null || taskController == null)
                return;

            if (Type == EchoType.Combat)
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