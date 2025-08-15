using System.Collections.Generic;
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
				var scale = def != null ? UnityEngine.Mathf.Max(0f, def.comparisonScale) : 1f;
				score += kv.Value * scale;
			}

			return score;
		}
	}
}


