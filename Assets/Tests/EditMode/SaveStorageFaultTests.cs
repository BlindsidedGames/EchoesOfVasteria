#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Blindsided.SaveData;
using NUnit.Framework;
using Sirenix.Serialization;

namespace Tests.EditMode
{
    public sealed class SaveStorageFaultTests
    {
        private string root;
        private string previousRoot;

        [SetUp]
        public void SetUp()
        {
            var field = typeof(SaveManager).GetField("rootPathOverride", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Cannot preserve existing save isolation.");
            previousRoot = (string)field.GetValue(null);
            root = Path.Combine(Path.GetTempPath(), "EchoesSaveFault_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            SetRoot(root);
        }

        [TearDown]
        public void TearDown()
        {
            SetRoot(previousRoot);
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(31)]
        [TestCase(-2)]
        [TestCase(-1)]
        public void InterruptedTemporaryWritePreservesDurableProgressAndRepairStopsRepeatedRecovery(int boundary)
        {
            Commit(12.5f);
            var durable = Snapshot();
            var original = File.ReadAllBytes(durable);
            Commit(83.25f);
            var newer = Snapshot();
            var bytes = File.ReadAllBytes(newer);
            var headerSize = BitConverter.ToUInt16(bytes, 6);
            var length = boundary == -2 ? headerSize : boundary == -1 ? bytes.Length - 1 : boundary;
            var temporary = Path.ChangeExtension(newer, ".tmp");
            File.Move(newer, temporary);
            using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.Write)) stream.SetLength(length);
            var partial = File.ReadAllBytes(temporary);
            SetRoot(root); // Cold cache, as after process restart.

            var recovered = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.AreEqual(SaveLoadStatus.Recovered, recovered.Status, recovered.Diagnostic);
            Assert.AreEqual(12.5f, recovered.Data.CompletionPercentage);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(durable));
            CollectionAssert.AreEqual(partial, File.ReadAllBytes(temporary));
            var repair = SaveManager.Instance.SaveDetailedAsync(recovered.Data, "Save1").GetAwaiter().GetResult();
            Assert.IsTrue(repair.Succeeded, repair.Error);
            SetRoot(root);
            var clean = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.AreEqual(SaveLoadStatus.Success, clean.Status, clean.Diagnostic);
            Assert.AreEqual(12.5f, clean.Data.CompletionPercentage);
            CollectionAssert.AreEqual(partial, File.ReadAllBytes(temporary));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifiedFutureSchemaOrFormatBlocksAutomaticFallbackAndRoutineOverwrite(bool futureFormat)
        {
            Commit(12.5f);
            var backup = Snapshot();
            var backupBytes = File.ReadAllBytes(backup);
            Commit(83.25f);
            var current = Snapshot();
            var currentBytes = File.ReadAllBytes(current);
            var cached = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.IsTrue(cached.Succeeded, cached.Diagnostic);
            var bytes = currentBytes;
            // Independent future-producer fixture: both payload and checksummed header
            // declare the newer schema. Append a new child in the same real lineage,
            // as a newer producer would, while this process still owns its loaded old head.
            var futureSchema = GameData.CurrentSchemaVersion + (futureFormat ? 0 : 1);
            var payload = SerializationUtility.SerializeValue(
                new GameData { SchemaVersion = futureSchema, CompletionPercentage = 83.25f }, DataFormat.Binary);
            var headerSize = BitConverter.ToUInt16(bytes, 6);
            var buildLength = BitConverter.ToUInt16(bytes, 32);
            var replacement = new byte[headerSize + payload.Length];
            Buffer.BlockCopy(bytes, 0, replacement, 0, headerSize);
            Buffer.BlockCopy(payload, 0, replacement, headerSize, payload.Length);
            bytes = replacement;
            var newWrite = Guid.NewGuid();
            var writeOffset = 67 + buildLength;
            Buffer.BlockCopy(currentBytes, writeOffset, bytes, 103 + buildLength, 16);
            Buffer.BlockCopy(newWrite.ToByteArray(), 0, bytes, writeOffset, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(3L), 0, bytes, 12, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(futureSchema), 0, bytes, 8, 4);
            if (futureFormat)
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(BitConverter.ToUInt16(currentBytes, 4) + 1)), 0, bytes, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, bytes, 28, 4);
            using (var sha = SHA256.Create())
            {
                Buffer.BlockCopy(sha.ComputeHash(payload), 0, bytes, 35 + buildLength, 32);
                Buffer.BlockCopy(sha.ComputeHash(bytes, 0, headerSize - 32), 0, bytes, headerSize - 32, 32);
            }
            var future = Path.Combine(root, "Saves", "Save1", $"snapshot.g{3L:D20}.{newWrite:N}.bin");
            File.WriteAllBytes(future, bytes);

            var load = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.AreEqual(SaveLoadStatus.UnsupportedNewer, load.Status, load.Diagnostic);
            Assert.IsNull(load.Data);
            if (!futureFormat) Assert.IsTrue(load.IntegrityVerified);
            var write = SaveManager.Instance.SaveDetailedAsync(cached.Data, "Save1").GetAwaiter().GetResult();
            Assert.AreEqual(SaveWriteStatus.ReloadRequired, write.Status, write.Error);
            Assert.AreEqual(SaveLoadStatus.Conflict, write.RecoveryStatus);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(future));
            CollectionAssert.AreEqual(backupBytes, File.ReadAllBytes(backup));
            CollectionAssert.AreEqual(currentBytes, File.ReadAllBytes(current));
            Assert.AreEqual(3, Directory.GetFiles(Path.Combine(root, "Saves", "Save1"), "*.bin").Length);
        }

        private static void SetRoot(string path)
        {
            var setter = typeof(SaveManager).GetMethod("SetRootPathForTests", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(setter, "Refusing disk tests without the test save root API.");
            setter.Invoke(null, new object[] { path });
        }

        private void Commit(float completion)
        {
            var result = SaveManager.Instance.SaveDetailedAsync(new GameData { CompletionPercentage = completion }, "Save1")
                .GetAwaiter().GetResult();
            Assert.IsTrue(result.Succeeded, result.Error);
        }

        private string Snapshot() => Directory.GetFiles(Path.Combine(root, "Saves", "Save1"), "snapshot.g*.bin")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal).First();
    }
}
#endif
