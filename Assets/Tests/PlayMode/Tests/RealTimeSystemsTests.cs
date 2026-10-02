#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tests.RealTime
{
    [UnityEngine.TestTools.PrebuildSetup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    [UnityEngine.TestTools.PostBuildCleanup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    public class RealTimeSystemsTests
    {
        [Test]
        public void PlaytimeAccumulatesWithProvidedUnscaledSeconds()
        {
            var originalScale = Time.timeScale;
            var go = new GameObject("Oracle_TestHarness");
            try
            {
                var oracle = go.AddComponent<Oracle>();
                Assert.IsNotNull(oracle.saveData, "Expected Oracle to create save data.");

                var accumulateMethod = typeof(Oracle).GetMethod(
                    "AccumulatePlayTime",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(accumulateMethod, "AccumulatePlayTime method missing.");

                Time.timeScale = 5f;
                var startingPlaytime = oracle.saveData.PlayTime;

                accumulateMethod.Invoke(oracle, new object[] { 10f });

                Assert.AreEqual(startingPlaytime + 10f, oracle.saveData.PlayTime, 1e-5, "Playtime should advance based on provided unscaled seconds.");
            }
            finally
            {
                Time.timeScale = originalScale;
                Oracle.oracle = null;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AlterEchoGeneratorsAdvanceWithUnscaledDelta()
        {
            var originalScale = Time.timeScale;
            var oracleGo = new GameObject("Oracle_AlterEchoTestHarness");
            var managerGo = new GameObject("AlterEchoGenerationManager_TestHarness");
            var generatorGo = new GameObject("AlterEchoGenerator_TestHarness");
            var resource = ScriptableObject.CreateInstance<Resource>();
            resource.name = "AlterEchoTestResource";

            try
            {
                var oracle = oracleGo.AddComponent<Oracle>();
                var manager = managerGo.AddComponent<AlterEchoGenerationManager>();
                var generator = generatorGo.AddComponent<AlterEchoGenerator>();
                Assert.IsNotNull(oracle.saveData);
                generator.Configure(resource, 60.0); // One cycle per real-time second.

                var generatorsField = typeof(AlterEchoGenerationManager).GetField("generators", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(generatorsField, "generators list missing.");
                var generators = generatorsField.GetValue(manager) as List<AlterEchoGenerator>;
                Assert.IsNotNull(generators, "generators list not initialised.");
                generators.Add(generator);

                var tickMethod = typeof(AlterEchoGenerationManager).GetMethod("TickGenerators", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(tickMethod, "TickGenerators method missing.");

                Time.timeScale = 8f;
                tickMethod.Invoke(manager, new object[] { 0.5f });
                Assert.AreEqual(0.5f, generator.Progress, 1e-4f, "Progress should follow supplied unscaled seconds.");

                var storedField = typeof(AlterEchoGenerator).GetField("stored", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(storedField, "stored field missing.");

                tickMethod.Invoke(manager, new object[] { 0.5f });

                Assert.AreEqual(0f, generator.Progress, 1e-4f, "Progress should wrap after accumulating a full interval.");
                Assert.AreEqual(generator.CycleAmount, (double)storedField.GetValue(generator), 1e-4f, "Stored amount should match cycle yield.");

                generator.ApplyOfflineProgress(1_800_000_000d);
                Assert.AreEqual(0f, generator.Progress, 1e-4f);
                Assert.AreEqual(1_800_000_001d, (double)storedField.GetValue(generator), 0.1d,
                    "Large offline spans should be advanced in constant time without losing cycles.");
            }
            finally
            {
                Time.timeScale = originalScale;
                Oracle.oracle = null;
                ScriptableObject.DestroyImmediate(resource);
                Object.DestroyImmediate(generatorGo);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(oracleGo);
            }
        }

        [Test]
        public void EquipmentRoundTripPreservesTemporarilyUnresolvedContent()
        {
            var oracleGo = new GameObject("Oracle_EquipmentTestHarness");
            var equipmentGo = new GameObject("EquipmentController_TestHarness");
            try
            {
                Oracle.oracle = null;
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
                var oracle = oracleGo.AddComponent<Oracle>();
                var controller = equipmentGo.AddComponent<EquipmentController>();
                oracle.saveData.EquipmentBySlot["Helmet"] = new GearItemRecord
                {
                    slot = "Helmet",
                    rarity = "TemporarilyMissingRarity",
                    affixes = new List<GearAffixRecord>
                    {
                        new()
                        {
                            statId = "temporarily-missing-stat",
                            value = 42f,
                            quality = 0.75d
                        }
                    }
                };

                var load = typeof(EquipmentController).GetMethod(
                    "LoadState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var save = typeof(EquipmentController).GetMethod(
                    "SaveState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(load);
                Assert.IsNotNull(save);
                load.Invoke(controller, null);
                save.Invoke(controller, null);

                var preserved = oracle.saveData.EquipmentBySlot["Helmet"];
                Assert.AreEqual("TemporarilyMissingRarity", preserved.rarity);
                Assert.AreEqual(1, preserved.affixes.Count);
                Assert.AreEqual("temporarily-missing-stat", preserved.affixes[0].statId);
                Assert.AreEqual(42f, preserved.affixes[0].value, 0.0001f);
                Assert.AreEqual(0.75d, preserved.affixes[0].quality, 0.000001d);
            }
            finally
            {
                Oracle.oracle = null;
                Object.DestroyImmediate(equipmentGo);
                Object.DestroyImmediate(oracleGo);
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
            }
        }

        [Test]
        public void EquipmentRoundTripCanonicalizesLegacyStatNameWithoutDuplication()
        {
            var oracleGo = new GameObject("Oracle_EquipmentAliasTestHarness");
            var equipmentGo = new GameObject("EquipmentController_AliasTestHarness");
            try
            {
                Oracle.oracle = null;
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);

                var stat = AssetCache.GetAll<StatDefSO>(string.Empty)
                    .FirstOrDefault(candidate => candidate != null &&
                                                 !string.IsNullOrWhiteSpace(candidate.id) &&
                                                 candidate.name != candidate.id);
                var rarity = AssetCache.GetAll<RaritySO>(string.Empty)
                    .FirstOrDefault(candidate => candidate != null);
                Assert.IsNotNull(stat, "Expected a stat asset with distinct legacy name and stable ID.");
                Assert.IsNotNull(rarity, "Expected at least one gear rarity asset.");

                var oracle = oracleGo.AddComponent<Oracle>();
                var controller = equipmentGo.AddComponent<EquipmentController>();
                oracle.saveData.EquipmentBySlot["Helmet"] = new GearItemRecord
                {
                    slot = "Helmet",
                    rarity = rarity.name,
                    affixes = new List<GearAffixRecord>
                    {
                        new()
                        {
                            statId = stat.name,
                            quality = 0.5d
                        }
                    }
                };

                var load = typeof(EquipmentController).GetMethod(
                    "LoadState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var save = typeof(EquipmentController).GetMethod(
                    "SaveState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(load);
                Assert.IsNotNull(save);

                load.Invoke(controller, null);
                save.Invoke(controller, null);

                var canonical = oracle.saveData.EquipmentBySlot["Helmet"];
                Assert.AreEqual(1, canonical.affixes.Count,
                    "A resolved legacy alias must not be retained beside its canonical stat ID.");
                Assert.AreEqual(stat.id, canonical.affixes[0].statId);

                load.Invoke(controller, null);
                var reloaded = controller.GetEquipped("Helmet");
                Assert.IsNotNull(reloaded);
                Assert.AreEqual(1, reloaded.affixes.Count(affix => affix?.stat == stat),
                    "The canonicalized round trip must apply the stat exactly once.");
            }
            finally
            {
                Oracle.oracle = null;
                Object.DestroyImmediate(equipmentGo);
                Object.DestroyImmediate(oracleGo);
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
            }
        }
    }
}
#endif
