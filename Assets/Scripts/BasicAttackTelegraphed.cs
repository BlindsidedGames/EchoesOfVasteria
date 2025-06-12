using UnityEngine;

[RequireComponent(typeof(Health))]
public class BasicAttackTelegraphed : MonoBehaviour
{
    [Header("General")] [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask allyMask;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private int baseDamage = 2;

    /// <summary>Base damage dealt by this hero.</summary>
    public int BaseDamage => baseDamage;
    [SerializeField] private float attackRange = 15f;

    [Header("Healing")] [SerializeField] private bool canHealAllies = false;
    [SerializeField] private float healRange = 10f;
    [SerializeField] private int healAmount = 2;

    // Public property to expose the range
    public float AttackRange => attackRange;

    [Header("Ranged")] [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float lookAtDuration = 0.2f;

    /// <summary>
    /// Called by <see cref="HeroBalance"/> to apply balance values.
    /// </summary>
    public void InitializeStats(int dmg, float rate, float range, float projSpeed,
        float lookDuration, GameObject projPrefab, bool healAllies, float hRange,
        int hAmount)
    {
        baseDamage = dmg;
        attackRate = rate;
        attackRange = range;
        projectileSpeed = projSpeed;
        lookAtDuration = lookDuration;
        projectilePrefab = projPrefab;
        canHealAllies = healAllies;
        healRange = hRange;
        healAmount = hAmount;
    }

    public bool IsPlayerControlled { get; set; }
    private LevelSystem levelSystem;
    private float nextAttackTime;

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
        if (levelSystem != null)
            levelSystem.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
            levelSystem.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        baseDamage += 1;
    }

    private void Update()
    {
        // Player-controlled attacks are still initiated from Update via clicks.
        if (IsPlayerControlled && Time.time >= nextAttackTime) TryPlayerAttack();
    }

    private void TryPlayerAttack()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Build a mask of potential targets. Explicitly cast to LayerMask so the
        // conditional expression does not confuse ints with masks.
        LayerMask combinedMask = targetMask;
        if (canHealAllies) combinedMask |= allyMask;

        var hit = Physics2D.OverlapPoint(mouseWorldPos, combinedMask);
        if (!hit) return;

        if (canHealAllies && ((allyMask.value & (1 << hit.gameObject.layer)) != 0))
        {
            if (Vector2.Distance(transform.position, hit.transform.position) > healRange) return;
            FireHeal(hit.transform, true);
        }
        else
        {
            if (Vector2.Distance(transform.position, hit.transform.position) > attackRange) return;
            FireAt(hit.transform, true);
        }
    }

    public void Attack(Transform target)
    {
        if (Time.time < nextAttackTime) return;

        if (canHealAllies && TryHealAlly()) return;

        if (target == null) return;

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
        proj.GetComponent<Projectile>().Init(target, projectileSpeed, finalDamage, gameObject);

        var anim = GetComponentInChildren<HeroAnimator>();
        if (target != null && anim != null)
            anim.OverrideLookDirection(target.position - transform.position, lookAtDuration);

        nextAttackTime = Time.time + attackRate;
    }

    private void FireHeal(Transform target, bool isPlayerAction)
    {
        var firePos = transform.position;
        var finalHeal = isPlayerAction ? healAmount * 2 : healAmount;

        if (projectilePrefab == null)
        {
            Debug.LogError($"{nameof(BasicAttackTelegraphed)} on {name} has no projectile prefab set.");
            return;
        }

        var proj = Instantiate(projectilePrefab, firePos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(target, projectileSpeed, finalHeal, gameObject, true);

        var anim = GetComponentInChildren<HeroAnimator>();
        if (target != null && anim != null)
            anim.OverrideLookDirection(target.position - transform.position, lookAtDuration);

        nextAttackTime = Time.time + attackRate;
    }

    private bool TryHealAlly()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, healRange, allyMask);
        Health lowest = null;
        float lowestPct = 1f;

        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            if (!h.TryGetComponent(out Health hp)) continue;
            if (hp.CurrentHP >= hp.MaxHP) continue;

            var pct = (float)hp.CurrentHP / hp.MaxHP;
            if (pct < lowestPct)
            {
                lowestPct = pct;
                lowest = hp;
            }
        }

        if (lowest != null)
        {
            FireHeal(lowest.transform, false);
            return true;
        }

        return false;
    }

}
