using Pathfinding;
using TimelessEchoes.Hero;
using TimelessEchoes.Utilities;
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
                hero = HeroController.Instance;
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (hero != null && ai != null)
                ai.maxSpeed = hero.MoveSpeed + 1f;
            Vector2 vel = ai != null ? ai.desiredVelocity : Vector2.zero;
            Vector2 dir = AnimatorMovementHelper.SnapToCardinal(vel, false);

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir;

            AnimatorMovementHelper.SetMovement(animator, lastMoveDir, vel.magnitude);

            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;
        }
    }
}
