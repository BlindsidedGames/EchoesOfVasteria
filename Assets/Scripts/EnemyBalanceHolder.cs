using UnityEngine;

/// <summary>
/// Holds a reference to <see cref="EnemyBalanceData"/> so enemy
/// components can access it without each needing a serialized field.
/// </summary>
public class EnemyBalanceHolder : MonoBehaviour
{
    [SerializeField] private EnemyBalanceData balance;

    public EnemyBalanceData Balance => balance;
}
