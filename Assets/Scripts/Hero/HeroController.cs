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
        [SerializeField] private HeroStats stats;
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
        private float nextAttack;
        private ITask currentTask;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Enemies.Health>();
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
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
            currentTask = null;
            taskController?.ResetTasks();
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

        public void SetTask(ITask task)
        {
            currentTask = task;
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

        private void UpdateBehavior()
        {
            if (stats == null) return;
            var target = setter.target;
            if (target == null) return;

            var enemy = target.GetComponent<Enemies.Health>();
            if (enemy == null) return;
            if (enemy.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, target.position);
            if (dist <= stats.visionRange)
            {
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.Play("Attack");
                    FireProjectile(target);
                }
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
