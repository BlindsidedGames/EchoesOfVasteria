#if UNITY_INCLUDE_TESTS
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class FieldsMigrationTests
    {
        private static GameData Historical()
        {
            var data = new GameData { SchemaVersion = 5, LastGameVersion = "9999.0.0" };
            data.Farm.OriginalBedsPrepared = true;
            data.Farm.OriginalBedsBuildOperationId = "paid-prototype-build";
            data.Farm.Seeds["seed.radish"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 7 };
            data.Farm.Beds[FarmCommands.WestBedId] = new FarmBedState
            {
                Unlocked = true, BatchId = "radish:paid", RecipeId = "radish",
                DurationSeconds = 1800, ElapsedSeconds = 123.5, HarvestYield = 10,
                LastGrowthUtcTicks = 638000000000000000, LegacyBatchId = "radish:paid"
            };
            data.Farm.PendingCredits["opaque-pending"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = 42 };
            data.Farm.Operations["future-operation"] = new FarmOperationReceipt { Fingerprint = "future:v9", CommittedAtUtcTicks = 41 };
            data.Quests["BarkleyFence"] = new GameData.QuestRecord { Completed = true };
            data.Resources["Radish"] = new GameData.ResourceEntry { Amount = 19.25 };
            return data;
        }

        [Test]
        public void PrototypeInvestmentSurvivesWithoutFreeFieldsProgressionOrOfflineGrowth()
        {
            var source = Historical();
            var result = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreNotSame(source, result.Data);
            var farm = result.Data.Farm;
            Assert.AreEqual(6, result.Data.SchemaVersion);
            Assert.AreEqual(1, farm.ProductionRevision);
            Assert.AreEqual(1, farm.TwinsLevel);
            Assert.AreEqual(0, farm.TwinsXp);
            Assert.AreEqual(0, farm.GardenCapacity);
            Assert.AreEqual(0, farm.OrchardCapacity);
            Assert.IsFalse(FarmCommands.AccessibleBed(farm, FarmCommands.WestBedId));
            Assert.IsTrue(farm.OriginalBedsPrepared);
            Assert.AreEqual("paid-prototype-build", farm.OriginalBedsBuildOperationId);
            Assert.AreEqual(2, farm.Seeds["seed.radish"].Quantity);
            Assert.AreEqual(7, farm.Seeds["seed.radish"].LifetimeAcquired);
            Assert.AreEqual(123.5, farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual(638000000000000000, farm.Beds[FarmCommands.WestBedId].LastGrowthUtcTicks);
            Assert.AreEqual("radish:paid", farm.Beds[FarmCommands.WestBedId].BatchId);
            Assert.AreEqual(10, farm.Beds[FarmCommands.WestBedId].HarvestYield);
            Assert.AreEqual(source.Farm.JournalLineage, farm.JournalLineage);
            Assert.AreEqual("future:v9", farm.Operations["future-operation"].Fingerprint);
            Assert.IsTrue(farm.PendingCredits["opaque-pending"].Rolled);
            Assert.AreEqual(19.25, result.Data.Resources["Radish"].Amount);
            Assert.IsFalse(result.Data.Quests.ContainsKey(FarmContent.IntroductionId));
            Assert.AreEqual(0, source.Farm.ProductionRevision);
            Assert.AreEqual(5, source.SchemaVersion);
        }

        [Test]
        public void ExportImportAndRepeatMigrationPreserveProgressAndPendingOwnership()
        {
            var migrated = SaveMigrationRunner.TryMigrate(Historical(), "9999.0.0");
            Assert.IsTrue(migrated.Succeeded, migrated.Error);
            migrated.Data.Farm.TwinsLevel = 60;
            migrated.Data.Farm.TwinsXp = 17;
            migrated.Data.Farm.GardenCapacity = 4;
            var imported = CurrentSaveCodec.Clone(migrated.Data);
            var repeated = SaveMigrationRunner.TryMigrate(imported, "9999.0.0");
            Assert.IsTrue(repeated.Succeeded, repeated.Error);
            Assert.IsFalse(repeated.Changed);
            Assert.AreEqual(60, repeated.Data.Farm.TwinsLevel);
            Assert.AreEqual(17, repeated.Data.Farm.TwinsXp);
            Assert.AreEqual(4, repeated.Data.Farm.GardenCapacity);
            Assert.AreEqual(imported.Farm.JournalLineage, repeated.Data.Farm.JournalLineage);
            Assert.IsTrue(repeated.Data.Farm.PendingCredits.ContainsKey("opaque-pending"));
        }

        [Test]
        public void CurrentSchemaWithoutReceiptInitializesFreshFieldsWithoutRewards()
        {
            var source = new GameData { LastGameVersion = "9999.0.0" };
            var result = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(1, result.Data.Farm.ProductionRevision);
            Assert.IsEmpty(result.Data.Farm.Seeds);
            Assert.IsEmpty(result.Data.Farm.Beds);
            Assert.AreEqual(0, result.Data.Farm.GardenCapacity);
            Assert.IsFalse(result.Data.Quests.ContainsKey(FarmContent.IntroductionId));
            Assert.IsTrue(result.Data.AppliedMigrationIds.Contains("SchemaV6Fields"));
        }
    }
}
#endif
