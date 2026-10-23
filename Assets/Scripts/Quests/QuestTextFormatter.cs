using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Quests
{
	public static class QuestTextFormatter
	{
		// Keep inline resource icons separated from their counts in both quest views.
		public const string ResourceIconSpacing = "\u00a0\u00a0";

		public static string BuildGoalText(QuestData data, GameData.QuestRecord rec, bool includeTitleLine)
		{
			if (data == null || data.requirements == null)
				return string.Empty;

			var sb = new StringBuilder();
            if (includeTitleLine) sb.AppendLine(data.questName.GetLocalizedString());
            foreach (var req in data.requirements)
                if (req != null && req.type != QuestData.RequirementType.Instant)
                    sb.AppendLine(QuestRequirementPresentation.Build(data, req, rec).text);
            return sb.ToString();
        }

		private static string FormatValue(QuestData data, double value)
		{
			return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0", LocaleNumbers.Culture) : CalcUtils.FormatNumber(value, true);
		}
	}
}

