using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class HeroAnimator : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AIPath aiPath;

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
        float magnitude = velocity.magnitude / aiPath.maxSpeed;
        Vector2 dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.zero;

        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
        animator.SetFloat("MoveMagnitude", magnitude);

        spriteRenderer.flipX = dir.x < 0f;
    }
}
