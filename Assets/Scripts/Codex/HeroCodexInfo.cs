using UnityEngine;

/// <summary>
/// Metadata component that defines a hero identifier for codex bonuses.
/// </summary>
public class HeroCodexInfo : MonoBehaviour
{
    [Tooltip("Unique identifier used for hero-specific codex bonuses.")]
    [SerializeField] private string heroId = "";

    public string HeroId => heroId;
}
