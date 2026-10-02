using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;

namespace TimelessEchoes.UI.Cauldron
{
    public static class CauldronMixingPresentation
    {
        public static bool IsFood(Resource resource) => resource != null && BuildFoodDefinitions().Contains(resource);

        public static List<Resource> BuildFoodDefinitions()
        {
            var taskFoods = new HashSet<Resource>();
            foreach (var task in Blindsided.Utilities.AssetCache.GetAll<TaskData>("Tasks"))
            {
                var name = task?.associatedSkill?.name;
                if (string.IsNullOrEmpty(name) ||
                    (name.IndexOf("farm", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                     name.IndexOf("fish", System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
                foreach (var drop in task.resourceDrops)
                    if (drop?.resource != null) taskFoods.Add(drop.resource);
            }
            return Blindsided.Utilities.AssetCache.GetAll<Resource>("")
                .Where(r => r && r.foodEligibility != Resource.FoodEligibility.NotFood &&
                    (r.foodEligibility == Resource.FoodEligibility.Food || taskFoods.Contains(r) ||
                     r.cauldronCategory == Resource.CauldronCategory.Farming || r.cauldronCategory == Resource.CauldronCategory.Fishing))
                .Distinct().ToList();
        }

        public static List<Resource> BuildEligibleFoods(ResourceManager resources) => SortFoods(
            BuildFoodDefinitions().Where(r => resources != null && resources.IsUnlocked(r)), resources);

        public static List<Resource> BuildDisplayFoods(ResourceManager resources) => SortFoods(BuildFoodDefinitions(), resources);

        public static List<Resource> SortFoods(IEnumerable<Resource> foods, ResourceManager resources) => foods
            .OrderBy(r => resources != null && resources.IsUnlocked(r) ? (resources.GetAmount(r) > 0 ? 0 : 1) : 2)
            .ThenByDescending(r => resources != null && resources.IsUnlocked(r) ? CauldronConversion.UnitValue(r) : 0)
            .ThenBy(r => r.resourceID).ThenBy(r => r.name).ToList();
    }
}
