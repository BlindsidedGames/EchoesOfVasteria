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
		public static string BuildGoalText(QuestData data, GameData.QuestRecord rec, bool includeTitleLine)
		{
			if (data == null || data.requirements == null)
				return string.Empty;

			var resourceManager = ResourceManager.Instance;
			var tracker = GameplayStatTracker.Instance;

			var sb = new StringBuilder();
			if (includeTitleLine)
				sb.AppendLine(data.questName.GetLocalizedString());

			foreach (var req in data.requirements)
			{
				if (req == null) continue;
				double current = 0;
				double target = req.amount;

				switch (req.type)
				{
					case QuestData.RequirementType.Resource:
						current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
						break;
					case QuestData.RequirementType.Kill:
						if (rec != null)
						{
							if (req.enemies != null && req.enemies.Count > 0)
							{
								foreach (var enemy in req.enemies)
									if (enemy != null && rec.KillProgress.TryGetValue(enemy.name, out var c))
										current += c;
							}
							else
							{
								if (rec.KillProgress.TryGetValue("ANY", out var any))
									current = any;
							}
						}
						break;
					case QuestData.RequirementType.DistanceRun:
						current = tracker ? tracker.LongestRun : 0f;
						break;
					case QuestData.RequirementType.DistanceTravel:
						current = rec != null ? rec.DistanceTravelProgress : 0;
						break;
					case QuestData.RequirementType.BuffCast:
						if (req.buffs == null || req.buffs.Count == 0)
						{
							current = tracker ? tracker.BuffsCast : 0;
							if (rec != null)
								current -= rec.BuffCastBaseline;
						}
						else
						{
							current = 0;
							if (rec != null && rec.BuffCastProgress != null)
							{
								foreach (var b in req.buffs)
								{
									if (b == null) continue;
									if (rec.BuffCastProgress.TryGetValue(b.name, out var c))
										current += c;
								}
							}
						}
						break;
					case QuestData.RequirementType.CriticalStrike:
						current = tracker ? tracker.CriticalHits : 0;
						if (rec != null)
							current -= rec.CriticalBaseline;
						break;
					case QuestData.RequirementType.ResourcesGathered:
						current = tracker ? tracker.TotalResourcesGathered : 0;
						if (rec != null)
							current -= rec.ResourcesBaseline;
						break;
					case QuestData.RequirementType.TasksCompleted:
						current = tracker ? tracker.TasksCompleted : 0;
						break;
					case QuestData.RequirementType.CauldronMix:
						current = rec != null ? rec.CauldronMixProgress : 0;
						break;
					case QuestData.RequirementType.Instant:
						current = target;
						break;
					case QuestData.RequirementType.Meet:
						current = (!string.IsNullOrEmpty(req.meetNpcId) && Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains(req.meetNpcId)) ? target : 0;
						break;
				}

				if (req.type == QuestData.RequirementType.Resource)
				{
					var iconTag = string.Empty;
					if (req.resource)
					{
						var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
						iconTag = unlocked
							? ResourceIconLookup.GetIconTag(req.resource.resourceID)
							: ResourceIconLookup.GetUnknownIconTag(req.resource.resourceID);
					}

					var fallbackName = req.resource ? req.resource.name : string.Empty;
					var label = string.IsNullOrEmpty(iconTag) ? fallbackName : iconTag;
					var separator = string.IsNullOrEmpty(iconTag) ? ": " : " ";

					if (target <= 0)
						sb.AppendLine($"<size=90%>{label}{separator}{FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=90%>{label}{separator}{FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.Kill && !string.IsNullOrEmpty(req.killName))
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Kill {req.killName}: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Kill {req.killName}: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.Kill && (req.enemies == null || req.enemies.Count == 0))
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Kill enemies: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Kill enemies: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.BuffCast && req.buffs != null && req.buffs.Count > 0 && !req.includeAutoCasts)
				{
					var label = !string.IsNullOrEmpty(req.buffCastName)
						? req.buffCastName
						: string.Join(", ", req.buffs.FindAll(b => b != null).ConvertAll(b => b.GetDisplayName()));
					if (target <= 0)
						sb.AppendLine($"<size=80%>Manually cast {label}: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Manually cast {label}: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.CriticalStrike)
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Critical hits: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Critical hits: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.ResourcesGathered)
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Gather resources: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Gather resources: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.TasksCompleted)
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Tasks completed: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Tasks completed: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.CauldronMix)
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Mix {FormatValue(data, current)} Resources</size>");
					else
						sb.AppendLine($"<size=80%>Mix {FormatValue(data, current)} / {FormatValue(data, target)} Resources</size>");
				}
				else if (req.type == QuestData.RequirementType.Meet)
				{
					// Meeting quests should display a simple instruction without any counts
					sb.AppendLine("<size=80%>Meet an NPC</size>");
				}
				else if (req.type == QuestData.RequirementType.DistanceTravel)
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>Steps Taken: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Steps Taken: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.BuffCast && req.buffs != null && req.buffs.Count > 0)
				{
					var label = !string.IsNullOrEmpty(req.buffCastName)
						? req.buffCastName
						: string.Join(", ", req.buffs.FindAll(b => b != null).ConvertAll(b => b.GetDisplayName()));
					if (target <= 0)
						sb.AppendLine($"<size=80%>Cast {label}: {FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>Cast {label}: {FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
				else if (req.type == QuestData.RequirementType.Instant)
				{
					// Leave instant quests blank until turned in
				}
				else
				{
					if (target <= 0)
						sb.AppendLine($"<size=80%>{FormatValue(data, current)}</size>");
					else
						sb.AppendLine($"<size=80%>{FormatValue(data, current)} / {FormatValue(data, target)}</size>");
				}
			}

			return sb.ToString();
		}

		private static string FormatValue(QuestData data, double value)
		{
			return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0") : CalcUtils.FormatNumber(value, true);
		}
	}
}

