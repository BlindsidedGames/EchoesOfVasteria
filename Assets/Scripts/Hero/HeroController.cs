#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
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
using UnityEngine;
using UnityEngine.Serialization;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(HeroHealth))]
    public class HeroController : MonoBehaviour
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
        [SerializeField] private Skill combatSkill;
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

        private Transform currentEnemy;
        private Health currentEnemyHealth;

        private AIPath ai;
        private float attackSpeedBonus;
        private float baseAttackSpeed;

        private float baseDamage;
        private float baseDefense;
        private float baseHealth;
        private float baseMoveSpeed;
        private float combatDamageMultiplier = 1f;

        private bool logicActive = true;

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

        private State state;

        private TaskController taskController;
        public ITask CurrentTask { get; private set; }
        public Animator Animator => animator;
        public bool InCombat => state == State.Combat;

        /// <summary>
        ///     Current attack damage after upgrades, buffs and dice multipliers.
        /// </summary>
        public float Damage =>
            (baseDamage + damageBonus) *
            (buffController != null ? buffController.DamageMultiplier : 1f) *
            combatDamageMultiplier;

        /// <summary>
        ///     Base attack damage after permanent upgrades.
        /// </summary>
        public float BaseDamage => baseDamage + damageBonus;

        /// <summary>
        ///     Current attacks per second after upgrades and buffs.
        /// </summary>
        public float AttackRate => CurrentAttackRate;

        /// <summary>
        ///     Current movement speed after upgrades and buffs.
        /// </summary>
        public float MoveSpeed =>
            (baseMoveSpeed + moveSpeedBonus) *
            (buffController != null ? buffController.MoveSpeedMultiplier : 1f);

        /// <summary>
        ///     Maximum health after upgrades.
        /// </summary>
        public float MaxHealthValue => baseHealth + healthBonus;

        private float CurrentAttackRate =>
            (baseAttackSpeed + attackSpeedBonus) *
            (buffController != null ? buffController.AttackSpeedMultiplier : 1f);

        public float Defense =>
            (baseDefense + defenseBonus) *
            (buffController != null ? buffController.DefenseMultiplier : 1f);

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

            diceUnlocked = QuestCompleted("Dice");
            if (diceRoller != null)
                diceRoller.gameObject.SetActive(diceUnlocked);

            state = State.Idle;

            ApplyStatUpgrades();

            if (stats != null)
            {
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
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
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
            UpdateAnimation();
            UpdateBehavior();
            if (mapUI != null)
                mapUI.UpdateDistance(transform.position.x);

            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
            {
                Log("GameplayStatTracker missing", TELogCategory.General, this);
            }
            else
            {
                tracker.RecordHeroPosition(transform.position);
                BuffManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
#if !DISABLESTEAMWORKS
                RichPresenceManager.Instance?.UpdateDistance(tracker.CurrentRunDistance);
#endif
            }
        }

        private void OnEnable()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>() ?? GetComponentInParent<TaskController>();

            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    Log("BuffManager missing", TELogCategory.Buff, this);
            }

            if (!IsEcho)
                buffController?.Resume();

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            ApplyStatUpgrades();
            if (stats != null)
            {
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
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

            if (idleWalkTarget == null)
            {
                idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
            }

            idleWalkTarget.position = transform.position;

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
        }

        private void OnMilestoneUnlocked(Skill skill, MilestoneBonus milestone)
        {
            if (milestone != null && milestone.type == MilestoneType.StatIncrease)
            {
                var oldMax = health != null ? health.MaxHealth : 0f;
                var oldCurrent = health != null ? health.CurrentHealth : 0f;
                ApplyStatUpgrades();

                if (health != null)
                {
                    var newMax = Mathf.RoundToInt(baseHealth + healthBonus);
                    if (newMax > 0 && Mathf.Abs(newMax - oldMax) > 0.01f)
                    {
                        var newCurrent = Mathf.Min(oldCurrent + (newMax - oldMax), newMax);
                        health?.Init(newMax);
                        if (newCurrent < newMax && health != null)
                            health.TakeDamage(newMax - newCurrent);
                    }
                }
            }
        }

        private void ApplyStatUpgrades()
        {
            var controller = StatUpgradeController.Instance;
            if (controller == null)
                Log("StatUpgradeController missing", TELogCategory.Upgrade, this);
            var skillController = SkillController.Instance;
            if (skillController == null)
                Log("SkillController missing", TELogCategory.Upgrade, this);
            if (controller == null) return;

            foreach (var upgrade in controller.AllUpgrades)
            {
                if (upgrade == null) continue;
                var baseVal = controller.GetBaseValue(upgrade);
                var levelIncrease = controller.GetIncrease(upgrade);
                var flatBonus = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                var percentBonus = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;

                var totalBeforePercent = baseVal + levelIncrease + flatBonus;
                var finalValue = totalBeforePercent * (1f + percentBonus);
                var increase = finalValue - baseVal;
                switch (upgrade.name)
                {
                    case "Health":
                        baseHealth = baseVal;
                        healthBonus = increase;
                        break;
                    case "Damage":
                        baseDamage = baseVal;
                        damageBonus = increase;
                        break;
                    case "Attack Rate":
                        baseAttackSpeed = baseVal;
                        attackSpeedBonus = increase;
                        break;
                    case "Move Speed":
                        baseMoveSpeed = baseVal;
                        moveSpeedBonus = increase;
                        break;
                    case "Defense":
                        baseDefense = baseVal;
                        defenseBonus = increase;
                        break;
                }
            }
        }

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
                lastMoveDir = dir;

            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            animator.SetFloat("MoveMagnitude", vel.magnitude);

            if (AutoBuffAnimator != null && AutoBuffAnimator.isActiveAndEnabled)
            {
                AutoBuffAnimator.SetFloat("MoveX", lastMoveDir.x);
                AutoBuffAnimator.SetFloat("MoveY", lastMoveDir.y);
                AutoBuffAnimator.SetFloat("MoveMagnitude", vel.magnitude);
            }

            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;

            if (autoBuffSpriteRenderer != null)
                autoBuffSpriteRenderer.flipX = lastMoveDir.x < 0f;
        }

        public void SetTask(ITask task)
        {
            if (CurrentTask is BaseTask oldBase)
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
                if (ai != null)
                    ai.Teleport(transform.position);
                else
                    ai?.SearchPath();
            }

            if (task is BaseTask newBase)
                newBase.Claim(this);
        }

        private void UpdateBehavior()
        {
            if (stats == null) return;

            if (currentEnemy != null)
            {
                var hp = currentEnemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f)
                {
                    currentEnemyHealth?.SetHealthBarVisible(false);
                    currentEnemy = null;
                    currentEnemyHealth = null;
                }
            }

            var nearest = allowAttacks && currentEnemy != null ? currentEnemy : null;
            if (IsEcho && allowAttacks && nearest == null)
            {
                var range = UnlimitedAggroRange ? float.PositiveInfinity : stats.visionRange;
                nearest = FindNearestEnemy(range);
            }

            if (allowAttacks && nearest != null)
            {
                if (currentEnemy != nearest)
                {
                    currentEnemyHealth?.SetHealthBarVisible(false);
                    currentEnemy = nearest;
                    currentEnemyHealth = nearest.GetComponent<Health>();
                    currentEnemyHealth?.SetHealthBarVisible(true);
                }
                else if (currentEnemyHealth == null)
                {
                    currentEnemyHealth = nearest.GetComponent<Health>();
                    currentEnemyHealth?.SetHealthBarVisible(true);
                }

                if (state == State.PerformingTask && CurrentTask != null) CurrentTask.OnInterrupt(this);
                HandleCombat(nearest);
                return;
            }

            if (state == State.Combat)
            {
                Log("Hero exiting combat", TELogCategory.Combat, this);
                combatDamageMultiplier = 1f;
                isRolling = false;
                diceRoller?.ResetRoll();
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemyHealth = null;
                state = State.Idle;
                taskController?.SelectEarliestTask(this);
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
            {
                CurrentTask = null;
                state = State.Idle;
                taskController?.SelectEarliestTask(this);
            }

            if (CurrentTask == null)
            {
                if (taskController == null || taskController.tasks.Count == 0)
                    AutoAdvance();
                else
                    setter.target = null;
                return;
            }

            var dest = CurrentTask.Target;
            if (setter.target != dest) setter.target = dest;

            if (IsAtDestination(dest))
            {
                if (state != State.PerformingTask)
                {
                    state = State.PerformingTask;
                    ai.canMove = !CurrentTask.BlocksMovement;
                    CurrentTask.OnArrival(this);
                }

                CurrentTask.Tick(this);
            }
            else
            {
                state = State.MovingToTask;
                ai.canMove = true;
            }
        }


        private Transform FindNearestEnemy(float range)
        {
            Transform nearest = null;
            var best = float.MaxValue;
#if UNITY_6000_0_OR_NEWER
            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
#else
            var enemies = Object.FindObjectsOfType<Enemy>();
#endif
            Vector2 pos = transform.position;
            foreach (var enemy in enemies)
            {
                var hp = enemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                var d = Vector2.Distance(pos, enemy.transform.position);
                if (d <= range && d < best)
                {
                    best = d;
                    nearest = enemy.transform;
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

            if (state != State.Combat)
            {
                Log($"Hero entering combat with {enemy.name}", TELogCategory.Combat, this);
                if (diceUnlocked && diceRoller != null && !isRolling)
                {
                    var rate = CurrentAttackRate;
                    var cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            state = State.Combat;
            setter.target = enemy;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                var rate = CurrentAttackRate;
                var cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    lastMoveDir = enemy.position - transform.position;
                    Attack(enemy);
                    lastAttack = Time.time;
                }
            }
        }

        private IEnumerator RollForCombat(float duration)
        {
            if (!diceUnlocked || diceRoller == null)
                yield break;

            isRolling = true;
            lastAttack = Time.time;

            yield return StartCoroutine(diceRoller.Roll(duration));

            combatDamageMultiplier = 1f + 0.1f * diceRoller.Result;
            isRolling = false;
        }

        private static bool QuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(questId, out var rec) && rec.Completed;
        }

        private void OnEnemyEngage(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (!allowAttacks)
                return;

            if (!enemy.IsEngaged)
                return;

            if (currentEnemy != null && currentEnemy != enemy.transform)
                return;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f)
                return;

            if (currentEnemy == null)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = enemy.transform;
                currentEnemyHealth = hp;
                currentEnemyHealth.SetHealthBarVisible(true);
            }

            if (state == State.PerformingTask && CurrentTask != null)
                CurrentTask.OnInterrupt(this);

            HandleCombat(enemy.transform);
        }

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");
            if (AutoBuffAnimator != null && AutoBuffAnimator.isActiveAndEnabled)
                AutoBuffAnimator.Play("Attack");

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                var killTracker = EnemyKillTracker.Instance;
                if (killTracker == null)
                    Log("EnemyKillTracker missing", TELogCategory.Combat, this);
                var enemyStats = target.GetComponent<Enemy>()?.Stats;
                var bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                var dmgBase = (baseDamage + damageBonus) *
                              (buffController != null ? buffController.DamageMultiplier : 1f) *
                              combatDamageMultiplier;
                var total = dmgBase * bonus;
                var bonusDamage = total - dmgBase;
                proj.Init(target, total, true, null, combatSkill, bonusDamage);
            }
        }

        private enum State
        {
            Idle,
            MovingToTask,
            PerformingTask,
            Combat
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

        #region Pathfinding Helpers

        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            setter.target = dest;
            ai?.SearchPath();
        }

        [Button("Mark Destination Reached")]
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
            if (idleWalkTarget == null)
            {
                idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
            }

            var pos = transform.position;
            if (idleWalkTarget.position.x - pos.x < 1f)
                idleWalkTarget.position = new Vector3(pos.x + idleWalkStep, pos.y, pos.z);

            if (setter.target != idleWalkTarget)
                setter.target = idleWalkTarget;

            ai.canMove = true;
        }

        private void OnAutoBuffChanged()
        {
            if (AutoBuffAnimator == null) return;
            AutoBuffAnimator.gameObject.SetActive(AutoBuff && !IsEcho);
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

        #endregion
    }
}