using System.Text;
using Blindsided.SaveData;
using TimelessEchoes.UI.Toolkit;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>Compatibility text for the retired Canvas view; active statistics use the Town page.</summary>
    public sealed class ForgeStatisticsPresentation
    {
        public string BuildStatsText(GameData.ForgeStats forge, bool preserveSectionIds = false)
        {
            var text = new StringBuilder();
            text.AppendLine(ToolkitLocalization.Text("town-stats.crafted", "Items crafted") + ": " + (forge?.TotalCrafts ?? 0).ToString("N0"));
            text.AppendLine(ToolkitLocalization.Text("town-stats.salvaged", "Items salvaged") + ": " + (forge?.TotalSalvaged ?? 0).ToString("N0"));
            return text.ToString();
        }
    }
}
