using Pathfinding;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    [RequireComponent(typeof(AIPath))]
    public class AnimalDecorationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector2 wanderInterval = new Vector2(1f, 3f);
        [SerializeField] private float wanderDistance = 1f;
        [SerializeField] private bool enableFlee = false;
        [SerializeField] private float fleeRadius = 2f;
        [SerializeField] private float fleeDistance = 3f;
        [SerializeField] private HeroController hero;

        private AIPath ai;
        private Vector3 spawnPos;
        private float nextWanderTime;
        private Vector2 lastMoveDir = Vector2.down;
        private LayerMask blockingMask;
        private bool isPaused;

        protected virtual void Awake()
        {
            ai = GetComponent<AIPath>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (hero == null)
                hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
            spawnPos = transform.position;
            blockingMask = LayerMask.GetMask("Blocking");
        }

        protected virtual void OnEnable()
        {
            nextWanderTime = Time.time;
            Wander();
        }

        private void Update()
        {
            UpdateAnimation();
            if (isPaused)
                return;
            if (enableFlee && hero != null && Vector2.Distance(transform.position, hero.transform.position) < fleeRadius)
                Flee();
            else
                Wander();
        }

        private void UpdateAnimation()
        {
            Vector2 vel = ai != null ? ai.desiredVelocity : Vector2.zero;
            var dir = vel;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir;
            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
                animator.SetFloat("MoveMagnitude", vel.magnitude);
            }
            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;
        }

        private void Wander()
        {
            if (Time.time < nextWanderTime)
                return;
            const int maxAttempts = 5;
            Vector2 wander = transform.position;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = (Vector2)spawnPos + Random.insideUnitCircle * wanderDistance;
                if (Physics2D.OverlapCircle(candidate, 0.2f, blockingMask) == null)
                {
                    wander = candidate;
                    break;
                }
            }
            if (ai != null)
                ai.destination = wander;
            nextWanderTime = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
        }

        private void Flee()
        {
            Vector2 dir = (Vector2)(transform.position - hero.transform.position).normalized;
            Vector2 target = (Vector2)transform.position + dir * fleeDistance;
            if (ai != null)
                ai.destination = target;
        }

        public void PauseMovement()
        {
            isPaused = true;
            if (ai != null)
                ai.canMove = false;
        }

        public void ResumeMovement()
        {
            isPaused = false;
            if (ai != null)
                ai.canMove = true;
        }

        protected Animator Animator => animator;
    }
}

