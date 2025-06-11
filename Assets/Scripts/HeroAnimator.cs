using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class HeroAnimator : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AIPath aiPath;
    private Vector2 lastMoveDir = Vector2.down;
    public bool logVelocity = false; // Toggle for logging velocity

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        aiPath = GetComponentInParent<AIPath>();
    }

    private void Update()
    {
        if (aiPath == null) return;

        Vector2 velocity = aiPath.desiredVelocity;
        if (logVelocity)
        {
            Debug.Log($"Velocity: {velocity}, Magnitude: {velocity.magnitude}");
        }
        float magnitude = velocity.magnitude / aiPath.maxSpeed;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            Vector2 norm = velocity.normalized;
            lastMoveDir = new Vector2(
                Mathf.Abs(norm.x) > 0.01f ? Mathf.Sign(norm.x) : 0f,
                Mathf.Abs(norm.y) > 0.01f ? Mathf.Sign(norm.y) : 0f);
        }

        animator.SetFloat("MoveX", lastMoveDir.x);
        animator.SetFloat("MoveY", lastMoveDir.y);
        animator.SetFloat("MoveMagnitude", magnitude);

        spriteRenderer.flipX = lastMoveDir.x < 0f;
    }
}
