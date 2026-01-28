using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Gear.UI
{
	/// <summary>
	/// Static utility for evaluating gear upgrades.
	/// Uses scratch dictionaries to avoid allocations in hot paths.
	/// For cached evaluations, use ScoreEvaluationService instead.
	/// </summary>
	public static class UpgradeEvaluator
	{
		// Scratch dictionaries to avoid allocations in hot paths
		private static readonly Dictionary<HeroStatMapping, float> _scratchDelta = new();
		private static readonly Dictionary<HeroStatMapping, float> _scratchTotals = new();

		// Cached theoretical max values per slot (computed once, slot name -> max value)
		private static readonly Dictionary<string, float> _theoreticalMaxCache = new(StringComparer.OrdinalIgnoreCase);
		private static readonly List<float> _scratchContributions = new(16);

		public static bool IsPotentialUpgrade(CraftingService crafting, GearItem candidate, GearItem current)
		{
			if (candidate == null) return false;
			var score = ComputeUpgradeScore(crafting, candidate, current);
			return score > 0.0001f;
		}

		public static float ComputeUpgradeScore(CraftingService crafting, GearItem candidate, GearItem current)
		{
			_scratchDelta.Clear();

			if (candidate?.affixes != null)
			{
				foreach (var a in candidate.affixes)
				{
					if (a?.stat == null) continue;
					var map = a.stat.heroMapping;
					_scratchDelta.TryGetValue(map, out var val);
					_scratchDelta[map] = val + a.value;
				}
			}

			if (current?.affixes != null)
			{
				foreach (var a in current.affixes)
				{
					if (a?.stat == null) continue;
					var map = a.stat.heroMapping;
					_scratchDelta.TryGetValue(map, out var val);
					_scratchDelta[map] = val - a.value;
				}
			}

			var score = 0f;
			foreach (var kv in _scratchDelta)
			{
				var def = crafting?.GetStatByMapping(kv.Key);
				var scale = def != null ? UnityEngine.Mathf.Max(0f, def.ComparisonScale) : 1f;
				score += kv.Value * scale;
			}

			return score;
		}

		public static float ComputeAbsoluteScore(CraftingService crafting, GearItem item)
		{
			if (item == null) return 0f;

			_scratchTotals.Clear();

			if (item.affixes != null)
			{
				foreach (var a in item.affixes)
				{
					if (a?.stat == null) continue;
					var map = a.stat.heroMapping;
					_scratchTotals.TryGetValue(map, out var val);
					_scratchTotals[map] = val + a.value;
				}
			}

			var score = 0f;
			foreach (var kv in _scratchTotals)
			{
				var def = crafting?.GetStatByMapping(kv.Key);
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
			// Return cached value if available
			if (_theoreticalMaxCache.TryGetValue(slot ?? string.Empty, out var cached))
				return cached;

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

			// Use scratch list to avoid allocations
			_scratchContributions.Clear();
			foreach (var stat in stats)
			{
				if (!IsAllowed(stat))
					continue;
				var scale = UnityEngine.Mathf.Max(0f, stat.ComparisonScale);
				_scratchContributions.Add(stat.maxRoll * scale);
			}

			if (_scratchContributions.Count == 0)
				return 0f;

			_scratchContributions.Sort((a, b) => b.CompareTo(a));
			var count = UnityEngine.Mathf.Clamp(maxAffixes, 1, _scratchContributions.Count);
			float sum = 0f;
			for (var i = 0; i < count; i++)
				sum += _scratchContributions[i];

			// Cache and return
			_theoreticalMaxCache[slot ?? string.Empty] = sum;
			return sum;
		}
	}
}


