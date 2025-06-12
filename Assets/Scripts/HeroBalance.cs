using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BasicAttackTelegraphed))]
[RequireComponent(typeof(HeroAI))]
public class HeroBalance : MonoBehaviour
{
    [BoxGroup("Stats"), SerializeField]
    private int baseHealth = 10;
    [BoxGroup("Stats"), SerializeField]
    private int baseDefense = 1;
    [BoxGroup("Combat"), SerializeField]
    private int baseDamage = 2;
    [BoxGroup("Combat"), SerializeField]
    private float attackRate = 1f;
    [BoxGroup("Combat"), SerializeField]
    private float attackRange = 4f;
    [BoxGroup("Combat"), SerializeField]
    private GameObject projectilePrefab;
    [BoxGroup("Combat"), SerializeField]
    private float projectileSpeed = 6f;
    [BoxGroup("Combat"), SerializeField]
    private float lookAtDuration = 0.2f;

    [BoxGroup("Healing"), SerializeField]
    private bool canHealAllies = false;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")]
    private float healRange = 10f;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")]
    private int healAmount = 2;

    [BoxGroup("AI"), SerializeField]
    private float visionRange = 20f;
    [BoxGroup("AI"), SerializeField]
    private float safeDistance = 8f;

    private void Start()
    {
        var hp = GetComponent<Health>();
        if (hp != null) hp.SetBaseStats(baseHealth, baseDefense);

        var atk = GetComponent<BasicAttackTelegraphed>();
        if (atk != null)
            atk.InitializeStats(baseDamage, attackRate, attackRange, projectileSpeed,
                lookAtDuration, projectilePrefab, canHealAllies, healRange, healAmount);

        var ai = GetComponent<HeroAI>();
        if (ai != null) ai.InitializeStats(visionRange, safeDistance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
        if (canHealAllies)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healRange);
        }
    }
#endif
}
