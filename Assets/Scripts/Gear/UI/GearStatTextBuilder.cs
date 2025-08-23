using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Gear;

namespace TimelessEchoes.Gear.UI
{
	public static class GearStatTextBuilder
	{
		public static string BuildCraftResultSummary(GearItem item, GearItem current)
		{
			var lines = new List<string>();
			var currentByMapping = new Dictionary<HeroStatMapping, (float value, bool isPercent, string name)>();
			if (current != null)
				foreach (var ca in current.affixes)
				{
					if (ca == null || ca.stat == null) continue;
					currentByMapping[ca.stat.heroMapping] = (ca.value, ca.stat.isPercent, ca.stat.GetName());
				}

			var craftedMappings = new HashSet<HeroStatMapping>();
			var currentMappings = new HashSet<HeroStatMapping>(currentByMapping.Keys);
			var sortedAffixes = new List<GearAffix>(item.affixes);
			sortedAffixes.Sort((x, y) => StatSortOrder.Compare(x?.stat != null ? x.stat.heroMapping : default, y?.stat != null ? y.stat.heroMapping : default));
			foreach (var a in sortedAffixes)
			{
				if (a == null || a.stat == null) continue;
				var iconTag = StatIconLookup.GetIconTag(a.stat.heroMapping);
				var valueText = $"{CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
				var nameText = a.stat.GetName();

				var cv = currentByMapping.TryGetValue(a.stat.heroMapping, out var cur) ? cur.value : 0f;
				var diff = a.value - cv;
				var arrow = diff > 0.0001f
					? StatIconLookup.GetIconTag(StatIconLookup.StatKey.UpArrow)
					: diff < -0.0001f
						? StatIconLookup.GetIconTag(StatIconLookup.StatKey.DownArrow)
						: StatIconLookup.GetIconTag(StatIconLookup.StatKey.RightArrow);
				var arrowPrefix = string.IsNullOrEmpty(arrow) ? string.Empty : arrow + " ";

				if (!currentMappings.Contains(a.stat.heroMapping))
					arrowPrefix = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Plus) + " ";

				if (!string.IsNullOrEmpty(iconTag))
					lines.Add($"{arrowPrefix}{iconTag} {valueText}");
				else
					lines.Add($"{arrowPrefix}{nameText} {valueText}");

				craftedMappings.Add(a.stat.heroMapping);
			}

			var remaining = new List<HeroStatMapping>();
			foreach (var kv in currentByMapping)
			{
				var mapping = kv.Key;
				if (!craftedMappings.Contains(mapping)) remaining.Add(mapping);
			}
			remaining.Sort((a, b) => StatSortOrder.Compare(a, b));
			foreach (var mapping in remaining)
			{
				var minus = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);
				var prefix = string.IsNullOrEmpty(minus) ? string.Empty : minus + " ";
				var iconTag = StatIconLookup.GetIconTag(mapping);
				var info = currentByMapping[mapping];
				var isPercent = info.isPercent;
				var name = info.name;
				var valueText = $"{CalcUtils.FormatNumber(0)}{(isPercent ? "%" : "")}";
				if (!string.IsNullOrEmpty(iconTag))
					lines.Add($"{prefix}{iconTag} {valueText}");
				else
					lines.Add($"{prefix}{name} {valueText}");
			}

			return string.Join("\n", lines);
		}

		public static string BuildEquippedStatsText(GearItem item, string slotName)
		{
			if (item == null)
				return StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);

			var lines = new List<string>();
			var sortedAffixes = new List<GearAffix>(item.affixes);
			sortedAffixes.Sort((x, y) => StatSortOrder.Compare(x?.stat != null ? x.stat.heroMapping : default, y?.stat != null ? y.stat.heroMapping : default));
			foreach (var a in sortedAffixes)
			{
				if (a == null || a.stat == null) continue;
				var iconTag = StatIconLookup.GetIconTag(a.stat.heroMapping);
				var valueText = $"{CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
				var nameText = a.stat.GetName();
				if (!string.IsNullOrEmpty(iconTag))
					lines.Add($"{iconTag} {valueText}");
				else
					lines.Add($"{nameText} {valueText}");
			}

			return string.Join("\n", lines);
		}
	}
}


