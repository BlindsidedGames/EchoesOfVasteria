using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// Draws gizmos representing the ranges defined in <see cref="CharacterBalanceData"/>.
/// Attach alongside <see cref="BalanceHolder"/> on the same GameObject.
/// </summary>
[ExecuteAlways]
public class BalanceGizmos : MonoBehaviour
{
    private BalanceHolder holder;
    private LevelSystem levelSystem;

    private void Awake()
    {
        holder = GetComponent<BalanceHolder>();
        levelSystem = GetComponent<LevelSystem>();
    }

    private void OnDrawGizmosSelected()
    {
        if (holder == null) holder = GetComponent<BalanceHolder>();
        if (levelSystem == null) levelSystem = GetComponent<LevelSystem>();

        var balance = holder ? holder.Balance : null;
        if (balance == null) return;

        var level = levelSystem ? levelSystem.Level : 1;

        float attackRange = balance.attackRange + balance.attackRangePerLevel * (level - 1);
        float healRange = balance.canHealAllies ? balance.healRange + balance.healRangePerLevel * (level - 1) : 0f;
        float visionRange = balance.visionRange + balance.visionRangePerLevel * (level - 1);
        float safeDistance = balance.safeDistance + balance.safeDistancePerLevel * (level - 1);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (balance.canHealAllies)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safeDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
#endif
