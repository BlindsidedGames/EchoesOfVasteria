using System;
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

			var crafting = CraftingService.Instance;
			var slot = item != null && !string.IsNullOrWhiteSpace(item.slot)
				? item.slot
				: current != null && !string.IsNullOrWhiteSpace(current.slot)
					? current.slot
					: string.Empty;
			var craftedQuality = item != null ? UpgradeEvaluator.ComputeQualityPercent(crafting, item, slot) : 0f;
			var currentQuality = current != null ? UpgradeEvaluator.ComputeQualityPercent(crafting, current, slot) : 0f;
			var qualityIcon = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Quality);
			var qualityValue = $"{craftedQuality:0.#}%";
			var qualityText = !string.IsNullOrEmpty(qualityIcon)
				? $"{qualityIcon} {qualityValue}"
				: $"Quality {qualityValue}";
			string qualityArrow;
			if (current == null)
				qualityArrow = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Plus);
			else
			{
				var qualityDelta = craftedQuality - currentQuality;
				qualityArrow = qualityDelta > 0.0001f
					? StatIconLookup.GetIconTag(StatIconLookup.StatKey.UpArrow)
					: qualityDelta < -0.0001f
						? StatIconLookup.GetIconTag(StatIconLookup.StatKey.DownArrow)
						: StatIconLookup.GetIconTag(StatIconLookup.StatKey.RightArrow);
			}
			var qualityPrefix = string.IsNullOrEmpty(qualityArrow) ? string.Empty : qualityArrow + " ";
			lines.Add($"{qualityPrefix}{qualityText}");

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

		public readonly struct AggregateStatsTextSections
		{
			public AggregateStatsTextSections(string totals, string weaponAndChest, string helmetAndBoots)
			{
				Totals = totals;
				WeaponAndChest = weaponAndChest;
				HelmetAndBoots = helmetAndBoots;
			}

			public string Totals { get; }
			public string WeaponAndChest { get; }
			public string HelmetAndBoots { get; }
		}

		public static AggregateStatsTextSections BuildAggregateStatsTextSections(IEnumerable<(string slot, GearItem item)> gearBySlot)
		{
			if (gearBySlot == null)
				return new AggregateStatsTextSections(string.Empty, string.Empty, string.Empty);

			var entries = new List<(string slot, GearItem item)>();
			foreach (var entry in gearBySlot)
			{
				if (string.IsNullOrWhiteSpace(entry.slot))
					continue;

				entries.Add(entry);
			}

			if (entries.Count == 0)
				return new AggregateStatsTextSections(string.Empty, string.Empty, string.Empty);

			var crafting = CraftingService.Instance;
			var totalsByMapping = new Dictionary<HeroStatMapping, float>();
			var percentByMapping = new Dictionary<HeroStatMapping, bool>();
			var statDefByMapping = new Dictionary<HeroStatMapping, StatDefSO>();
			float totalQuality = 0f;
			var hasAnyItem = false;

			foreach (var entry in entries)
			{
				var item = entry.item;
				if (item == null)
					continue;

				hasAnyItem = true;
				totalQuality += UpgradeEvaluator.ComputeQualityPercent(crafting, item, entry.slot);

				if (item.affixes == null)
					continue;

				foreach (var affix in item.affixes)
				{
					if (affix == null || affix.stat == null)
						continue;

					var mapping = affix.stat.heroMapping;
					if (totalsByMapping.TryGetValue(mapping, out var existing))
						totalsByMapping[mapping] = existing + affix.value;
					else
						totalsByMapping[mapping] = affix.value;

					if (!percentByMapping.ContainsKey(mapping))
						percentByMapping[mapping] = affix.stat.isPercent;
					if (!statDefByMapping.ContainsKey(mapping))
						statDefByMapping[mapping] = affix.stat;
				}
			}

			var totalsLines = new List<string> { "<b>Totals</b>" };

			var qualityIcon = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Quality);
			var qualityValue = $"{totalQuality:0.#}%";
			totalsLines.Add(!string.IsNullOrEmpty(qualityIcon)
				? $"{qualityIcon} {qualityValue}"
				: $"Quality {qualityValue}");

			var mappings = new List<HeroStatMapping>(totalsByMapping.Keys);
			mappings.Sort(StatSortOrder.Compare);
			foreach (var mapping in mappings)
			{
				if (!totalsByMapping.TryGetValue(mapping, out var totalValue))
					continue;

				if (Math.Abs(totalValue) <= 0.0001f)
					continue;

				var iconTag = StatIconLookup.GetIconTag(mapping);
				var isPercent = percentByMapping.TryGetValue(mapping, out var percent) && percent;
				var valueText = $"{CalcUtils.FormatNumber(totalValue)}{(isPercent ? "%" : string.Empty)}";
				if (!string.IsNullOrEmpty(iconTag))
				{
					totalsLines.Add($"{iconTag} {valueText}");
				}
				else
				{
					statDefByMapping.TryGetValue(mapping, out var statDef);
					var name = statDef != null ? statDef.GetName() : mapping.ToString();
					totalsLines.Add($"{name} {valueText}");
				}
			}

			var minusIcon = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);
			if (!hasAnyItem)
				totalsLines.Add(string.IsNullOrEmpty(minusIcon) ? "-" : minusIcon);

			string NormalizeSlot(string slot)
			{
				return string.IsNullOrWhiteSpace(slot) ? string.Empty : slot.Trim().ToLowerInvariant();
			}

			var entriesBySlot = new Dictionary<string, (string slot, GearItem item)>(StringComparer.OrdinalIgnoreCase);
			foreach (var entry in entries)
			{
				var normalized = NormalizeSlot(entry.slot);
				if (string.IsNullOrEmpty(normalized))
					continue;

				entriesBySlot[normalized] = entry;
			}

			(string slot, GearItem item)? ResolveSlot(params string[] keys)
			{
				foreach (var key in keys)
				{
					if (entriesBySlot.TryGetValue(key, out var value))
						return value;
				}

				return null;
			}

			List<string> BuildSlotSection(string slotName, GearItem item, string fallbackLabel)
			{
				var lines = new List<string>();
				var label = !string.IsNullOrWhiteSpace(slotName) ? slotName : fallbackLabel;
				if (string.IsNullOrWhiteSpace(label))
					label = "Slot";

				lines.Add($"<b>{label}</b>");

				if (item == null)
				{
					lines.Add(string.IsNullOrEmpty(minusIcon) ? "-" : minusIcon);
					return lines;
				}

				var sectionText = BuildEquippedStatsText(item, slotName);
				if (string.IsNullOrWhiteSpace(sectionText))
				{
					lines.Add(string.IsNullOrEmpty(minusIcon) ? "-" : minusIcon);
					return lines;
				}

				var parts = sectionText.Split('\n');
				foreach (var part in parts)
					lines.Add(part);

				return lines;
			}

			void AppendSection(List<string> destination, List<string> source)
			{
				if (source == null || source.Count == 0)
					return;

				if (destination.Count > 0)
					destination.Add(string.Empty);

				destination.AddRange(source);
			}

			var weaponEntry = ResolveSlot("weapon");
			var chestEntry = ResolveSlot("chest");
			var helmetEntry = ResolveSlot("helmet", "helm");
			var bootsEntry = ResolveSlot("boots");

			var weaponLines = BuildSlotSection(weaponEntry.HasValue ? weaponEntry.Value.slot : null, weaponEntry.HasValue ? weaponEntry.Value.item : null, "Weapon");
			var chestLines = BuildSlotSection(chestEntry.HasValue ? chestEntry.Value.slot : null, chestEntry.HasValue ? chestEntry.Value.item : null, "Chest");
			var helmetLines = BuildSlotSection(helmetEntry.HasValue ? helmetEntry.Value.slot : null, helmetEntry.HasValue ? helmetEntry.Value.item : null, "Helmet");
			var bootsLines = BuildSlotSection(bootsEntry.HasValue ? bootsEntry.Value.slot : null, bootsEntry.HasValue ? bootsEntry.Value.item : null, "Boots");

			var weaponAndChestLines = new List<string>();
			AppendSection(weaponAndChestLines, weaponLines);
			AppendSection(weaponAndChestLines, chestLines);

			var helmetAndBootsLines = new List<string>();
			AppendSection(helmetAndBootsLines, helmetLines);
			AppendSection(helmetAndBootsLines, bootsLines);

			foreach (var entry in entries)
			{
				var normalized = NormalizeSlot(entry.slot);
				if (string.IsNullOrEmpty(normalized))
					continue;

				if (normalized == "weapon" || normalized == "chest" || normalized == "helmet" || normalized == "helm" || normalized == "boots")
					continue;

				AppendSection(totalsLines, BuildSlotSection(entry.slot, entry.item, entry.slot));
			}

			return new AggregateStatsTextSections(
				string.Join("\n", totalsLines),
				string.Join("\n", weaponAndChestLines),
				string.Join("\n", helmetAndBootsLines));
		}
		public static string BuildEquippedStatsText(GearItem item, string slotName)
		{
			if (item == null)
				return StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);

			var lines = new List<string>();
			var crafting = CraftingService.Instance;
			var resolvedSlot = !string.IsNullOrWhiteSpace(slotName) ? slotName : item.slot ?? string.Empty;
			var equippedQuality = UpgradeEvaluator.ComputeQualityPercent(crafting, item, resolvedSlot);
			var equippedIcon = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Quality);
			var equippedValue = string.Format("{0:0.#}%", equippedQuality);
			if (!string.IsNullOrEmpty(equippedIcon))
				lines.Add(string.Format("{0} {1}", equippedIcon, equippedValue));
			else
				lines.Add(string.Format("Quality {0}", equippedValue));

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
