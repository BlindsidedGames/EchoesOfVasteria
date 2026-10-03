#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    // Real component/save integration in the runner's isolated empty scene. This does
    // not load Main's Steam/cloud bootstrap and is not a normal-scene visual proof.
    [PrebuildSetup(typeof(IsolatedPlayModeScene))]
    [PostBuildCleanup(typeof(IsolatedPlayModeScene))]
    public sealed class FieldsProductionPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string root, previousRoot;
        private GameObject oracleObject, serviceObject;
        private Oracle previousOracle, oracle;
        private FarmService service;
        private FarmContent content;
        private static readonly DateTime Now = new(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            var rootField = typeof(SaveManager).GetField("rootPathOverride", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(rootField, "Cannot preserve save isolation override.");
            previousRoot = (string)rootField.GetValue(null);
            root = Path.Combine(Application.temporaryCachePath, "FieldsPlay_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            SaveManager.SetRootPathForTests(root);
            previousOracle = Oracle.oracle;
            Assert.IsNull(previousOracle, "This fixture requires the runner's isolated empty scene.");
            oracleObject = new GameObject("Fields protected transaction owner");
            oracle = oracleObject.AddComponent<Oracle>();
            // Disable before a frame: Start must never enter the production bootstrap.
            oracle.enabled = false;
            SetOracle("loaded", true);
            SetOracle("_saveDataSlot", oracle.CurrentSlot);
            content = FarmContent.Load();
            Assert.NotNull(content, "Authored Fields content missing.");
            oracle.saveData.General.MaxRunDistance = 100;
            serviceObject = new GameObject("Fields real lifecycle owner");
            service = serviceObject.AddComponent<FarmService>();
            service.enabled = false; // Explicit frame/time seams; no wall-clock test flakiness.
            InvokeService("SettleResume");
        }

        [TearDown]
        public void TearDown()
        {
            if (oracle) SetOracle("loaded", false); // Teardown must not perform a lifecycle save.
            if (serviceObject) UnityEngine.Object.DestroyImmediate(serviceObject);
            if (oracleObject) UnityEngine.Object.DestroyImmediate(oracleObject);
            Oracle.oracle = previousOracle;
            SaveManager.SetRootPathForTests(previousRoot);
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void RealOwnerCommitsIntroductionAndExactlyOneConstructionPaymentThenReloads()
        {
            Assert.False(Commit(s => FarmCommands.BuildFields(oracle.saveData, FarmContent.IntroductionId,
                FarmJournal.NextOperation(s, "build:" + FarmContent.IntroductionId), Now, content)));
            oracle.saveData.CompletedNpcTasks.Add("Farmers1");
            Build(FarmContent.IntroductionId);
            Assert.AreEqual(1, service.State.TwinsLevel);
            Assert.AreEqual(0, service.State.TwinsXp);
            Assert.AreEqual(0, service.State.GardenCapacity);
            Assert.IsEmpty(service.State.Seeds);
            oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
            oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
            Build("Farm.Garden.Build01.v1");
            Assert.AreEqual(0, oracle.saveData.Resources["Log"].Amount);
            Assert.AreEqual(0, oracle.saveData.Resources["Stick"].Amount);
            Assert.AreEqual(1, service.State.GardenCapacity);
            Assert.False(Commit(s => FarmCommands.BuildFields(oracle.saveData, "Farm.Garden.Build01.v1",
                FarmJournal.NextOperation(s, "build:Farm.Garden.Build01.v1"), Now, content)));
            var loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(loaded.Succeeded, loaded.Diagnostic);
            Assert.True(FarmContent.Completed(loaded.Data, "Farm.Garden.Build01.v1"));
            Assert.AreEqual(1, loaded.Data.Farm.GardenCapacity);
            Assert.AreEqual(0, loaded.Data.Resources["Log"].Amount);
        }

        [Test]
        public void RealServiceResumeAndOwnerReloadNeverSettleUtcGrowth()
        {
            PreparePaidCrop();
            FarmCommands.TickFields(service.State, 120);
            var bed = service.State.Beds[FarmCommands.WestBedId];
            bed.LastGrowthUtcTicks = Now.AddYears(-1).Ticks;
            InvokeService("OnApplicationPause", true);
            InvokeService("OnApplicationPause", false);
            Assert.AreEqual(120, bed.ElapsedSeconds);
            InvokeService("Loaded");
            Assert.AreEqual(120, service.State.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.True(Commit(s => FarmCommands.Water(s, FarmCommands.WestBedId,
                FarmJournal.NextOperation(s, "water:" + FarmCommands.WestBedId), Now)));
            Assert.AreEqual(120, service.State.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual(content.baseDurationSeconds / 2, service.State.Beds[FarmCommands.WestBedId].ReadyAfterSeconds);
        }

        [Test]
        public void DurableManualHarvestPaysOnceAndRepeatConsumesOnlyOneNewDryBatch()
        {
            PreparePaidCrop();
            service.State.TwinsLevel = 20; // Controlled progression fixture, not natural earning proof.
            Assert.True(Commit(s => FarmCommands.SetRepeat(s, FarmCommands.WestBedId, true,
                FarmJournal.NextOperation(s, "repeat:" + FarmCommands.WestBedId + ":1"), Now)));
            FarmCommands.TickFields(service.State, content.baseDurationSeconds * 100);
            var before = oracle.saveData.Resources.TryGetValue("Radish", out var balance) ? balance.Amount : 0;
            Assert.True(Commit(s => FarmCommands.HarvestFields(oracle.saveData,
                FarmJournal.NextOperation(s, "harvest-fields"), Now, content, false)));
            var bed = service.State.Beds[FarmCommands.WestBedId];
            Assert.True(bed.IsPlanted);
            Assert.False(bed.Watered);
            Assert.AreEqual(0, bed.ElapsedSeconds);
            Assert.AreEqual(0, service.State.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(before + 10, oracle.saveData.Resources["Radish"].Amount, 1e-9);
            Assert.False(Commit(s => FarmCommands.HarvestFields(oracle.saveData,
                FarmJournal.NextOperation(s, "harvest-fields"), Now.AddDays(10), content, false)));
            Assert.AreEqual(before + 10, oracle.saveData.Resources["Radish"].Amount, 1e-9);
        }

        private void PreparePaidCrop()
        {
            oracle.saveData.CompletedNpcTasks.Add("Farmers1");
            Build(FarmContent.IntroductionId);
            oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
            oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
            Build("Farm.Garden.Build01.v1");
            var recipe = content.recipes.Single(r => r.seedId == FarmCommands.RadishSeedId);
            service.State.Seeds[recipe.seedId] = new FarmSeedState { Quantity = 2, LifetimeAcquired = 2 };
            Assert.True(Commit(s => FarmCommands.PlantRecipe(oracle.saveData, FarmCommands.WestBedId, recipe.id,
                FarmJournal.NextOperation(s, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, recipe.id)), Now, content)));
        }
        private void Build(string id) => Assert.True(Commit(s => FarmCommands.BuildFields(oracle.saveData, id,
            FarmJournal.NextOperation(s, "build:" + id), Now, content)), id);
        private bool Commit(Func<FarmState, FarmCommandResult> command) => oracle.TryCommitFarmCommand(command, out _);
        private void SetOracle(string field, object value) => typeof(Oracle).GetField(field, Private).SetValue(oracle, value);
        private void InvokeService(string method, params object[] args) => typeof(FarmService).GetMethod(method, Private).Invoke(service, args);
    }
}
#endif
