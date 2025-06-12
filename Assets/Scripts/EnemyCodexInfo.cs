using UnityEngine;

/// <summary>
/// Metadata component for enemies providing a codex identifier.
/// </summary>
public class EnemyCodexInfo : MonoBehaviour
{
    [Tooltip("Unique identifier used for codex kill tracking.")]
    [SerializeField] private string enemyId = "";

    public string EnemyId => enemyId;
}
