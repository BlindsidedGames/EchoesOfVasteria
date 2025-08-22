using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class SaveSystemStressPlayModeTests
    {
        private string testRoot;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            testRoot = Path.Combine(Application.temporaryCachePath, "PMStress_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            TrySetRootPathOverride(testRoot);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            TrySetRootPathOverride(null);
            try { if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true); } catch { }
            yield return null;
        }

        private static Type SaveManagerType => FindType("Blindsided.SaveData.SaveManager");
        private static Type GameDataType => FindType("Blindsided.SaveData.GameData");

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
                try
                {
                    var tt = asm.GetTypes();
                    var match = tt.FirstOrDefault(x => x != null && x.FullName == fullName);
                    if (match != null) return match;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var match = ex.Types?.FirstOrDefault(x => x != null && x.FullName == fullName);
                    if (match != null) return match;
                }
                catch { }
            }
            return null;
        }

        private static object GetSaveManagerInstance()
        {
            var t = SaveManagerType;
            Assert.IsNotNull(t, "SaveManager type not found.");
            var prop = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(prop, "SaveManager.Instance missing.");
            return prop.GetValue(null);
        }

        private static void TrySetRootPathOverride(string path)
        {
            var t = SaveManagerType;
            if (t == null) return;
            var m = t.GetMethod("SetRootPathForTests", BindingFlags.Public | BindingFlags.Static);
            if (m != null)
            {
                m.Invoke(null, new object[] { path });
            }
        }

        private static object NewGameData(float completion)
        {
            var t = GameDataType;
            Assert.IsNotNull(t, "GameData type not found.");
            var o = Activator.CreateInstance(t);
            var fComp = t.GetField("CompletionPercentage");
            if (fComp != null) fComp.SetValue(o, completion);
            var fVer = t.GetField("SchemaVersion");
            if (fVer != null) fVer.SetValue(o, 1);
            var fDate = t.GetField("DateStarted");
            if (fDate != null) fDate.SetValue(o, DateTime.UtcNow.ToString("o"));
            return o;
        }

        private static void SetCurrentSlot(object mgr, string slot)
        {
            var m = SaveManagerType.GetMethod("SetCurrentSlot", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(m);
            m.Invoke(mgr, new object[] { slot });
        }

        private static bool Save(object mgr, object gameData)
        {
            var m = SaveManagerType.GetMethod("SaveAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(m);
            var task = m.Invoke(mgr, new object[] { gameData, System.Threading.CancellationToken.None });
            var prop = task.GetType().GetProperty("Result");
            return (bool)prop.GetValue(task);
        }

        private static (bool ok, object data) Load(object mgr)
        {
            var m = SaveManagerType.GetMethod("LoadAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(m);
            var task = m.Invoke(mgr, new object[] { System.Threading.CancellationToken.None });
            var prop = task.GetType().GetProperty("Result");
            var tuple = prop.GetValue(task);
            var t = tuple.GetType();
            var fOk = t.GetField("ok") ?? t.GetField("Item1");
            var fData = t.GetField("data") ?? t.GetField("Item2");
            return ((bool)fOk.GetValue(tuple), fData.GetValue(tuple));
        }

        private static float GetCompletion(object gameData)
        {
            var f = GameDataType.GetField("CompletionPercentage");
            return f != null ? (float)f.GetValue(gameData) : 0f;
        }

        [UnityTest]
        public IEnumerator RapidSavesAcrossFrames_AreValid()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(Save(mgr, NewGameData(i)));
                yield return null;
            }

            var r = Load(mgr);
            Assert.IsTrue(r.ok);
            Assert.IsNotNull(r.data);
        }

        [UnityTest]
        public IEnumerator SlotSwitching_PreservesLatestPerSlot()
        {
            var mgr = GetSaveManagerInstance();

            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(11f)));
            yield return null;

            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(22f)));
            yield return null;

            // Validate
            SetCurrentSlot(mgr, "Save1");
            var r1 = Load(mgr);
            Assert.IsTrue(r1.ok);
            Assert.AreEqual(11f, GetCompletion(r1.data), 0.0001f);

            SetCurrentSlot(mgr, "Save2");
            var r2 = Load(mgr);
            Assert.IsTrue(r2.ok);
            Assert.AreEqual(22f, GetCompletion(r2.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator CorruptPrimary_FallsBackToPrev1()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");

            Assert.IsTrue(Save(mgr, NewGameData(100f)));
            Assert.IsTrue(Save(mgr, NewGameData(200f)));

            // Corrupt primary to force fallback
            var slotDir = Path.Combine(testRoot ?? Application.persistentDataPath, "Saves", "Save3");
            Directory.CreateDirectory(slotDir);
            var primary = Path.Combine(slotDir, "snapshot.bin");
            File.AppendAllText(primary, "CORRUPT");
            yield return null;

            var r = Load(mgr);
            Assert.IsTrue(r.ok);
            // Should be previous value (100)
            Assert.AreEqual(100f, GetCompletion(r.data), 0.0001f);
        }
    }
}


