using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "EnemyBalance", menuName = "SO/Enemy Balance")]
public class EnemyBalanceData : CharacterBalanceData
{
    [BoxGroup("Gear"), SerializeField] public int enemyLevel = 1;
    [BoxGroup("Gear"), SerializeField, Range(0f,1f)] public float gearDropRate = 0.1f;
}
