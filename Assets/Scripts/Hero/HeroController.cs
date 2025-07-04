using System.Collections;
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
        [SerializeField] private BuffManager buffController;
        [SerializeField] private MapUI mapUI;
        [SerializeField] private EnemyKillTracker killTracker;
        [SerializeField] private StatUpgradeController statUpgradeController;
        [SerializeField] private TimelessEchoes.Skills.SkillController skillController;
        [SerializeField] private GameplayStatTracker statTracker;
        [SerializeField] private CombatController combatController;
        [SerializeField] private MovementController movementController;
        [SerializeField] private HeroStateMachine stateMachine;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        private readonly bool allowAttacks = true;

        private Transform currentEnemy;

        private float attackSpeedBonus;
        private float baseAttackSpeed;

        private float baseDamage;
        private float baseDefense;
        private float baseHealth;
        private float baseMoveSpeed;

        private float damageBonus;
        private float defenseBonus;

        private Health health;
        private float healthBonus;

        private Vector2 lastMoveDir = Vector2.down;
        private float moveSpeedBonus;
        private MapUI mapUI;

        private TaskController taskController;
        public ITask CurrentTask { get; private set; }
        public Animator Animator => animator;

        private float CurrentAttackRate =>
            (baseAttackSpeed + attackSpeedBonus) *
            (buffController != null ? buffController.AttackSpeedMultiplier : 1f);
        public float Defense =>
            (baseDefense + defenseBonus) *
            (buffController != null ? buffController.DefenseMultiplier : 1f);

        private void Awake()
        {
            movementController ??= GetComponent<MovementController>();
            combatController ??= GetComponent<CombatController>();
            stateMachine ??= GetComponent<HeroStateMachine>();
            health = GetComponent<Health>();
            if (buffController == null)
                buffController = BuffManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Buffs.BuffManager>();
            if (taskController == null)
                taskController = GetComponentInParent<TaskController>();

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();

            if (combatController != null)
            {
                combatController.Stats = stats;
                combatController.AnimatorRef = animator;
                combatController.ProjectileOrigin = projectileOrigin;
                combatController.DiceRollerRef = diceRoller;
                combatController.BuffController = buffController;
                combatController.EnemyMask = enemyMask;
                combatController.KillTracker = killTracker;
            }


            stateMachine.ChangeState(HeroState.Idle);

            ApplyStatUpgrades();

            if (stats != null)
            {
                movementController.Path.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
                var hp = Mathf.RoundToInt(baseHealth + healthBonus);
                health.Init(hp);
            }
        }


        private void Update()
        {
            BuffManager.Instance?.Tick(Time.deltaTime);
            if (stats != null)
                movementController.Path.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
                              (buffController != null ? buffController.MoveSpeedMultiplier : 1f);
            UpdateAnimation();
            UpdateBehavior();
            if (mapUI != null)
                mapUI.UpdateDistance(transform.position.x);

            if (statTracker == null)
                statTracker = FindFirstObjectByType<GameplayStatTracker>();
            statTracker?.RecordHeroPosition(transform.position);
        }

        private void OnEnable()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();

            if (buffController == null)
                buffController = BuffManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Buffs.BuffManager>();
            buffController?.Resume();

            if (mapUI == null)
                mapUI = FindFirstObjectByType<MapUI>();
            if (combatController != null)
            {
                combatController.Stats = stats;
                combatController.AnimatorRef = animator;
                combatController.ProjectileOrigin = projectileOrigin;
                combatController.DiceRollerRef = diceRoller;
                combatController.BuffController = buffController;
                combatController.EnemyMask = enemyMask;
                combatController.KillTracker = killTracker;
            }

            ApplyStatUpgrades();
            if (stats != null)
            {
                movementController.Path.maxSpeed = (baseMoveSpeed + moveSpeedBonus) *
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
            stateMachine.ChangeState(HeroState.Idle);
            movementController.MarkDestinationReached();
        }

        private void OnDisable()
        {
            buffController?.Pause();
        }

        private void ApplyStatUpgrades()
        {
            if (statUpgradeController == null)
                statUpgradeController = FindFirstObjectByType<StatUpgradeController>();
            if (skillController == null)
                skillController = FindFirstObjectByType<TimelessEchoes.Skills.SkillController>();
            var controller = statUpgradeController;
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
            Vector2 vel = movementController.Path != null ? movementController.Path.desiredVelocity : Vector2.zero;
            var dir = vel;

            if (dir.sqrMagnitude < 0.0001f && movementController.Destination != null)
                dir = movementController.Destination.position - transform.position;

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
            stateMachine.ChangeState(HeroState.Idle);

            movementController.SetDestination(task?.Target);
            if (movementController.Path != null)
                movementController.Path.Teleport(transform.position);
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

            var nearest = currentEnemy != null ? currentEnemy : combatController.FindNearestEnemy();
            if (nearest != null)
            {
                currentEnemy = nearest;
                if (stateMachine.CurrentState == HeroState.PerformingTask && CurrentTask != null)
                    CurrentTask.OnInterrupt(this);
                combatController.HandleCombat(nearest, CurrentAttackRate, baseDamage, damageBonus);
                return;
            }

            if (stateMachine.CurrentState == HeroState.Combat)
            {
                Log("Hero exiting combat", this);
                diceRoller?.ResetRoll();
                stateMachine.ChangeState(HeroState.Idle);
                taskController?.SelectEarliestTask();
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
            {
                CurrentTask = null;
                stateMachine.ChangeState(HeroState.Idle);
                taskController?.SelectEarliestTask();
            }

            if (CurrentTask == null)
            {
                movementController.Destination = null;
                return;
            }

            var dest = CurrentTask.Target;
            if (movementController.Destination != dest) movementController.Destination = dest;

            if (movementController.IsAtDestination(dest))
            {
                if (stateMachine.CurrentState != HeroState.PerformingTask)
                {
                    stateMachine.ChangeState(HeroState.PerformingTask);
                    movementController.EnableMovement(!CurrentTask.BlocksMovement);
                    CurrentTask.OnArrival(this);
                }

                CurrentTask.Tick(this);
            }
            else
            {
                stateMachine.ChangeState(HeroState.Moving);
                movementController.EnableMovement(true);
            }
        }




        #region Pathfinding Helpers

        public void SetDestination(Transform dest)
        {
            movementController.SetDestination(dest);
        }

        [Button("Mark Destination Reached")]
        public void SetDestinationReached()
        {
            movementController.MarkDestinationReached();
        }

        private bool IsAtDestination(Transform dest)
        {
            return movementController.IsAtDestination(dest);
        }

        #endregion
    }
}