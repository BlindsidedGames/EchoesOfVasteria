using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using TimelessEchoes.Enemies;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Stats;
using TimelessEchoes.UI;
using TimelessEchoes.Buffs;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    public class HeroController : MonoBehaviour
    {
        [SerializeField] private HeroStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private TimelessEchoes.Skills.Skill combatSkill;
        private bool diceUnlocked;
        [SerializeField] private TimelessEchoes.Buffs.BuffManager buffController;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        private readonly bool allowAttacks = true;

        private Transform currentEnemy;

        private AIPath ai;
        private float attackSpeedBonus;
        private float baseAttackSpeed;

        private float baseDamage;
        private float baseDefense;
        private float baseHealth;
        private float baseMoveSpeed;
        private float combatDamageMultiplier = 1f;

        private float damageBonus;
        private float defenseBonus;

        private bool destinationOverride;
        private Health health;
        private float healthBonus;

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
        /// Current attack damage after upgrades, buffs and dice multipliers.
        /// </summary>
        public float Damage =>
            (baseDamage + damageBonus) *
            (buffController != null ? buffController.DamageMultiplier : 1f) *
            combatDamageMultiplier;

        /// <summary>
        /// Current attacks per second after upgrades and buffs.
        /// </summary>
        public float AttackRate => CurrentAttackRate;

        /// <summary>
        /// Current movement speed after upgrades and buffs.
        /// </summary>
        public float MoveSpeed =>
            (baseMoveSpeed + moveSpeedBonus) *
            (buffController != null ? buffController.MoveSpeedMultiplier : 1f);

        /// <summary>
        /// Maximum health after upgrades.
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
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    TELogger.Log("BuffManager missing", TELogCategory.Buff, this);
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
                health.Init(hp);
            }
        }


        private void Update()
        {
            BuffManager.Instance?.Tick(Time.deltaTime);
            if (stats != null)
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
            UpdateAnimation();
            UpdateBehavior();
            if (mapUI != null)
                mapUI.UpdateDistance(transform.position.x);

            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (tracker == null)
                TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
            else
                tracker.RecordHeroPosition(transform.position);
        }

        private void OnEnable()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();

            if (buffController == null)
            {
                buffController = BuffManager.Instance;
                if (buffController == null)
                    TELogger.Log("BuffManager missing", TELogCategory.Buff, this);
            }
            buffController?.Resume();

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            ApplyStatUpgrades();
            if (stats != null)
            {
                ai.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
                health.Init(hp);
            }

            // Hero no longer relocates to a task controller entry point
            if (animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(stateInfo.fullPathHash, 0, Random.value);
            }

            CurrentTask = null;
            state = State.Idle;
            destinationOverride = false;
            lastAttack = Time.time - 1f / CurrentAttackRate;
        }

        private void OnDisable()
        {
            buffController?.Pause();
        }

        private void ApplyStatUpgrades()
        {
            var controller = StatUpgradeController.Instance;
            if (controller == null)
                TELogger.Log("StatUpgradeController missing", TELogCategory.Upgrade, this);
            var skillController = TimelessEchoes.Skills.SkillController.Instance;
            if (skillController == null)
                TELogger.Log("SkillController missing", TELogCategory.Upgrade, this);
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
                    case "Attack Speed":
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
            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;
        }

        public void SetTask(ITask task)
        {
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
        }

        private void UpdateBehavior()
        {
            if (stats == null) return;

            if (currentEnemy != null)
            {
                var hp = currentEnemy.GetComponent<Health>();
                var dist = Vector2.Distance(transform.position, currentEnemy.position);
                if (hp == null || hp.CurrentHealth <= 0f || dist > stats.visionRange)
                    currentEnemy = null;
            }

            var nearest = currentEnemy != null ? currentEnemy : FindNearestEnemy();
            if (nearest != null)
            {
                currentEnemy = nearest;
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
                state = State.Idle;
                taskController?.SelectEarliestTask();
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
            {
                CurrentTask = null;
                state = State.Idle;
                taskController?.SelectEarliestTask();
            }

            if (CurrentTask == null)
            {
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


        private Transform FindNearestEnemy()
        {
            Transform nearest = null;
            var best = float.MaxValue;
            var hits = Physics2D.OverlapCircleAll(transform.position, stats.visionRange, enemyMask);
            foreach (var h in hits)
            {
                var hp = h.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                var d = Vector2.Distance(transform.position, h.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = h.transform;
                }
            }

            return nearest;
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

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                var killTracker = EnemyKillTracker.Instance;
                if (killTracker == null)
                    TELogger.Log("EnemyKillTracker missing", TELogCategory.Combat, this);
                var enemyStats = target.GetComponent<Enemy>()?.Stats;
                float bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                float dmg = (baseDamage + damageBonus) *
                            (buffController != null ? buffController.DamageMultiplier : 1f);
                proj.Init(target, dmg * combatDamageMultiplier * bonus, true, null, combatSkill);
            }
        }

        private enum State
        {
            Idle,
            MovingToTask,
            PerformingTask,
            Combat
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

        #endregion
    }
}