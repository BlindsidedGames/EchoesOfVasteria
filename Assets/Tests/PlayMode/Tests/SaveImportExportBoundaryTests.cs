#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    [PrebuildSetup(typeof(IsolatedPlayModeScene))]
    [PostBuildCleanup(typeof(IsolatedPlayModeScene))]
    public sealed class SaveImportExportBoundaryTests
    {
        [UnityTest]
        public IEnumerator CurrentExportImportsExactProgressAndReceiptsIntoDurableSlot()
        {
            Assert.IsNull(Oracle.oracle, "Run only in the disposable empty scene; never replace a live Oracle.");
            var rootField = typeof(SaveManager).GetField("rootPathOverride", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(rootField);
            var previousRoot = (string)rootField.GetValue(null);
            var root = Path.Combine(Path.GetTempPath(), "EchoesExport_" + Guid.NewGuid().ToString("N"));
            var setter = typeof(SaveManager).GetMethod("SetRootPathForTests", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(setter, "Refusing import without save isolation.");
            GameObject host = null;
            try
            {
                Directory.CreateDirectory(root);
                setter.Invoke(null, new object[] { root });
                host = new GameObject("Disposable export oracle");
                var oracle = host.AddComponent<Oracle>();
                oracle.enabled = false; // Never run Start's Loading-scene transition.
                var source = new GameData();
                source.CompletionPercentage = 38.125f;
                source.PlayTime = 987654.25;
                source.Resources["Log"] = new GameData.ResourceEntry { Amount = 1e100, Earned = true, Tier = 3 };
                source.SkillData["Farming"] = new GameData.SkillProgress { Level = 72, CurrentXP = 8192.125f };
                source.RetroQuestRewardsApplied.Add("reward-paid-once");
                source.Farm.PendingCredits["pending-recovery"] = new TimelessEchoes.Farming.FarmPendingCredit
                    { Rolled = true, CompletedAtUtcTicks = 638000000000000000L };
                oracle.saveData = source;
                Set(oracle, "_saveDataSlot", oracle.CurrentSlot);
                Set(oracle, "loaded", true);
                var exported = SaveImportExport.ExportCurrentSlot(false, out var saved);
                Assert.IsTrue(saved);
                StringAssert.StartsWith("TE2:", exported);
                oracle.saveData = new GameData();
                Assert.IsTrue(SaveImportExport.TryImportToCurrentSlot(exported, out var error, out var committed), error);
                Assert.IsTrue(committed);
                Assert.AreEqual(38.125f, oracle.saveData.CompletionPercentage);
                Assert.AreEqual(1e100, oracle.saveData.Resources["Log"].Amount);
                Assert.AreEqual(72, oracle.saveData.SkillData["Farming"].Level);
                Assert.AreEqual(8192.125f, oracle.saveData.SkillData["Farming"].CurrentXP);
                Assert.IsTrue(oracle.saveData.RetroQuestRewardsApplied.Contains("reward-paid-once"));
                Assert.IsTrue(oracle.saveData.Farm.PendingCredits["pending-recovery"].Rolled);
                Assert.AreEqual(638000000000000000L,
                    oracle.saveData.Farm.PendingCredits["pending-recovery"].CompletedAtUtcTicks);
                var beforeInvalidImport = oracle.saveData;
                var filesBefore = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .ToDictionary(path => path, File.ReadAllBytes);
                Assert.IsNull(Blindsided.Utilities.FeedbackForm.Instance, "Malformed input must be tested without a live feedback singleton.");
                Assert.IsFalse(SaveImportExport.TryImportToCurrentSlot(CorruptChecksum(exported),
                    out var rejected, out var invalidCommitted));
                StringAssert.Contains("checksum", rejected.ToLowerInvariant());
                Assert.IsFalse(invalidCommitted);
                Assert.IsNull(Blindsided.Utilities.FeedbackForm.Instance, "Expected malformed save input must not create feedback traffic.");
                Assert.AreSame(beforeInvalidImport, oracle.saveData);
                CollectionAssert.AreEquivalent(filesBefore.Keys, Directory.GetFiles(root, "*", SearchOption.AllDirectories));
                foreach (var file in filesBefore) CollectionAssert.AreEqual(file.Value, File.ReadAllBytes(file.Key));
                var slot = oracle.GetSlotDirectoryName(oracle.CurrentSlot);
                setter.Invoke(null, new object[] { root });
                var durable = SaveManager.Instance.LoadDetailedAsync(slot).GetAwaiter().GetResult();
                Assert.IsTrue(durable.Succeeded, durable.Diagnostic);
                Assert.AreEqual(1e100, durable.Data.Resources["Log"].Amount);
                Assert.IsTrue(durable.Data.RetroQuestRewardsApplied.Contains("reward-paid-once"));
            }
            finally
            {
                if (host != null)
                {
                    var oracle = host.GetComponent<Oracle>();
                    Set(oracle, "loaded", false);
                    UnityEngine.Object.DestroyImmediate(host);
                }
                if (Blindsided.Utilities.FeedbackForm.Instance != null)
                    UnityEngine.Object.DestroyImmediate(Blindsided.Utilities.FeedbackForm.Instance.gameObject);
                setter.Invoke(null, new object[] { previousRoot });
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            yield return null;
        }

        // Independent wire mutation: retain a valid compressed stream and envelope length,
        // but damage the advertised checksum so the production ingress owns rejection.
        private static string CorruptChecksum(string exported)
        {
            var encoded = exported.Substring(4).Replace('-', '+').Replace('_', '/');
            encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
            byte[] envelope;
            using (var input = new MemoryStream(Convert.FromBase64String(encoded)))
            using (var inflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                inflate.CopyTo(output);
                envelope = output.ToArray();
            }
            envelope[8] ^= 1;
            using (var output = new MemoryStream())
            {
                using (var deflate = new DeflateStream(output, CompressionMode.Compress, true))
                    deflate.Write(envelope, 0, envelope.Length);
                return "TE2:" + Convert.ToBase64String(output.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        private static void Set(Oracle oracle, string field, object value)
        {
            var member = typeof(Oracle).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(member);
            member.SetValue(oracle, value);
        }
    }
}
#endif
