#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using NUnit.Framework;
using Sirenix.Serialization;

namespace Tests.EditMode
{
    public class CurrentSaveCodecTests
    {
        [Test]
        public void NewSnapshot_OmitsObsoleteFieldsAndKeepsExactProgressAndFiveActiveChoices()
        {
            var source = new GameData { StatUpgradesMigratedToGear = true, DuckHelmetSanitized = true };
            source.UpgradeLevels["Damage"] = 25;
            var skill = new GameData.SkillProgress { Level = 115, CurrentXP = 123.4567f };
            for (var index = 0; index < 5; index++)
                skill.Milestones.Add(new GameData.MilestoneProgressRecord
                    { Id = "chosen." + index, IsActive = true, TierIndex = 7 });
            skill.Milestones.Add(new GameData.MilestoneProgressRecord { Id = "automatic.old", TierIndex = 9 });
            skill.Milestones.Add(new GameData.MilestoneProgressRecord { Id = "chosen.0", IsActive = true });
            source.SkillData["Farming"] = skill;

            var bytes = CurrentSaveCodec.Serialize(source);
            Assert.IsTrue(ContainsField(bytes, "ActiveMilestoneIds"), "Verify the actual wire projection.");
            foreach (var field in new[] { "UpgradeLevels", "StatUpgradesMigratedToGear", "DuckHelmetSanitized",
                         "TierIndex", "MilestoneRecords", "Milestones" })
                Assert.IsFalse(ContainsField(bytes, field), field + " must be absent from the written payload.");

            var reloaded = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.Binary);
            var loadedSkill = reloaded.SkillData["Farming"];
            Assert.AreEqual(skill.Level, loadedSkill.Level);
            CollectionAssert.AreEqual(BitConverter.GetBytes(skill.CurrentXP), BitConverter.GetBytes(loadedSkill.CurrentXP));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 5).Select(index => "chosen." + index),
                loadedSkill.Milestones.Select(record => record.Id));
            Assert.That(loadedSkill.Milestones.All(record => record.IsActive && record.TierIndex == -1));
            Assert.IsTrue(reloaded.UpgradeLevels == null || reloaded.UpgradeLevels.Count == 0);
            Assert.IsFalse(reloaded.StatUpgradesMigratedToGear);
            Assert.IsFalse(reloaded.DuckHelmetSanitized);
            Assert.AreEqual(25, source.UpgradeLevels["Damage"], "Projection must not modify its source.");
            Assert.AreEqual(7, skill.Milestones[0].TierIndex);
        }

        [Test]
        public void NewSnapshot_KeepsMeaningfulQuestInventoryActualGearAndMigrationReceipts()
        {
            var source = new GameData();
            source.Resources["Retired Onion"] = new GameData.ResourceEntry { Amount = 79780.06889283087d, Earned = true, Tier = 3 };
            source.Quests["Fence2"] = new GameData.QuestRecord
            {
                DistanceTravelProgress = 17.75d,
                KillProgress = new Dictionary<string, double> { ["Unknown enemy"] = 12d },
                BuffCastProgress = new Dictionary<string, int> { ["Unknown buff"] = 3 }
            };
            source.Quests["Unknown paid quest"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 1234567 };
            source.EquipmentBySlot["Helmet"] = new GearItemRecord
            {
                slot = "Helmet", rarity = "Owned actual rarity",
                affixes = new List<GearAffixRecord> { new GearAffixRecord { statId = "owned.stat", quality = 0.75d, value = 42f } }
            };
            source.AppliedMigrationIds.Add("already-paid-transition");
            source.CompletedNpcTasks.Add("Barkley");

            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(source), DataFormat.Binary);
            CollectionAssert.AreEqual(BitConverter.GetBytes(source.Resources["Retired Onion"].Amount),
                BitConverter.GetBytes(loaded.Resources["Retired Onion"].Amount));
            Assert.IsTrue(loaded.Resources["Retired Onion"].Earned);
            Assert.AreEqual(3, loaded.Resources["Retired Onion"].Tier);
            Assert.AreEqual(17.75d, loaded.Quests["Fence2"].DistanceTravelProgress);
            Assert.AreEqual(12d, loaded.Quests["Fence2"].KillProgress["Unknown enemy"]);
            Assert.AreEqual(3, loaded.Quests["Fence2"].BuffCastProgress["Unknown buff"]);
            Assert.IsTrue(loaded.Quests["Unknown paid quest"].Completed);
            Assert.AreEqual(1234567, loaded.Quests["Unknown paid quest"].CompletedTimestamp);
            Assert.AreEqual("Owned actual rarity", loaded.EquipmentBySlot["Helmet"].rarity);
            Assert.AreEqual(0.75d, loaded.EquipmentBySlot["Helmet"].affixes[0].quality);
            Assert.AreEqual(42f, loaded.EquipmentBySlot["Helmet"].affixes[0].value);
            CollectionAssert.Contains(loaded.AppliedMigrationIds, "already-paid-transition");
            CollectionAssert.Contains(loaded.CompletedNpcTasks, "Barkley");
            Assert.IsEmpty(loaded.Farm.Seeds, "Old earned resources and receipts cannot discover seed packs.");
        }

        [Test]
        public void CompatibilityClone_RetainsPurchasesAndOneTimeGuardsForHistoricalConsumers()
        {
            var source = new GameData { StatUpgradesMigratedToGear = true, DuckHelmetSanitized = true };
            source.UpgradeLevels["AttackRate"] = 90;
            var clone = CurrentSaveCodec.Clone(source);
            Assert.AreNotSame(source, clone);
            Assert.AreEqual(90, clone.UpgradeLevels["AttackRate"]);
            Assert.IsTrue(clone.StatUpgradesMigratedToGear);
            Assert.IsTrue(clone.DuckHelmetSanitized);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void DirectCurrentWrite_NonfiniteKnownXpIsRejectedWithoutZeroingProgress(float invalidXp)
        {
            var source = new GameData();
            source.SkillData["Farming"] = new GameData.SkillProgress { Level = 7, CurrentXP = invalidXp };
            var xpBytes = BitConverter.GetBytes(invalidXp);
            var error = Assert.Throws<InvalidDataException>(() => CurrentSaveCodec.Serialize(source));
            StringAssert.Contains("nonfinite XP", error.Message);
            CollectionAssert.AreEqual(xpBytes, BitConverter.GetBytes(source.SkillData["Farming"].CurrentXP));
            Assert.AreEqual(7, source.SkillData["Farming"].Level);
        }

        [Test]
        public void UnknownNonfiniteProgress_RemainsPreservedHistoryWithoutReinterpretingIt()
        {
            var source = new GameData();
            source.SkillData["Unknown retired skill"] = new GameData.SkillProgress { Level = 7, CurrentXP = float.NaN };
            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(source), DataFormat.Binary);
            Assert.AreEqual(7, loaded.SkillData["Unknown retired skill"].Level);
            Assert.IsTrue(float.IsNaN(loaded.SkillData["Unknown retired skill"].CurrentXP));
        }

        [TestCase("Milestones")]
        [TestCase("MilestoneRecords")]
        public void ActualTypedLegacyWire_PreservesRecordsAndExactXpBeforeProjection(string field)
        {
            var records = new List<GameData.MilestoneProgressRecord>
            {
                new GameData.MilestoneProgressRecord { Id = "chosen", IsActive = true, TierIndex = 2 },
                new GameData.MilestoneProgressRecord { Id = "inactive", IsActive = false, TierIndex = 3 }
            };
            var loaded = ReadLegacySkillWire(field, records);
            Assert.AreEqual(115, loaded.Level);
            CollectionAssert.AreEqual(BitConverter.GetBytes(7967.081f), BitConverter.GetBytes(loaded.CurrentXP));
            Assert.AreEqual(2, loaded.Milestones.Count);
            Assert.IsTrue(loaded.Milestones[0].IsActive);
            Assert.AreEqual(2, loaded.Milestones[0].TierIndex);
            Assert.AreEqual("inactive", loaded.Milestones[1].Id);
        }

        [Test]
        public void ActualStringEraWire_PreservesProgressWithoutTurningAutomaticUnlocksIntoChoices()
        {
            var loaded = ReadLegacySkillWire("Milestones", new List<string> { "automatic.Farming7", "automatic.Farming13" });
            Assert.AreEqual(115, loaded.Level);
            CollectionAssert.AreEqual(BitConverter.GetBytes(7967.081f), BitConverter.GetBytes(loaded.CurrentXP));
            Assert.IsEmpty(loaded.Milestones);
        }

        [Test]
        public void UnknownWireEntries_AreSkippedWithoutLosingFollowingSkillFields()
        {
            var loaded = ReadLegacySkillWire("Milestones", new List<string> { "obsolete" }, true);
            Assert.AreEqual(115, loaded.Level);
            Assert.AreEqual(7967.081f, loaded.CurrentXP);
            Assert.IsEmpty(loaded.Milestones);
        }

        [TestCase("Blindsided.SaveData.GameData,Assembly-CSharp")]
        [TestCase("GameData, Assembly-CSharp")]
        public void Es3StringEraBank_UnquotedIntegerKeysPreserveExactProgressAndUnknownVersion(string type)
        {
            var bank = "{\"Beta5Data\":{\"__type\":\"" + type + "\",\"value\":{" +
                "\"SkillData\":{\"Farming\":{\"Level\":90,\"CurrentXP\":7967.081,\"Milestones\":[\"automatic\"]}}," +
                "\"TaskRecords\":{28:{\"TotalCompleted\":3,\"TimeSpent\":6.5,\"XpGained\":1.25}}," +
                "\"Quests\":{\"literal {28: \\\"quote\\\"\":{\"Completed\":true}}," +
                "\"Resources\":{\"Radish\":{\"Amount\":12.345678901234567,\"Earned\":true,\"Tier\":1}}}}}";
            Assert.IsTrue(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.SchemaVersion);
            Assert.IsNull(loaded.LastGameVersion);
            Assert.AreEqual(90, loaded.SkillData["Farming"].Level);
            CollectionAssert.AreEqual(BitConverter.GetBytes(7967.081f), BitConverter.GetBytes(loaded.SkillData["Farming"].CurrentXP));
            Assert.IsEmpty(loaded.SkillData["Farming"].Milestones);
            Assert.AreEqual(3, loaded.TaskRecords[28].TotalCompleted);
            Assert.AreEqual(6.5f, loaded.TaskRecords[28].TimeSpent);
            Assert.IsTrue(loaded.Quests["literal {28: \"quote\""].Completed, "String contents cannot be normalized.");
            Assert.AreEqual(12.345678901234567d, loaded.Resources["Radish"].Amount);
            Assert.IsEmpty(loaded.Farm.Seeds);
        }

        [Test]
        public void Es3TypedBank_KeepsMeaningfulSelectionsAtIngress()
        {
            const string bank = "{\"Data0\":{\"__type\":\"Blindsided.SaveData.GameData,Assembly-CSharp\",\"value\":{" +
                "\"LastGameVersion\":\"1.3.1\",\"SkillData\":{\"Farming\":{\"Level\":7,\"CurrentXP\":1.5," +
                "\"Milestones\":[{\"Id\":\"chosen\",\"IsActive\":true,\"TierIndex\":2}]}}}}}";
            Assert.IsTrue(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error), error);
            Assert.AreEqual("1.3.1", loaded.LastGameVersion);
            Assert.AreEqual("chosen", loaded.SkillData["Farming"].Milestones.Single().Id);
            Assert.IsTrue(loaded.SkillData["Farming"].Milestones.Single().IsActive);
        }

        [Test]
        public void Es3AmbiguousBanks_FailWithoutSelectingOrFabricatingFreshData()
        {
            const string bank = "{\"Data0\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":{}}," +
                "\"Beta5Data\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":{}}}";
            Assert.IsFalse(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error));
            Assert.IsNull(loaded);
            StringAssert.Contains("Multiple GameData entries", error);
        }

        [TestCase("5")]
        [TestCase("4")]
        [TestCase("2")]
        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("null")]
        [TestCase("\"1\"")]
        [TestCase("1.0")]
        public void Es3UnsupportedSchemaDeclaration_IsRejectedBeforeDowngradingOrMaterialization(string schema)
        {
            var bank = "{\"Data0\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":{" +
                "\"SchemaVersion\":" + schema + ",\"Resources\":{\"Radish\":{\"Amount\":99}}}}}";
            Assert.IsFalse(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error));
            Assert.IsNull(loaded, "Unsupported input cannot become a schema-1 publication candidate.");
            StringAssert.Contains("Unsupported ES3 schema declaration", error);
        }

        [Test]
        public void Es3ExplicitSchemaOne_RemainsSupported()
        {
            const string bank = "{\"Data0\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":{" +
                "\"SchemaVersion\":1,\"Resources\":{\"Radish\":{\"Amount\":99}}}}}";
            Assert.IsTrue(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error), error);
            Assert.AreEqual(1, loaded.SchemaVersion);
            Assert.AreEqual(99d, loaded.Resources["Radish"].Amount);
        }

        [Test]
        public void Es3MalformedRecognizedLiveEntry_CannotSilentlySelectLaterValidBetaEntry()
        {
            const string bank = "{\"Data0\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":null}," +
                "\"Beta5Data\":{\"__type\":\"GameData,Assembly-CSharp\",\"value\":{}}}";
            Assert.IsFalse(LegacyEs3Adapter.TryDecode(bank, out var loaded, out var error));
            Assert.IsNull(loaded);
            StringAssert.Contains("Malformed ES3 GameData entry", error);
        }

        private static GameData.SkillProgress ReadLegacySkillWire<T>(string field, T milestones, bool unknown = false)
        {
            using (var stream = new MemoryStream())
            {
                var context = new SerializationContext();
                context.TryRegisterInternalReference(new GameData.SkillProgress(), out var rootId);
                using (var writer = SerializationUtility.CreateWriter(stream, context, DataFormat.Binary))
                {
                    writer.BeginReferenceNode(null, typeof(GameData.SkillProgress), rootId);
                    if (unknown) Serializer.Get<List<string>>().WriteValue("RemovedLegacyField", new List<string> { "preserve parsing" }, writer);
                    Serializer.Get<T>().WriteValue(field, milestones, writer);
                    Serializer.Get<int>().WriteValue("Level", 115, writer);
                    Serializer.Get<float>().WriteValue("CurrentXP", 7967.081f, writer);
                    writer.EndNode(null);
                    writer.FlushToStream();
                }
                return SerializationUtility.DeserializeValue<GameData.SkillProgress>(stream.ToArray(), DataFormat.Binary);
            }
        }

        private static bool ContainsField(byte[] payload, string name) =>
            Encoding.UTF8.GetString(payload).Contains(name) || Encoding.Unicode.GetString(payload).Contains(name) ||
            Encoding.Unicode.GetString(payload, 1, payload.Length - 1).Contains(name);
    }
}
#endif
