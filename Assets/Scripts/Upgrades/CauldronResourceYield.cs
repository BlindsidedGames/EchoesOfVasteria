using System;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades.Cauldron;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>Card and category yield for genuine resource acquisition; never an inventory multiplier.</summary>
    public static class CauldronResourceYield
    {
        private static AEResourceGroupClassifier classifier = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => classifier = new AEResourceGroupClassifier();

        public static CauldronConfig Config => Resources.Load<CauldronConfig>("Cauldron/CauldronConfig");
        public static float CardBonusPercent(int tier, CauldronConfig config) => Bonus(tier, config?.resourceYieldBonusPerTier, 20f);
        public static float CategoryBonusPercent(int tier, CauldronConfig config) => Bonus(tier, config?.categoryYieldBonusPerTier, 50f);
        private static float Bonus(int tier, float[] values, float cap)
        {
            if (tier <= 0 || values == null || values.Length == 0) return 0;
            var value = values[Mathf.Min(tier - 1, values.Length - 1)];
            return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp(value, 0, cap);
        }

        public static int CategoryTier(GameData data, CauldronManager.AEResourceGroup group, CauldronConfig config)
        {
            if (data?.Resources == null || config == null) return 0;
            var calculator = new CardTierCalculator(config, () => data.CauldronCardCounts);
            var lowest = int.MaxValue;
            var inventory = ResourceManager.Instance;
            foreach (var resource in AssetCache.GetAll<Resource>(string.Empty))
            {
                if (!resource || resource.DisableAlterEcho || classifier.Classify(resource) != group ||
                    !(inventory ? inventory.IsUnlocked(resource, data) :
                      data.Resources.TryGetValue(resource.name, out var entry) && entry?.Earned == true)) continue;
                lowest = Math.Min(lowest, calculator.GetResourceTier(resource.name));
            }
            return lowest == int.MaxValue ? 0 : lowest;
        }

        public static float BonusPercent(GameData data, Resource resource, CauldronConfig config = null)
        {
            if (data == null || !resource || resource.DisableAlterEcho) return 0;
            config = config ? config : Config;
            if (!config) return 0;
            var calculator = new CardTierCalculator(config, () => data.CauldronCardCounts);
            return CardBonusPercent(calculator.GetResourceTier(resource.name), config) +
                   CategoryBonusPercent(CategoryTier(data, classifier.Classify(resource), config), config);
        }

        public static float BonusPercent(GameData data, string resourceName)
        {
            foreach (var resource in AssetCache.GetAll<Resource>(string.Empty))
                if (resource && resource.name == resourceName) return BonusPercent(data, resource);
            return 0;
        }

        public static double Supplement(GameData data, Resource resource, double baseYield)
        {
            if (!(baseYield > 0) || double.IsNaN(baseYield) || double.IsInfinity(baseYield)) return 0;
            return baseYield * BonusPercent(data, resource) / 100d;
        }
    }
}
