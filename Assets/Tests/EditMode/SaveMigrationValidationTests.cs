#if UNITY_INCLUDE_TESTS
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;

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
