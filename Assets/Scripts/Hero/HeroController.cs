#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;
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
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(HeroHealth))]
    /// <summary>
    /// Controls the main hero and echo clones: movement (A*), combat targeting and attacks,
    /// task interaction, stat application, and hooks into Buffs/Skills/Stats/UI.
    /// Exposes computed properties like Damage, AttackRate, MoveSpeed, Defense, and MaxHealthValue
    /// which include permanent upgrades and active buff multipliers.
    /// </summary>
    public partial class HeroController : MonoBehaviour
    {
        public static HeroController Instance { get; private set; }
        private static bool nextIsEcho;

        public static void PrepareForEcho()
        {
            nextIsEcho = true;
        }

        [HideInInspector] public bool IsEcho;
        [SerializeField] private HeroStats stats;
        [SerializeField] private Animator animator;

        [FormerlySerializedAs("autoBuffAnimator")] [SerializeField]
        public Animator AutoBuffAnimator;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer autoBuffSpriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private string diceQuestID = "Protect the Town";
        [SerializeField] private Skill combatSkill;

        [Header("Skill Indicators")] [SerializeField]
        private GameObject combatIndicator;

        [SerializeField] private GameObject miningIndicator;
        [SerializeField] private GameObject woodcuttingIndicator;
        [SerializeField] private GameObject fishingIndicator;
        [SerializeField] private GameObject farmingIndicator;
        [SerializeField] private GameObject lootingIndicator;
        [SerializeField] private GameObject echoDurationBar;
        [SerializeField] private SlicedFilledImage echoDurationFill;

        public GameObject CombatIndicator => combatIndicator;
        public GameObject MiningIndicator => miningIndicator;
        public GameObject WoodcuttingIndicator => woodcuttingIndicator;
        public GameObject FishingIndicator => fishingIndicator;
        public GameObject FarmingIndicator => farmingIndicator;
        public GameObject LootingIndicator => lootingIndicator;
        public GameObject EchoDurationBar => echoDurationBar;
        public SlicedFilledImage EchoDurationFill => echoDurationFill;
        private bool diceUnlocked;
        [SerializeField] private BuffManager buffController;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        [SerializeField] private bool allowAttacks = true;

        public bool AllowAttacks
        {
            get => allowAttacks;
            set => allowAttacks = value;
        }

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

        private readonly HashSet<Enemy> engagedEnemies = new();
        private readonly Dictionary<Enemy, Action> enemyDeathHandlers = new();
        private readonly Dictionary<Enemy, Action<Enemy>> enemyDisengageHandlers = new();
        private readonly List<Enemy> enemyRemovalBuffer = new();

        private AIPath ai;
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
        private AIDestinationSetter setter;
        private MapUI mapUI;
        
#if !DISABLESTEAMWORKS
        [SerializeField] private float richPresenceUpdateInterval = 2f;
        private float nextRichPresenceUpdate;
#endif

        private State state;

        private TaskController taskController;
        public ITask CurrentTask { get; private set; }
        public Animator Animator => animator;
        public bool InCombat => state == State.Combat;

        private void Awake()
        {
            if (nextIsEcho)
            {
                IsEcho = true;
                nextIsEcho = false;
            }

            if (!IsEcho)
            {
                if (Instance != null && Instance != this) Destroy(Instance.gameObject);
                Instance = this;
            }

            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<HeroHealth>();
            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    Log("BuffManager missing", TELogCategory.Buff, this);
            }

            if (taskController == null)
                taskController = GetComponentInParent<TaskController>();

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            diceUnlocked = QuestCompleted(diceQuestID);
            if (diceRoller != null)
                diceRoller.gameObject.SetActive(diceUnlocked);

            state = State.Idle;

            ApplyStatUpgrades();

            // Subscribe to equipment changes and initialize gear bonuses
            var equipInit = TimelessEchoes.Gear.EquipmentController.Instance ??
                            FindFirstObjectByType<TimelessEchoes.Gear.EquipmentController>();
            if (equipInit != null)
                equipInit.OnEquipmentChanged += RecalculateGearBonuses;
            RecalculateGearBonuses();

            if (stats != null)
            {
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus + gearMoveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus + gearHealthBonus);
                health?.Init(hp);
            }

            if (AutoBuffAnimator != null)
            {
                if (autoBuffSpriteRenderer == null)
                    autoBuffSpriteRenderer = AutoBuffAnimator.GetComponent<SpriteRenderer>();

                if (AutoBuffAnimator.gameObject.GetComponent<HeroAudio>() == null)
                    AutoBuffAnimator.gameObject.AddComponent<HeroAudio>();
            }
        }


        private void Update()
        {
            if (!logicActive)
                return;
            if (!IsEcho)
                BuffManager.Instance?.Tick(Time.deltaTime);
            if (stats != null)
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus + gearMoveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
            UpdateAnimation();
            UpdateBehavior();
            if (!IsEcho && mapUI != null)
                mapUI.UpdateDistance(transform.position.x);

            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
            {
                Log("GameplayStatTracker missing", TELogCategory.General, this);
            }
            else
            {
                if (!IsEcho)
                    tracker.RecordHeroPosition(transform.position);
                BuffManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
#if !DISABLESTEAMWORKS
                if (Time.unscaledTime >= nextRichPresenceUpdate)
                {
                    RichPresenceManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
                    nextRichPresenceUpdate = Time.unscaledTime + richPresenceUpdateInterval;
                }
#endif
                if (!IsEcho && !ReaperSpawnedByDistance &&
                    transform.position.x >= tracker.MaxRunDistance *
                        (buffController != null ? buffController.MaxDistanceMultiplier : 1f) +
                        (buffController != null ? buffController.MaxDistanceFlatBonus : 0f))
                {
                    var gm = GameManager.Instance;
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

        private void OnEnable()
        {
            if (taskController == null)
            {
                var echo = GetComponent<EchoController>();
                var skip = IsEcho && echo != null &&
                           (echo.Type == EchoType.Combat || echo.Type == EchoType.TaskOnly);
                if (!skip)
                    taskController = GetComponent<TaskController>() ?? GetComponentInParent<TaskController>();
            }

            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    Log("BuffManager missing", TELogCategory.Buff, this);
            }

            if (!IsEcho)
                buffController?.Resume();

            ReaperSpawnedByDistance = false;

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            ApplyStatUpgrades();
            if (stats != null)
            {
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus + gearMoveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus + gearHealthBonus);
                health?.Init(hp);
            }

            // Hero no longer relocates to a task controller entry point
            if (animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                var offset = Random.value;
                animator.Play(stateInfo.fullPathHash, 0, offset);
                if (AutoBuffAnimator != null && AutoBuffAnimator.isActiveAndEnabled)
                    AutoBuffAnimator.Play(stateInfo.fullPathHash, 0, offset);
            }

            CurrentTask = null;
            state = State.Idle;
            destinationOverride = false;
            lastAttack = Time.time - 1f / CurrentAttackRate;

            if (!IsEcho)
            {
                if (idleWalkTarget == null)
                {
                    idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                    idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
                }

                idleWalkTarget.position = transform.position;
            }

            var skillController = SkillController.Instance;
            if (!IsEcho && skillController != null)
                skillController.OnMilestoneUnlocked += OnMilestoneUnlocked;

            AutoBuffChanged += OnAutoBuffChanged;
            OnAutoBuffChanged();

            if (!IsEcho)
                Enemy.OnEngage += OnEnemyEngage;
        }

        private void OnDisable()
        {
            if (!IsEcho)
                buffController?.Pause();

            if (CurrentTask is BaseTask baseTask)
                baseTask.ReleaseClaim(this);

            var skillController = SkillController.Instance;
            if (!IsEcho && skillController != null)
                skillController.OnMilestoneUnlocked -= OnMilestoneUnlocked;

            AutoBuffChanged -= OnAutoBuffChanged;

            if (!IsEcho)
                Enemy.OnEngage -= OnEnemyEngage;

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

        private void OnDestroy()
        {
            if (CurrentTask is BaseTask baseTask)
                baseTask.ReleaseClaim(this);

            if (!IsEcho && Instance == this)
                Instance = null;

            if (idleWalkTarget != null)
                Destroy(idleWalkTarget.gameObject);

            var equip = TimelessEchoes.Gear.EquipmentController.Instance ??
                        FindFirstObjectByType<TimelessEchoes.Gear.EquipmentController>();
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

        private void OnAutoBuffChanged()
        {
            if (AutoBuffAnimator == null) return;
            var manager = BuffManager.Instance;
            var active = false;
            if (manager != null && !IsEcho)
            {
                active = manager.ActiveBuffs.Any(b =>
                    b.effects.Any(e =>
                        (e.type == BuffEffectType.MaxDistancePercent ||
                         e.type == BuffEffectType.MaxDistanceIncrease) && e.value > 0f));
            }
            AutoBuffAnimator.gameObject.SetActive(active);
            if (animator != null && AutoBuffAnimator.isActiveAndEnabled)
            {
                AutoBuffAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
                AutoBuffAnimator.avatar = animator.avatar;
                AutoBuffAnimator.updateMode = animator.updateMode;
                AutoBuffAnimator.speed = animator.speed;

                var state = animator.GetCurrentAnimatorStateInfo(0);
                AutoBuffAnimator.Play(state.fullPathHash, 0, state.normalizedTime);
            }
        }

    }
}