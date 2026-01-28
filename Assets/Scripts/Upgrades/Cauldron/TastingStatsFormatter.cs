using System.Text;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Formats TastingStats for UI display.
    /// Provides consistent formatting of cauldron tasting statistics across the UI.
    /// </summary>
    public static class TastingStatsFormatter
    {
        /// <summary>
        /// Formats the tasting stats into the provided StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to write to. Will be cleared first.</param>
        /// <param name="stats">The tasting stats to format.</param>
        /// <param name="showSubcategories">
        /// When true and AE subcategory data exists, shows individual AE categories (Farming, Fishing, etc.).
        /// When false or no subcategory data exists, shows only the total Alter-Echo count.
        /// </param>
        public static void Format(StringBuilder sb, CauldronManager.TastingStats stats, bool showSubcategories = true)
        {
            sb.Clear();

            // Header - Totals section
            sb.Append("<b>Totals</b>\n");
            sb.Append("• Tastings: ");
            sb.Append(stats.tastings.ToString("N0"));
            sb.Append('\n');
            sb.Append("• Cards Gained: ");
            sb.Append(stats.cardsGained.ToString("N0"));
            sb.Append('\n');

            // Roll Distribution section
            sb.Append("<b>Roll Distribution</b>\n");
            sb.Append("• Gained Nothing: ");
            sb.Append(stats.gainedNothing.ToString("N0"));
            sb.Append('\n');

            // AE subcategories or total
            var hasSubcategoryData = stats.aeFarming + stats.aeFishing + stats.aeMining +
                                     stats.aeWoodcutting + stats.aeLooting + stats.aeCombat > 0;

            if (showSubcategories && hasSubcategoryData)
            {
                sb.Append("• AE - Farming: ");
                sb.Append(stats.aeFarming.ToString("N0"));
                sb.Append('\n');
                sb.Append("• AE - Fishing: ");
                sb.Append(stats.aeFishing.ToString("N0"));
                sb.Append('\n');
                sb.Append("• AE - Mining: ");
                sb.Append(stats.aeMining.ToString("N0"));
                sb.Append('\n');
                sb.Append("• AE - Logging: ");
                sb.Append(stats.aeWoodcutting.ToString("N0"));
                sb.Append('\n');
                sb.Append("• AE - Looting: ");
                sb.Append(stats.aeLooting.ToString("N0"));
                sb.Append('\n');
                sb.Append("• AE - Combat: ");
                sb.Append(stats.aeCombat.ToString("N0"));
                sb.Append('\n');
            }
            else
            {
                sb.Append("• Alter-Echo: ");
                sb.Append(stats.alterEcho.ToString("N0"));
                sb.Append('\n');
            }

            // Remaining roll types
            sb.Append("• Buffs: ");
            sb.Append(stats.buffs.ToString("N0"));
            sb.Append('\n');
            sb.Append("• Low Cards: ");
            sb.Append(stats.lowCards.ToString("N0"));
            sb.Append('\n');
            sb.Append("• Eva's Blessing: ");
            sb.Append(stats.evasBlessing.ToString("N0"));
            sb.Append('\n');
            sb.Append("• The Vast One's Surge: ");
            sb.Append(stats.vastSurge.ToString("N0"));
            // Note: No trailing newline after the last line to match original behavior
        }

        /// <summary>
        /// Formats the tasting stats and returns them as a new string.
        /// </summary>
        /// <param name="stats">The tasting stats to format.</param>
        /// <param name="showSubcategories">
        /// When true and AE subcategory data exists, shows individual AE categories.
        /// When false or no subcategory data exists, shows only the total Alter-Echo count.
        /// </param>
        /// <returns>The formatted stats string.</returns>
        public static string FormatToString(CauldronManager.TastingStats stats, bool showSubcategories = true)
        {
            var sb = new StringBuilder(512);
            Format(sb, stats, showSubcategories);
            return sb.ToString();
        }
    }
}
