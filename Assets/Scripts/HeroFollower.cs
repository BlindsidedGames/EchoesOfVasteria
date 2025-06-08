using UnityEngine;

/// <summary>Keeps non-active heroes near the active one.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HeroFollower : MonoBehaviour
{
    public Transform target;                // set by PartyManager

    [SerializeField] private float preferredDist = 1.5f;
    [SerializeField] private float moveSpeed     = 3.5f;

    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        if (!target) return;

        Vector2 dir = (Vector2)(target.position - transform.position);
        if (dir.magnitude > preferredDist)
            rb.MovePosition(rb.position + dir.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}