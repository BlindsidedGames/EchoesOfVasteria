using System;
using System.Collections.Generic;

namespace TimelessEchoes.Gear
{
    /// <summary>
    ///     Helper utilities for working with StatDefSO definitions in migrations and systems.
    ///     Centralizes id/name resolution and matching to avoid duplicated logic.
    /// </summary>
    public static class StatDefUtils
    {
        /// <summary>
        ///     Resolves a stat definition by matching the provided identifier against the
        ///     definition's id (preferred) or asset name, using case-insensitive comparison.
        /// </summary>
        public static StatDefSO ResolveStatByIdOrName(IEnumerable<StatDefSO> allStats, string idOrName)
        {
            if (allStats == null || string.IsNullOrWhiteSpace(idOrName))
                return null;

            // Prefer matching against explicit id
            foreach (var s in allStats)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.id)) continue;
                if (string.Equals(s.id, idOrName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }

            // Fallback to asset name match
            foreach (var s in allStats)
            {
                if (s == null) continue;
                if (string.Equals(s.name, idOrName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }

            return null;
        }

        /// <summary>
        ///     Returns true if the provided statId matches the definition's id or asset name
        ///     (case-insensitive). Null/empty inputs never match.
        /// </summary>
        public static bool MatchesStatId(string statId, StatDefSO def)
        {
            if (def == null || string.IsNullOrWhiteSpace(statId)) return false;
            if (!string.IsNullOrWhiteSpace(def.id) &&
                string.Equals(statId, def.id, StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(statId, def.name, StringComparison.OrdinalIgnoreCase);
        }
    }
}

