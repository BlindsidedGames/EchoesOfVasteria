using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;

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
			foreach (var a in item.affixes)
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

			foreach (var kv in currentByMapping)
			{
				var mapping = kv.Key;
				if (craftedMappings.Contains(mapping)) continue;

				var minus = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);
				var prefix = string.IsNullOrEmpty(minus) ? string.Empty : minus + " ";
				var iconTag = StatIconLookup.GetIconTag(mapping);
				var isPercent = kv.Value.isPercent;
				var name = kv.Value.name;
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
			foreach (var a in item.affixes)
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


