using System.Collections.Generic;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Defines a consistent display order for hero-related stats across the UI.
    /// Desired order: Attack, Attack Speed, Crit Chance, Health, Regeneration, Defense, Movement.
    /// </summary>
    public static class StatSortOrder
    {
        private static readonly Dictionary<HeroStatMapping, int> orderIndex = new()
        {
            { HeroStatMapping.Damage, 0 },
            { HeroStatMapping.AttackRate, 1 },
            { HeroStatMapping.CritChance, 2 },
            { HeroStatMapping.MaxHealth, 3 },
            { HeroStatMapping.HealthRegen, 4 },
            { HeroStatMapping.Defense, 5 },
            { HeroStatMapping.MoveSpeed, 6 }
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


