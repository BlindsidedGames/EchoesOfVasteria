using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Gear.UI
{
	public static class UpgradeEvaluator
	{
		public static bool IsPotentialUpgrade(CraftingService crafting, GearItem candidate, GearItem current)
		{
			if (candidate == null) return false;
			var score = ComputeUpgradeScore(crafting, candidate, current);
			return score > 0.0001f;
		}

		public static float ComputeUpgradeScore(CraftingService crafting, GearItem candidate, GearItem current)
		{
			var deltaByMapping = new Dictionary<HeroStatMapping, float>();
			if (candidate != null)
				for (var i = 0; i < candidate.affixes.Count; i++)
				{
					var a = candidate.affixes[i];
					if (a == null || a.stat == null) continue;
					var map = a.stat.heroMapping;
					if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
					deltaByMapping[map] += a.value;
				}

			if (current != null)
				for (var i = 0; i < current.affixes.Count; i++)
				{
					var a = current.affixes[i];
					if (a == null || a.stat == null) continue;
					var map = a.stat.heroMapping;
					if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
					deltaByMapping[map] -= a.value;
				}

			var score = 0f;
			foreach (var kv in deltaByMapping)
			{
				var def = crafting != null ? crafting.GetStatByMapping(kv.Key) : null;
				var scale = def != null ? UnityEngine.Mathf.Max(0f, def.ComparisonScale) : 1f;
				score += kv.Value * scale;
			}

			return score;
		}

		public static float ComputeAbsoluteScore(CraftingService crafting, GearItem item)
		{
			if (item == null) return 0f;
			var totalsByMapping = new Dictionary<HeroStatMapping, float>();
			if (item.affixes != null)
			{
				for (var i = 0; i < item.affixes.Count; i++)
				{
					var a = item.affixes[i];
					if (a == null || a.stat == null) continue;
					var map = a.stat.heroMapping;
					if (!totalsByMapping.ContainsKey(map)) totalsByMapping[map] = 0f;
					totalsByMapping[map] += a.value;
				}
			}

			var score = 0f;
			foreach (var kv in totalsByMapping)
			{
				var def = crafting != null ? crafting.GetStatByMapping(kv.Key) : null;
				var scale = def != null ? UnityEngine.Mathf.Max(0f, def.ComparisonScale) : 1f;
				score += kv.Value * scale;
			}

			return score;
		}

		public static float ComputeQualityPercent(CraftingService crafting, GearItem item, string slot)
		{
			if (item == null)
				return 0f;

			var absoluteScore = ComputeAbsoluteScore(crafting, item);
			var maxForSlot = ComputeTheoreticalMaxForSlot(slot);
			if (maxForSlot <= 0f)
				return 0f;

			return UnityEngine.Mathf.Clamp01(absoluteScore / maxForSlot) * 100f;
		}

		public static float ComputeTheoreticalMaxForSlot(string slot)
		{
			var maxAffixes = 1;
			foreach (var rarity in AssetCache.GetAll<RaritySO>(string.Empty))
				if (rarity != null && rarity.affixCount > maxAffixes)
					maxAffixes = rarity.affixCount;

			var stats = AssetCache.GetAll<StatDefSO>(string.Empty);
			if (stats == null || stats.Length == 0)
				return 0f;

			bool IsAllowed(StatDefSO stat)
			{
				if (stat == null)
					return false;
				if (stat.heroMapping == HeroStatMapping.MoveSpeed &&
					!string.Equals(slot, "Boots", StringComparison.OrdinalIgnoreCase))
					return false;
				return true;
			}

			var contributions = new List<float>();
			foreach (var stat in stats)
			{
				if (!IsAllowed(stat))
					continue;
				var scale = UnityEngine.Mathf.Max(0f, stat.ComparisonScale);
				contributions.Add(stat.maxRoll * scale);
			}

			if (contributions.Count == 0)
				return 0f;

			contributions.Sort((a, b) => b.CompareTo(a));
			var count = UnityEngine.Mathf.Clamp(maxAffixes, 1, contributions.Count);
			float sum = 0f;
			for (var i = 0; i < count; i++)
				sum += contributions[i];

			return sum;
		}
	}
}


