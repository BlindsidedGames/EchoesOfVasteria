using System.Collections.Generic;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Defines a consistent display order for hero-related stats across the UI.
    /// Desired order: Attack, Attack Speed, Crit Chance, Crit Damage, Health, Regeneration, Defense, Movement.
    /// </summary>
    public static class StatSortOrder
    {
        private static readonly Dictionary<HeroStatMapping, int> orderIndex = new()
        {
            { HeroStatMapping.Damage, 0 },
            { HeroStatMapping.AttackRate, 1 },
            { HeroStatMapping.CritChance, 2 },
            { HeroStatMapping.CritDamage, 3 },
            { HeroStatMapping.MaxHealth, 4 },
            { HeroStatMapping.HealthRegen, 5 },
            { HeroStatMapping.Defense, 6 },
            { HeroStatMapping.MoveSpeed, 7 }
        };

        public static int GetIndex(HeroStatMapping mapping)
        {
            return orderIndex.TryGetValue(mapping, out var idx) ? idx : int.MaxValue;
        }

        public static int Compare(HeroStatMapping a, HeroStatMapping b)
        {
            return GetIndex(a).CompareTo(GetIndex(b));
        }
    }
}



