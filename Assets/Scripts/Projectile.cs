using UnityEngine;

/// <summary>Moves forward, damages first valid collider, then destroys itself.</summary>
public class Projectile : MonoBehaviour
{
    private int       damage;
    private LayerMask targetMask;
    private Vector2   velocity;

    /// <param name="dir">Normalized direction.</param>
    public void Init(Vector2 dir, float speed, int dmg, LayerMask mask)
    {
        velocity   = dir * speed;
        damage     = dmg;
        targetMask = mask;

        // Rotate sprite to face travel direction (optional)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, 5f); // auto-cleanup
    }

    private void Update() => transform.position += (Vector3)(velocity * Time.deltaTime);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetMask & (1 << other.gameObject.layer)) == 0) return;

        if (other.TryGetComponent(out IDamageable d)) d.TakeDamage(damage);
        Destroy(gameObject);
    }
}