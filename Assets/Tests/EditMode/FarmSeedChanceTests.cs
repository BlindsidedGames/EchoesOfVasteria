#if UNITY_INCLUDE_TESTS
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Tasks;
using UnityEngine;
namespace Tests.EditMode
{
    public sealed class FarmSeedChanceTests
    {
        [TestCase("Apple")]
        [TestCase("Pear")]
        [TestCase("Peach")]
        [TestCase("Cherry")]
        [TestCase("Oak")]
        [TestCase("Birch")]
        [TestCase("Spruce")]
        public void SaplingBonusRollsAtTwentyFivePercentAfterQuest(string species)
        {
            var sapling = Resources.Load<TimelessEchoes.Upgrades.Resource>("Resource Items/" + species + " Sapling");
            Assert.NotNull(sapling);
            var tasks = Resources.LoadAll<TaskData>("Tasks").Where(task =>
                task.bonusDrops.Any(drop => drop.resource == sapling)).ToArray();
            Assert.IsNotEmpty(tasks, "Every sapling must have an adventure source.");
            foreach (var task in tasks)
            {
                var owner = new GameData();
                var drop = task.bonusDrops.Single(entry => entry.resource == sapling);
                Assert.NotNull(drop.requiredQuest, task.name);
                var drops = new[] { drop };
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(drops, () => 0, owner), task.name);
                owner.Quests[drop.requiredQuest.questId] = new GameData.QuestRecord { Completed = true };
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(drops, () => .25f, owner), task.name);
                var hit = TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(drops, () => .249999f, owner);
                Assert.AreEqual(1, hit.Count, task.name);
                Assert.AreSame(sapling, hit[0].resource, task.name);
                Assert.AreEqual(1, hit[0].count, "Propagation stays a single unmultiplied item.");
            }
        }

        [Test]
        public void EveryCropPackIsAnAdditiveQuestGatedBonus()
        {
            var content = Resources.Load<FarmContent>("Farming/FarmContent");
            var crops = content.recipes.Where(recipe => !recipe.orchard).ToArray();
            Assert.AreEqual(17, crops.Length);
            foreach (var recipe in crops)
            {
                var pack = recipe.source.bonusDrops.Single(drop => drop.resource == recipe.paidInput);
                Assert.NotNull(pack.requiredQuest, recipe.id);
                var owner = new GameData();
                var randomCalls = 0;
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(new[] {pack}, () => { randomCalls++; return 0; }, owner));
                Assert.Zero(randomCalls, "Locked packs must not consume a roll.");
                owner.Quests[pack.requiredQuest.questId] = new GameData.QuestRecord { Completed = true };
                var hit = TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(new[] {pack}, () => .249999f, owner);
                Assert.AreEqual(1, hit.Count, recipe.id);
                Assert.AreSame(recipe.paidInput, hit[0].resource);
                Assert.AreEqual(1, hit[0].count);
                Assert.IsEmpty(TimelessEchoes.Upgrades.DropResolver.RollBonusDrops(new[] {pack}, () => .25f, owner));
                Assert.IsTrue(recipe.source.resourceDrops.Any(drop => drop.resource == recipe.output), "Seeds cannot replace the crop reward.");
            }
        }
    }
}
#endif
