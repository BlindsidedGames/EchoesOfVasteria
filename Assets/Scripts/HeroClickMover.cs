using UnityEngine;

/// <summary>
/// Moves toward a point when commanded.  
/// Still allows BasicAttackTelegraphed to run every frame.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HeroClickMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDist  = 0.05f;   // when we consider we’ve arrived

    private Vector2       destination;
    private bool          hasDest;
    private Rigidbody2D   rb;

    /* highlight sprite tint for debugging (optional) */
    private SpriteRenderer sr;
    private Color          normalColor;

    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        if (sr) normalColor = sr.color;
    }

    private void FixedUpdate()
    {
        if (!hasDest) return;

        Vector2 pos = rb.position;
        Vector2 dir = destination - pos;

        if (dir.sqrMagnitude <= stopDist * stopDist)
        {
            rb.linearVelocity = Vector2.zero;
            hasDest     = false;
            return;
        }

        Vector2 step = dir.normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(pos + step);
    }

    /* ─── API ────────────────────────────────────────────────────────────── */
    public void SetDestination(Vector3 dest) => SetDestination((Vector2)dest);
    public void SetDestination(Vector2 dest)
    {
        destination = dest;
        hasDest     = true;
    }


    public void SetSelected(bool isSel)
    {
        // Optional: tint sprite yellow when selected
        if (sr) sr.color = isSel ? Color.yellow : normalColor;
    }
}