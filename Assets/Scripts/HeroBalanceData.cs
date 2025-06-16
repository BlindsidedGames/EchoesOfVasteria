using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "HeroBalance", menuName = "SO/Hero Balance")]
public class HeroBalanceData : CharacterBalanceData
{
    [BoxGroup("Movement"), SerializeField] public float moveSpeed = 4f;
    [BoxGroup("Movement"), SerializeField] public float moveSpeedPerLevel = 0f;
}
