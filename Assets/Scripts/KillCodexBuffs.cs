using System;

public static class KillCodexBuffs
{
    public static int DamageBonus { get; private set; }
    public static int HealthBonus { get; private set; }

    public static event Action OnBuffsChanged;

    public static void ApplyBuff(int damageBonus, int healthBonus)
    {
        DamageBonus += damageBonus;
        HealthBonus += healthBonus;
        OnBuffsChanged?.Invoke();
    }
}
