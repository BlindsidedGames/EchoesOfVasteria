using System;

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

    /// <summary>
    /// Invoked whenever any buff value changes.
    /// </summary>
    public static event Action BuffsChanged;

    /// <summary>
    /// Set new buff values and notify listeners if anything changed.
    /// </summary>
    public static void SetBuffs(int health, int defense, int damage)
    {
        if (BonusHealth == health && BonusDefense == defense && BonusDamage == damage)
            return;

        BonusHealth = health;
        BonusDefense = defense;
        BonusDamage = damage;
        BuffsChanged?.Invoke();
    }
}
