using System.Collections;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using TimelessEchoes;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Enemies.Health))]
    public class HeroController : MonoBehaviour
    {
        [SerializeField] private HeroStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private LayerMask enemyMask = ~0;

        private float baseDamage = 0f;
        private float baseAttackSpeed = 0f;
        private float baseMoveSpeed = 0f;
        private float baseHealth = 0f;
        private float baseDefense = 0f;

        private float damageBonus = 0f;
        private float attackSpeedBonus = 0f;
        private float moveSpeedBonus = 0f;
        private float healthBonus = 0f;
        private float defenseBonus = 0f;

        private TaskController taskController;

        // Remember the last movement direction so the attack blend tree can
        // continue to use it even when the hero stops moving.
        private Vector2 lastMoveDir = Vector2.down;

        private AIPath ai;
        private Enemies.Health health;
        private AIDestinationSetter setter;
        private float lastAttack = float.NegativeInfinity;
        private bool isRolling;
        private bool allowAttacks = true;
        private ITask currentTask;
        public ITask CurrentTask => currentTask;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;

        private enum State
        {
            Idle,
            Moving,
            Mining,
            Combat
        }

        private State state;
        private Tasks.MiningTask miningTask;
        private float miningTimer;

        private bool inCombat;
        private float combatDamageMultiplier = 1f;

        private void ApplyStatUpgrades()
        {
            var controller = FindFirstObjectByType<StatUpgradeController>();
            if (controller == null) return;

            foreach (var upgrade in controller.AllUpgrades)
            {
                if (upgrade == null) continue;
                float increase = controller.GetIncrease(upgrade);
                float baseVal = controller.GetBaseValue(upgrade);
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

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Enemies.Health>();
            if (taskController == null)
                taskController = GetComponent<TaskController>();

            state = State.Idle;

            ApplyStatUpgrades();

            if (stats != null)
            {
                ai.maxSpeed = baseMoveSpeed + moveSpeedBonus;
                int hp = Mathf.RoundToInt(baseHealth + healthBonus);
                health.Init(hp);
            }
        }

        private void OnEnable()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();

            ApplyStatUpgrades();
            if (stats != null)
            {
                ai.maxSpeed = baseMoveSpeed + moveSpeedBonus;
                int hp = Mathf.RoundToInt(baseHealth + healthBonus);
                health.Init(hp);
            }

            var start = taskController != null ? taskController.EntryPoint : null;
            if (start != null)
                transform.position = start.position;
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
            currentTask = null;
            miningTask = null;
            state = State.Idle;
            lastAttack = Time.time - 1f / CurrentAttackRate;
        }


        private void Update()
        {
            UpdateAnimation();
            UpdateBehavior();
        }

        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            var dir = vel;

            // If we're nearly stationary but have a target, face the target so
            // attack animations look correct when an enemy passes by.
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
            TELogger.Log($"Hero assigned task: {task?.GetType().Name ?? "None"}", this);
            currentTask = task;
            currentTaskName = task != null ? task.GetType().Name : "None";
            currentTaskObject = task as MonoBehaviour;
            miningTask = null;
            state = State.Idle;
            // Ensure the destination setter is initialized even if Awake has not run yet
            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();

            if (setter != null)
                setter.target = task != null ? task.Target : null;
        }

        public void SetDestination(Transform dest)
        {
            setter.target = dest;
        }

        private bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null)
                return false;

            // Consider the destination reached if the pathfinding component
            // reports it cannot move any closer. This handles cases where the
            // target point is not directly reachable by the navmesh.
            if (ai.reachedDestination || ai.reachedEndOfPath)
                return true;

            float threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }

        private float CurrentAttackRate => baseAttackSpeed + attackSpeedBonus;
        public float Defense => baseDefense + defenseBonus;

        private void UpdateBehavior()
        {
            if (stats == null) return;

            var nearest = FindNearestEnemy();
            if (nearest != null)
            {
                HandleCombat(nearest);
                return;
            }
            else if (state == State.Combat)
            {
                TELogger.Log("Hero exiting combat", this);
                combatDamageMultiplier = 1f;
                diceRoller?.ResetRoll();
                inCombat = false;
                state = State.Idle;
                taskController?.SelectEarliestTask();
            }

            if (state == State.Mining)
            {
                HandleMining();
                return;
            }

            if (currentTask == null || currentTask.IsComplete())
                taskController?.SelectEarliestTask();

            if (currentTask == null) return;

            if (currentTask is MiningTask mt)
            {
                var dest = mt.Target;
                if (setter.target != dest)
                    setter.target = dest;

                if (state != State.Mining && IsAtDestination(dest))
                    BeginMining(mt);
                else
                    state = State.Moving;
            }
            else if (currentTask is KillEnemyTask ke)
            {
                if (ke.target != null)
                    setter.target = ke.target;
                state = State.Moving;
            }
            else
            {
                setter.target = currentTask.Target;
                state = State.Moving;
            }
        }

        private Transform FindNearestEnemy()
        {
            Transform nearest = null;
            float best = float.MaxValue;
            var hits = Physics2D.OverlapCircleAll(transform.position, stats.visionRange, enemyMask);
            foreach (var h in hits)
            {
                var hp = h.GetComponent<Enemies.Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                float d = Vector2.Distance(transform.position, h.transform.position);
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
            if (state != State.Combat)
                TELogger.Log($"Hero entering combat with {enemy.name}", this);
            state = State.Combat;
            setter.target = enemy;
            if (!inCombat && diceRoller != null && !isRolling)
            {
                float rate = CurrentAttackRate;
                float cooldown = rate > 0f ? 1f / rate : 0.5f;
                StartCoroutine(RollForCombat(cooldown));
            }

            var hp = enemy.GetComponent<Enemies.Health>();
            if (hp == null || hp.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                float rate = CurrentAttackRate;
                float cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    lastMoveDir = enemy.position - transform.position;
                    Attack(enemy);
                    lastAttack = Time.time;
                }
            }

            inCombat = true;
        }

        private void BeginMining(MiningTask task)
        {
            TELogger.Log($"Begin mining {task.name}", this);
            miningTask = task;
            miningTimer = 0f;
            state = State.Mining;
            ai.canMove = false;
            setter.target = task.transform;
            if (task.ProgressBar != null)
            {
                task.ProgressBar.gameObject.SetActive(true);
                task.ProgressBar.fillAmount = 1f;
            }
            animator?.Play("Mining");
        }

        private void HandleMining()
        {
            if (miningTask == null)
            {
                state = State.Idle;
                return;
            }

            miningTimer += Time.deltaTime;
            if (miningTask.ProgressBar != null)
                miningTask.ProgressBar.fillAmount = Mathf.Clamp01((miningTask.MineTime - miningTimer) / miningTask.MineTime);

            if (miningTimer >= miningTask.MineTime)
            {
                TELogger.Log($"Finished mining {miningTask.name}", this);
                ai.canMove = true;
                animator?.SetTrigger("StopMining");
                taskController?.RemoveTask(miningTask);
                miningTask.CompleteTask();
                SetTask(null);
                taskController?.SelectEarliestTask();
                miningTask = null;
                state = State.Idle;
            }
        }

        private IEnumerator RollForCombat(float duration)
        {
            if (diceRoller == null)
                yield break;

            isRolling = true;
            lastAttack = Time.time;

            yield return StartCoroutine(diceRoller.Roll(duration));

            combatDamageMultiplier = 1f + 0.1f * diceRoller.Result;
            isRolling = false;
        }

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;

            var enemy = target.GetComponent<Enemies.Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(target, (baseDamage + damageBonus) * combatDamageMultiplier);
        }
    }
}
