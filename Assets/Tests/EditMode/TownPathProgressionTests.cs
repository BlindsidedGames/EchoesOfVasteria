#if UNITY_INCLUDE_TESTS
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using TimelessEchoes;

namespace Tests.EditMode
{
    public sealed class TownPathProgressionTests
    {
        [TestCase(0f)]
        [TestCase(10f)]
        [TestCase(25f)]
        [TestCase(50f)]
        [TestCase(100f)]
        [TestCase(200f)]
        [TestCase(1000000f)]
        public void EachRoadUpgradeGivesItsPromisedGainWithinTheExistingCeiling(float rating)
        {
            var grass = TownPathProgression.UnbuffedSpeed(rating, 0);
            var dirt = TownPathProgression.UnbuffedSpeed(rating, 1);
            var paving = TownPathProgression.UnbuffedSpeed(rating, 2);
            Assert.That(dirt / grass, Is.EqualTo(1.15f).Within(.00001f));
            Assert.That(paving / dirt, Is.EqualTo(1.20f).Within(.00001f));
            Assert.That(grass, Is.GreaterThanOrEqualTo(3f));
            Assert.That(paving, Is.LessThanOrEqualTo(9f));
            Assert.That(TownPathProgression.UnbuffedSpeed(rating + 1f, 2), Is.GreaterThanOrEqualTo(paving));
        }

        [TestCase(false, false, 0)]
        [TestCase(true, false, 1)]
        [TestCase(false, true, 2)]
        [TestCase(true, true, 2)]
        public void MigrationPreservesEarnedRoadsWithoutSpendingOrChangingTheOriginal(bool dirt, bool paving, int expectedTier)
        {
            var old = new GameData { SchemaVersion = 9, LastGameVersion = "1.5.8" };
            old.Farm.ProductionRevision = 1;
            old.Quests["Chunky Business"] = new GameData.QuestRecord { Completed = dirt, CompletedTimestamp = 123 };
            old.Quests["Unlock Idle"] = new GameData.QuestRecord { Completed = paving, CompletedTimestamp = 456 };
            old.Resources["Stone"] = new GameData.ResourceEntry { Amount = 789 };
            var result = SaveMigrationRunner.TryMigrate(old, "1.5.8");
            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(TownPathProgression.Tier(result.Data), Is.EqualTo(expectedTier));
            Assert.That(result.Data.Resources["Stone"].Amount, Is.EqualTo(789));
            Assert.That(old.Quests.ContainsKey(TownPathProgression.DirtQuest), Is.False);
            Assert.That(old.Quests.ContainsKey(TownPathProgression.PavingQuest), Is.False);
            var again = SaveMigrationRunner.TryMigrate(result.Data, "1.5.8");
            Assert.That(again.Succeeded, Is.True, again.Error);
            Assert.That(again.Changed, Is.False);
        }

        [Test]
        public void NewSavesDoNotGetRoadUpgradesByCompletingOreQuests()
        {
            var current = new GameData { LastGameVersion = "1.5.8" };
            current.Farm.ProductionRevision = 1;
            current.Quests["Chunky Business"] = new GameData.QuestRecord { Completed = true };
            current.Quests["Unlock Idle"] = new GameData.QuestRecord { Completed = true };
            var result = SaveMigrationRunner.TryMigrate(current, "1.5.8");
            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(TownPathProgression.Tier(result.Data), Is.Zero);
        }
    }
}
#endif
