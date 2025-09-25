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
using Blindsided.Utilities.Pooling;
using UnityEngine;
using UnityEngine.Serialization;
using static TimelessEchoes.TELogger;
using static TimelessEchoes.Quests.QuestUtils;
using static Blindsided.SaveData.StaticReferences;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    /// <summary>
    /// Controls the main hero and echo clones: movement (A*), combat targeting and attacks,
    /// task interaction, stat application, and hooks into Buffs/Skills/Stats/UI.
    /// Exposes computed properties like Damage, AttackRate, MoveSpeed, Defense, and MaxHealthValue
    /// which include permanent upgrades and active buff multipliers.
    /// </summary>
    public abstract class HeroBase : MonoBehaviour
    {
        public static event System.Action OnMainHeroDiceChanged;
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

        // Echo skill indicators are defined on EchoController only
        private bool diceUnlocked;
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

        private Transform currentEnemy;
        private Health currentEnemyHealth;
        private Enemy currentEnemyComp;

        private readonly HashSet<Enemy> engagedEnemies = new();
        private readonly Dictionary<Enemy, Action> enemyDeathHandlers = new();
        private readonly Dictionary<Enemy, Action<Enemy>> enemyDisengageHandlers = new();
        private readonly List<Enemy> enemyRemovalBuffer = new();
        private readonly Dictionary<Enemy, Transform> enemyTargets = new();

        [SerializeField] private AIPath ai;

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
        private float combatDamageMultiplier = 1f;
        public float CombatDamageMultiplier => combatDamageMultiplier;

        private bool logicActive = true;

        public bool ReaperSpawnedByDistance { get; private set; }

        private float damageBonus;
        private float defenseBonus;

        private bool destinationOverride;
        private HeroHealth health;
        private float healthBonus;

        [SerializeField] private float idleWalkStep = 5f;
        private Transform idleWalkTarget;

        private bool isRolling;
        private float lastAttack = float.NegativeInfinity;

        private Vector2 lastMoveDir = Vector2.down;
        private float moveSpeedBonus;
        [SerializeField] private AIDestinationSetter setter;
        private MapUI mapUI;

#if !DISABLESTEAMWORKS
        [SerializeField] private float richPresenceUpdateInterval = 2f;
        private float nextRichPresenceUpdate;
#endif

        private State state;

        [NonSerialized] protected TaskController taskCtrl;
        public ITask CurrentTask { get; private set; }
        public Animator Animator => animator;
        public bool InCombat => state == State.Combat;

        protected virtual void Awake()
        {
            if (ai == null)
                ai = GetComponent<AIPath>();
            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<HeroHealth>();
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

            diceUnlocked = QuestCompleted(diceQuestID);
            if (diceRoller != null)
                diceRoller.gameObject.SetActive(diceUnlocked);

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
                ai.maxSpeed = HeroStatSystem.GetSnapshot().movementSpeed;
                var hp = Mathf.RoundToInt(HeroStatSystem.GetSnapshot().maxHealth);
                health?.Init(hp);
            }

            // Initialize centralized hero stat system on scene load
            // Stat system is initialized by the concrete HeroController subclass

            OnPostAwakeAnimatorSetup();
        }


        protected virtual void Update()
        {
            if (!logicActive)
                return;
            if (stats != null)
                ai.maxSpeed = HeroStatSystem.GetSnapshot().movementSpeed;
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
                ai.maxSpeed = HeroStatSystem.GetSnapshot().movementSpeed;
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
            destinationOverride = false;
            lastAttack = Time.time - 1f / CurrentAttackRate;

            if (!IsEchoActor)
            {
                if (idleWalkTarget == null)
                {
                    idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                    idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
                }

                idleWalkTarget.position = transform.position;
            }

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

            foreach (var enemy in engagedEnemies)
            {
                if (enemyDeathHandlers.TryGetValue(enemy, out var death))
                {
                    var hp = enemy != null ? enemy.GetComponent<Health>() : null;
                    if (hp != null)
                        hp.OnDeath -= death;
                }

                if (enemyDisengageHandlers.TryGetValue(enemy, out var disengage))
                    Enemy.OnEngage -= disengage;
            }

            engagedEnemies.Clear();
            enemyDeathHandlers.Clear();
            enemyDisengageHandlers.Clear();

            if (idleWalkTarget != null)
                Destroy(idleWalkTarget.gameObject);
            idleWalkTarget = null;
        }

        protected virtual void OnDestroy()
        {
            if (CurrentTask is BaseTask baseTask)
                baseTask.ReleaseClaim(this);

            // No-op for base; Instance is managed by HeroController subclass

            if (idleWalkTarget != null)
                Destroy(idleWalkTarget.gameObject);

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

        // Combat (selected key methods needed internally)
        private float EstimateDpsAgainst(TimelessEchoes.Enemies.Enemy enemy, HeroBase attacker)
        {
            if (enemy == null || attacker == null)
                return 0f;
            if (!attacker.AllowAttacks || attacker.stats == null)
                return 0f;

            Transform et; try { et = enemy.transform; } catch { et = null; }
            if (et == null) return 0f;

            var snap = HeroStatSystem.GetSnapshot();
            var killTracker = TimelessEchoes.Stats.EnemyKillTracker.Instance;
            var enemyStats = enemy.Stats;
            float killMult = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
            float critChance = Mathf.Clamp01(snap.critChancePercent / 100f);
            float critDamageMultiplier = 1f + Mathf.Max(0f, snap.critDamagePercent) / 100f;
            float expectedCritFactor = 1f + critChance * (critDamageMultiplier - 1f);
            float perHitBeforeDefense = snap.damage * attacker.CombatDamageMultiplier * killMult * expectedCritFactor;
            float defense = enemy.GetDefense();
            float perHitAfterDefense = TimelessEchoes.Combat.ApplyDefense(perHitBeforeDefense, defense);
            float attacksPerSecond = Mathf.Max(0f, snap.attacksPerSecond);
            return perHitAfterDefense * attacksPerSecond;
        }

        private void HudDistanceTick()
        {
            if (mapUI != null)
                mapUI.UpdateDistance(transform.position.x);
        }

        private float EstimatePerHitAgainst(TimelessEchoes.Enemies.Enemy enemy, HeroBase attacker)
        {
            if (enemy == null || attacker == null) return 0f;
            if (!attacker.AllowAttacks || attacker.stats == null) return 0f;
            var snap = HeroStatSystem.GetSnapshot();
            var killTracker = TimelessEchoes.Stats.EnemyKillTracker.Instance;
            var enemyStats = enemy.Stats;
            float killMult = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
            float perHitBeforeDefense = snap.damage * attacker.CombatDamageMultiplier * killMult;
            float defense = enemy.GetDefense();
            return TimelessEchoes.Combat.ApplyDefense(perHitBeforeDefense, defense);
        }

        private float EstimateCombinedDps(Transform enemyTransform)
        {
            if (enemyTransform == null)
                return 0f;

            var enemy = enemyTransform.GetComponent<TimelessEchoes.Enemies.Enemy>();
            if (enemy == null)
                return 0f;

            float dps = 0f;

            // Main hero if targeting this enemy
            var main = HeroController.Instance;
            if (main != null && (object)main != this)
            {
                Transform t = null;
                Transform st = null;
                try { t = main.currentEnemy; } catch { t = null; }
                try { st = main.setter != null ? main.setter.target : null; } catch { st = null; }
                if ((t != null && t == enemyTransform) || (st != null && st == enemyTransform))
                {
                    var add = EstimateDpsAgainst(enemy, main);
                    dps += add;
                }
            }

            // Other combat-enabled echoes targeting this enemy
            foreach (var echo in EchoController.CombatEchoes)
            {
                if (echo == null || !echo.isActiveAndEnabled) continue;
                var hc = echo as HeroBase;
                if (hc == null || (object)hc == this) continue;

                Transform t = null;
                Transform st = null;
                try { t = hc.currentEnemy; } catch { t = null; }
                try { st = hc.setter != null ? hc.setter.target : null; } catch { st = null; }
                if ((t != null && t == enemyTransform) || (st != null && st == enemyTransform))
                {
                    var add = EstimateDpsAgainst(enemy, hc);
                    dps += add;
                }
            }

            return dps;
        }

        private Transform FindNearestEnemyTimeAware(float range, float thresholdSec)
        {
            if (thresholdSec <= 0f)
                return FindNearestEnemy(range);

            Transform nearest = null;
            var best = float.MaxValue;
            var enemies = TimelessEchoes.Enemies.EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;
            Vector2 pos = transform.position;

            var cam = TimelessEchoes.Enemies.EnemyActivator.Instance != null
                ? TimelessEchoes.Enemies.EnemyActivator.Instance.GetComponent<Camera>()
                : null;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var checkBounds = false;
            if (cam != null)
            {
                const float padding = 2f;
                min = cam.ViewportToWorldPoint(Vector3.zero) - Vector3.one * padding;
                max = cam.ViewportToWorldPoint(Vector3.one) + Vector3.one * padding;
                checkBounds = true;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Transform enemyTransform = null;
                try { enemyTransform = enemy.transform; }
                catch { continue; }

                if (enemyTransform == null) continue;

                if (checkBounds)
                {
                    var p = enemyTransform.position;
                    if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y)
                        continue;
                }

                var hp = enemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                var d = Vector2.Distance(pos, enemyTransform.position);
                if (d > range) continue;

                var combinedDps = EstimateCombinedDps(enemyTransform);
                if (combinedDps > 0f)
                {
                    var maxHp = Mathf.Max(0.0001f, hp.MaxHealth);
                    var ttk = maxHp / combinedDps;
                    if (ttk <= thresholdSec)
                        continue;
                }

                if (d < best)
                {
                    best = d;
                    nearest = enemyTransform;
                }
            }

            if (nearest == null)
                return FindNearestEnemy(range);
            return nearest;
        }

        private Transform FindNearestEnemy(float range)
        {
            Transform nearest = null;
            var best = float.MaxValue;
            var enemies = TimelessEchoes.Enemies.EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;
            Vector2 pos = transform.position;

            var cam = TimelessEchoes.Enemies.EnemyActivator.Instance != null
                ? TimelessEchoes.Enemies.EnemyActivator.Instance.GetComponent<Camera>()
                : null;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var checkBounds = false;
            if (cam != null)
            {
                const float padding = 2f;
                min = cam.ViewportToWorldPoint(Vector3.zero) - Vector3.one * padding;
                max = cam.ViewportToWorldPoint(Vector3.one) + Vector3.one * padding;
                checkBounds = true;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Transform enemyTransform = null;
                try { enemyTransform = enemy.transform; }
                catch { continue; }

                if (enemyTransform == null) continue;

                if (checkBounds)
                {
                    var p = enemyTransform.position;
                    if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y)
                        continue;
                }

                var hp = enemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                var d = Vector2.Distance(pos, enemyTransform.position);
                if (d <= range && d < best)
                {
                    best = d;
                    nearest = enemyTransform;
                }
            }

            return nearest;
        }

        private Transform FindNearestEnemy()
        {
            return FindNearestEnemy(stats.visionRange);
        }

        private void HandleCombat(Transform enemy)
        {
            ai.canMove = true;

            var wasPerformingTask = state == State.PerformingTask;
            if (state != State.Combat)
            {
                Log($"Hero entering combat with {enemy.name}", TELogCategory.Combat, this);
                if (diceUnlocked && diceRoller != null && !isRolling)
                {
                    var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                    var cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            // If the main hero already had a task, release it so echoes can keep working during combat
            if (!IsEchoActor && CurrentTask != null)
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

            state = State.Combat;
            setter.target = enemy;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                var cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    lastMoveDir = enemy.position - transform.position;
                    Attack(enemy);
                    lastAttack = Time.time;
                }
            }
        }

        private System.Collections.IEnumerator RollForCombat(float duration)
        {
            if (!diceUnlocked || diceRoller == null)
                yield break;

            isRolling = true;
            lastAttack = Time.time;

            yield return StartCoroutine(diceRoller.Roll(duration));

            combatDamageMultiplier = 1f + 0.1f * diceRoller.Result;
            isRolling = false;

            if (!IsEchoActor)
            {
                HeroStatSystem.MarkDirty(DirtyMask.Damage, DirtyReason.DiceUsed);
                HeroStatSystem.ForceRunStartRefresh();
                OnMainHeroDiceChanged?.Invoke();
            }
        }

        private void OnEnemyEngage(TimelessEchoes.Enemies.Enemy enemy)
        {
            if (enemy == null)
                return;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f)
            {
                UnregisterEngagedEnemy(enemy);
                return;
            }

            // Determine who the enemy is targeting at the time of engagement
            Transform engagedTarget = null;
            try
            {
                var dst = enemy.GetComponent<AIDestinationSetter>();
                engagedTarget = dst != null ? dst.target : null;
            }
            catch { engagedTarget = null; }

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

                Transform et = null;
                try { et = enemy.transform; } catch { et = null; }
                if (et == null)
                    return;
                var dist = Vector2.Distance(transform.position, et.position);
                if (dist > assistEchoThreatRadius)
                    return;
            }

            if (enemy.IsEngaged)
            {
                if (!engagedEnemies.Contains(enemy))
                {
                    engagedEnemies.Add(enemy);
                    // Track the target we saw at engagement time
                    enemyTargets[enemy] = engagedTarget;

                    System.Action deathHandler = () => UnregisterEngagedEnemy(enemy);
                    hp.OnDeath += deathHandler;
                    enemyDeathHandlers[enemy] = deathHandler;

                    System.Action<TimelessEchoes.Enemies.Enemy> disengageHandler = null;
                    disengageHandler = e =>
                    {
                        if (e == enemy && !e.IsEngaged)
                            UnregisterEngagedEnemy(enemy);
                    };
                    TimelessEchoes.Enemies.Enemy.OnEngage += disengageHandler;
                    enemyDisengageHandlers[enemy] = disengageHandler;
                }
                else
                {
                    // Update the known target on re-engagement
                    enemyTargets[enemy] = engagedTarget;
                }
            }
            else
            {
                UnregisterEngagedEnemy(enemy);
                return;
            }

            if (!allowAttacks)
                return;

            if (currentEnemy != null && currentEnemy != enemy.transform)
                return;

            if (currentEnemy == null)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = enemy.transform;
                currentEnemyHealth = hp;
                currentEnemyHealth.SetHealthBarVisible(true);
                if (currentEnemyComp == null)
                {
                    try { currentEnemyComp = enemy; } catch { currentEnemyComp = null; }
                }
            }

            if (state == State.PerformingTask && CurrentTask != null)
                CurrentTask.OnInterrupt(this);

            HandleCombat(enemy.transform);
        }

        private void UnregisterEngagedEnemy(TimelessEchoes.Enemies.Enemy enemy)
        {
            if (enemy == null)
                return;

            if (engagedEnemies.Remove(enemy))
            {
                if (enemyTargets.ContainsKey(enemy))
                    enemyTargets.Remove(enemy);
                if (enemyDeathHandlers.TryGetValue(enemy, out var death))
                {
                    var hp = enemy.GetComponent<Health>();
                    if (hp != null)
                        hp.OnDeath -= death;
                    enemyDeathHandlers.Remove(enemy);
                }

                if (enemyDisengageHandlers.TryGetValue(enemy, out var disengage))
                {
                    TimelessEchoes.Enemies.Enemy.OnEngage -= disengage;
                    enemyDisengageHandlers.Remove(enemy);
                }
            }

            Transform enemyTransformSafe = null;
            try { enemyTransformSafe = enemy.transform; } catch { enemyTransformSafe = null; }

            if (enemyTransformSafe != null && currentEnemy == enemyTransformSafe)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = null;
                currentEnemyHealth = null;
                currentEnemyComp = null;
            }
        }

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;
            var go = target.gameObject;
            // Do not attack targets that are inactive or already returned to the pool
            if (go == null || !go.activeInHierarchy)
                return;
            var pooled = go.GetComponent<PooledObject>();
            if (pooled != null && pooled.inPool)
                return;

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");
            OnAttackAnimationStarted();

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Blindsided.Utilities.Pooling.PoolManager.Get(stats.projectilePrefab);
            projObj.transform.position = origin.position;
            projObj.transform.rotation = Quaternion.identity;
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                var killTracker = TimelessEchoes.Stats.EnemyKillTracker.Instance;
                if (killTracker == null)
                    Log("EnemyKillTracker missing", TELogCategory.Combat, this);
                var enemyStats = target.GetComponent<TimelessEchoes.Enemies.Enemy>()?.Stats;
                var bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                var snap = HeroStatSystem.GetSnapshot();
                var dmgBase = snap.damage * combatDamageMultiplier;
                var critDamageMultiplier = 1f + Mathf.Max(0f, snap.critDamagePercent) / 100f;
                var total = dmgBase * bonus;

                var critChance = Mathf.Clamp01(snap.critChancePercent / 100f);
                var isCritical = false;
                if (critChance > 0f && UnityEngine.Random.value < Mathf.Clamp01(critChance))
                {
                    total *= critDamageMultiplier;
                    isCritical = true;
                    var tracker = GameplayStatTracker.Instance ??
                                  FindFirstObjectByType<GameplayStatTracker>();
                    tracker?.AddCriticalHit();
                }

                var bonusDamage = total - dmgBase;
                proj.Init(target, total, true, null, combatSkill, bonusDamage, isCritical);
            }
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

            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();

            if (setter != null)
            {
                setter.target = task?.Target;
                if (task != null)
                {
                    if (ai != null)
                        ai.Teleport(transform.position);
                    else
                        ai?.SearchPath();
                }
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

            enemyRemovalBuffer.Clear();
            foreach (var enemy in engagedEnemies)
            {
                var hp = enemy != null ? enemy.GetComponent<Health>() : null;
                if (enemy == null || hp == null || hp.CurrentHealth <= 0f || !enemy.IsEngaged)
                    enemyRemovalBuffer.Add(enemy);
            }

            foreach (var enemy in enemyRemovalBuffer)
                UnregisterEngagedEnemy(enemy);

            if (currentEnemy != null)
            {
                var hp = currentEnemyHealth;
                if (hp == null)
                {
                    hp = currentEnemy.GetComponent<Health>();
                    currentEnemyHealth = hp;
                }
                var enemyComp = currentEnemyComp;
                if (enemyComp == null)
                {
                    enemyComp = currentEnemy.GetComponent<TimelessEchoes.Enemies.Enemy>();
                    currentEnemyComp = enemyComp;
                }

                // Clear stale targets if enemy is inactive or pooled
                var enemyGo = currentEnemy != null ? currentEnemy.gameObject : null;
                var isInactive = enemyGo == null || !enemyGo.activeInHierarchy || (enemyComp != null && !enemyComp.isActiveAndEnabled);
                var pooledMarker = enemyGo != null ? enemyGo.GetComponent<PooledObject>() : null;
                var isPooled = pooledMarker != null && pooledMarker.inPool;

                if (hp == null || hp.CurrentHealth <= 0f || enemyComp == null || (!IsEchoActor && !engagedEnemies.Contains(enemyComp)) || isInactive || isPooled)
                {
                    currentEnemyHealth?.SetHealthBarVisible(false);
                    currentEnemy = null;
                    currentEnemyHealth = null;
                    currentEnemyComp = null;
                }
                else if (IsEchoActor && allowAttacks)
                {
                    var otherDps = EstimateCombinedDps(currentEnemy);
                    if (otherDps > 0f)
                    {
                        var enemy = currentEnemyComp;
                        float perHit = EstimatePerHitAgainst(enemy, this);
                        if (perHit > 0f && hp.CurrentHealth <= perHit)
                        {
                            currentEnemyHealth?.SetHealthBarVisible(false);
                            currentEnemy = null;
                            currentEnemyHealth = null;
                            currentEnemyComp = null;
                        }
                    }
                }
            }

            var nearest = ResolveNearestEnemyTarget();

            if (allowAttacks && nearest != null && TryEngageEnemy(nearest))
                return;
            if (state == State.Combat && engagedEnemies.Count == 0)
            {
                Log("Hero exiting combat", TELogCategory.Combat, this);
                combatDamageMultiplier = 1f;
                isRolling = false;
                diceRoller?.ResetRoll();
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemyHealth = null;
                currentEnemyComp = null;
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
                    setter.target = null;
                return;
            }

            var task = CurrentTask;
            var dest = task.Target;
            if (setter.target != dest) setter.target = dest;

            if (IsAtDestination(dest))
            {
                if (state != State.PerformingTask)
                {
                    state = State.PerformingTask;
                    ai.canMove = !task.BlocksMovement;
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
                ai.canMove = true;
            }
        }

        private Transform ResolveNearestEnemyTarget()
        {
            var nearest = currentEnemy;

            if (!allowAttacks || stats == null)
                return nearest;

            if (nearest != null)
                return nearest;

            if (engagedEnemies.Count > 0)
            {
                var best = float.PositiveInfinity;
                TimelessEchoes.Enemies.Enemy chosen = null;
                foreach (var enemy in engagedEnemies)
                {
                    if (enemy == null) continue;
                    Transform enemyTransform = null;
                    try { enemyTransform = enemy.transform; } catch { continue; }
                    if (enemyTransform == null) continue;

                    var ok = true;
                    if (!enemyTargets.TryGetValue(enemy, out var target))
                        target = null;

                    if (target != transform)
                    {
                        if (state == State.PerformingTask && !assistEchoWhileOnTask)
                        {
                            ok = false;
                        }
                        else
                        {
                            var distance = Vector2.Distance(transform.position, enemyTransform.position);
                            ok = distance <= assistEchoThreatRadius;
                        }
                    }

                    if (!ok)
                        continue;

                    var dist = Vector2.Distance(transform.position, enemyTransform.position);
                    if (dist < best)
                    {
                        best = dist;
                        chosen = enemy;
                    }
                }

                if (chosen != null)
                {
                    try { nearest = chosen.transform; }
                    catch { nearest = null; }
                }
                else
                {
                    nearest = null;
                }
            }
            else
            {
                var range = UnlimitedAggroRange ? combatAggroRange : stats.visionRange;
                nearest = IsEchoActor
                    ? FindNearestEnemyTimeAware(range, EchoAvoidIfTTKBelowSeconds)
                    : FindNearestEnemy(range);
            }

            return nearest;
        }

        private bool TryEngageEnemy(Transform enemy)
        {
            if (!allowAttacks || stats == null || enemy == null)
                return false;

            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();
            if (ai == null)
                ai = GetComponent<AIPath>();

            if (setter == null || ai == null)
                return false;

            if (currentEnemy != enemy)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = enemy;
                currentEnemyHealth = enemy.GetComponent<Health>();
                currentEnemyComp = enemy.GetComponent<TimelessEchoes.Enemies.Enemy>();
            }
            else if (currentEnemyHealth == null)
            {
                currentEnemyHealth = enemy.GetComponent<Health>();
                if (currentEnemyComp == null)
                    currentEnemyComp = enemy.GetComponent<TimelessEchoes.Enemies.Enemy>();
            }

            setter.target = enemy;
            ai.canMove = true;

            var enemyComp = currentEnemyComp != null
                ? currentEnemyComp
                : enemy.GetComponent<TimelessEchoes.Enemies.Enemy>();

            var enemyEngaged = enemyComp != null && engagedEnemies.Contains(enemyComp);
            var range = UnlimitedAggroRange ? combatAggroRange : stats.visionRange;
            var delta = (Vector2)(enemy.position - transform.position);
            var withinRange = delta.sqrMagnitude <= range * range;

            if (!enemyEngaged && !withinRange)
                return true;

            if (currentEnemyHealth != null)
                currentEnemyHealth.SetHealthBarVisible(true);

            if (state == State.PerformingTask && CurrentTask != null)
                CurrentTask.OnInterrupt(this);

            HandleCombat(enemy);
            return true;
        }

        private Transform FindFallbackEnemyTarget()
        {
            var enemies = TimelessEchoes.Enemies.EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;

            var origin = (Vector2)transform.position;
            Transform nearest = null;
            var bestSqr = float.PositiveInfinity;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Transform enemyTransform = null;
                try { enemyTransform = enemy.transform; } catch { continue; }
                if (enemyTransform == null) continue;

                var hp = enemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;

                var offset = (Vector2)enemyTransform.position - origin;
                var sqr = offset.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = enemyTransform;
                }
            }

            return nearest;
        }
        // Stats accessors and updates
        public float Damage => HeroStatSystem.GetSnapshot().damage * combatDamageMultiplier;
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

            var oldMax = health != null ? health.MaxHealth : 0f;
            var oldCurrent = health != null ? health.CurrentHealth : 0f;
            ApplyStatUpgrades();

            if (health != null)
            {
                var newMax = Mathf.RoundToInt(baseHealth + healthBonus);
                if (Mathf.Abs(newMax - oldMax) > 0.01f)
                    health.ApplyMaxHealthChange(newMax, true);
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

        // ==== Inlined from partials (Movement, Combat, Tasks, Stats) ====
        // Movement
        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            var dir = vel;

            if (dir.sqrMagnitude < 0.0001f && setter != null && setter.target != null)
                dir = setter.target.position - transform.position;

            if (fourDirectional)
            {
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    dir.y = 0f;
                else
                    dir.x = 0f;
            }
            else
            {
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir.normalized;

            var speed = vel.magnitude;

            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
                animator.SetFloat("MoveMagnitude", speed);
            }

            UpdateSecondaryAnimatorMovement(lastMoveDir, speed);

            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;

            UpdateSecondarySpriteFlip(lastMoveDir.x < 0f);

        }

        public void SetActiveState(bool active)
        {
            if (ai != null) ai.enabled = active;
            if (setter != null) setter.enabled = active;
            logicActive = active;

            if (!active && animator != null)
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 0f);
                animator.SetFloat("MoveMagnitude", 0f);
            }
        }

        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            setter.target = dest;
            ai?.SearchPath();
        }

        public void SetDestinationReached()
        {
            destinationOverride = true;
        }

        private bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null) return false;
            if (destinationOverride) return true;
            if (ai.reachedDestination || ai.reachedEndOfPath) return true;

            var threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }

        private void AutoAdvance()
        {
            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();
            if (ai == null)
                ai = GetComponent<AIPath>();

            if (IsEchoActor && HeroController.Instance != null && (object)HeroController.Instance != this)
            {
                var mainHero = HeroController.Instance.transform;

                if (setter.target != mainHero)
                {
                    setter.target = mainHero;
                    ai?.SearchPath();
                }

                ai.canMove = true;
                return;
            }

            if (!IsEchoActor && allowAttacks)
            {
                var fallbackEnemy = FindFallbackEnemyTarget();
                if (fallbackEnemy != null && TryEngageEnemy(fallbackEnemy))
                    return;
            }
            if (idleWalkTarget == null)
            {
                idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
            }

            var pos = transform.position;
            if (idleWalkTarget.position.x - pos.x < 1f)
            {
                var nextIdle = ResolveIdleAdvanceTarget(pos);
                idleWalkTarget.position = nextIdle;
            }

            if (setter.target != idleWalkTarget)
            {
                setter.target = idleWalkTarget;
                ai?.SearchPath();
            }

            ai.canMove = true;
        }

        private Vector3 ResolveIdleAdvanceTarget(Vector3 currentPosition)
        {
            var fallback = new Vector3(currentPosition.x + idleWalkStep, currentPosition.y, currentPosition.z);
            var pathfinder = AstarPath.active;
            if (pathfinder == null)
                return fallback;

            var constraint = NNConstraint.Walkable;
            constraint.constrainTags = false;

            var startInfo = pathfinder.GetNearest(currentPosition, constraint);
            var startNode = startInfo.node;
            if (startNode == null || !startNode.Walkable)
                return fallback;

            var verticalStep = Mathf.Max(0.5f, Mathf.Abs(idleWalkStep) * 0.5f);
            var candidates = new[]
            {
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y + verticalStep, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y - verticalStep, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y + verticalStep * 2f, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y - verticalStep * 2f, currentPosition.z)
            };

            foreach (var candidate in candidates)
            {
                var goalInfo = pathfinder.GetNearest(candidate, constraint);
                var node = goalInfo.node;
                if (node == null || !node.Walkable)
                    continue;

                if (!PathUtilities.IsPathPossible(startNode, node))
                    continue;

                var resolved = goalInfo.position;
                resolved.z = currentPosition.z;

                if (resolved.x < currentPosition.x)
                    continue;

                return resolved;
            }

            return fallback;
        }
        // Public API for main-hero-only secondary animator/visuals
        public void PlaySecondaryAnimation(string stateName) => OnPlaySecondaryAnimation(stateName);
        public void SetSecondaryTrigger(string triggerName) => OnSetSecondaryTrigger(triggerName);
        public void ResetSecondaryTrigger(string triggerName) => OnResetSecondaryTrigger(triggerName);

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
