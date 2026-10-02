#if UNITY_INCLUDE_TESTS
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using NUnit.Framework;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Upgrades.Cauldron;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class CauldronResourceYieldTests
    {
        [TestCase(0, 0, 0)] [TestCase(1, 1, 2)] [TestCase(25, 2, 5)]
        [TestCase(75, 3, 10)] [TestCase(200, 5, 15)] [TestCase(750, 7, 20)]
        [TestCase(1500, 10, 30)] [TestCase(3500, 15, 40)] [TestCase(10000, 20, 50)]
        [TestCase(int.MaxValue, 20, 50)]
        public void AuthoredCardProgressionAwardsApprovedCappedYield(int copies, float card, float category)
        {
            var config = CauldronResourceYield.Config;
            Assert.IsNotNull(config);
            var tier = new CardTierCalculator(config, () => new System.Collections.Generic.Dictionary<string, int> { ["RES:Radish"] = copies }).GetResourceTier("Radish");
            Assert.AreEqual(card, CauldronResourceYield.CardBonusPercent(tier, config));
            Assert.AreEqual(category, CauldronResourceYield.CategoryBonusPercent(tier, config));
        }

        [Test]
        public void UnownedUnlockedMatchingCardBlocksCategoryUntilItIsCollected()
        {
            var radish = AssetCache.GetAll<Resource>("").Single(r => r.name == "Radish");
            var corn = AssetCache.GetAll<Resource>("").Single(r => r.name == "Corn");
            var data = new GameData();
            data.Resources[radish.name] = new GameData.ResourceEntry { Earned = true };
            data.CauldronCardCounts[radish.CardId] = 10000;
            Assert.AreEqual(70, CauldronResourceYield.BonusPercent(data, radish));
            data.Resources[corn.name] = new GameData.ResourceEntry { Earned = true };
            Assert.AreEqual(20, CauldronResourceYield.BonusPercent(data, radish));
            data.CauldronCardCounts[corn.CardId] = 25;
            Assert.AreEqual(25, CauldronResourceYield.BonusPercent(data, radish));
            data.Resources[corn.name].Earned = false;
            Assert.AreEqual(70, CauldronResourceYield.BonusPercent(data, radish));
            Assert.AreEqual(7, CauldronResourceYield.Supplement(data, radish, 10), 1e-12);
            Assert.AreEqual(0, CauldronResourceYield.BonusPercent(new GameData(), radish));
        }

        [Test]
        public void CraftedOrPropagationResourceCannotReceiveCardYield()
        {
            var resource = ScriptableObject.CreateInstance<Resource>();
            try
            {
                resource.name = "Excluded"; resource.DisableAlterEcho = true;
                var data = new GameData();
                data.Resources[resource.name] = new GameData.ResourceEntry { Earned = true };
                data.CauldronCardCounts[resource.CardId] = 10000;
                Assert.AreEqual(0, CauldronResourceYield.Supplement(data, resource, 100));
            }
            finally { Object.DestroyImmediate(resource); }
        }
    }
}
#endif
