using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Provides skill-to-icon mapping for floating text and related rich text tags.
    /// </summary>
    public static class SkillIconLookup
    {
        private static readonly Dictionary<string, int> nameToIndex = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Combat", 207 },
            { "Mining", 208 },
            { "Woodcutting", 209 },
            { "Logging", 209 },
            { "Fishing", 210 },
            { "Farming", 211 },
            { "Looting", 212 }
        };

        /// <summary>
        /// Attempts to resolve the sprite index used for the supplied skill or display name.
        /// </summary>
        public static bool TryGetIconIndex(string skillName, out int index)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                index = 0;
                return false;
            }

            return nameToIndex.TryGetValue(skillName.Trim(), out index);
        }

        /// <summary>
        /// Returns a TMP rich text tag (<sprite=...>) for the provided skill, or an empty string if none exists.
        /// </summary>
        public static string GetIconTag(Skill skill)
        {
            if (skill == null)
                return string.Empty;

            if (TryGetIconIndex(skill.skillName, out var idx) || TryGetIconIndex(skill.name, out idx))
                return $"<sprite={idx}>";

            return string.Empty;
        }

        /// <summary>
        /// Formats an XP line for floating text, e.g. "<icon> +25 XP". Optionally shrinks the text size by one point when a base size is provided.
        /// Returns an empty string when the input is insufficient.
        /// </summary>
        public static string FormatXpLine(Skill skill, float xpAmount, float? baseFontSize = null)
        {
            if (skill == null || xpAmount <= 0f)
                return string.Empty;

            var icon = GetIconTag(skill);
            if (string.IsNullOrEmpty(icon))
                return string.Empty;

            var formattedAmount = FormatXpAmount(xpAmount);
            var xpText = $"{icon} +{formattedAmount} XP";

            if (baseFontSize.HasValue)
            {
                float sizedown = Mathf.Max(1f, baseFontSize.Value - 1f);
                return $"<size={sizedown:0.#}>{xpText}</size>";
            }

            return xpText;
        }

        private static string FormatXpAmount(float xpAmount)
        {
            var rounded = Mathf.RoundToInt(xpAmount);
            if (Mathf.Approximately(xpAmount, rounded))
                return rounded.ToString();

            return xpAmount.ToString("0.##");
        }
    }
}
