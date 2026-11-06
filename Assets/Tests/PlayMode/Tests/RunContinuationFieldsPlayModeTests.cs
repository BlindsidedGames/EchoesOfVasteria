using System;
using System.Collections;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using UnityEngine;
using UnityEngine.TestTools;
using Events = Blindsided.EventHandler;

namespace Tests.PlayMode
{
    public sealed partial class RunContinuationPlayModeTests
    {
        private FarmService CreateTownFields(int seeds, bool automatic)
        {
            var data = Progress();
            data.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
            data.Farm.GardenCapacity = 1;
            data.Farm.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked = true };
            if (automatic) data.Farm.Projects = FarmProject.MillBody | FarmProject.MillSails | FarmProject.Well;
            data.Resources["Radish Seed Pack"] = new GameData.ResourceEntry { Amount = seeds, Earned = true, Tier = 1 };
            CreateRuntime(data, "Farmlands");
            var fields = owner.AddComponent<FarmService>();
            Events.LoadData();
            Assert.True(fields.Plant(FarmCommands.WestBedId), fields.LastError);
            if (automatic) Assert.True(fields.SetRepeat(FarmCommands.WestBedId, true), fields.LastError);
            return fields;
        }

        [UnityTest]
        public IEnumerator TownBackgroundCapsGrowthAndPersistsTheSharedCheckpoint()
        {
            PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", 1);
            CreateTownFields(1, false);
            var data = oracle.saveData;
            var bed = data.Farm.Beds[FarmCommands.WestBedId];
            bed.DurationSeconds = 7200;
            Events.ApplicationBackground();
            data.BackgroundProgressUtcTicks = DateTime.UtcNow.AddHours(-8).Ticks;
            Events.ApplicationForeground();
            Assert.That(bed.ElapsedSeconds, Is.EqualTo(3600).Within(.01));
            Assert.That(data.Farm.TotalHarvests, Is.Zero);
            Assert.That(data.ActiveRun, Is.Null);
            var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
            Assert.That(saved.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds, Is.EqualTo(3600).Within(.01));
            Assert.That(saved.BackgroundProgressUtcTicks, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10).Ticks));
            Events.ApplicationForeground();
            Assert.That(bed.ElapsedSeconds, Is.EqualTo(3600).Within(.01), "Duplicate foreground callbacks cannot replay the absence.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ColdTownResumeHarvestsAndSavesWhileRepeatWaitsForSeeds()
        {
            PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", 1);
            CreateTownFields(2, true);
            oracle.saveData.BackgroundProgressUtcTicks = DateTime.UtcNow.AddHours(-2).Ticks;
            var departure = CurrentSaveCodec.Clone(oracle.saveData);
            DestroyRuntime();
            CreateRuntime(departure, "Farmlands");
            owner.AddComponent<FarmService>();
            Events.LoadData();
            yield return null;
            yield return null;
            Assert.That(oracle.saveData.Farm.TotalHarvests, Is.EqualTo(2));
            Assert.That(oracle.saveData.Resources["Radish"].Amount, Is.EqualTo(201).Within(.001));
            Assert.That(oracle.saveData.Resources["Radish Seed Pack"].Amount, Is.Zero);
            Assert.True(oracle.saveData.Farm.Beds[FarmCommands.WestBedId].Repeat);
            Assert.False(oracle.saveData.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
            Assert.That(saved.Farm.TotalHarvests, Is.EqualTo(2));
            Assert.That(saved.Resources["Radish"].Amount, Is.EqualTo(201).Within(.001));
        }

        [UnityTest]
        public IEnumerator DisabledBackgroundProgressDoesNotAdvanceTownFields()
        {
            PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", 0);
            CreateTownFields(1, false);
            Events.ApplicationBackground();
            oracle.saveData.BackgroundProgressUtcTicks = DateTime.UtcNow.AddHours(-8).Ticks;
            Events.ApplicationForeground();
            Assert.That(oracle.saveData.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds, Is.Zero);
            Assert.That(oracle.saveData.Farm.TotalHarvests, Is.Zero);
            yield return null;
        }
    }
}
