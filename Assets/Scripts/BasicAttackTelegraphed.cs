using UnityEngine;

[RequireComponent(typeof(Health))]
public class BasicAttackTelegraphed : MonoBehaviour
{
    [Header("General")] [SerializeField] private LayerMask targetMask;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private int baseDamage = 2;
    [SerializeField] private float attackRange = 15f;

    // Public property to expose the range
    public float AttackRange => attackRange;

    [Header("Ranged")] [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float lookAtDuration = 0.2f;

    public bool IsPlayerControlled { get; set; }
    private float nextAttackTime;

    private void Update()
    {
        // Player-controlled attacks are still initiated from Update via clicks.
        if (IsPlayerControlled && Time.time >= nextAttackTime) TryPlayerAttack();
    }

    private void TryPlayerAttack()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(mouseWorldPos, targetMask);
        if (!hit) return;

        if (Vector2.Distance(transform.position, hit.transform.position) > attackRange) return;

        // Attack with player damage bonus
        FireAt(hit.transform, true);
    }

    public void Attack(Transform target)
    {
        if (Time.time < nextAttackTime || target == null) return;

        // AI should only attack if the target is within its weapon range.
        if (Vector2.Distance(transform.position, target.position) > attackRange) return;

        // Attack without player damage bonus
        FireAt(target, false);
    }

    private void FireAt(Transform target, bool isPlayerAttack)
    {
        var firePos = transform.position;
        var finalDamage = isPlayerAttack ? baseDamage * 2 : baseDamage;

        if (projectilePrefab == null)
        {
            Debug.LogError($"{nameof(BasicAttackTelegraphed)} on {name} has no projectile prefab set.");
            return;
        }

        var proj = Instantiate(projectilePrefab, firePos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(target, projectileSpeed, finalDamage);

        var anim = GetComponentInChildren<HeroAnimator>();
        if (target != null && anim != null)
            anim.OverrideLookDirection(target.position - transform.position, lookAtDuration);

        nextAttackTime = Time.time + attackRate;

        if (CompareTag("Hero") && TryGetComponent(out LevelSystem lvl))
            lvl.GrantXP(1);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}