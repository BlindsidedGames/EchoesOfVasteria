using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "HeroBalance", menuName = "SO/Hero Balance")]
public class HeroBalanceData : ScriptableObject
{
    [BoxGroup("Stats"), SerializeField] public int baseHealth = 10;
    [BoxGroup("Stats"), SerializeField] public int healthPerLevel = 0;
    [BoxGroup("Stats"), SerializeField] public int baseDefense = 1;
    [BoxGroup("Stats"), SerializeField] public int defensePerLevel = 0;

    [BoxGroup("Combat"), SerializeField] public int baseDamage = 2;
    [BoxGroup("Combat"), SerializeField] public int damagePerLevel = 1;
    [BoxGroup("Combat"), SerializeField, Tooltip("Time between attacks in seconds. 0.5 means one attack every half second (2 attacks per second).")]
    public float attackRate = 1f;
    [BoxGroup("Combat"), SerializeField] public float attackRatePerLevel = 0f;
    [BoxGroup("Combat"), SerializeField] public float attackRange = 4f;
    [BoxGroup("Combat"), SerializeField] public float attackRangePerLevel = 0f;
    [BoxGroup("Combat"), SerializeField] public GameObject projectilePrefab;
    [BoxGroup("Combat"), SerializeField] public float projectileSpeed = 6f;
    [BoxGroup("Combat"), SerializeField] public float lookAtDuration = 0.2f;

    [BoxGroup("Healing"), SerializeField] public bool canHealAllies = false;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")] public float healRange = 10f;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")] public float healRangePerLevel = 0f;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")] public int healAmount = 2;
    [BoxGroup("Healing"), SerializeField, ShowIf("canHealAllies")] public int healAmountPerLevel = 0;

    [BoxGroup("AI"), SerializeField] public float visionRange = 20f;
    [BoxGroup("AI"), SerializeField] public float visionRangePerLevel = 0f;
    [BoxGroup("AI"), SerializeField] public float safeDistance = 8f;
    [BoxGroup("AI"), SerializeField] public float safeDistancePerLevel = 0f;
}
