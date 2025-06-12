using System;

public static class KillCodexBuffs
{
    public struct Buffs
    {
        public int DamageBonus;
        public int HealthBonus;
        public float CritChanceBonus;
    }

    private static Buffs globalBuffs;
    private static readonly Dictionary<string, Buffs> heroBuffs = new();

    public static event Action OnBuffsChanged;

    public static void ApplyGlobalBuff(int damageBonus, int healthBonus, float critChance)
    {
        globalBuffs.DamageBonus += damageBonus;
        globalBuffs.HealthBonus += healthBonus;
        globalBuffs.CritChanceBonus += critChance;
        OnBuffsChanged?.Invoke();
    }

    public static void ApplyHeroBuff(string hero, int damageBonus, int healthBonus, float critChance)
    {
        if (!heroBuffs.TryGetValue(hero, out var b))
            b = new Buffs();

        b.DamageBonus += damageBonus;
        b.HealthBonus += healthBonus;
        b.CritChanceBonus += critChance;
        heroBuffs[hero] = b;
        OnBuffsChanged?.Invoke();
    }

    public static int GetDamageBonus(string hero)
    {
        int dmg = globalBuffs.DamageBonus;
        if (heroBuffs.TryGetValue(hero, out var b))
            dmg += b.DamageBonus;
        return dmg;
    }

    public static int GetHealthBonus(string hero)
    {
        int hp = globalBuffs.HealthBonus;
        if (heroBuffs.TryGetValue(hero, out var b))
            hp += b.HealthBonus;
        return hp;
    }

    public static float GetCritChanceBonus(string hero)
    {
        float c = globalBuffs.CritChanceBonus;
        if (heroBuffs.TryGetValue(hero, out var b))
            c += b.CritChanceBonus;
        return c;
    }
}
