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
    public sealed partial class FieldsProductionPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string root, previousRoot;
        private GameObject oracleObject, serviceObject;
        private Oracle previousOracle, oracle;
        private FarmService service;
        private FarmContent content;
        private static readonly DateTime Now = new(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);

#if UNITY_EDITOR
        private uint previousRenderWidth, previousRenderHeight;

        [UnitySetUp]
        public IEnumerator UseDesktopRenderingResolution()
        {
            // Batch-mode GameView defaults to 640x480 and ignores the player
            // -screen-width flags. These desktop layout tests need a known viewport.
            UnityEditor.PlayModeWindow.GetRenderingResolution(out previousRenderWidth, out previousRenderHeight);
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Desktop UI tests");
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreRenderingResolution()
        {
            if (previousRenderWidth > 0 && previousRenderHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(previousRenderWidth, previousRenderHeight, "Restored test viewport");
            yield return null;
        }
#endif

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
            // Clear the runner's backbuffer so UI screenshots cannot retain a
            // previous fixture's panel outside the current window.
            var cameraObject = new GameObject("Isolated UI backdrop", typeof(Camera));
            cameraObject.transform.SetParent(oracleObject.transform, false);
            var backdrop = cameraObject.GetComponent<Camera>();
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = Color.black;
            backdrop.cullingMask = 0;
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
            if (oracle)
                typeof(Oracle).GetMethod("CompleteActiveSaveBlocking", Private).Invoke(oracle, null);
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
        public void ResourceCapturePreservesUnknownRecordsAndCurrentBalances()
        {
            const string unknown = "Unavailable future resource";
            oracle.saveData.Resources[unknown] = new GameData.ResourceEntry { Amount = 88, Tier = 7, Earned = true, BestPerMinute = 42 };
            oracle.saveData.ResourceStats[unknown] = new GameData.ResourceRecord { TotalReceived = 90, TotalSpent = 2 };
            var inventoryObject = new GameObject("Protected resource capture");
            try
            {
                var inventory = inventoryObject.AddComponent<TimelessEchoes.Upgrades.ResourceManager>();
                var radish = Resources.LoadAll<TimelessEchoes.Upgrades.Resource>("").Single(r => r.name == "Radish");
                inventory.Add(radish, 12, eligibleForTierRoll: false);
                Blindsided.EventHandler.SaveData(force: true);
                Blindsided.EventHandler.SaveData(force: true);
                Assert.True(inventory.Spend(radish, 4));
                Assert.True((bool)typeof(Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                var disk = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(disk.Succeeded, disk.Diagnostic);
                Assert.AreEqual(8, disk.Data.Resources[radish.name].Amount);
                Assert.AreEqual(12, disk.Data.ResourceStats[radish.name].TotalReceived);
                Assert.AreEqual(4, disk.Data.ResourceStats[radish.name].TotalSpent);
                Assert.AreEqual(88, disk.Data.Resources[unknown].Amount);
                Assert.AreEqual(7, disk.Data.Resources[unknown].Tier);
                Assert.AreEqual(42, disk.Data.Resources[unknown].BestPerMinute);
                Assert.AreEqual(90, disk.Data.ResourceStats[unknown].TotalReceived);
                Assert.AreEqual(2, disk.Data.ResourceStats[unknown].TotalSpent);
            }
            finally { UnityEngine.Object.DestroyImmediate(inventoryObject); }
        }

        [Test]
        public void ManualFarmActionWaitsForCheckpoint()
        {
            PreparePaidCrop();
            var operation = FarmJournal.NextOperation(service.State, "water:" + FarmCommands.WestBedId);
            Assert.True(oracle.TryCommitFarmCommand(s => FarmCommands.Water(s, FarmCommands.WestBedId, operation, Now), out var error), error);
            Assert.True(service.State.Beds[FarmCommands.WestBedId].Watered);
            var disk = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(disk.Succeeded, disk.Diagnostic);
            Assert.False(disk.Data.Farm.Beds[FarmCommands.WestBedId].Watered, "Manual farm actions must wait for a checkpoint.");
            Assert.True((bool)typeof(Oracle).GetMethod("SaveNowBlocking", Private)
                .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
            disk = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(disk.Succeeded, disk.Diagnostic);
            Assert.True(disk.Data.Farm.Beds[FarmCommands.WestBedId].Watered);
            Assert.True(oracle.TryCommitFarmCommand(s => FarmCommands.Water(s, FarmCommands.WestBedId, operation, Now), out error), error);
            Assert.True(service.State.Beds[FarmCommands.WestBedId].Watered);
        }

        [Test]
        public void RealServiceResumeAndOwnerReloadNeverSettleUtcGrowth()
        {
            service.State.FirstHarvestCompleted = true;
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

        [UnityTest]
        public IEnumerator DurableHarvestRepeatsAndRunningServiceStopsAtOnePackExhaustion()
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
            Assert.AreEqual(0, oracle.saveData.Resources["Radish Seed Pack"].Amount);
            Assert.AreEqual(before + 10, oracle.saveData.Resources["Radish"].Amount, 1e-9);
            Assert.False(Commit(s => FarmCommands.HarvestFields(oracle.saveData,
                FarmJournal.NextOperation(s, "harvest-fields"), Now.AddDays(10), content, false)));
            Assert.AreEqual(before + 10, oracle.saveData.Resources["Radish"].Amount, 1e-9);
            FarmCommands.TickFields(service.State, content.baseDurationSeconds);
            service.enabled = true;
            // Let ordinary Update rebind the committed bank and auto-harvest it.
            yield return null; yield return null; yield return null;
            service.enabled = false;
            bed = service.State.Beds[FarmCommands.WestBedId];
            Assert.False(bed.IsPlanted); Assert.False(bed.Repeat);
            Assert.AreEqual(0, oracle.saveData.Resources["Radish Seed Pack"].Amount);
            Assert.AreEqual(before + 21.9, oracle.saveData.Resources["Radish"].Amount, 1e-9, "The repeated batch froze the existing 19% level-20 yield bonus.");
            var loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(loaded.Succeeded, loaded.Diagnostic);
            Assert.True(loaded.Data.Farm.Beds[FarmCommands.WestBedId].IsPlanted,
                "Automatic harvest must not write a per-harvest snapshot.");
            Assert.True((bool)typeof(Oracle).GetMethod("SaveNowBlocking", Private)
                .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
            loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(loaded.Succeeded, loaded.Diagnostic);
            Assert.False(loaded.Data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.False(loaded.Data.Farm.Beds[FarmCommands.WestBedId].Repeat);
        }

        [Test]
        public void FreshFieldsHideLegacyArtAndShowOnlyNormallyConstructedBeds()
        {
            var town = new GameObject("Fields isolated town");
            try
            {
                var farmers = new GameObject("Farmers"); farmers.transform.SetParent(town.transform);
                var tasks = new GameObject("FarmingTasks"); tasks.transform.SetParent(farmers.transform);
                var oldCrop = tasks.AddComponent<SpriteRenderer>();
                var oldFenceObject = new GameObject("Fence"); oldFenceObject.transform.SetParent(farmers.transform);
                var oldFence = oldFenceObject.AddComponent<SpriteRenderer>();
                var crops = new GameObject("Crops"); crops.transform.SetParent(farmers.transform);
                var oldDecoration = crops.AddComponent<SpriteRenderer>();
                var collider = tasks.AddComponent<BoxCollider2D>();
                var unrelated = new GameObject("Unrelated town decoration"); unrelated.transform.SetParent(town.transform);
                var retained = unrelated.AddComponent<SpriteRenderer>();
                var view = serviceObject.AddComponent<FieldsWorldView>(); view.enabled = false;
                typeof(FieldsWorldView).GetField("town", Private).SetValue(view, town.transform);
                typeof(FieldsWorldView).GetField("service", Private).SetValue(view, service);
                typeof(FieldsWorldView).GetField("appearance", Private).SetValue(view,
                    Resources.Load<FieldsWorldAppearance>("Farming/FieldsWorldAppearance"));
                typeof(FieldsWorldView).GetMethod("Start", Private).Invoke(view, null);
                var geometry = town.transform.Find("Fields world");
                Assert.NotNull(geometry);
                Assert.False(oldCrop.enabled, "Fresh Fields must not display legacy planted crops.");
                Assert.False(oldFence.enabled); Assert.False(oldDecoration.enabled);
                Assert.True(tasks.activeSelf); Assert.True(collider.enabled);
                Assert.True(retained.enabled, "Unrelated town art must remain intact.");
                Assert.False(geometry.Find(FarmCommands.WestBedId).gameObject.activeSelf);
                Assert.False(geometry.Find(FarmCommands.EastBedId).gameObject.activeSelf);
                Assert.False(geometry.Find("Fields enclosure 1").gameObject.activeSelf);
                oracle.saveData.CompletedNpcTasks.Add("Farmers1"); Build(FarmContent.IntroductionId);
                oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
                oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
                Build("Farm.Garden.Build01.v1"); view.Refresh();
                Assert.True(geometry.Find(FarmCommands.WestBedId).gameObject.activeSelf);
                Assert.True(geometry.Find("Fields enclosure 1").gameObject.activeSelf);
                Assert.False(geometry.Find(FarmCommands.EastBedId).gameObject.activeSelf);
                Assert.False(geometry.Find(FarmCommands.OrchardBeds[0]).gameObject.activeSelf);
                Assert.False(oldCrop.enabled); Assert.True(retained.enabled);
                oldCrop.enabled = true; oldFence.enabled = true; oldDecoration.enabled = true;
                view.Refresh();
                Assert.False(oldCrop.enabled); Assert.False(oldFence.enabled); Assert.False(oldDecoration.enabled);
                Assert.True(collider.enabled, "Cached art suppression must not disable task colliders.");
            }
            finally { UnityEngine.Object.DestroyImmediate(town); }
        }

        private void PreparePaidCrop()
        {
            oracle.saveData.CompletedNpcTasks.Add("Farmers1");
            Build(FarmContent.IntroductionId);
            oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
            oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
            Build("Farm.Garden.Build01.v1");
            var recipe = content.recipes.Single(r => r.seedId == FarmCommands.RadishSeedId);
            oracle.saveData.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 2, Earned = true, Tier = 1 };
            Assert.True(Commit(s => FarmCommands.PlantRecipe(oracle.saveData, FarmCommands.WestBedId, recipe.id,
                FarmJournal.NextOperation(s, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, recipe.id)), Now, content)));
        }
        private void Build(string id) => Assert.True(Commit(s => FarmCommands.BuildFields(oracle.saveData, id,
            FarmJournal.NextOperation(s, "build:" + id), Now, content)), id);
        private bool Commit(Func<FarmState, FarmCommandResult> command)
        {
            if (!oracle.TryCommitFarmCommand(command, out _)) return false;
            return (bool)typeof(Oracle).GetMethod("SaveNowBlocking", Private)
                .Invoke(oracle, new object[] { oracle.CurrentSlot, false });
        }
        private void SetOracle(string field, object value) => typeof(Oracle).GetField(field, Private).SetValue(oracle, value);
        private void InvokeService(string method, params object[] args) => typeof(FarmService).GetMethod(method, Private).Invoke(service, args);
    }
}
#endif
