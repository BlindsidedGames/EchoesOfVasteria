#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Blindsided;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tests.RealTime
{
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
            var managerGo = new GameObject("AlterEchoGenerationManager_TestHarness");
            var generatorGo = new GameObject("AlterEchoGenerator_TestHarness");
            var resource = ScriptableObject.CreateInstance<Resource>();

            try
            {
                var manager = managerGo.AddComponent<AlterEchoGenerationManager>();
                var generator = generatorGo.AddComponent<AlterEchoGenerator>();

                generator.UpdateRate(60.0); // One cycle per real-time second.

                var resourceField = typeof(AlterEchoGenerator).GetField("resource", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(resourceField, "resource field missing.");
                resourceField.SetValue(generator, resource);

                var setupField = typeof(AlterEchoGenerator).GetField("setup", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(setupField, "setup field missing.");
                setupField.SetValue(generator, true);

                var generatorsField = typeof(AlterEchoGenerationManager).GetField("generators", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(generatorsField, "generators list missing.");
                var generators = generatorsField.GetValue(manager) as List<AlterEchoGenerator>;
                Assert.IsNotNull(generators, "generators list not initialised.");
                generators.Add(generator);

                var tickMethod = typeof(AlterEchoGenerationManager).GetMethod("TickGenerators", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(tickMethod, "TickGenerators method missing.");

                Time.timeScale = 8f;
                tickMethod.Invoke(manager, new object[] { 0.5f });
                tickMethod.Invoke(manager, new object[] { 0.5f });
                Assert.AreEqual(1f, generator.Progress, 1e-4f, "Progress should follow supplied unscaled seconds.");

                var storedField = typeof(AlterEchoGenerator).GetField("stored", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(storedField, "stored field missing.");

                tickMethod.Invoke(manager, new object[] { 1f });

                Assert.AreEqual(0f, generator.Progress, 1e-4f, "Progress should wrap after accumulating a full interval.");
                Assert.AreEqual(generator.CycleAmount, (double)storedField.GetValue(generator), 1e-4f, "Stored amount should match cycle yield.");
            }
            finally
            {
                Time.timeScale = originalScale;
                ScriptableObject.DestroyImmediate(resource);
                Object.DestroyImmediate(generatorGo);
                Object.DestroyImmediate(managerGo);
            }
        }
    }
}
#endif
