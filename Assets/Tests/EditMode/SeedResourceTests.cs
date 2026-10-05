#if UNITY_INCLUDE_TESTS
using System;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class SeedResourceTests
    {
        private const string Pack = "Radish Seed Pack";
        private static readonly DateTime Now = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);
        private static GameData Historical() => new GameData { SchemaVersion = 6, LastGameVersion = "9999.0.0" };

        [Test] public void FreshMigrationCreatesReceiptsWithoutGrantingPacks()
        {
            var data = new GameData();
            FarmSeedResources.Upgrade(data);
            Assert.AreEqual(1, data.Farm.SeedResourceRevision);
            Assert.AreEqual(17, data.Farm.MigratedSeedIds.Count);
            Assert.IsEmpty(data.Resources); Assert.IsEmpty(data.ResourceStats);
            Assert.IsEmpty(data.Farm.Seeds);
            Assert.IsFalse(FarmSeedResources.NeedsRepair(data));
        }

        [Test] public void KnownStockHistoryDiscoveryAndUnknownLedgerSurviveConversionAndReload()
        {
            var source = Historical();
            source.Farm.Seeds["seed.radish"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 7 };
            source.Farm.Seeds["seed.corn"] = new FarmSeedState { Quantity = 0, LifetimeAcquired = 3 };
            source.Farm.Seeds["future.seed"] = new FarmSeedState { Quantity = 17, LifetimeAcquired = 21 };
            source.Resources[Pack] = new GameData.ResourceEntry { Amount = 4, Earned = true, Tier = 3, BestPerMinute = 12 };
            source.ResourceStats[Pack] = new GameData.ResourceRecord { TotalReceived = 5, TotalSpent = 1 };
            var result = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            var data = CurrentSaveCodec.Clone(result.Data);
            Assert.AreEqual(6, data.Resources[Pack].Amount);
            Assert.AreEqual(3, data.Resources[Pack].Tier);
            Assert.AreEqual(12, data.Resources[Pack].BestPerMinute);
            Assert.AreEqual(12, data.ResourceStats[Pack].TotalReceived);
            Assert.AreEqual(6, data.ResourceStats[Pack].TotalSpent);
            Assert.IsTrue(data.Resources["Corn Seed Pack"].Earned);
            Assert.AreEqual(0, data.Resources["Corn Seed Pack"].Amount);
            Assert.AreEqual(0, data.Farm.Seeds["seed.radish"].Quantity);
            Assert.AreEqual(7, data.Farm.Seeds["seed.radish"].LifetimeAcquired);
            Assert.AreEqual(17, data.Farm.Seeds["future.seed"].Quantity);
            Assert.AreEqual(2, source.Farm.Seeds["seed.radish"].Quantity);
            var repeated = SaveMigrationRunner.TryMigrate(data, "9999.0.0");
            Assert.IsTrue(repeated.Succeeded, repeated.Error);
            Assert.IsFalse(repeated.Changed);
            Assert.AreEqual(6, repeated.Data.Resources[Pack].Amount);
        }

        [Test] public void PartialMigrationResumesOnlyUnreceiptedStockEvenWithGlobalReceipt()
        {
            var data = new GameData { LastGameVersion = "9999.0.0" };
            data.AppliedMigrationIds.Add(FarmSeedResources.MigrationId);
            data.Farm.SeedResourceRevision = 1;
            data.Farm.MigratedSeedIds.Add("seed.radish");
            data.Farm.Seeds["seed.radish"] = new FarmSeedState { Quantity = 0, LifetimeAcquired = 8 };
            data.Resources[Pack] = new GameData.ResourceEntry { Amount = 5, Earned = true };
            data.ResourceStats[Pack] = new GameData.ResourceRecord { TotalReceived = 8, TotalSpent = 3 };
            data.Farm.Seeds["seed.corn"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 4 };
            var result = SaveMigrationRunner.TryMigrate(data, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(5, result.Data.Resources[Pack].Amount);
            Assert.AreEqual(8, result.Data.ResourceStats[Pack].TotalReceived);
            Assert.AreEqual(2, result.Data.Resources["Corn Seed Pack"].Amount);
            Assert.AreEqual(0, result.Data.Farm.Seeds["seed.corn"].Quantity);
            Assert.AreEqual(17, result.Data.Farm.MigratedSeedIds.Count);
        }

        [TestCase(-1L)] [TestCase(9007199254740992L)]
        public void InvalidKnownStockFailsBeforeAnyTransfer(long badStock)
        {
            var data = Historical();
            data.Farm.Seeds["seed.radish"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 2 };
            data.Farm.Seeds["seed.corn"] = new FarmSeedState { Quantity = badStock, LifetimeAcquired = 2 };
            Assert.Throws<InvalidOperationException>(() => FarmSeedResources.Upgrade(data));
            Assert.IsEmpty(data.Resources); Assert.IsEmpty(data.Farm.MigratedSeedIds);
            Assert.AreEqual(2, data.Farm.Seeds["seed.radish"].Quantity);
            Assert.AreEqual(6, data.SchemaVersion);
        }

        [Test] public void ResidualStockBesideReceiptFailsWithoutDiscardingOrDuplicatingIt()
        {
            var data = Historical();
            data.Farm.MigratedSeedIds.Add("seed.radish");
            data.Farm.Seeds["seed.radish"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 2 };
            Assert.Throws<InvalidOperationException>(() => FarmSeedResources.Upgrade(data));
            Assert.AreEqual(2, data.Farm.Seeds["seed.radish"].Quantity); Assert.IsEmpty(data.Resources);
        }

        [Test] public void CreditAndLegacySpendUseCanonicalInventoryAndCannotSpendOpaqueLedger()
        {
            var data = new GameData();
            data.Farm.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked = true };
            var id = FarmJournal.StageSeed(data.Farm, "seed.radish", true, Now);
            var proposal = FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true);
            Assert.AreEqual(1, proposal.ResourceDeltas[Pack]);
            Assert.IsTrue(FarmTransaction.TryPrepare(data, proposal, out var credited, out var error), error);
            Assert.IsEmpty(data.Resources); Assert.IsEmpty(credited.Farm.Seeds);
            Assert.AreEqual(1, credited.Resources[Pack].Amount);
            Assert.AreEqual(1, credited.ResourceStats[Pack].TotalReceived);
            var plant = FarmCommands.Plant(credited.Farm, FarmCommands.WestBedId,
                FarmJournal.NextOperation(credited.Farm, "plant-radish:" + FarmCommands.WestBedId), Now, new FarmTuning());
            Assert.AreEqual(-1, plant.ResourceDeltas[Pack]);
            Assert.IsTrue(FarmTransaction.TryPrepare(credited, plant, out var paid, out error), error);
            paid = CurrentSaveCodec.Clone(paid);
            Assert.AreEqual(0, paid.Resources[Pack].Amount); Assert.IsTrue(paid.Resources[Pack].Earned);
            Assert.AreEqual(1, paid.ResourceStats[Pack].TotalSpent);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.CreditSeed(paid.Farm, id, Now, "seed.radish", true).Status);
            paid.Farm.Beds[FarmCommands.EastBedId] = new FarmBedState { Unlocked = true };
            paid.Farm.Seeds["future.seed"] = new FarmSeedState { Quantity = 999, LifetimeAcquired = 999 };
            var unpaid = FarmCommands.Plant(paid.Farm, FarmCommands.EastBedId,
                FarmJournal.NextOperation(paid.Farm, "plant-radish:" + FarmCommands.EastBedId), Now, new FarmTuning());
            Assert.IsFalse(FarmTransaction.TryPrepare(paid, unpaid, out _, out _));
            Assert.IsFalse(paid.Farm.Beds[FarmCommands.EastBedId].IsPlanted);
            Assert.AreEqual(999, paid.Farm.Seeds["future.seed"].Quantity);
        }

        [Test] public void MigrationTransfersJoinExistingCommittedInventoryPublicationKeys()
        {
            var data = Historical();
            data.Farm.Seeds["seed.corn"] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 2 };
            var id = FarmJournal.StageSeed(data.Farm, "seed.radish", true, Now);
            var proposal = FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true);
            Assert.IsTrue(FarmTransaction.TryPrepare(data, proposal, out var candidate, out var error), error);
            Assert.AreEqual(0, proposal.ResourceDeltas["Corn Seed Pack"]);
            Assert.AreEqual(2, candidate.Resources["Corn Seed Pack"].Amount);
            Assert.AreEqual(1, candidate.Resources[Pack].Amount);
            Assert.AreEqual(2, candidate.ResourceStats["Corn Seed Pack"].TotalReceived);
            Assert.AreEqual(2, data.Farm.Seeds["seed.corn"].Quantity);
        }

        [TestCase("Radish Seed Pack", 85, 226, "SeedPack_Radish")]
        [TestCase("Corn Seed Pack", 86, 216, "SeedPack_Corn")]
        [TestCase("Wheat Seed Pack", 87, 234, "SeedPack_Wheat")]
        [TestCase("Watermelone Seed Pack", 88, 233, "SeedPack_Watermelon")]
        [TestCase("Carrot Seed Pack", 89, 215, "SeedPack_Carrot")]
        [TestCase("Spud Seed Pack", 90, 224, "SeedPack_Potato")]
        [TestCase("Tomato Seed Pack", 91, 232, "SeedPack_Tomato")]
        [TestCase("Lettuce Seed Pack", 92, 214, "SeedPack_Cabbage")]
        [TestCase("Cucumber Seed Pack", 93, 217, "SeedPack_Cucumber")]
        [TestCase("Leek Seed Pack", 94, 221, "SeedPack_Leek")]
        [TestCase("Parsnip Seed Pack", 95, 222, "SeedPack_PaleRoot")]
        [TestCase("Pepper Seed Pack", 96, 213, "SeedPack_Beet")]
        [TestCase("Chillie Seed Pack", 97, 227, "SeedPack_RedPepper")]
        [TestCase("Pumking Seed Pack", 98, 225, "SeedPack_Pumpkin")]
        [TestCase("Strawberry Seed Pack", 99, 228, "SeedPack_Strawberry")]
        [TestCase("Funion Seed Pack", 100, 230, "SeedPack_TanBulb_A")]
        [TestCase("Turnip Seed Pack", 101, 226, "SeedPack_Radish")]
        public void SeedResourcesReuseMatchingExistingTmpAndTextCoreGlyphs(string name, int resourceId, int knownIndex, string glyphName)
        {
            var resource = Resources.Load<Resource>("Resource Items/" + name);
            Assert.NotNull(resource); Assert.AreEqual(resourceId, resource.resourceID);
            Assert.IsTrue(ResourceIconLookup.TryGetIconIndex(resourceId, out var known));
            Assert.IsTrue(ResourceIconLookup.TryGetUnknownIconIndex(resourceId, out var unknown));
            Assert.AreEqual(knownIndex, known); Assert.AreEqual(235, unknown);
            Assert.AreEqual("<sprite=" + known + ">", ResourceIconLookup.GetIconTag(resourceId));
            Assert.AreEqual("<sprite=235>", ResourceIconLookup.GetUnknownIconTag(resourceId));
            var tmp = Resources.Load<TMP_SpriteAsset>("Fonts/FloatingTextIcons");
            Assert.AreEqual(268, tmp.spriteGlyphTable.Count); Assert.AreEqual(256, tmp.spriteSheet.height);
            // Inline/drop variants retain their authored halo; inventory sprites have it removed.
            Assert.AreEqual(glyphName, tmp.spriteCharacterTable[known].name);
            Assert.AreEqual(resource.icon.name, tmp.spriteCharacterTable[known].name);
            Assert.AreEqual(resource.UnknownIcon.name, tmp.spriteCharacterTable[unknown].name);
            var inline = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.SpriteAsset>("Assets/UI/Toolkit/InlineSprites.asset"));
            var glyphs = inline.FindProperty("m_SpriteGlyphTable");
            Assert.AreEqual(268, glyphs.arraySize);
            foreach (var index in new[] { known, unknown })
            {
                var glyph = tmp.spriteGlyphTable[index];
                Assert.NotNull(glyph.sprite); Assert.AreEqual(16, glyph.glyphRect.width); Assert.AreEqual(16, glyph.glyphRect.height);
                Assert.GreaterOrEqual(glyph.glyphRect.x, 0); Assert.GreaterOrEqual(glyph.glyphRect.y, 0);
                Assert.LessOrEqual(glyph.glyphRect.x + glyph.glyphRect.width, tmp.spriteSheet.width);
                Assert.LessOrEqual(glyph.glyphRect.y + glyph.glyphRect.height, tmp.spriteSheet.height);
                var actual = glyphs.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue;
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(glyph.sprite, out string expectedGuid, out long expectedId));
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out string actualGuid, out long actualId));
                Assert.AreEqual(expectedGuid, actualGuid); Assert.AreEqual(expectedId, actualId);
            }
        }
    }
}
#endif
