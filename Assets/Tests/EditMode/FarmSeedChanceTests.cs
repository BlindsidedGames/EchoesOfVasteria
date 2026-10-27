#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Tasks;
using UnityEngine;
namespace Tests.EditMode
{
    public sealed class FarmSeedChanceTests
    {
        [TestCase("Apple", .045f, .045f)]
        [TestCase("Pear", .0405f, .0405f)]
        [TestCase("Peach", .03645f, .03645f)]
        [TestCase("Cherry", .032805f, .032805f)]
        [TestCase("Oak", .045f, .0225f)]
        [TestCase("Birch", .0405f, .02025f)]
        [TestCase("Spruce", .03645f, .018225f)]
        public void SaplingBonusRollsUseSeparateCurvesAndSizeAdjustedChances(string species, float chance, float smallChance)
        {
            var sapling = Resources.Load<TimelessEchoes.Upgrades.Resource>("Resource Items/" + species + " Sapling");
            Assert.NotNull(sapling);
            var tasks = Resources.LoadAll<TaskData>("Tasks").Where(task =>
                task.bonusDrops.Any(drop => drop.resource == sapling)).ToArray();
            Assert.IsNotEmpty(tasks, "Every sapling must have an adventure source.");
            foreach (var task in tasks)
            {
                var expected = task.name.StartsWith("Small ", System.StringComparison.Ordinal) ? smallChance : chance;
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(task.bonusDrops, () => expected + .000001f), task.name);
                var hit = TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(task.bonusDrops, () => expected - .000001f);
                Assert.AreEqual(1, hit.Count, task.name);
                Assert.AreSame(sapling, hit[0].resource, task.name);
                Assert.AreEqual(1, hit[0].count, "Propagation stays a single unmultiplied item.");
            }
        }

        [Test]
        public void EveryCropHasSevenPercentSeedChanceAndOrchardsDoNotDropSeedPacks()
        {
            var content = Resources.Load<FarmContent>("Farming/FarmContent");
            Assert.NotNull(content);
            var crops = content.recipes.Where(recipe => !recipe.orchard).ToArray();
            Assert.IsNotEmpty(crops);
            foreach (var recipe in crops)
                Assert.AreEqual(.07f, content.SeedDropChance(recipe), .000001f, recipe.id);
            foreach (var recipe in content.recipes.Where(recipe => recipe.orchard))
                Assert.Zero(content.SeedDropChance(recipe), recipe.id);
            Assert.Zero(content.SeedDropChance(null));
        }
    }
}
#endif
