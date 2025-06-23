using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using TimelessEchoes.Tasks;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Enemies.Health))]
    public class HeroController : MonoBehaviour
    {
        [SerializeField] private Enemies.EnemyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;

        [SerializeField] private TaskController taskController;
        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform exitPoint;

        private AIPath ai;
        private Enemies.Health health;
        private AIDestinationSetter setter;
        private TargetRegistry registry;
        private float nextAttack;
        private int taskIndex = -1;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Enemies.Health>();
            registry = TargetRegistry.Instance;
            if (stats != null)
            {
                ai.maxSpeed = stats.moveSpeed;
                health.Init(stats.maxHealth);
            }
        }

        private void OnEnable()
        {
            if (entryPoint != null)
                transform.position = entryPoint.position;
            registry?.Register(transform);
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
            taskIndex = -1;
            taskController?.SelectNextTask();
            taskIndex++;
        }

        private void OnDisable()
        {
            registry?.Unregister(transform);
        }

        private void Update()
        {
            UpdateAnimation();
            UpdateBehavior();
            UpdateTask();
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
            if (stats == null) return;
            var closest = TargetRegistry.Instance?.FindClosest(transform.position, LayerMask.GetMask("Enemy"));
            if (closest == null) return;
            var dist = Vector2.Distance(transform.position, closest.position);
            if (dist <= stats.visionRange)
            {
                ai.destination = closest.position;
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.SetTrigger("Attack");
                    FireProjectile(closest);
                }
            }
        }

        private void UpdateTask()
        {
            if (taskController == null) return;
            if (taskIndex < 0 || taskIndex >= taskController.tasks.Count)
            {
                if (exitPoint != null)
                    setter.target = exitPoint;
                return;
            }
            var task = taskController.tasks[taskIndex];
            setter.target = task.Target;
            if (task.Target != null && ai.reachedDestination && task.IsComplete())
            {
                taskController.SelectNextTask();
                taskIndex++;
            }
        }

        private void FireProjectile(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;
            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(target, stats.damage);
        }
    }
}
