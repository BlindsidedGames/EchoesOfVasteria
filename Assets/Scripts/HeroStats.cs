using UnityEngine;

/// <summary>Simple container for hero base stats.</summary>
public class HeroStats : MonoBehaviour
{
    [SerializeField] private int defense = 1;

    public int Defense => defense;
}
