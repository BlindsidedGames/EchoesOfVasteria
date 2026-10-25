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
        [TestCase("Apple", .045f)]
        [TestCase("Pear", .0405f)]
        [TestCase("Peach", .03645f)]
        [TestCase("Cherry", .032805f)]
        [TestCase("Oak", .045f)]
        [TestCase("Birch", .0405f)]
        [TestCase("Spruce", .03645f)]
        public void SaplingBonusRollsUseSeparateFruitAndTimberCurves(string species, float chance)
        {
            var sapling = Resources.Load<TimelessEchoes.Upgrades.Resource>("Resource Items/" + species + " Sapling");
            Assert.NotNull(sapling);
            var tasks = Resources.LoadAll<TaskData>("Tasks").Where(task =>
                task.bonusDrops.Any(drop => drop.resource == sapling)).ToArray();
            Assert.IsNotEmpty(tasks, "Every sapling must have an adventure source.");
            foreach (var task in tasks)
            {
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(task.bonusDrops, () => chance + .000001f), task.name);
                var hit = TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(task.bonusDrops, () => chance - .000001f);
                Assert.AreEqual(1, hit.Count, task.name);
                Assert.AreSame(sapling, hit[0].resource, task.name);
                Assert.AreEqual(1, hit[0].count, "Propagation stays a single unmultiplied item.");
            }
        }

        [Test]
        public void LaterCropSeedsAreRarerAndOrchardsDoNotChangeTheirChances()
        {
            var content = ScriptableObject.CreateInstance<FarmContent>();
            var sources = Enumerable.Range(1, 17).Select(level => { var task = ScriptableObject.CreateInstance<TaskData>(); task.requiredSkillLevel = level; return task; }).ToArray();
            try
            {
                content.recipes = sources.Select((task, i) => new FarmRecipe { id = "crop-" + i, source = task }).ToList();
                Assert.AreEqual(.05f, content.SeedDropChance(content.recipes[0]), .000001f);
                var previous = .06f;
                foreach (var recipe in content.recipes)
                {
                    var chance = content.SeedDropChance(recipe);
                    Assert.Greater(chance, 0); Assert.Less(chance, previous);
                    previous = chance;
                }
                Assert.Less(previous, .01f, "The final crop should drop fewer than one pack per 100 eligible tasks on average.");
                var first = content.recipes[0]; var last = content.recipes[16];
                var orchard = new FarmRecipe { id = "earlier-tree", source = sources[0], orchard = true };
                content.recipes.Reverse(); content.recipes.Insert(0, orchard);
                Assert.AreEqual(.05f, content.SeedDropChance(first), .000001f);
                Assert.AreEqual(previous, content.SeedDropChance(last), .000001f);
                Assert.Zero(content.SeedDropChance(orchard));
                Assert.Zero(content.SeedDropChance(null));
            }
            finally { foreach (var source in sources) Object.DestroyImmediate(source); Object.DestroyImmediate(content); }
        }
    }
}
#endif
