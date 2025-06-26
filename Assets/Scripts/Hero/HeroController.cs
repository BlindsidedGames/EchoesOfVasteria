using System.Collections;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using TimelessEchoes.Enemies;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static TimelessEchoes.TELogger;

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
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        private readonly bool allowAttacks = true;

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

        // Allows other systems to manually flag that the destination
        // has been reached even if the pathfinding system does not
        // report it. This can be useful for scripted events or when
        // the destination is obstructed.
        private bool destinationOverride;
        private Health health;
        private float healthBonus;

        private bool isRolling;
        private float lastAttack = float.NegativeInfinity;

        // Remember the last movement direction so the attack blend tree can
        // continue to use it even when the hero stops moving.
        private Vector2 lastMoveDir = Vector2.down;
        private MiningTask miningTask;
        private float miningTimer;
        private FishingTask fishingTask;
        private float fishingTimer;
        private float moveSpeedBonus;
        private AIDestinationSetter setter;

        private State state;

        private TaskController taskController;
        public ITask CurrentTask { get; private set; }

        private float CurrentAttackRate => baseAttackSpeed + attackSpeedBonus;
        public float Defense => baseDefense + defenseBonus;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            if (taskController == null)
                taskController = GetComponentInParent<TaskController>();

            state = State.Idle;

            ApplyStatUpgrades();

            if (stats != null)
            {
                ai.maxSpeed = baseMoveSpeed + moveSpeedBonus;
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
                health.Init(hp);
            }
        }


        private void Update()
        {
            UpdateAnimation();
            UpdateBehavior();
        }

        private void OnEnable()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();

            ApplyStatUpgrades();
            if (stats != null)
            {
                ai.maxSpeed = baseMoveSpeed + moveSpeedBonus;
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
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

            CurrentTask = null;
            miningTask = null;
            state = State.Idle;
            destinationOverride = false;
            lastAttack = Time.time - 1f / CurrentAttackRate;
        }

        private void ApplyStatUpgrades()
        {
            var controller = FindFirstObjectByType<StatUpgradeController>();
            if (controller == null) return;

            foreach (var upgrade in controller.AllUpgrades)
            {
                if (upgrade == null) continue;
                var increase = controller.GetIncrease(upgrade);
                var baseVal = controller.GetBaseValue(upgrade);
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
            Log($"Hero assigned task: {task?.GetType().Name ?? "None"}", this);
            CurrentTask = task;
            currentTaskName = task != null ? task.GetType().Name : "None";
            currentTaskObject = task as MonoBehaviour;
            miningTask = null;
            state = State.Idle;
            // Ensure the destination setter is initialized even if Awake has not run yet
            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();

            if (setter != null)
            {
                setter.target = task != null ? task.Target : null;
                // Reset the AI path so reachedDestination isn't true
                // before the new destination has been processed.
                if (ai != null)
                    ai.Teleport(transform.position); // Clears path and searches again
                else
                    ai?.SearchPath();
            }
        }

        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            setter.target = dest;
            ai?.SearchPath();
        }

        /// <summary>
        ///     Manually flag that the hero has reached its destination.
        ///     This bypasses the built-in pathfinding checks in <see cref="IsAtDestination" />.
        /// </summary>
        [Button("Mark Destination Reached")]
        public void SetDestinationReached()
        {
            destinationOverride = true;
        }

        private bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null)
                return false;

            if (destinationOverride)
                return true;

            // Consider the destination reached if the pathfinding component
            // reports it cannot move any closer. This handles cases where the
            // target point is not directly reachable by the navmesh.
            if (ai.reachedDestination || ai.reachedEndOfPath)
                return true;

            var threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }

        private void UpdateBehavior()
        {
            if (stats == null) return;

            var nearest = FindNearestEnemy();
            if (nearest != null)
            {
                HandleCombat(nearest);
                return;
            }

            if (state == State.Combat)
            {
                Log("Hero exiting combat", this);
                combatDamageMultiplier = 1f;
                isRolling = false;
                diceRoller?.ResetRoll();
                state = State.Idle;
                taskController?.SelectEarliestTask();
            }

            if (state == State.Mining)
            {
                HandleMining();
                return;
            }

            if (state == State.Fishing)
            {
                HandleFishing();
                return;
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
                taskController?.SelectEarliestTask();

            if (CurrentTask == null) return;

            if (CurrentTask is MiningTask mt)
            {
                var dest = mt.Target;
                if (setter.target != dest)
                    setter.target = dest;

                if (state != State.Mining && IsAtDestination(dest))
                    BeginMining(mt);
                else
                    state = State.Moving;
            }
            else if (CurrentTask is FishingTask ft)
            {
                var dest = ft.Target;
                if (setter.target != dest)
                    setter.target = dest;

                if (state != State.Fishing && IsAtDestination(dest))
                    BeginFishing(ft);
                else
                    state = State.Moving;
            }
            else if (CurrentTask is KillEnemyTask ke)
            {
                if (ke.target != null)
                    setter.target = ke.target;
                state = State.Moving;
            }
            else
            {
                setter.target = CurrentTask.Target;
                state = State.Moving;
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
            if (state == State.Mining)
            {
                ai.canMove = true;
                animator?.SetTrigger("StopMining");
                if (miningTask?.ProgressBarObject != null)
                    miningTask.ProgressBarObject.SetActive(false);
                else if (miningTask?.ProgressBar != null)
                    miningTask.ProgressBar.gameObject.SetActive(false);
                miningTask = null;
            }
            else if (state == State.Fishing)
            {
                ai.canMove = true;
                animator?.SetTrigger("CatchFish");
                if (fishingTask?.ProgressBarObject != null)
                    fishingTask.ProgressBarObject.SetActive(false);
                else if (fishingTask?.ProgressBar != null)
                    fishingTask.ProgressBar.gameObject.SetActive(false);
                fishingTask = null;
            }

            if (state != State.Combat)
            {
                Log($"Hero entering combat with {enemy.name}", this);
                if (diceRoller != null && !isRolling)
                {
                    var rate = CurrentAttackRate;
                    var cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            state = State.Combat;
            ai.canMove = true;
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

        private void BeginMining(MiningTask task)
        {
            Log($"Begin mining {task.name}", this);
            miningTask = task;
            miningTimer = 0f;
            state = State.Mining;
            ai.canMove = false;
            setter.target = task.transform;
            if (task.ProgressBar != null)
            {
                if (task.ProgressBarObject != null)
                    task.ProgressBarObject.SetActive(true);
                else
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
                miningTask.ProgressBar.fillAmount =
                    Mathf.Clamp01((miningTask.MineTime - miningTimer) / miningTask.MineTime);

            if (miningTimer >= miningTask.MineTime)
            {
                Log($"Finished mining {miningTask.name}", this);
                ai.canMove = true;
                animator?.SetTrigger("StopMining");
                miningTask.CompleteTask();
                miningTask = null;
                taskController?.SelectEarliestTask();
                state = State.Idle;
            }
        }

        private void BeginFishing(FishingTask task)
        {
            Log($"Begin fishing {task.name}", this);
            fishingTask = task;
            fishingTimer = 0f;
            state = State.Fishing;
            ai.canMove = false;
            setter.target = task.transform;
            if (task.ProgressBar != null)
            {
                if (task.ProgressBarObject != null)
                    task.ProgressBarObject.SetActive(true);
                else
                    task.ProgressBar.gameObject.SetActive(true);
                task.ProgressBar.fillAmount = 1f;
            }

            animator?.Play("Fishing");
        }

        private void HandleFishing()
        {
            if (fishingTask == null)
            {
                state = State.Idle;
                return;
            }

            fishingTimer += Time.deltaTime;
            if (fishingTask.ProgressBar != null)
                fishingTask.ProgressBar.fillAmount =
                    Mathf.Clamp01((fishingTask.FishTime - fishingTimer) / fishingTask.FishTime);

            if (fishingTimer >= fishingTask.FishTime)
            {
                Log($"Finished fishing {fishingTask.name}", this);
                ai.canMove = true;
                animator?.SetTrigger("CatchFish");
                fishingTask.CompleteTask();
                fishingTask = null;
                taskController?.SelectEarliestTask();
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

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(target, (baseDamage + damageBonus) * combatDamageMultiplier);
        }

        private enum State
        {
            Idle,
            Moving,
            Mining,
            Fishing,
            Combat
        }
    }
}