#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using Pathfinding;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Gear;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI;
using TimelessEchoes.Utilities;
using Blindsided.Utilities.Pooling;
using UnityEngine;
using UnityEngine.Serialization;
using static TimelessEchoes.TELogger;
using static TimelessEchoes.Quests.QuestUtils;
using static Blindsided.SaveData.StaticReferences;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(EnemyEngagementTracker))]
    [RequireComponent(typeof(HeroCombatController))]
    [RequireComponent(typeof(HeroMovementController))]
    /// <summary>
    /// Controls the main hero and echo clones: movement (A*), combat targeting and attacks,
    /// task interaction, stat application, and hooks into Buffs/Skills/Stats/UI.
    /// Exposes computed properties like Damage, AttackRate, MoveSpeed, Defense, and MaxHealthValue
    /// which include permanent upgrades and active buff multipliers.
    /// </summary>
    public abstract class HeroBase : MonoBehaviour
    {
        public static event System.Action OnMainHeroDiceChanged;

        /// <summary>
        /// Invoke the dice changed event from combat controller.
        /// </summary>
        internal static void InvokeMainHeroDiceChanged() => OnMainHeroDiceChanged?.Invoke();
        // Derived classes specify whether this actor is an Echo
        protected abstract bool IsEchoActor { get; }
        public bool IsEcho => IsEchoActor;
        [SerializeField] private HeroStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private string diceQuestID = "Protect the Town";
        [SerializeField] private Skill combatSkill;

        /// <summary>
        /// Handles combat logic: targeting, DPS estimation, attack execution, dice rolling.
        /// </summary>
        protected HeroCombatController combatController;

        /// <summary>
        /// Handles movement logic: pathfinding, animation sync, destination management.
        /// </summary>
        protected HeroMovementController movementController;

        [SerializeField] private BuffManager buffController;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        [SerializeField] private bool allowAttacks = true;
        
        [Header("Echo Targeting")]
        [SerializeField] [Range(0f, 10f)] private float echoAvoidIfTTKBelowSeconds = 1.0f;
        
        [Header("Assist Echo Combat")]
        [SerializeField] private bool assistEchoWhileOnTask = true;
        [SerializeField] private float assistEchoThreatRadius = 5f;

        public bool AllowAttacks
        {
            get => allowAttacks;
            set => allowAttacks = value;
        }

        public float EchoAvoidIfTTKBelowSeconds => echoAvoidIfTTKBelowSeconds;

        public bool UnlimitedAggroRange { get; set; }

        /// <summary>
        ///     Maximum distance echoes will search for combat targets when
        ///     <see cref="UnlimitedAggroRange" /> is enabled.
        /// </summary>
        [SerializeField] private float combatAggroRange = 20f;

        public float CombatAggroRange
        {
            get => combatAggroRange;
            set => combatAggroRange = value;
        }

        /// <summary>
        /// Tracks enemies that have engaged with this hero/echo.
        /// Handles death/disengage subscriptions and provides query methods.
        /// </summary>
        protected EnemyEngagementTracker engagementTracker;

        private float attackSpeedBonus;

        // Gear-derived additive bonuses
        private float gearAttackSpeedBonus;
        private float baseAttackSpeed;

        private float baseDamage;
        private float baseDefense;
        private float baseHealth;
        private float baseMoveSpeed;
        private float gearDamageBonus;
        private float gearDefenseBonus;
        private float gearHealthBonus;
        private float gearMoveSpeedBonus;

        /// <summary>
        /// Current combat damage multiplier from dice roll.
        /// </summary>
        public float CombatDamageMultiplier => combatController != null ? combatController.CombatDamageMultiplier : 1f;

        public bool ReaperSpawnedByDistance { get; private set; }

        private float damageBonus;
        private float defenseBonus;

        private HeroHealth health;
        private float healthBonus;
        private float moveSpeedBonus;
        private MapUI mapUI;

#if !DISABLESTEAMWORKS
        [SerializeField] private float richPresenceUpdateInterval = 2f;
        private float nextRichPresenceUpdate;
#endif

        private State state;

        [NonSerialized] protected TaskController taskCtrl;
        public ITask CurrentTask { get; private set; }
        public Animator Animator => animator;
        public bool InCombat => combatController != null && combatController.IsInCombat;

        protected virtual void Awake()
        {
            engagementTracker = GetComponent<EnemyEngagementTracker>();
            combatController = GetComponent<HeroCombatController>();
            movementController = GetComponent<HeroMovementController>();
            health = GetComponent<HeroHealth>();

            // Initialize movement controller first (it owns ai and setter)
            movementController?.Init(IsEchoActor);

            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    Log("BuffManager missing", TELogCategory.Buff, this);
            }

            if (taskCtrl == null)
            {
                taskCtrl = TimelessEchoes.Tasks.TaskController.Instance
                           ?? GetComponent<TimelessEchoes.Tasks.TaskController>()
                           ?? GetComponentInParent<TimelessEchoes.Tasks.TaskController>()
                           ?? FindFirstObjectByType<TimelessEchoes.Tasks.TaskController>();
            }

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            // Initialize combat controller (uses ai/setter from movementController)
            var ai = movementController?.AI;
            var setter = movementController?.Setter;
            combatController?.Init(this, stats, animator, projectileOrigin, combatSkill, ai, setter, diceRoller, diceQuestID);

            state = State.Idle;

            ApplyStatUpgrades();

            // Subscribe to equipment changes and initialize gear bonuses
            var equipInit = EquipmentController.Instance ??
                            FindFirstObjectByType<EquipmentController>();
            if (equipInit != null)
                equipInit.OnEquipmentChanged += RecalculateGearBonuses;
            RecalculateGearBonuses();

            if (stats != null)
            {
                movementController?.UpdateMaxSpeed(HeroStatSystem.GetSnapshot().movementSpeed);
                var hp = Mathf.RoundToInt(HeroStatSystem.GetSnapshot().maxHealth);
                health?.Init(hp);
            }

            // Initialize centralized hero stat system on scene load
            // Stat system is initialized by the concrete HeroController subclass

            OnPostAwakeAnimatorSetup();
        }


        protected virtual void Update()
        {
            if (movementController != null && !movementController.LogicActive)
                return;
            if (stats != null)
                movementController?.UpdateMaxSpeed(HeroStatSystem.GetSnapshot().movementSpeed);
            UpdateAnimation();
            UpdateBehavior();

            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
            {
                Log("GameplayStatTracker missing", TELogCategory.General, this);
            }
            else
            {
                if (!IsEchoActor)
                {
                    // Avoid excessive micro-updates: only record when moved a small threshold
                    // to prevent floating point jitter from accumulating.
                    tracker.RecordHeroPosition(transform.position);
                }
                BuffManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
#if !DISABLESTEAMWORKS
                if (Time.unscaledTime >= nextRichPresenceUpdate)
                {
                    RichPresenceManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
                    nextRichPresenceUpdate = Time.unscaledTime + richPresenceUpdateInterval;
                }
#endif
                var gmInstance = GameManager.Instance;
                var killScalingActive = gmInstance != null && gmInstance.IsKillScalingMode;
                if (!IsEchoActor && !ReaperSpawnedByDistance && !killScalingActive)
                {
                    var baseMax = tracker.MaxRunDistance;
                    var mult = buffController != null ? buffController.MaxDistanceMultiplier : 1f;
                    var flat = buffController != null ? buffController.MaxDistanceFlatBonus : 0f;
                    var buffed = baseMax * mult + flat;
                    var oc = Blindsided.Oracle.oracle;
                    var isDemo = oc != null && oc.demo;
                    var threshold = isDemo ? Mathf.Min(buffed, 300f) : buffed;

                    if (transform.position.x >= threshold)
                    {
                        var gm = gmInstance;
                        var hp = health != null ? health : GetComponent<HeroHealth>();
                        if (gm != null && hp != null && hp.CurrentHealth > 0f && gm.ReaperPrefab != null &&
                            gm.CurrentMap != null)
                        {
                        ReaperManager.Spawn(gm.ReaperPrefab, gameObject, gm.CurrentMap.transform, false,
                            () =>
                            {
                                gameObject.SetActive(false);
                                if (gm.GravestonePrefab != null)
                                    Instantiate(gm.GravestonePrefab, transform.position, Quaternion.identity,
                                        gm.CurrentMap.transform);
                            }, gm.ReaperSpawnOffset);
                        ReaperSpawnedByDistance = true;
                    }
                }
            }
        }
        }

        protected virtual void OnEnable()
        {
            if (taskCtrl == null)
            {
                var echo = GetComponent<EchoController>();
                var skip = IsEchoActor && echo != null && echo.Type == EchoType.Combat;
                if (!skip)
                {
                    taskCtrl = TimelessEchoes.Tasks.TaskController.Instance
                               ?? GetComponent<TimelessEchoes.Tasks.TaskController>()
                               ?? GetComponentInParent<TimelessEchoes.Tasks.TaskController>()
                               ?? FindFirstObjectByType<TimelessEchoes.Tasks.TaskController>();
                }
            }

            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    Log("BuffManager missing", TELogCategory.Buff, this);
            }

            if (!IsEchoActor)
                buffController?.Resume();

            ReaperSpawnedByDistance = false;

            if (mapUI == null)
                mapUI = GameManager.Instance?.mapUIInstance;

            ApplyStatUpgrades();
            if (stats != null)
            {
                movementController?.UpdateMaxSpeed(HeroStatSystem.GetSnapshot().movementSpeed);
                var hp = Mathf.RoundToInt(HeroStatSystem.GetSnapshot().maxHealth);
                health?.Init(hp);
            }

            // Keep HeroHealth synchronized with centralized stat snapshot updates
            HeroStatSystem.OnStatsRecalculated += HandleHeroStatsRecalculated;

            // Hero no longer relocates to a task controller entry point
            if (animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                var offset = Random.value;
                animator.Play(stateInfo.fullPathHash, 0, offset);
                OnApplyRandomAnimatorOffset(stateInfo);
            }

            CurrentTask = null;
            state = State.Idle;
            combatController?.ResetState();
            movementController?.ResetState();

            var skillController = SkillController.Instance;
            if (!IsEchoActor && skillController != null)
                skillController.OnMilestoneDataChanged += OnMilestoneDataChangedHandler;

            AutoBuffChanged += OnAutoBuffChanged;
            OnAutoBuffChanged();

            if (!IsEchoActor)
                Enemy.OnEngage += OnEnemyEngage;

            // Throttle HUD distance updates via UITicker (5 Hz)
            if (!IsEchoActor && mapUI != null)
            {
                TimelessEchoes.UI.UITicker.Instance?.Subscribe(HudDistanceTick, 0.2f);
                // Push an immediate refresh on enable so HUD isn't stale
                HudDistanceTick();
            }
        }

        protected virtual void OnDisable()
        {
            if (CurrentTask is BaseTask baseTask)
                baseTask.ReleaseClaim(this);

            var skillController = SkillController.Instance;
            if (!IsEchoActor && skillController != null)
                skillController.OnMilestoneDataChanged -= OnMilestoneDataChangedHandler;

            AutoBuffChanged -= OnAutoBuffChanged;

            if (!IsEchoActor)
                Enemy.OnEngage -= OnEnemyEngage;

            // Unsubscribe from UITicker to avoid leaks when disabled
            if (!IsEchoActor)
                TimelessEchoes.UI.UITicker.Instance?.Unsubscribe(HudDistanceTick);

            // Unsubscribe to avoid leaks
            HeroStatSystem.OnStatsRecalculated -= HandleHeroStatsRecalculated;

            // Clear tracked enemies and their event subscriptions
            engagementTracker?.Clear();

            movementController?.Cleanup();
        }

        protected virtual void OnDestroy()
        {
            if (CurrentTask is BaseTask baseTask)
                baseTask.ReleaseClaim(this);

            // No-op for base; Instance is managed by HeroController subclass
            movementController?.Cleanup();

            var equip = EquipmentController.Instance ??
                        FindFirstObjectByType<EquipmentController>();
            if (equip != null)
                equip.OnEquipmentChanged -= RecalculateGearBonuses;
        }

        private enum State
        {
            Idle,
            MovingToTask,
            PerformingTask,
            Combat
        }

        private void OnAutoBuffChanged() => OnAutoBuffChangedImpl();

        private void HudDistanceTick()
        {
            if (mapUI != null)
                mapUI.UpdateDistance(transform.position.x);
        }

        /// <summary>
        /// Handle combat with the specified enemy. Delegates to combat controller.
        /// </summary>
        private void HandleCombat(Transform enemy)
        {
            var wasPerformingTask = state == State.PerformingTask;
            state = State.Combat;

            combatController?.HandleCombat(enemy, wasPerformingTask, () =>
            {
                // Release current task when entering combat
                if (CurrentTask != null)
                {
                    var releasedTask = CurrentTask;

                    if (!wasPerformingTask)
                        releasedTask.OnInterrupt(this);

                    if (releasedTask is BaseTask baseTask)
                        baseTask.ReleaseClaim(this);

                    CurrentTask = null;
                    currentTaskName = "None";
                    currentTaskObject = null;
                }
            });
        }

        private void OnEnemyEngage(TimelessEchoes.Enemies.Enemy enemy)
        {
            if (enemy == null)
                return;

            var hp = enemy.Health;
            if (hp == null || hp.CurrentHealth <= 0f)
            {
                engagementTracker?.UnregisterEnemy(enemy);
                combatController?.ClearCurrentEnemyIfMatch(enemy);
                return;
            }

            // Determine who the enemy is targeting at the time of engagement
            var dst = enemy.GetComponent<AIDestinationSetter>();
            var engagedTarget = dst != null ? dst.target : null;

            bool targetingMainHero = engagedTarget == transform;

            // Assist rule:
            // - If the enemy targets the main hero: assist regardless of distance.
            // - If the enemy targets an echo: only assist if the ENEMY is within
            //   assistEchoThreatRadius of the hero. While on a task, also require
            //   assistEchoWhileOnTask to be enabled.
            if (!targetingMainHero)
            {
                if (state == State.PerformingTask && !assistEchoWhileOnTask)
                    return;

                if (!enemy.TryGetTransformSafe(out var et))
                    return;
                var dist = Vector2.Distance(transform.position, et.position);
                if (dist > assistEchoThreatRadius)
                    return;
            }

            if (enemy.IsEngaged)
            {
                // Register with tracker (handles death/disengage subscriptions)
                engagementTracker?.RegisterEnemy(enemy, engagedTarget);
            }
            else
            {
                engagementTracker?.UnregisterEnemy(enemy);
                combatController?.ClearCurrentEnemyIfMatch(enemy);
                return;
            }

            if (!allowAttacks)
                return;

            // Check if already targeting a different enemy via combat controller
            var currentTarget = combatController?.CurrentTarget;
            if (currentTarget != null && currentTarget != enemy.transform)
                return;

            if (state == State.PerformingTask && CurrentTask != null)
                CurrentTask.OnInterrupt(this);

            HandleCombat(enemy.transform);
        }

        // Tasks orchestration
        public void SetTask(TimelessEchoes.Tasks.ITask task)
        {
            if (CurrentTask is TimelessEchoes.Tasks.BaseTask oldBase)
                oldBase.ReleaseClaim(this);

            Log($"Hero assigned task: {task?.GetType().Name ?? "None"}", TELogCategory.Task, this);
            CurrentTask = task;
            currentTaskName = task != null ? task.GetType().Name : "None";
            currentTaskObject = task as MonoBehaviour;
            state = State.Idle;

            var setter = movementController?.Setter;
            if (setter != null)
            {
                setter.target = task?.Target;
                if (task != null)
                    movementController.TeleportToCurrent();
            }

            if (task is TimelessEchoes.Tasks.BaseTask newBase)
                newBase.Claim(this);
        }

        public void ClearTaskController()
        {
            taskCtrl = null;
        }

        private void UpdateBehavior()
        {
            if (stats == null) return;

            var targetAnimSpeed = 1f;
            if (state == State.PerformingTask)
            {
                var bm = buffController != null ? buffController : TimelessEchoes.Buffs.BuffManager.Instance;
                if (bm != null)
                    targetAnimSpeed *= bm.TaskSpeedMultiplier;
            }
            if (animator != null)
                animator.speed = targetAnimSpeed;
            SetSecondaryAnimatorSpeed(targetAnimSpeed);

            // Clean up stale enemies (dead, disengaged, or null)
            engagementTracker?.CleanupStaleEnemies();

            // Update combat state via combat controller
            var stillInCombat = combatController?.UpdateCombat() ?? false;

            // Resolve nearest enemy target via combat controller
            var isPerformingTask = state == State.PerformingTask;
            var nearest = combatController?.ResolveNearestTarget(isPerformingTask, assistEchoWhileOnTask, assistEchoThreatRadius);

            // Try to engage enemy
            if (allowAttacks && nearest != null)
            {
                var engaged = combatController?.TryEngageEnemy(nearest, () =>
                {
                    if (isPerformingTask && CurrentTask != null)
                        CurrentTask.OnInterrupt(this);
                }) ?? false;

                if (engaged)
                {
                    state = State.Combat;
                    return;
                }
            }

            // Exit combat if no engaged enemies
            if (state == State.Combat && !stillInCombat)
            {
                combatController?.ExitCombat();
                state = State.Idle;
                taskCtrl?.SelectEarliestTask(this);
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
            {
                CurrentTask = null;
                state = State.Idle;
                taskCtrl?.SelectEarliestTask(this);
            }

            if (CurrentTask == null)
            {
                var noVisibleTasks = taskCtrl == null || !taskCtrl.HasVisibleTasksForHero(this);
                if (taskCtrl == null || taskCtrl.tasks.Count == 0 || (IsEchoActor && noVisibleTasks))
                    AutoAdvance();
                else
                    movementController?.ClearTarget();
                return;
            }

            var task = CurrentTask;
            var dest = task.Target;
            var setter = movementController?.Setter;
            if (setter != null && setter.target != dest)
                setter.target = dest;

            if (movementController != null && movementController.IsAtDestination(dest))
            {
                if (state != State.PerformingTask)
                {
                    state = State.PerformingTask;
                    movementController.SetSimulateMovement(!task.BlocksMovement);
                    task.OnArrival(this);
                    var bm = buffController != null ? buffController : TimelessEchoes.Buffs.BuffManager.Instance;
                    var speed = 1f;
                    if (bm != null) speed *= bm.TaskSpeedMultiplier;
                    if (animator != null) animator.speed = speed;
                    SetSecondaryAnimatorSpeed(speed);
                }

                if (task != null && task == CurrentTask && !task.IsComplete())
                {
                    task.Tick(this);
                }
            }
            else
            {
                state = State.MovingToTask;
                movementController?.SetSimulateMovement(true);
            }
        }

        // Stats accessors and updates
        public float Damage => HeroStatSystem.GetSnapshot().damage * CombatDamageMultiplier;
        public float BaseDamage => baseDamage + damageBonus + gearDamageBonus;
        public float AttackRate => HeroStatSystem.GetSnapshot().attacksPerSecond;
        public float MoveSpeed => HeroStatSystem.GetSnapshot().movementSpeed;
        public float MaxHealthValue => HeroStatSystem.GetSnapshot().maxHealth;
        private float CurrentAttackRate => HeroStatSystem.GetSnapshot().attacksPerSecond;
        public float Defense => HeroStatSystem.GetSnapshot().defense;

        private void OnMilestoneDataChangedHandler()
        {
            if (IsEchoActor)
                return;

            var skillController = SkillController.Instance;
            if (skillController == null)
                return;

            ApplyStatUpgrades();

            if (health == null)
                health = GetComponent<HeroHealth>();

            HeroStatSystem.MarkDirty(DirtyMask.All, DirtyReason.PerksChanged);
            var snapshot = HeroStatSystem.GetSnapshot();

            if (health != null)
            {
                var targetMax = Mathf.RoundToInt(snapshot.maxHealth);
                var currentMax = Mathf.RoundToInt(health.MaxHealth);
                if (targetMax != currentMax)
                    health.ApplyMaxHealthChange(targetMax, true);
            }
        }


        // Editor visualization for assist radius (main hero only)
        private void OnDrawGizmosSelected()
        {
            // Only visualize for the main hero, and only when assisting echoes while on task is enabled
            if (IsEchoActor)
                return;

            // If the field hasn't been serialized yet in edit mode, skip
            if (assistEchoThreatRadius <= 0f)
                return;

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.75f); // orange
            Gizmos.DrawWireSphere(transform.position, assistEchoThreatRadius);
        }

        private void ApplyStatUpgrades()
        {
            foreach (var stat in BaseStatService.AllStats)
            {
                if (stat == null)
                    continue;

                var baseValue = BaseStatService.GetBaseValue(stat);
                var totalValue = BaseStatService.GetTotalValue(stat);
                var bonus = totalValue - baseValue;

                switch (stat.name)
                {
                    case "Health":
                        baseHealth = baseValue;
                        healthBonus = bonus;
                        break;
                    case "Damage":
                        baseDamage = baseValue;
                        damageBonus = bonus;
                        break;
                    case "Attack Rate":
                        baseAttackSpeed = baseValue;
                        attackSpeedBonus = bonus;
                        break;
                    case "Move Speed":
                        baseMoveSpeed = baseValue;
                        moveSpeedBonus = bonus;
                        break;
                    case "Defense":
                        baseDefense = baseValue;
                        defenseBonus = bonus;
                        break;
                }
            }
        }

        private void RecalculateGearBonuses()
        {
            var equip = TimelessEchoes.Gear.EquipmentController.Instance ??
                        FindFirstObjectByType<TimelessEchoes.Gear.EquipmentController>();
            if (equip == null)
            {
                gearDamageBonus = gearAttackSpeedBonus = gearDefenseBonus = gearHealthBonus = gearMoveSpeedBonus = 0f;
                return;
            }

            gearDamageBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.Damage);
            gearAttackSpeedBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.AttackRate);
            gearDefenseBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.Defense);
            gearHealthBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.MaxHealth);
            gearMoveSpeedBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.MoveSpeed);

            HeroStatSystem.MarkDirty(
                DirtyMask.Damage | DirtyMask.AttackRate | DirtyMask.CritChance | DirtyMask.CritDamage | DirtyMask.Defense | DirtyMask.Move | DirtyMask.MaxHealth | DirtyMask.Regen,
                DirtyReason.EquipmentChanged);

            if (health != null)
            {
                var oldMax = Mathf.RoundToInt(health.MaxHealth);
                // Use the centralized stat snapshot so Infinity (cauldron) and all sources are included
                var newMax = Mathf.RoundToInt(HeroStatSystem.GetSnapshot().maxHealth);
                if (Mathf.Abs(newMax - oldMax) > 0.01f)
                    health.ApplyMaxHealthChange(newMax, true);
            }
        }

        // --- Stat synchronization --- ensure HeroHealth tracks HeroStatSystem max health
        private void HandleHeroStatsRecalculated(HeroStatsSnapshot snap)
        {
            if (health == null)
                health = GetComponent<HeroHealth>();
            if (health == null)
                return;

            var target = Mathf.RoundToInt(snap.maxHealth);
            var current = Mathf.RoundToInt(health.MaxHealth);
            if (target != current)
                health.ApplyMaxHealthChange(target, true);
        }

        // ==== Movement Methods (delegate to HeroMovementController) ====
        private void UpdateAnimation()
        {
            movementController?.UpdateAnimation(animator, spriteRenderer, fourDirectional,
                UpdateSecondaryAnimatorMovement, UpdateSecondarySpriteFlip);
        }

        public void SetActiveState(bool active)
        {
            movementController?.SetActiveState(active, animator);
        }

        public void SetDestination(Transform dest)
        {
            movementController?.SetDestination(dest);
        }

        public void SetDestinationReached()
        {
            movementController?.SetDestinationReached();
        }

        private void AutoAdvance()
        {
            // Echo follows main hero
            var mainHeroInstance = HeroController.Instance;
            if (movementController != null &&
                movementController.TryFollowMainHero(mainHeroInstance != null ? mainHeroInstance.transform : null))
                return;

            // Main hero tries combat fallback
            if (!IsEchoActor && allowAttacks && combatController != null)
            {
                var fallbackEnemy = combatController.FindFallbackEnemyTarget();
                if (fallbackEnemy != null)
                {
                    var engaged = combatController.TryEngageEnemy(fallbackEnemy, () =>
                    {
                        if (state == State.PerformingTask && CurrentTask != null)
                            CurrentTask.OnInterrupt(this);
                    });
                    if (engaged)
                    {
                        state = State.Combat;
                        return;
                    }
                }
            }

            // Otherwise, idle walk
            movementController?.IdleAdvance();
        }

        // Public API for main-hero-only secondary animator/visuals
        public void PlaySecondaryAnimation(string stateName) => OnPlaySecondaryAnimation(stateName);
        public void SetSecondaryTrigger(string triggerName) => OnSetSecondaryTrigger(triggerName);
        public void ResetSecondaryTrigger(string triggerName) => OnResetSecondaryTrigger(triggerName);

        /// <summary>
        /// Called by HeroCombatController when an attack animation starts.
        /// </summary>
        internal void OnAttackStarted() => OnAttackAnimationStarted();

        // Hooks for main-hero-only visual mirroring (no-op for echoes)
        protected virtual void OnPostAwakeAnimatorSetup() {}
        protected virtual void OnApplyRandomAnimatorOffset(AnimatorStateInfo stateInfo) {}
        protected virtual void OnAutoBuffChangedImpl() {}
        protected virtual void OnAttackAnimationStarted() {}
        protected virtual void SetSecondaryAnimatorSpeed(float speed) {}
        protected virtual void UpdateSecondaryAnimatorMovement(Vector2 lastMove, float speed) {}
        protected virtual void UpdateSecondarySpriteFlip(bool flipX) {}
        protected virtual void OnPlaySecondaryAnimation(string stateName) {}
        protected virtual void OnSetSecondaryTrigger(string triggerName) {}
        protected virtual void OnResetSecondaryTrigger(string triggerName) {}
    }
}
