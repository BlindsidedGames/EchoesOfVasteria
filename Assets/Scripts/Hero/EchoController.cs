using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using UnityEngine;
using Pathfinding;
using TimelessEchoes.Enemies;

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
        private Sprite durationBaseSprite;
        private Sprite durationYellowSprite;
        private Sprite durationRedSprite;
        private Sprite durationLastAppliedSprite;

        // Expiration deferral state: wait for current enemy kill or task completion
        private bool expirationDeferred;
        private Health deferredEnemyHealth;
        private ITask deferredTask;
        private float deferStartTime;
        [SerializeField] private float maxLingerOnExpiry = 0f; // 0 = disabled

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
                if (durationFill != null)
                {
                    // Cache sprite references for dynamic color changes
                    durationBaseSprite = durationFill.sprite;
                    durationYellowSprite = hero.EchoDurationYellowSprite;
                    durationRedSprite = hero.EchoDurationRedSprite;
                    durationLastAppliedSprite = durationBaseSprite;
                    if (!float.IsPositiveInfinity(duration))
                        durationFill.fillAmount = 1f;
                }
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
            // Countdown until expiration unless already deferring
            if (!expirationDeferred)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f)
                {
                    if (!BeginExpirationDeferral())
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // Update duration UI
            if (durationBarParent != null && durationBarParent.activeSelf && durationFill != null &&
                !float.IsPositiveInfinity(lifetime))
            {
                var pct = expirationDeferred ? 0f : Mathf.Clamp01(remaining / lifetime);
                durationFill.fillAmount = pct;

                // Swap sprite based on remaining percent thresholds
                // Green (base) > 50%, Yellow <= 50% and > 10%, Red <= 10%
                var targetSprite = durationBaseSprite;
                if (pct <= 0.10f && durationRedSprite != null)
                    targetSprite = durationRedSprite;
                else if (pct <= 0.50f && durationYellowSprite != null)
                    targetSprite = durationYellowSprite;

                if (targetSprite != durationLastAppliedSprite && targetSprite != null)
                {
                    durationFill.sprite = targetSprite;
                    durationLastAppliedSprite = targetSprite;
                }
            }

            // Handle deferral waiting conditions
            if (expirationDeferred)
            {
                if (maxLingerOnExpiry > 0f && Time.time - deferStartTime >= maxLingerOnExpiry)
                {
                    Destroy(gameObject);
                    return;
                }

                if (deferredEnemyHealth != null)
                {
                    // Unity null-safe: destroyed objects compare equal to null
                    if (deferredEnemyHealth == null || deferredEnemyHealth.CurrentHealth <= 0f)
                    {
                        Destroy(gameObject);
                    }
                    return; // keep waiting while enemy is alive
                }

                if (deferredTask != null)
                {
                    var taskMb = deferredTask as MonoBehaviour;
                    if (taskMb == null || deferredTask.IsComplete())
                    {
                        Destroy(gameObject);
                    }
                    return; // keep waiting while task is incomplete
                }

                // Nothing to wait on anymore
                Destroy(gameObject);
                return;
            }

            // Normal lifetime behavior (only when not deferring)
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

        private bool BeginExpirationDeferral()
        {
            var captured = false;
            // Prefer finishing the current enemy if in combat
            if (hero != null && hero.InCombat)
            {
                var setter = hero.GetComponent<AIDestinationSetter>();
                var target = setter != null ? setter.target : null;
                var hp = target != null ? target.GetComponent<Health>() : null;
                if (hp != null && hp.CurrentHealth > 0f)
                {
                    deferredEnemyHealth = hp;
                    captured = true;
                }
            }

            // Otherwise, finish the current task if any
            if (!captured && hero != null && hero.CurrentTask != null && !hero.CurrentTask.IsComplete())
            {
                deferredTask = hero.CurrentTask;
                captured = true;
            }

            if (captured)
            {
                expirationDeferred = true;
                deferStartTime = Time.time;
                // Prevent picking up new tasks during the deferral window
                hero.ClearTaskController();
            }

            return captured;
        }

        public bool TryDeferExpiration()
        {
            if (expirationDeferred) return true;
            return BeginExpirationDeferral();
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