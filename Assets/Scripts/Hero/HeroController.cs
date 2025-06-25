using System.Collections;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using TimelessEchoes;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;

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

        private enum HeroState { Idle, Moving, Mining, Combat }
        private HeroState state = HeroState.Idle;
        private float miningTimer;
        private MiningTask activeMiningTask;

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
            state = HeroState.Idle;
            miningTimer = 0f;
            activeMiningTask = null;
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
            currentTask = task;
            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();

            miningTimer = 0f;
            activeMiningTask = null;

            if (task == null)
            {
                state = HeroState.Idle;
                if (setter != null)
                    setter.target = null;
                ai.canMove = true;
                return;
            }

            if (task is MiningTask mine)
            {
                activeMiningTask = mine;
                if (setter != null)
                    setter.target = mine.GetNearestPoint(transform);
            }
            else
            {
                if (setter != null)
                    setter.target = task.Target;
            }
            ai.canMove = true;
            state = HeroState.Moving;
        }

        public void SetDestination(Transform dest)
        {
            setter.target = dest;
        }

        private float CurrentAttackRate => baseAttackSpeed + attackSpeedBonus;
        public float Defense => baseDefense + defenseBonus;

        private void UpdateBehavior()
        {
            if (stats == null) return;

            var hits = Physics2D.OverlapCircleAll(transform.position, stats.visionRange, enemyMask);
            Transform nearest = null;
            float best = float.MaxValue;
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

            bool nowInCombat = nearest != null;
            if (nowInCombat)
            {
                if (setter != null)
                    setter.target = nearest;
                if (!inCombat && diceRoller != null && !isRolling)
                {
                    float rate = CurrentAttackRate;
                    float cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
                state = HeroState.Combat;
            }
            else
            {
                if (inCombat)
                {
                    combatDamageMultiplier = 1f;
                    diceRoller?.ResetRoll();
                    state = HeroState.Idle;
                    taskController?.SelectEarliestTask();
                }
            }

            inCombat = nowInCombat;

            if (state == HeroState.Combat)
            {
                if (nearest == null) return;
                var enemy = nearest.GetComponent<Enemies.Health>();
                if (enemy == null || enemy.CurrentHealth <= 0f) return;

                var dist = Vector2.Distance(transform.position, nearest.position);
                if (dist <= stats.visionRange)
                {
                    float rate = CurrentAttackRate;
                    float cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                    if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                    {
                        lastMoveDir = nearest.position - transform.position;
                        Attack(nearest);
                        lastAttack = Time.time;
                    }
                }
                return;
            }

            switch (state)
            {
                case HeroState.Idle:
                    if ((currentTask == null || currentTask.IsComplete() || setter.target == null) && taskController != null)
                        taskController.SelectEarliestTask();
                    break;
                case HeroState.Moving:
                    if (currentTask == null)
                    {
                        state = HeroState.Idle;
                        break;
                    }
                    if (ai.reachedDestination)
                    {
                        if (currentTask is MiningTask mine)
                        {
                            activeMiningTask = mine;
                            ai.canMove = false;
                            if (setter != null)
                                setter.target = transform;
                            miningTimer = 0f;
                            mine.BeginMining();
                            animator?.Play("Mining");
                            state = HeroState.Mining;
                        }
                        else if (currentTask is TalkToNpcTask talk)
                        {
                            talk.Interact();
                            taskController?.RemoveTask(currentTask);
                            currentTask = null;
                            state = HeroState.Idle;
                            taskController?.SelectEarliestTask();
                        }
                        else if (currentTask is OpenChestTask chest)
                        {
                            chest.Open();
                            taskController?.RemoveTask(currentTask);
                            currentTask = null;
                            state = HeroState.Idle;
                            taskController?.SelectEarliestTask();
                        }
                        else
                        {
                            state = HeroState.Idle;
                        }
                    }
                    break;
                case HeroState.Mining:
                    if (activeMiningTask == null)
                    {
                        ai.canMove = true;
                        state = HeroState.Idle;
                        break;
                    }
                    miningTimer += Time.deltaTime;
                    activeMiningTask.UpdateProgress((activeMiningTask.MineTime - miningTimer) / activeMiningTask.MineTime);
                    if (miningTimer >= activeMiningTask.MineTime)
                    {
                        activeMiningTask.FinishMining();
                        taskController?.RemoveTask(activeMiningTask);
                        activeMiningTask = null;
                        ai.canMove = true;
                        currentTask = null;
                        animator?.SetTrigger("StopMining");
                        state = HeroState.Idle;
                        taskController?.SelectEarliestTask();
                    }
                    break;
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
