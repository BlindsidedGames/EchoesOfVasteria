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
    public class EchoController : HeroBase
    {
        public static readonly List<EchoController> CombatEchoes = new();
        public static readonly List<EchoController> AllEchoes = new();

        public List<Skill> capableSkills = new();
        public float lifetime = 10f;
        public bool disableSkills;
        public bool combatEnabled;
        public EchoType Type { get; private set; } = EchoType.All;

        // Echo-only indicators/UI
        [Header("Indicators")] [SerializeField] private GameObject combatIndicator;
        [SerializeField] private GameObject miningIndicator;
        [SerializeField] private GameObject woodcuttingIndicator;
        [SerializeField] private GameObject fishingIndicator;
        [SerializeField] private GameObject farmingIndicator;
        [SerializeField] private GameObject lootingIndicator;

        [Header("Duration UI")] [SerializeField] private GameObject durationBarParent;
        [SerializeField] private SlicedFilledImage durationFill;
        [SerializeField] private Sprite durationYellowSprite;
        [SerializeField] private Sprite durationRedSprite;

        // Uses protected taskController from HeroBase; do not redeclare here
        private float remaining;
        [SerializeField] private float taskAcquireGraceSeconds = 1.0f;
        private float spawnTime;
        private float defaultAggroRange;
        private Sprite durationBaseSprite;
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

        protected override bool IsEchoActor => true;

        protected override void Awake()
        {
            base.Awake();
            taskCtrl = TimelessEchoes.Tasks.TaskController.Instance
                       ?? GetComponent<TimelessEchoes.Tasks.TaskController>()
                       ?? GetComponentInParent<TimelessEchoes.Tasks.TaskController>()
                       ?? FindFirstObjectByType<TimelessEchoes.Tasks.TaskController>();
            remaining = lifetime;
            spawnTime = Time.time;
            if (!AllEchoes.Contains(this))
                AllEchoes.Add(this);
            defaultAggroRange = CombatAggroRange;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Initialized)
                return;

            if (!AllEchoes.Contains(this))
                AllEchoes.Add(this);

            if ((Type == EchoType.Combat || Type == EchoType.All) && !CombatEchoes.Contains(this))
                CombatEchoes.Add(this);

            UpdateIndicators();

            if (taskCtrl == null && Type != EchoType.Combat)
            {
                var inst = TimelessEchoes.Tasks.TaskController.Instance;
                if (inst != null)
                    taskCtrl = inst;
            }

            if (taskCtrl != null && Type != EchoType.Combat)
                AssignTask();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CombatEchoes.Remove(this);
            AllEchoes.Remove(this);
            if (durationBarParent != null)
                durationBarParent.SetActive(false);
            UnlimitedAggroRange = false;
            CombatAggroRange = defaultAggroRange;
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

            spawnTime = Time.time;
            // When skills are not restricted, echoes should still focus on tasks
            // rather than roaming across the map for combat. Treat an empty
            // skill list as "all skills" instead of "combat only".
            var combatOnly = combatEnabled && disableSkills;
            UnlimitedAggroRange = combatOnly;
            if (combatOnly)
                CombatAggroRange = defaultAggroRange;
            if (durationBarParent != null)
                durationBarParent.SetActive(!float.IsPositiveInfinity(duration));
            if (durationFill != null)
            {
                // Cache sprite references for dynamic color changes
                durationBaseSprite = durationFill.sprite;
                durationLastAppliedSprite = durationBaseSprite;
                if (!float.IsPositiveInfinity(duration))
                    durationFill.fillAmount = 1f;
            }

            Initialized = true;

            UpdateIndicators();

            if (combatEnabled && isActiveAndEnabled && !CombatEchoes.Contains(this))
                CombatEchoes.Add(this);

            if (!disableSkills && isActiveAndEnabled && taskCtrl != null)
                AssignTask();
        }

        protected override void Update()
        {
            base.Update();
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
            if (!disableSkills && taskCtrl != null)
            {
                var hasTask = false;
                if (capableSkills == null || capableSkills.Count == 0)
                    hasTask = taskCtrl.tasks.Any(t => t is BaseTask b && !t.IsComplete());
                else
                    foreach (var s in capableSkills)
                    {
                        if (s == null) continue;
                        if (taskCtrl.tasks.Any(t => t is BaseTask b && b.associatedSkill == s && !t.IsComplete()))
                        {
                            hasTask = true;
                            break;
                        }
                    }

                if (!hasTask)
                {
                    // Allow a short grace window at spawn so echoes don't immediately expire
                    // while the map is still generating tasks.
                    if (Time.time - spawnTime < taskAcquireGraceSeconds)
                        return;
                    // If no tasks anywhere but combat is allowed, stay for combat.
                    if (combatEnabled && AllowAttacks)
                        return;
                    // If the controller reports there are visible tasks for this echo, keep waiting
                    if (taskCtrl.HasVisibleTasksForHero(this))
                        return;
                    Destroy(gameObject);
                }
            }
            else if (disableSkills)
            {
                if (combatEnabled && AllowAttacks)
                    return; // stay alive for combat
                Destroy(gameObject);
            }
        }

        private bool BeginExpirationDeferral()
        {
            var captured = false;
            // Prefer finishing the current enemy if in combat
            if (InCombat)
            {
                var setter = GetComponent<AIDestinationSetter>();
                var target = setter != null ? setter.target : null;
                var hp = target != null ? target.GetComponent<Health>() : null;
                if (hp != null && hp.CurrentHealth > 0f)
                {
                    deferredEnemyHealth = hp;
                    captured = true;
                }
            }

            // Otherwise, finish the current task if any
            if (!captured && CurrentTask != null && !CurrentTask.IsComplete())
            {
                deferredTask = CurrentTask;
                captured = true;
            }

            if (captured)
            {
                expirationDeferred = true;
                deferStartTime = Time.time;
                // Prevent picking up new tasks during the deferral window
                ClearTaskController();
            }

            return captured;
        }

        public bool TryDeferExpiration()
        {
            if (expirationDeferred) return true;
            return BeginExpirationDeferral();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CombatEchoes.Remove(this);
            AllEchoes.Remove(this);
            if (durationBarParent != null)
                durationBarParent.SetActive(false);
            UnlimitedAggroRange = false;
            CombatAggroRange = defaultAggroRange;
        }

        private void UpdateIndicators()
        {
            void SetActive(GameObject obj, bool state)
            {
                if (obj != null)
                    obj.SetActive(state);
            }

            SetActive(combatIndicator, Type == EchoType.Combat || Type == EchoType.All);
            SetActive(miningIndicator, false);
            SetActive(woodcuttingIndicator, false);
            SetActive(fishingIndicator, false);
            SetActive(farmingIndicator, false);
            SetActive(lootingIndicator, false);

            var hasSkills = capableSkills != null && capableSkills.Count > 0;

            if (!hasSkills && Type != EchoType.Combat && Type != EchoType.Selective)
            {
                SetActive(miningIndicator, true);
                SetActive(woodcuttingIndicator, true);
                SetActive(fishingIndicator, true);
                SetActive(farmingIndicator, true);
                SetActive(lootingIndicator, true);
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
                        SetActive(miningIndicator, true);
                        break;
                    case "Woodcutting":
                        SetActive(woodcuttingIndicator, true);
                        break;
                    case "Fishing":
                        SetActive(fishingIndicator, true);
                        break;
                    case "Farming":
                        SetActive(farmingIndicator, true);
                        break;
                    case "Looting":
                        SetActive(lootingIndicator, true);
                        break;
                }
            }
        }

        private void AssignTask()
        {
            if (taskCtrl == null)
                return;

            if (Type == EchoType.Combat)
                return;

            if (capableSkills == null || capableSkills.Count == 0)
            {
                taskCtrl.SelectEarliestTask(this);
                return;
            }

            if (capableSkills.Count == 1)
            {
                var s = capableSkills[0];
                if (s != null)
                    taskCtrl.SelectEarliestTask(this, s);
                return;
            }

            taskCtrl.SelectEarliestTask(this, capableSkills);
        }
    }
}
