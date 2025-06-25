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
        private Transform hero;
        private Transform wanderTarget;
        private LayerMask blockingMask;

        public bool IsEngaged => setter != null && setter.target == hero;

        public static event System.Action<Enemy> OnEngage;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            spawnPos = transform.position;

            var controller = GetComponentInParent<TimelessEchoes.Tasks.TaskController>();
            if (controller != null)
                hero = controller.hero != null ? controller.hero.transform : null;
            wanderTarget = new GameObject("WanderTarget").transform;
            wanderTarget.hideFlags = HideFlags.HideInHierarchy;
            wanderTarget.position = transform.position;
            blockingMask = LayerMask.GetMask("Blocking");
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
            EnemyActivator.Instance?.Register(this);
            OnEngage += HandleAllyEngaged;

            Wander();

            // Offset the animator's starting time so enemies don't animate
            // in perfect sync when spawned simultaneously.
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
        }

        private void OnDisable()
        {
            EnemyActivator.Instance?.Unregister(this);
            OnEngage -= HandleAllyEngaged;
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
            if (stats == null)
                return;

            bool heroInRange = false;
            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                float hDist = Vector2.Distance(transform.position, hero.position);
                if (hDist <= stats.visionRange)
                {
                    heroInRange = true;
                    setter.target = hero;
                    OnEngage?.Invoke(this);
                }
            }

            if (heroInRange)
            {
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.Play("Attack");
                    FireProjectile();
                }
            }
            else
            {
                if (setter.target == hero)
                    setter.target = wanderTarget;
                Wander();
            }
        }

        private void Wander()
        {
            if (setter.target != wanderTarget)
                setter.target = wanderTarget;
            if (!ai.reachedEndOfPath) return;

            const int maxAttempts = 5;
            Vector2 wander = (Vector2)transform.position;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = (Vector2)spawnPos + Random.insideUnitCircle * stats.wanderDistance;
                if (Physics2D.OverlapCircle(candidate, 0.2f, blockingMask) == null)
                {
                    wander = candidate;
                    break;
                }
            }

            wanderTarget.position = wander;
            setter.target = wanderTarget;
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
            OnEngage -= HandleAllyEngaged;
            if (wanderTarget != null)
                Destroy(wanderTarget.gameObject);
        }

        public void SetActiveState(bool active)
        {
            if (ai != null) ai.enabled = active;
            if (setter != null) setter.enabled = active;
            if (animator != null) animator.enabled = active;
            if (spriteRenderer != null) spriteRenderer.enabled = active;
        }

        private void HandleAllyEngaged(Enemy other)
        {
            if (other != this && other != null && hero != null)
            {
                float dist = Vector2.Distance(transform.position, other.transform.position);
                if (dist <= stats.assistRange)
                    setter.target = hero;
            }
        }
    }
}