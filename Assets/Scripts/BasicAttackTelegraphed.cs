using UnityEngine;
using System.Collections;

/// <summary>
/// Handles both melee (circular telegraph) and ranged (projectile) attacks.
/// Active hero flag doubles damage and lets mouse aim override auto-targeting.
/// </summary>
[RequireComponent(typeof(Health))]
public class BasicAttackTelegraphed : MonoBehaviour
{
    /* ─── General ───────────────────────────────────────────────────────────── */
    [Header("General")]
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField] private LayerMask  targetMask;
    [SerializeField] private float      attackRate = 1f;
    [SerializeField] private int        baseDamage = 2;

    /* ─── Melee ─────────────────────────────────────────────────────────────── */
    [Header("Melee")]
    [SerializeField] private float      meleeRange           = 0.75f;
    [SerializeField] private float      telegraphTime        = 0.25f;
    [SerializeField] private GameObject circleTelegraphPrefab;

    /* ─── Ranged ────────────────────────────────────────────────────────────── */
    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float      projectileSpeed = 6f;

    /* Flag toggled by PartyManager */
    public bool IsPlayerControlled { get; set; }

    private float nextAttackTime;

    private void Update()
    {
        if (Time.time < nextAttackTime) return;

        if (attackType == AttackType.Melee)
            TryMeleeAttack();
        else
            TryRangedAttack();
    }

    /* ─── Melee Logic ───────────────────────────────────────────────────────── */
    private void TryMeleeAttack()
    {
        Collider2D target = IsPlayerControlled
            ? GetMouseTarget(meleeRange)
            : Physics2D.OverlapCircle(transform.position, meleeRange, targetMask);

        if (!target) return;

        StartCoroutine(MeleeRoutine());
        nextAttackTime = Time.time + attackRate;
    }

    private IEnumerator MeleeRoutine()
    {
        // 1. Telegraph
        GameObject ring = Instantiate(circleTelegraphPrefab, transform.position, Quaternion.identity);
        float scale = meleeRange * 2f; // diameter
        ring.transform.localScale = new Vector3(scale, scale, 1f);

        yield return new WaitForSeconds(telegraphTime);

        // 2. Apply damage to everything still in range
        var hits = Physics2D.OverlapCircleAll(transform.position, meleeRange, targetMask);
        foreach (var h in hits)
        {
            if (h.TryGetComponent(out IDamageable d))
            {
                int dmg = IsPlayerControlled ? baseDamage * 2 : baseDamage;
                d.TakeDamage(dmg);

                // Grant XP to hero attackers
                if (CompareTag("Hero") && TryGetComponent(out LevelSystem lvl)) lvl.GrantXP(dmg);
            }
        }

        Destroy(ring);
    }

    /* ─── Ranged Logic ──────────────────────────────────────────────────────── */
    private void TryRangedAttack()
    {
        Vector3 firePos   = transform.position;
        Vector3 targetPos;

        if (IsPlayerControlled)
        {
            targetPos      = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPos.z    = 0f;
        }
        else
        {
            Collider2D tgt = Physics2D.OverlapCircle(transform.position, 50f, targetMask);
            if (!tgt) return;
            targetPos      = tgt.transform.position;
        }

        Vector2 dir = (targetPos - firePos).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(
            dir,
            projectileSpeed,
            IsPlayerControlled ? baseDamage * 2 : baseDamage,
            targetMask);

        nextAttackTime = Time.time + attackRate;

        // Grant XP immediately for ranged heroes?  -> Comment out if undesired
        if (CompareTag("Hero") && TryGetComponent(out LevelSystem lvl)) lvl.GrantXP(1);
    }

    /* ─── Helpers ───────────────────────────────────────────────────────────── */
    private Collider2D GetMouseTarget(float radius)
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        return Physics2D.OverlapCircle(mw, radius, targetMask);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackType == AttackType.Melee)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, meleeRange);
        }
    }
#endif
}
