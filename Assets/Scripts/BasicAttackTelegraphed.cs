using UnityEngine;

[RequireComponent(typeof(Health))]
public class BasicAttackTelegraphed : MonoBehaviour
{
    [Header("General")] [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask allyMask;
    [SerializeField] private HeroBalanceData balance;

    private LevelSystem levelSystem;
    private float nextAttackTime;

    private int Level => levelSystem ? levelSystem.Level : 1;

    public int BaseDamage => balance ? balance.baseDamage + balance.damagePerLevel * (Level - 1) : 0;
    public float AttackRange => balance ? balance.attackRange + balance.attackRangePerLevel * (Level - 1) : 0f;
    private float AttackRate => balance ? balance.attackRate + balance.attackRatePerLevel * (Level - 1) : 1f;
    private bool CanHealAllies => balance && balance.canHealAllies;
    private float HealRange => balance ? balance.healRange + balance.healRangePerLevel * (Level - 1) : 0f;
    private int HealAmount => balance ? balance.healAmount + balance.healAmountPerLevel * (Level - 1) : 0;
    private GameObject ProjectilePrefab => balance ? balance.projectilePrefab : null;
    private float ProjectileSpeed => balance ? balance.projectileSpeed : 0f;
    private float LookAtDuration => balance ? balance.lookAtDuration : 0.2f;

    public bool IsPlayerControlled { get; set; }

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
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
        if (CanHealAllies) combinedMask |= allyMask;

        var hit = Physics2D.OverlapPoint(mouseWorldPos, combinedMask);
        if (!hit) return;

        if (CanHealAllies && ((allyMask.value & (1 << hit.gameObject.layer)) != 0))
        {
            if (Vector2.Distance(transform.position, hit.transform.position) > HealRange) return;
            FireHeal(hit.transform, true);
        }
        else
        {
            if (Vector2.Distance(transform.position, hit.transform.position) > AttackRange) return;
            FireAt(hit.transform, true);
        }
    }

    public void Attack(Transform target)
    {
        if (Time.time < nextAttackTime) return;

        if (CanHealAllies && TryHealAlly()) return;

        if (target == null) return;

        // AI should only attack if the target is within its weapon range.
        if (Vector2.Distance(transform.position, target.position) > AttackRange) return;

        // Attack without player damage bonus
        FireAt(target, false);
    }

    private void FireAt(Transform target, bool isPlayerAttack)
    {
        var firePos = transform.position;
        var finalDamage = isPlayerAttack ? BaseDamage * 2 : BaseDamage;

        if (ProjectilePrefab == null)
        {
            Debug.LogError($"{nameof(BasicAttackTelegraphed)} on {name} has no projectile prefab set.");
            return;
        }

        var proj = Instantiate(ProjectilePrefab, firePos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(target, ProjectileSpeed, finalDamage, gameObject);

        var anim = GetComponentInChildren<HeroAnimator>();
        if (target != null && anim != null)
            anim.OverrideLookDirection(target.position - transform.position, LookAtDuration);

        nextAttackTime = Time.time + AttackRate;
    }

    private void FireHeal(Transform target, bool isPlayerAction)
    {
        var firePos = transform.position;
        var finalHeal = isPlayerAction ? HealAmount * 2 : HealAmount;

        if (ProjectilePrefab == null)
        {
            Debug.LogError($"{nameof(BasicAttackTelegraphed)} on {name} has no projectile prefab set.");
            return;
        }

        var proj = Instantiate(ProjectilePrefab, firePos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(target, ProjectileSpeed, finalHeal, gameObject, true);

        var anim = GetComponentInChildren<HeroAnimator>();
        if (target != null && anim != null)
            anim.OverrideLookDirection(target.position - transform.position, LookAtDuration);

        nextAttackTime = Time.time + AttackRate;
    }

    private bool TryHealAlly()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, HealRange, allyMask);
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
