using UnityEngine;
using Pathfinding;
using TimelessEchoes;

namespace TimelessEchoes.Enemies
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform projectileOrigin;

        private AIPath ai;
        private AIDestinationSetter setter;
        private Health health;
        private TargetRegistry registry;
        private Transform startTarget;
        private Vector3 spawnPos;
        private float nextAttack;

        private void OnEnable()
        {
            registry = TargetRegistry.Instance;
            registry?.Register(transform);
        }

        private void OnDisable()
        {
            registry?.Unregister(transform);
        }

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            spawnPos = transform.position;
            if (stats != null)
            {
                ai.maxSpeed = stats.moveSpeed;
                health.Init(stats.maxHealth);
            }
            startTarget = setter.target;
        }

        private void Update()
        {
            UpdateAnimation();
            UpdateBehavior();
        }

        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            Vector2 dir = vel;
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) dir.y = 0f; else dir.x = 0f;
            animator.SetFloat("MoveX", dir.x);
            animator.SetFloat("MoveY", dir.y);
            animator.SetFloat("MoveMagnitude", vel.magnitude);
            if (spriteRenderer != null)
                spriteRenderer.flipX = vel.x < 0f;
        }

        private void UpdateBehavior()
        {
            if (setter.target == null || stats == null)
            {
                Wander();
                return;
            }

            float dist = Vector2.Distance(transform.position, setter.target.position);
            if (dist <= stats.visionRange)
            {
                ai.destination = setter.target.position;
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.SetTrigger("Attack");
                    FireProjectile();
                }
            }
            else
            {
                Wander();
            }
        }

        private void Wander()
        {
            if (ai.reachedEndOfPath)
            {
                Vector2 wander = spawnPos + (Vector3)Random.insideUnitCircle * stats.wanderDistance;
                ai.destination = wander;
            }
        }

        private void FireProjectile()
        {
            if (stats.projectilePrefab == null || setter.target == null) return;
            Transform origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(setter.target, stats.damage);
        }
    }
}
