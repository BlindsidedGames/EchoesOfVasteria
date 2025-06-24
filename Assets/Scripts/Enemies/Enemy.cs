using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using TimelessEchoes.Upgrades;
using System.Collections.Generic;

namespace TimelessEchoes.Enemies
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [System.Serializable]
        public class ResourceDrop
        {
            public Resource resource;
            public Vector2Int dropRange = new Vector2Int(1, 1);
            [Range(0f, 1f)] public float dropChance = 1f;
        }
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private ResourceManager resourceManager;

        private AIPath ai;
        private Health health;
        private float nextAttack;
        private AIDestinationSetter setter;
        private Vector3 spawnPos;
        private Transform startTarget;

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
            resourceManager = FindFirstObjectByType<ResourceManager>();
            if (health != null)
                health.OnDeath += OnDeath;
        }

        private void Update()
        {
            UpdateAnimation();
            UpdateBehavior();
        }

        private void OnEnable()
        {
            

            // Offset the animator's starting time so enemies don't animate
            // in perfect sync when spawned simultaneously.
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
        }


        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            var dir = vel;
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

            var dist = Vector2.Distance(transform.position, setter.target.position);
            if (dist <= stats.visionRange)
            {
                ai.destination = setter.target.position;
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.Play("Attack");
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
            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(setter.target, stats.damage);
        }

        private void OnDeath()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager == null) return;

            foreach (var drop in resourceDrops)
            {
                if (drop.resource == null) continue;
                if (Random.value > drop.dropChance) continue;

                int min = drop.dropRange.x;
                int max = drop.dropRange.y;
                if (max < min) max = min;
                float t = Random.value;
                t *= t; // bias towards lower values
                int count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
                if (count > 0)
                {
                    resourceManager.Add(drop.resource, count);
                    TimelessEchoes.FloatingText.Spawn($"{drop.resource.name} x{count}", transform.position + Vector3.up, Color.yellow);
                }
            }
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
        }
    }
}