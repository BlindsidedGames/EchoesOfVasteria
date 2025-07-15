using Pathfinding;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    /// <summary>
    /// Simple movement and animation controller for Mildred the cat.
    /// </summary>
    [RequireComponent(typeof(AIPath))]
    public class MildredMovementController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private HeroController hero;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private AIPath ai;
        private Vector2 lastMoveDir = Vector2.down;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            if (hero == null)
                hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (hero != null && ai != null)
                ai.maxSpeed = hero.MoveSpeed + 1f;
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
    }
}
