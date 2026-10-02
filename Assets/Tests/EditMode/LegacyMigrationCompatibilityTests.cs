#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class LegacyMigrationCompatibilityTests
    {
        [SetUp]
        [TearDown]
        public void ResetMigrationRegistration()
        {
            typeof(SaveMigrationRunner).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
        }

        [Test]
        public void Released143_ValidInvestmentsRemainInTheirOriginalCards()
        {
            var source = Legacy("1.4.3");
            source.CauldronCardCounts["RES:Radish"] = 10000;
            source.CauldronCardCounts["BUFF:Haste"] = 3000;
            var result = Migrate(source);

            Assert.AreEqual(10000, result.Data.CauldronCardCounts["RES:Radish"]);
            Assert.AreEqual(3000, result.Data.CauldronCardCounts["BUFF:Haste"]);
            CollectionAssert.AreEquivalent(source.CauldronCardCounts, result.Data.CauldronCardCounts);
            Assert.AreEqual(1, source.SchemaVersion, "The loaded source is not the publication candidate.");
        }

        [Test]
        public void Released143_GenuineOverflowPreservesEveryCardAndRunsOnce()
        {
            var source = Legacy("1.4.3");
            source.CauldronCardCounts["RES:Radish"] = 10002;
            source.CauldronCardCounts["BUFF:Haste"] = 3001;
            var first = Migrate(source);
            Assert.AreEqual(10000, first.Data.CauldronCardCounts["RES:Radish"]);
            Assert.AreEqual(3000, first.Data.CauldronCardCounts["BUFF:Haste"]);
            Assert.AreEqual(13003L, first.Data.CauldronCardCounts.Values.Sum(value => (long)value));
            Assert.AreEqual(3L, first.Data.CauldronCardCounts
                .Where(pair => pair.Key.StartsWith("INF:")).Sum(pair => (long)pair.Value));
            Assert.AreEqual(10002, source.CauldronCardCounts["RES:Radish"]);

            var second = Migrate(first.Data);
            Assert.IsFalse(second.Changed);
            CollectionAssert.AreEquivalent(first.Data.CauldronCardCounts, second.Data.CauldronCardCounts);
        }

        [TestCase("1.2.16")]
        [TestCase("0.0.0")]
        [TestCase("1.4.4")]
        public void UnverifiedProducer_DefersRedistributionAndReturnsDiagnostic(string version)
        {
            var source = Legacy(version);
            source.GameVersionCreated = "1.4.3";
            source.CauldronCardCounts["RES:Radish"] = 750000;
            source.CauldronCardCounts["BUFF:Haste"] = 50000;
            var result = Migrate(source);
            CollectionAssert.AreEquivalent(source.CauldronCardCounts, result.Data.CauldronCardCounts);
            Assert.That(result.Warnings.Any(message => message.Contains("redistribution deferred")));
            var reloaded = Migrate(result.Data);
            CollectionAssert.AreEquivalent(source.CauldronCardCounts, reloaded.Data.CauldronCardCounts);
            Assert.That(reloaded.Warnings.Any(message => message.Contains("redistribution deferred")),
                "Migration's new LastGameVersion cannot relabel an unknown source profile.");
        }

        [Test]
        public void MissingLastProducer_UsesVerifiedCreatedVersion()
        {
            var source = Legacy(null);
            source.GameVersionCreated = "1.4.3";
            source.CauldronCardCounts["RES:Radish"] = 750;
            var result = Migrate(source);
            Assert.AreEqual(750, result.Data.CauldronCardCounts["RES:Radish"]);
            Assert.IsFalse(result.Warnings.Any(message => message.Contains("redistribution deferred")));
        }

        [Test]
        public void PaidMildredReceipt_MapsTimestampWithoutPaymentRewardOrCapacityReplay()
        {
            var source = Legacy("1.4.3");
            source.Quests["Mildred1"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 123456 };
            source.Resources["Fish"] = new GameData.ResourceEntry { Amount = 432.25d, Earned = true, Tier = 3 };
            source.CompletedNpcTasks.Add("Mildred");
            source.UnlockedAutoBuffSlots = 2;
            var first = Migrate(source);

            Assert.IsTrue(first.Data.Quests["BuffSlot2"].Completed);
            Assert.AreEqual(123456, first.Data.Quests["BuffSlot2"].CompletedTimestamp);
            Assert.IsTrue(first.Data.Quests["Mildred1"].Completed);
            Assert.AreEqual(2, first.Data.UnlockedBuffSlots);
            Assert.AreEqual(2, first.Data.UnlockedAutoBuffSlots);
            Assert.AreEqual(432.25d, first.Data.Resources["Fish"].Amount);
            CollectionAssert.AreEquivalent(source.CompletedNpcTasks, first.Data.CompletedNpcTasks);
            Assert.IsFalse(source.Quests.ContainsKey("BuffSlot2"));

            var second = Migrate(first.Data);
            Assert.IsFalse(second.Changed);
            Assert.AreEqual(2, second.Data.UnlockedBuffSlots);
            Assert.AreEqual(432.25d, second.Data.Resources["Fish"].Amount);
        }

        [Test]
        public void IncompleteMildred_DoesNotBecomeAPaidReceipt()
        {
            var source = Legacy("1.4.3");
            source.Quests["Mildred1"] = new GameData.QuestRecord { DistanceTravelProgress = 42d };
            var result = Migrate(source);
            Assert.IsFalse(result.Data.Quests.ContainsKey("BuffSlot2"));
            Assert.AreEqual(1, result.Data.UnlockedBuffSlots);
            Assert.AreEqual(42d, result.Data.Quests["Mildred1"].DistanceTravelProgress);
        }

        [Test]
        public void BothCompletedNames_CountOnceAndKeepExistingCanonicalReceipt()
        {
            var source = Legacy("1.4.3");
            source.Quests["Mildred1"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 111 };
            source.Quests["BuffSlot2"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 222 };
            var result = Migrate(source);
            Assert.AreEqual(2, result.Data.UnlockedBuffSlots);
            Assert.AreEqual(222, result.Data.Quests["BuffSlot2"].CompletedTimestamp);
            Assert.AreEqual(111, result.Data.Quests["Mildred1"].CompletedTimestamp);
        }

        [Test]
        public void AliasOverPendingCanonical_PreservesObjectiveEvidenceWithoutCharging()
        {
            var source = Legacy("1.4.3");
            source.Quests["Mildred1"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 111 };
            source.Quests["BuffSlot2"] = new GameData.QuestRecord
            {
                KillProgress = new Dictionary<string, double> { ["Saved enemy"] = 4d },
                ResourcesBaseline = 23d,
                ResourcesBaselineSet = true
            };
            var result = Migrate(source);
            Assert.IsTrue(result.Data.Quests["BuffSlot2"].Completed);
            Assert.AreEqual(111, result.Data.Quests["BuffSlot2"].CompletedTimestamp);
            Assert.AreEqual(4d, result.Data.Quests["BuffSlot2"].KillProgress["Saved enemy"]);
            Assert.AreEqual(23d, result.Data.Quests["BuffSlot2"].ResourcesBaseline);
        }

        [TestCase(1, 5)]
        [TestCase(100, 5)]
        public void AllFourUniqueReceipts_ReachFiveRegardlessOfAliasDuplicate(int oldCapacity, int expected)
        {
            var source = Legacy("1.4.3");
            source.UnlockedBuffSlots = oldCapacity;
            foreach (var id in new[] { "Mildred1", "BuffSlot2", "BuffSlot3", "BuffSlot4", "BuffSlot5" })
                source.Quests[id] = new GameData.QuestRecord { Completed = true };
            Assert.AreEqual(expected, Migrate(source).Data.UnlockedBuffSlots);
        }

        [Test]
        public void UnmatchedEarnedCapacityAndMeaningfulHistory_AreRetained()
        {
            var source = Legacy("1.4.3");
            source.UnlockedBuffSlots = 4;
            source.Quests["Fence2"] = new GameData.QuestRecord { DistanceTravelProgress = 17d };
            source.Quests["Unknown retired quest"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 7 };
            var result = Migrate(source);
            Assert.AreEqual(4, result.Data.UnlockedBuffSlots);
            Assert.AreEqual(17d, result.Data.Quests["Fence2"].DistanceTravelProgress);
            Assert.AreEqual(7, result.Data.Quests["Unknown retired quest"].CompletedTimestamp);
            Assert.IsFalse(result.Data.Quests.ContainsKey("Farm.PrepareBeds"));
        }

        [Test]
        public void MissingFarm_InitializesEmptyWithoutLegacyOwnershipOrSeedGrants()
        {
            var source = Legacy("1.4.3");
            source.Farm = null;
            source.Quests["Fence1"] = new GameData.QuestRecord { Completed = true };
            source.Resources["Radish"] = new GameData.ResourceEntry { Amount = 1000d, Earned = true, Tier = 8 };
            var result = Migrate(source);
            Assert.IsNotNull(result.Data.Farm);
            Assert.IsEmpty(result.Data.Farm.Seeds);
            Assert.IsEmpty(result.Data.Farm.Beds);
            Assert.IsFalse(result.Data.Farm.OriginalBedsPrepared);
            Assert.IsNull(source.Farm);
            Assert.IsTrue(result.Data.Quests["Fence1"].Completed);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void NullKnownSkillRecord_NormalizesBeforePublicationAndKeepsUnknownNullHistory(int schema)
        {
            var source = Legacy("1.4.3");
            source.SchemaVersion = schema;
            source.SkillData["Farming"] = null;
            source.SkillData["Unknown retired skill"] = null;
            source.SkillData["Mining"] = new GameData.SkillProgress { Level = 113, CurrentXP = 7967.081f };
            var result = Migrate(source);
            Assert.IsNotNull(result.Data.SkillData["Farming"]);
            Assert.AreEqual(1, result.Data.SkillData["Farming"].Level);
            Assert.AreEqual(0f, result.Data.SkillData["Farming"].CurrentXP);
            Assert.IsNull(result.Data.SkillData["Unknown retired skill"]);
            Assert.AreEqual(113, result.Data.SkillData["Mining"].Level);
            CollectionAssert.AreEqual(BitConverter.GetBytes(7967.081f), BitConverter.GetBytes(result.Data.SkillData["Mining"].CurrentXP));
            Assert.IsNull(source.SkillData["Farming"], "Normalization cannot mutate the decoded original.");
            Assert.IsEmpty(result.Data.Farm.Seeds);
        }

        [Test]
        public void AlreadyStampedSchemaFour_RepairsNewlyMissingKnownRecordsAndFarmWithoutRewardReplay()
        {
            var source = Migrate(Legacy("1.4.3")).Data;
            Assert.That(source.AppliedMigrationIds.Contains("SchemaV4CurrentCollections"));
            source.SkillData["Farming"] = null;
            source.SkillData["Unknown retired skill"] = null;
            source.Farm = null;
            source.Resources["Fish"] = new GameData.ResourceEntry { Amount = 432.25d, Earned = true, Tier = 3 };
            source.Quests["BuffSlot2"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 123 };
            var first = Migrate(source);
            Assert.IsTrue(first.Changed);
            Assert.IsNotNull(first.Data.SkillData["Farming"]);
            Assert.IsNull(first.Data.SkillData["Unknown retired skill"]);
            Assert.IsNotNull(first.Data.Farm);
            Assert.IsEmpty(first.Data.Farm.Seeds);
            Assert.IsEmpty(first.Data.Farm.Beds);
            Assert.AreEqual(432.25d, first.Data.Resources["Fish"].Amount);
            Assert.AreEqual(123, first.Data.Quests["BuffSlot2"].CompletedTimestamp);
            Assert.IsNull(source.SkillData["Farming"]);
            Assert.IsNull(source.Farm);
            Assert.IsFalse(Migrate(first.Data).Changed, "The repaired payload settles after one publication.");
        }

        [TestCase(3, float.NaN)]
        [TestCase(3, float.PositiveInfinity)]
        [TestCase(3, float.NegativeInfinity)]
        [TestCase(4, float.NaN)]
        [TestCase(4, float.PositiveInfinity)]
        [TestCase(4, float.NegativeInfinity)]
        public void NonfiniteKnownXp_IsRejectedWithoutChangingDecodedGraph(int schema, float invalidXp)
        {
            var source = Legacy("1.4.3");
            source.SchemaVersion = schema;
            source.SkillData["Farming"] = new GameData.SkillProgress { Level = 7, CurrentXP = invalidXp };
            source.Resources["Fish"] = new GameData.ResourceEntry { Amount = 432.25d, Earned = true, Tier = 3 };
            source.Quests["Mildred1"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 123 };
            source.UpgradeLevels["Damage"] = 25;
            var before = Sirenix.Serialization.SerializationUtility.SerializeValue(source, Sirenix.Serialization.DataFormat.Binary);
            var result = SaveMigrationRunner.TryMigrate(source, "1.4.3");
            Assert.IsFalse(result.Succeeded);
            Assert.IsFalse(result.Changed);
            Assert.AreSame(source, result.Data);
            Assert.IsEmpty(result.AppliedIds);
            StringAssert.Contains("Farming", result.Error);
            StringAssert.Contains("nonfinite XP", result.Error);
            CollectionAssert.AreEqual(before, Sirenix.Serialization.SerializationUtility.SerializeValue(source, Sirenix.Serialization.DataFormat.Binary));
            Assert.IsFalse(source.Quests.ContainsKey("BuffSlot2"), "No receipt mutation precedes validation.");
            Assert.AreEqual(25, source.UpgradeLevels["Damage"]);
        }

        [Test]
        public void AlreadyStampedSchemaFour_NonfiniteKnownXpCannotBypassValidationThroughNoOpRoute()
        {
            var source = Migrate(Legacy("1.4.3")).Data;
            source.SkillData["Farming"] = new GameData.SkillProgress { Level = 7, CurrentXP = float.NaN };
            var result = SaveMigrationRunner.TryMigrate(source, "1.4.3");
            Assert.IsFalse(result.Succeeded);
            Assert.AreSame(source, result.Data);
            Assert.IsTrue(float.IsNaN(source.SkillData["Farming"].CurrentXP));
        }

        [Test]
        public void PurchaseOmission_OnlyAfterHistoricalConsumerWithoutGearCompensation()
        {
            var source = Legacy("1.4.3");
            source.SchemaVersion = 3;
            source.UpgradeLevels["Damage"] = 25;
            var consumer = new LegacyPurchaseConsumer();
            SaveMigrationRunner.Register(consumer);
            var result = Migrate(source);
            Assert.IsTrue(consumer.SawLegacyPurchase);
            Assert.AreEqual(25, source.UpgradeLevels["Damage"]);
            Assert.IsEmpty(result.Data.UpgradeLevels);
            Assert.IsEmpty(result.Data.EquipmentBySlot);
            CollectionAssert.Contains(result.AppliedIds, "TestLegacyPurchaseConsumer");
        }

        [Test]
        public void AlreadyRepairedFixture_DoesNotInventReconstructionAndReportsLimitation()
        {
            var source = Legacy("1.4.3");
            source.SchemaVersion = 3;
            source.AppliedMigrationIds.Add("SchemaV2ZZCauldronOverflowRepair");
            source.CauldronCardCounts["RES:Radish"] = 500;
            source.CauldronCardCounts["INF:Damage"] = 250;
            var result = Migrate(source);
            CollectionAssert.AreEquivalent(source.CauldronCardCounts, result.Data.CauldronCardCounts);
            Assert.That(result.Warnings.Any(message => message.Contains("cannot be reconstructed")));
        }

        private static GameData Legacy(string version) => new GameData
        {
            SchemaVersion = 1,
            LastGameVersion = version,
            GameVersionCreated = version
        };

        private static SaveMigrationResult Migrate(GameData source)
        {
            var result = SaveMigrationRunner.TryMigrate(source, "1.4.3");
            Assert.IsTrue(result.Succeeded, result.Error);
            return result;
        }

        private sealed class LegacyPurchaseConsumer : ISaveMigration
        {
            public bool SawLegacyPurchase;
            public int? TargetSchema => null;
            public string TargetVersion => "1.4.3";
            public string Id => "TestLegacyPurchaseConsumer";
            public void Apply(GameData data) => SawLegacyPurchase = data.UpgradeLevels["Damage"] == 25;
        }
    }
}
#endif
