using System.Collections.Generic;
using TimelessEchoes.Upgrades;
namespace TimelessEchoes.UI.Cauldron
{
    public static class CauldronMixingPresentation
    {
		public static List<Resource> BuildEligibleFoods(ResourceManager rm)
		{
			// Only allow mixing with foods: resources that appear in Farming or Fishing task drop tables
			var eligibleFromTasks = new HashSet<Resource>();
			foreach (var t in Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Tasks.TaskData>("Tasks"))
			{
				if (t == null) continue;
				var skillName = t.associatedSkill != null ? t.associatedSkill.name : null;
				if (string.IsNullOrEmpty(skillName)) continue;
				var isFarming = skillName.IndexOf("farm", System.StringComparison.OrdinalIgnoreCase) >= 0;
				var isFishing = skillName.IndexOf("fish", System.StringComparison.OrdinalIgnoreCase) >= 0;
				if (!isFarming && !isFishing) continue;
				foreach (var drop in t.resourceDrops)
				{
					if (drop == null || drop.resource == null) continue;
					eligibleFromTasks.Add(drop.resource);
				}
			}

			// Include manual overrides: any Resource categorized as Farming or Fishing
			var eligible = new HashSet<Resource>(eligibleFromTasks);
			foreach (var r in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
			{
				if (r == null) continue;
				if (r.cauldronCategory == Resource.CauldronCategory.Farming || r.cauldronCategory == Resource.CauldronCategory.Fishing)
					eligible.Add(r);
			}

			// Build sorted list of unlocked eligible resources
			var result = new List<Resource>();
			foreach (var r in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
			{
				if (r != null && eligible.Contains(r) && rm != null && rm.IsUnlocked(r))
					result.Add(r);
			}
			result.Sort((a, b) =>
			{
				int cmp = a.resourceID.CompareTo(b.resourceID);
				return cmp != 0 ? cmp : string.Compare(a.name, b.name, System.StringComparison.Ordinal);
			});
			return result;
		}

    }
}
