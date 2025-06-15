using UnityEngine;

/// <summary>
/// Holds a reference to <see cref="CharacterBalanceData"/> so other
/// components can access it without each needing a serialized field.
/// </summary>
public class BalanceHolder : MonoBehaviour
{
    [SerializeField] private CharacterBalanceData balance;

    public CharacterBalanceData Balance => balance;
}
