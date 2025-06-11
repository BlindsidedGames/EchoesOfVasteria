/// <summary>Standard interface so any object can receive damage.</summary>
using UnityEngine;

public interface IDamageable
{
    /// <summary>Apply damage from an attacker.</summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="attacker">The GameObject responsible for the damage.</param>
    void TakeDamage(int amount, GameObject attacker);
}