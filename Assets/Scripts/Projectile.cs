using UnityEngine;

/// <summary>Moves towards a target, damages it on arrival, then destroys itself.</summary>
public class Projectile : MonoBehaviour
{
    private Transform target;
    private int damage;
    private float speed;
    private bool isHealing;

    // How close we need to be to "hit" the target.
    public float TARGET_RADIUS = 0.1f;

    /// <summary>
    ///     Initializes the homing projectile.
    /// </summary>
    /// <param name="target">The transform to home in on.</param>
    /// <param name="speed">How fast the projectile moves.</param>
    /// <param name="damage">Amount to apply on impact.</param>
    /// <param name="healing">If true the amount will heal instead of damage.</param>
    public void Init(Transform target, float speed, int damage, bool healing = false)
    {
        this.target = target;
        this.speed = speed;
        this.damage = damage;
        this.isHealing = healing;

        Destroy(gameObject, 5f); // Failsafe to clean up projectiles that never hit.
    }

    private void Update()
    {
        // If the target is gone before we reach it, just destroy the projectile.
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // --- Move towards target ---
        var step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // --- Rotate towards target ---
        Vector2 dir = (target.position - transform.position).normalized;
        if (dir != Vector2.zero)
        {
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // --- Check for impact ---
        if (Vector2.Distance(transform.position, target.position) < TARGET_RADIUS)
        {
            if (isHealing)
            {
                if (target.TryGetComponent(out Health h)) h.Heal(damage);
            }
            else
            {
                if (target.TryGetComponent(out IDamageable d)) d.TakeDamage(damage);
            }
            Destroy(gameObject); // Destroy self on impact.
        }
    }
}