#if UNITY_INCLUDE_TESTS
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using System.Linq;

namespace Tests.EditMode
{
    public sealed class SaveMigrationValidationTests
    {
        [Test]
        public void HistoricalZeroSchemaRemainsReadableWithExactProgress()
        {
            var source = new GameData { SchemaVersion = 0, LastGameVersion = "9999.0.0" };
            source.SkillData["Farming"] = new GameData.SkillProgress { Level = 72, CurrentXP = 8192.125f };
            source.Resources["preserved-unknown"] = new GameData.ResourceEntry { Amount = 123456789.25, Tier = 7 };
            var result = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(GameData.CurrentSchemaVersion, result.Data.SchemaVersion);
            Assert.AreEqual(8192.125f, result.Data.SkillData["Farming"].CurrentXP);
            Assert.AreEqual(123456789.25, result.Data.Resources["preserved-unknown"].Amount);
            Assert.AreEqual(0, source.SchemaVersion);
            Assert.IsEmpty(source.AppliedMigrationIds);
        }

        [TestCase(0)] [TestCase(3)] [TestCase(4)]
        public void ProductionRetirementPreservesBalancesAndQuestsWithoutPayoutAcrossReload(int schema)
        {
            var source = new GameData { SchemaVersion = schema, LastGameVersion = "9999.0.0", DisciplePercent = .03f };
            source.Resources["Radish"] = new GameData.ResourceEntry { Amount = 17.25, Earned = true, Tier = 3, BestPerMinute = 90 };
            source.Disciples["Radish"] = new GameData.DiscipleGenerationRecord
            {
                StoredResources = new System.Collections.Generic.Dictionary<string, double> { ["Radish"] = 1000.5 },
                TotalCollected = new System.Collections.Generic.Dictionary<string, double> { ["Radish"] = 42 },
                LastGenerationTime = 1234, Progress = .5f
            };
            source.Quests["Finishing Touches"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 456 };
            source.CauldronCardCounts["RES:Radish"] = 3500;
            var migrated = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsTrue(migrated.Succeeded, migrated.Error);
            Assert.AreEqual(GameData.CurrentSchemaVersion, migrated.Data.SchemaVersion);
            Assert.Contains("SchemaV5AlterEchoRetirement", migrated.Data.AppliedMigrationIds.ToArray());
            var imported = Sirenix.Serialization.SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(migrated.Data), Sirenix.Serialization.DataFormat.Binary);
            var repeated = SaveMigrationRunner.TryMigrate(imported, "9999.0.0");
            Assert.IsTrue(repeated.Succeeded, repeated.Error);
            Assert.AreEqual(17.25, repeated.Data.Resources["Radish"].Amount);
            Assert.AreEqual(1000.5, repeated.Data.Disciples["Radish"].StoredResources["Radish"]);
            Assert.AreEqual(42, repeated.Data.Disciples["Radish"].TotalCollected["Radish"]);
            Assert.AreEqual(1234, repeated.Data.Disciples["Radish"].LastGenerationTime);
            Assert.AreEqual(.5f, repeated.Data.Disciples["Radish"].Progress);
            Assert.AreEqual(.03f, repeated.Data.DisciplePercent);
            Assert.IsTrue(repeated.Data.Quests["Finishing Touches"].Completed);
            Assert.AreEqual(456, repeated.Data.Quests["Finishing Touches"].CompletedTimestamp);
            Assert.AreEqual(3500, repeated.Data.CauldronCardCounts["RES:Radish"]);
            Assert.AreEqual(schema, source.SchemaVersion);
            Assert.IsEmpty(source.AppliedMigrationIds);
            var rollbackSource = CurrentSaveCodec.Clone(source);
            rollbackSource.LastGameVersion = "0.0.0"; // Make the throwing version migration eligible even for schema zero.
            var rollback = SaveMigrationRunner.RunRollbackProbeForTests(rollbackSource);
            Assert.IsFalse(rollback.Succeeded); Assert.AreSame(rollbackSource, rollback.Data);
            Assert.AreEqual(17.25, rollbackSource.Resources["Radish"].Amount);
            Assert.IsEmpty(rollbackSource.AppliedMigrationIds);
            Assert.AreEqual(17.25, source.Resources["Radish"].Amount);
            Assert.IsEmpty(source.AppliedMigrationIds);
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void InvalidNegativeSchemaIsRejectedWithoutTransformingOriginalProgress(int schema)
        {
            var source = new GameData { SchemaVersion = schema, LastGameVersion = "1.0.0" };
            source.Resources["preserved-unknown"] = new GameData.ResourceEntry { Amount = 123456789.25, Tier = 7 };
            source.SkillData["Farming"] = new GameData.SkillProgress { Level = 72, CurrentXP = 8192.125f };
            source.RetroQuestRewardsApplied.Add("already-paid");
            var result = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsFalse(result.Succeeded, "Negative schema is invalid data, not a historical version.");
            StringAssert.Contains("schema", result.Error.ToLowerInvariant());
            Assert.IsFalse(result.Changed);
            Assert.AreSame(source, result.Data);
            Assert.AreEqual(schema, source.SchemaVersion);
            Assert.AreEqual("1.0.0", source.LastGameVersion);
            Assert.AreEqual(123456789.25, source.Resources["preserved-unknown"].Amount);
            Assert.AreEqual(8192.125f, source.SkillData["Farming"].CurrentXP);
            Assert.IsTrue(source.RetroQuestRewardsApplied.Contains("already-paid"));
            Assert.IsEmpty(source.AppliedMigrationIds);
        }
    }
}
#endif
