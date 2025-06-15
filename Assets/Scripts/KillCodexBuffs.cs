using System;
using UnityEngine;

/// <summary>
/// Global bonuses granted from the kill codex. Other systems update these
/// values when certain kill thresholds are met and notify listeners via
/// <see cref="BuffsChanged"/>.
/// </summary>
public static class KillCodexBuffs
{
    public static int BonusHealth { get; private set; }
    public static int BonusDefense { get; private set; }
    public static int BonusDamage { get; private set; }
    public static float BonusCritChance { get; private set; }

    /// <summary>
    /// Invoked whenever any buff value changes.
    /// </summary>
    public static event Action BuffsChanged;

    /// <summary>
    /// Set new buff values and notify listeners if anything changed.
    /// </summary>
    public static void SetBuffs(int health, int defense, int damage, float crit)
    {
        if (BonusHealth == health && BonusDefense == defense && BonusDamage == damage &&
            Mathf.Approximately(BonusCritChance, crit))
            return;

        BonusHealth = health;
        BonusDefense = defense;
        BonusDamage = damage;
        BonusCritChance = crit;
        BuffsChanged?.Invoke();
    }

    public static void AddBuffs(CodexBonusStats stats)
    {
        SetBuffs(BonusHealth + stats.bonusHealth,
                 BonusDefense + stats.bonusDefense,
                 BonusDamage + stats.bonusDamage,
                 BonusCritChance + stats.bonusCritChance);
    }

    /// <summary>
    /// Reset all codex buffs to zero.
    /// </summary>
    public static void Reset()
    {
        SetBuffs(0, 0, 0, 0f);
    }
}
