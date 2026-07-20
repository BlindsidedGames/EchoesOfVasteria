using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Skills;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Provides cached access to <see cref="BaseStat"/> assets and utilities to calculate their totals
    /// including bonuses from the skill system.
    /// </summary>
    public static class BaseStatService
    {
        private static BaseStat[] cachedStats;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => cachedStats = null;

        /// <summary>
        /// Returns every <see cref="BaseStat"/> available to the project. Falls back to an empty sequence
        /// when nothing can be loaded so callers never handle nulls.
        /// </summary>
        public static IEnumerable<BaseStat> AllStats
        {
            get
            {
                EnsureLoaded();
                return cachedStats;
            }
        }

        /// <summary>
        /// Finds a base stat by name (exact match) or returns null when missing.
        /// </summary>
        public static BaseStat GetStat(string statName)
        {
            if (string.IsNullOrEmpty(statName))
                return null;

            EnsureLoaded();
            return cachedStats.FirstOrDefault(s => s != null && string.Equals(s.name, statName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Raw value authored on the ScriptableObject.
        /// </summary>
        public static float GetBaseValue(BaseStat stat)
        {
            return stat != null ? stat.BaseValue : 0f;
        }

        /// <summary>
        /// Total value after adding flat and percent bonuses granted by the skill system.
        /// </summary>
        public static float GetTotalValue(BaseStat stat)
        {
            if (stat == null)
                return 0f;

            var baseValue = stat.BaseValue;
            var skillController = SkillController.Instance;
            var flat = skillController ? skillController.GetFlatStatBonus(stat) : 0f;
            var percent = skillController ? skillController.GetPercentStatBonus(stat) : 0f;

            var totalFlat = baseValue + flat;
            if (Mathf.Approximately(percent, 0f))
                return totalFlat;

            var associated = stat.AssociatedStat;
            if (associated != null && associated.isPercent)
                return totalFlat + percent * 100f;

            return totalFlat * (1f + percent);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Clears the cached assets so the next access will refresh from disk. Useful during editor tooling/tests.
        /// </summary>
        public static void Reload()
        {
            cachedStats = null;
        }
#endif

        private static void EnsureLoaded()
        {
            if (cachedStats != null)
                return;

            var stats = Resources.LoadAll<BaseStat>("StatUpgrades");
            if (stats == null || stats.Length == 0)
                stats = Resources.LoadAll<BaseStat>(string.Empty);

            cachedStats = stats != null && stats.Length > 0 ? stats : Array.Empty<BaseStat>();
        }
    }
}

