using TimelessEchoes.UI.Toolkit;
using System.Text;

namespace TimelessEchoes.Upgrades.Cauldron
{
    public static class TastingStatsFormatter
    {
        public static void Format(StringBuilder sb, CauldronManager.TastingStats stats, bool showSubcategories = true)
        {
            sb.Clear();
            sb.Append(ToolkitLocalization.Text("town-stats.tastings", "Tastings")).Append(": ").Append(stats.tastings.ToString("N0")).Append('\n');
            sb.Append(ToolkitLocalization.Text("town-stats.cards", "Cards gained")).Append(": ").Append(stats.cardsGained.ToString("N0"));
        }
        public static string FormatToString(CauldronManager.TastingStats stats, bool showSubcategories = true)
        {
            var sb = new StringBuilder(); Format(sb, stats, showSubcategories); return sb.ToString();
        }
    }
}
