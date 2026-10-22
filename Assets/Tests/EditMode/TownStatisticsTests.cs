#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using Blindsided.Utilities;
using NUnit.Framework;
using Sirenix.Serialization;
using TimelessEchoes.Gear;
using TimelessEchoes.Farming;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class TownStatisticsTests
    {
        [Test]
        public void CropFruitAndTimberPayoutsCountOnceAcrossManualAutoAndReload()
        {
            var content = FarmContent.Load();
            var now = new System.DateTime(2026, 10, 22, 0, 0, 0, System.DateTimeKind.Utc);
            var data = new GameData();
            data.Farm.TwinsLevel = 20; data.Farm.GardenCapacity = 1; data.Farm.OrchardCapacity = 4;
            data.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
            var ids = new[] { "recipe.radish.v1", "recipe.apple.v1", "recipe.oak.v1", "recipe.birch.v1", "recipe.spruce.v1" };
            var beds = new[] { FarmCommands.WestBedId }.Concat(FarmCommands.OrchardBeds.Take(4)).ToArray();
            void Commit(FarmCommandResult command)
            {
                Assert.IsTrue(FarmTransaction.TryPrepare(data, command, out var next, out var error), error);
                data = next;
            }
            for (var i = 0; i < ids.Length; i++)
            {
                var recipe = content.Recipe(ids[i]);
                data.Farm.Beds[beds[i]] = new FarmBedState { Unlocked = true };
                data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = 500 };
                data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 2, Earned = true };
                var op = FarmJournal.NextOperation(data.Farm, FarmCommands.PlantFingerprint(beds[i], recipe.id));
                Commit(FarmCommands.PlantRecipe(data, beds[i], recipe.id, op, now, content));
            }
            var repeat = FarmJournal.NextOperation(data.Farm, "repeat:" + beds[0] + ":1");
            Commit(FarmCommands.SetRepeat(data.Farm, beds[0], true, repeat, now));
            FarmCommands.TickFields(data.Farm, 1000000);
            var auto = FarmJournal.NextOperation(data.Farm, "harvest-auto");
            Commit(FarmCommands.HarvestFields(data, auto, now, content, true));
            Assert.AreEqual(1, data.Farm.TotalHarvests);
            Assert.IsTrue(data.Farm.Beds[beds[0]].IsPlanted);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(data, auto, now, content, true).Status);
            var manual = FarmJournal.NextOperation(data.Farm, "harvest-fields");
            Commit(FarmCommands.HarvestFields(data, manual, now, content, false));
            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(data), DataFormat.Binary);
            Assert.AreEqual(5, loaded.Farm.TotalHarvests);
            Assert.AreEqual(loaded.Resources["Radish"].Amount + loaded.Resources["Apple"].Amount, loaded.Farm.CropsGathered, 1e-9);
            Assert.AreEqual(loaded.Resources["Oak Log"].Amount + loaded.Resources["Birch Log"].Amount + loaded.Resources["Spruce Log"].Amount, loaded.Farm.LogsGathered, 1e-9);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(loaded, manual, now, content, false).Status);
            Assert.AreEqual(5, loaded.Farm.TotalHarvests);
        }

        [Test]
        public void MissingLegacyHighlightDoesNotInventABronzeCraft()
        {
            var old = new GameData { SchemaVersion = 7, LastGameVersion = "9999.0.0" };
            old.Forge.HighestCraftedRarityTier = 0; // Missing fields in older wire formats default to zero.
            var result = SaveMigrationRunner.TryMigrate(old, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(-1, result.Data.Forge.HighestCraftedRarityTier);
        }
        [Test]
        public void OldHistoryProjectsOnceAndSurvivesCurrentWireWithoutChangingProgression()
        {
            var rarity = AssetCache.GetAll<RaritySO>("").Single(r => r && r.tierIndex == 3);
            var old = new GameData { SchemaVersion = 7, LastGameVersion = "9999.0.0", CraftingMasteryLevel = 5919, CraftingMasteryXP = 42.125f };
            old.Forge.TotalCrafts = 56466777; old.Forge.TotalSalvaged = 56466700;
            old.Forge.CraftsByRarity[rarity.name] = 12;
            old.Forge.HighestRollByStat["Damage"] = 123;
            old.Forge.StatRolls["Damage"] = new GameData.ForgeStats.StatAgg { count = 500, max = 125 };
            old.Forge.IvanXpGainedTotal = 9999999;
            old.CauldronTotals.TotalTastings = 62581227; old.CauldronTotals.TotalCards = 123456789;
            old.CauldronTotals.GainedNothing = 99;
            old.Resources["Oak Log"] = new GameData.ResourceEntry { Amount = 12345.25 };
            var result = SaveMigrationRunner.TryMigrate(old, "9999.0.0");
            Assert.IsTrue(result.Succeeded, result.Error);
            var current = result.Data;
            Assert.AreEqual(3, current.Forge.HighestCraftedRarityTier);
            Assert.AreEqual(125, current.Forge.HighestRollByStat["Damage"]);
            Assert.IsTrue(current.Farm.StatisticsSinceUpdate);
            Assert.AreEqual(0, current.Farm.TotalHarvests);
            var bytes = CurrentSaveCodec.Serialize(current);
            var loaded = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.Binary);
            Assert.AreEqual(56466777, loaded.Forge.TotalCrafts);
            Assert.AreEqual(56466700, loaded.Forge.TotalSalvaged);
            Assert.AreEqual(3, loaded.Forge.HighestCraftedRarityTier);
            Assert.AreEqual(125, loaded.Forge.HighestRollByStat["Damage"]);
            Assert.AreEqual(62581227, loaded.CauldronTotals.TotalTastings);
            Assert.AreEqual(123456789, loaded.CauldronTotals.TotalCards);
            Assert.AreEqual(5919, loaded.CraftingMasteryLevel);
            Assert.AreEqual(42.125f, loaded.CraftingMasteryXP);
            Assert.AreEqual(12345.25, loaded.Resources["Oak Log"].Amount);
            Assert.AreEqual(0, loaded.Forge.CraftsByRarity?.Count ?? 0);
            Assert.AreEqual(0, loaded.Forge.IvanXpGainedTotal);
            Assert.AreEqual(0, loaded.CauldronTotals.GainedNothing);
            var again = SaveMigrationRunner.TryMigrate(loaded, "9999.0.0");
            Assert.IsTrue(again.Succeeded, again.Error);
            Assert.AreEqual(3, again.Data.Forge.HighestCraftedRarityTier);
            Assert.AreEqual(12, old.Forge.CraftsByRarity[rarity.name], "Migration must not mutate the input.");
        }

        [Test]
        public void ImmediateHighlightsSurviveItemReuseAndFastCraftMode()
        {
            var stat = AssetCache.GetAll<StatDefSO>("").First(s => s && s.heroMapping == HeroStatMapping.Damage);
            var rarity = AssetCache.GetAll<RaritySO>("").First(r => r && r.tierIndex == 2);
            var forge = new GameData.ForgeStats();
            var previous = StaticReferences.FastCraftMode;
            try
            {
                StaticReferences.FastCraftMode = true;
                var item = new GearItem { rarity = rarity, affixes = new List<GearAffix> { new GearAffix { stat = stat, value = 123.5f } } };
                ForgeAnalyticsService.RecordCraft(forge, item);
                item.Reset();
                item.affixes.Add(new GearAffix { stat = stat, value = 1 });
                ForgeAnalyticsService.RecordCraft(forge, item);
                Assert.AreEqual(2, forge.TotalCrafts);
                Assert.AreEqual(2, forge.HighestCraftedRarityTier);
                Assert.AreEqual(123.5f, forge.HighestRollByStat[stat.id]);
                Assert.IsEmpty(forge.CraftsByRarity);
                Assert.IsEmpty(forge.StatRolls);
            }
            finally { StaticReferences.FastCraftMode = previous; }
        }
    }
}
#endif
