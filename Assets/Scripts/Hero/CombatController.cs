using System.Collections;
using Pathfinding;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    public class CombatController : MonoBehaviour
    {
        [SerializeField] private HeroStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private BuffManager buffController;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private EnemyKillTracker killTracker;

        public HeroStats Stats { get => stats; set => stats = value; }
        public Animator AnimatorRef { get => animator; set => animator = value; }
        public Transform ProjectileOrigin { get => projectileOrigin; set => projectileOrigin = value; }
        public DiceRoller DiceRollerRef { get => diceRoller; set => diceRoller = value; }
        public BuffManager BuffController { get => buffController; set => buffController = value; }
        public LayerMask EnemyMask { get => enemyMask; set => enemyMask = value; }
        public EnemyKillTracker KillTracker { get => killTracker; set => killTracker = value; }

        private AIPath ai;
        private AIDestinationSetter setter;
        private HeroStateMachine stateMachine;
        private bool isRolling;
        private float lastAttack = float.NegativeInfinity;
        private float combatDamageMultiplier = 1f;
        private const bool allowAttacks = true;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            stateMachine = GetComponent<HeroStateMachine>();
        }

        public Transform FindNearestEnemy()
        {
            if (stats == null) return null;
            Transform nearest = null;
            float best = float.MaxValue;
            var hits = Physics2D.OverlapCircleAll(transform.position, stats.visionRange, enemyMask);
            foreach (var h in hits)
            {
                var hp = h.GetComponent<Health>();
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

        public void HandleCombat(Transform enemy, float attackRate, float baseDamage, float damageBonus)
        {
            if (enemy == null || stats == null) return;
            if (ai != null) ai.canMove = true;

            if (stateMachine != null && stateMachine.CurrentState != HeroState.Combat)
            {
                stateMachine.ChangeState(HeroState.Combat);
                if (diceRoller != null && !isRolling)
                {
                    float cooldown = attackRate > 0f ? 1f / attackRate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            if (setter != null) setter.target = enemy;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f) return;

            float dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                float cooldown = attackRate > 0f ? 1f / attackRate : float.PositiveInfinity;
                if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    Attack(enemy, baseDamage, damageBonus);
                    lastAttack = Time.time;
                }
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

        private void Attack(Transform target, float baseDamage, float damageBonus)
        {
            if (stats == null || stats.projectilePrefab == null || target == null) return;

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            if (animator != null)
                animator.Play("Attack");

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                var enemyStats = target.GetComponent<Enemy>()?.Stats;
                float bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                float dmg = (baseDamage + damageBonus) * (buffController != null ? buffController.DamageMultiplier : 1f);
                proj.Init(target, dmg * combatDamageMultiplier * bonus, true);
            }
        }
    }
}
