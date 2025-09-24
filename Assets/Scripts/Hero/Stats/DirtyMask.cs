using System;

namespace TimelessEchoes.Hero
{
    [Flags]
    public enum DirtyMask
    {
        None = 0,
        Damage = 1 << 0,
        AttackRate = 1 << 1,
        CritChance = 1 << 2,
        MaxHealth = 1 << 3,
        Regen = 1 << 4,
        Move = 1 << 5,
        Defense = 1 << 6,
        CritDamage = 1 << 7,
        All = ~0
    }
}


