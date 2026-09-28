#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            Assert.IsNotNull(SaveManagerType, "SaveManager type not found; save tests cannot be safely isolated.");
            Assert.IsNotNull(GameDataType, "GameData type not found; save tests cannot create fixtures.");
            SetRootPathOverride(testRoot);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            SetRootPathOverride(null);
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

        private static void SetRootPathOverride(string path)
        {
            var t = SaveManagerType;
            Assert.IsNotNull(t, "SaveManager type not found; refusing to run against the real save root.");
            var m = t.GetMethod("SetRootPathForTests", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m, "SaveManager.SetRootPathForTests missing; refusing to run against the real save root.");
            m.Invoke(null, new object[] { path });
        }

        private static object NewGameData(float completion)
        {
            var t = GameDataType;
            Assert.IsNotNull(t, "GameData type not found.");
            var o = Activator.CreateInstance(t);
            var fComp = t.GetField("CompletionPercentage");
            if (fComp != null) fComp.SetValue(o, completion);
            var fVer = t.GetField("SchemaVersion");
            if (fVer != null) fVer.SetValue(o, 2);
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

        private static object InvokeTaskResult(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, methodName + " missing.");
            var task = method.Invoke(target, arguments);
            var result = task.GetType().GetProperty("Result");
            Assert.IsNotNull(result, methodName + " did not return a result task.");
            return result.GetValue(task);
        }

        private static bool ResultSucceeded(object result)
        {
            var property = result?.GetType().GetProperty("Succeeded");
            Assert.IsNotNull(property, "Result does not expose Succeeded.");
            return (bool)property.GetValue(result);
        }

        private static (string status, object data) LoadDetailed(object mgr, string slot)
        {
            var result = InvokeTaskResult(
                mgr,
                "LoadDetailedAsync",
                slot,
                System.Threading.CancellationToken.None);
            var type = result.GetType();
            return (
                type.GetProperty("Status")?.GetValue(result)?.ToString(),
                type.GetProperty("Data")?.GetValue(result));
        }

        private static float GetCompletion(object gameData)
        {
            var f = GameDataType.GetField("CompletionPercentage");
            return f != null ? (float)f.GetValue(gameData) : 0f;
        }

        private string[] GetCompletedGenerations(string slot)
        {
            var slotDir = Path.Combine(testRoot, "Saves", slot);
            return Directory.Exists(slotDir)
                ? Directory.GetFiles(slotDir, "snapshot.g*.bin").OrderBy(path => path).ToArray()
                : Array.Empty<string>();
        }

        private static string[] GetAuthorityStates(string root, string slot)
        {
            var stateDir = Path.Combine(root, "Saves", "SlotState");
            return Directory.Exists(stateDir)
                ? Directory.GetFiles(stateDir, slot + ".s*.state").OrderBy(path => path).ToArray()
                : Array.Empty<string>();
        }

        private static void MergeAuthorityStates(string sourceRoot, string destinationRoot, string slot)
        {
            var destination = Path.Combine(destinationRoot, "Saves", "SlotState");
            Directory.CreateDirectory(destination);
            foreach (var state in GetAuthorityStates(sourceRoot, slot))
            {
                var destinationPath = Path.Combine(destination, Path.GetFileName(state));
                if (!File.Exists(destinationPath))
                    File.Copy(state, destinationPath);
            }
        }

        private static void RewriteAuthorityFormatVersion(string path, ushort formatVersion)
        {
            var bytes = File.ReadAllBytes(path);
            Assert.GreaterOrEqual(bytes.Length, 40);
            bytes[4] = (byte)(formatVersion & 0xff);
            bytes[5] = (byte)(formatVersion >> 8);
            var contentLength = bytes.Length - 32;
            using var sha = SHA256.Create();
            var checksum = sha.ComputeHash(bytes, 0, contentLength);
            Buffer.BlockCopy(checksum, 0, bytes, contentLength, checksum.Length);
            File.WriteAllBytes(path, bytes);
        }

        private static string WriteAuthorityState(
            string root,
            string slot,
            long revision,
            Guid operationId,
            Guid lineageId,
            params Guid[] parents)
        {
            var parentIds = parents ?? Array.Empty<Guid>();
            var serializedSize = checked((ushort)(59 + parentIds.Length * 16 + 32));
            using var output = new MemoryStream(serializedSize);
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            writer.Write(new[] { (byte)'T', (byte)'E', (byte)'S', (byte)'2' });
            writer.Write((ushort)2);
            writer.Write(serializedSize);
            writer.Write(revision);
            writer.Write((byte)1);
            writer.Write(operationId.ToByteArray());
            writer.Write(lineageId.ToByteArray());
            writer.Write(DateTime.UtcNow.Ticks);
            writer.Write((ushort)parentIds.Length);
            foreach (var parent in parentIds)
                writer.Write(parent.ToByteArray());
            writer.Flush();

            using (var sha = SHA256.Create())
                writer.Write(sha.ComputeHash(output.ToArray()));
            writer.Flush();

            var stateDirectory = Path.Combine(root, "Saves", "SlotState");
            Directory.CreateDirectory(stateDirectory);
            var path = Path.Combine(
                stateDirectory,
                $"{slot}.s{revision:D20}.{operationId:N}.state");
            File.WriteAllBytes(path, output.ToArray());
            return path;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            foreach (var directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }

        private static void MergeSnapshots(string sourceRoot, string destinationRoot, string slot)
        {
            var source = Path.Combine(sourceRoot, "Saves", slot);
            var destination = Path.Combine(destinationRoot, "Saves", slot);
            Directory.CreateDirectory(destination);
            foreach (var snapshot in Directory.GetFiles(source, "snapshot.g*.*"))
            {
                var destinationPath = Path.Combine(destination, Path.GetFileName(snapshot));
                if (!File.Exists(destinationPath))
                    File.Copy(snapshot, destinationPath);
            }
        }

        private static byte[] BuildLegacySnapshotFromCurrentGeneration(
            string generationPath,
            DateTime? timestampUtc = null,
            string historicIntegrity = null)
        {
            var current = File.ReadAllBytes(generationPath);
            Assert.GreaterOrEqual(current.Length, 8);
            var headerSize = BitConverter.ToUInt16(current, 6);
            Assert.Greater(headerSize, 0);
            Assert.Less(headerSize, current.Length);
            var payload = new byte[current.Length - headerSize];
            Buffer.BlockCopy(current, headerSize, payload, 0, payload.Length);

            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            var build = Encoding.UTF8.GetBytes("legacy-fixture");
            var integrity = Encoding.UTF8.GetBytes(historicIntegrity ?? string.Empty);
            writer.Write(2);
            writer.Write((timestampUtc ?? DateTime.UtcNow).ToBinary());
            writer.Write((ushort)build.Length);
            writer.Write(build);
            writer.Write(payload.Length);
            writer.Write((ushort)integrity.Length);
            writer.Write(integrity);
            writer.Write(payload);
            writer.Flush();
            return output.ToArray();
        }

        private static Task<bool> SaveWithoutWaiting(object mgr, object gameData)
        {
            var method = SaveManagerType.GetMethod("SaveAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method);
            return (Task<bool>)method.Invoke(
                mgr,
                new object[] { gameData, System.Threading.CancellationToken.None });
        }

        private static void AddLargeFixture(object gameData, int count)
        {
            var field = GameDataType.GetField("EnemyKills");
            Assert.IsNotNull(field);
            var values = (System.Collections.IDictionary)Activator.CreateInstance(field.FieldType);
            for (var index = 0; index < count; index++)
                values.Add($"fixture-enemy-{index:D6}", (double)index);
            field.SetValue(gameData, values);
        }

        private string CreateIndependentActiveBranch(
            object mgr,
            string branchName,
            string slot,
            float completion)
        {
            var branchRoot = Path.Combine(testRoot, branchName);
            Directory.CreateDirectory(branchRoot);
            SetRootPathOverride(branchRoot);
            SetCurrentSlot(mgr, slot);
            Assert.IsTrue(Save(mgr, NewGameData(completion)));
            Assert.AreEqual(1, GetAuthorityStates(branchRoot, slot).Length);
            return branchRoot;
        }

        private string CreateIndependentDeletedBranch(object mgr, string branchName, string slot)
        {
            var branchRoot = Path.Combine(testRoot, branchName);
            Directory.CreateDirectory(branchRoot);
            SetRootPathOverride(branchRoot);
            var deletion = InvokeTaskResult(
                mgr,
                "DeleteSlotDetailedAsync",
                slot,
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(deletion));
            Assert.AreEqual(1, GetAuthorityStates(branchRoot, slot).Length);
            return branchRoot;
        }

        [UnityTest]
        public IEnumerator RapidSavesAcrossFrames_AreValid()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");

            for (int i = 0; i < 12; i++)
            {
                Assert.IsTrue(Save(mgr, NewGameData(i)));
                yield return null;
            }

            var r = Load(mgr);
            Assert.IsTrue(r.ok);
            Assert.IsNotNull(r.data);
            Assert.AreEqual(11f, GetCompletion(r.data), 0.0001f);
            Assert.LessOrEqual(GetCompletedGenerations("Save1").Length, 5);
        }

        [UnityTest]
        public IEnumerator OverlappingRoutineSaves_DoNotForkTheCachedHead()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));
            Assert.IsNotNull(LoadDetailed(mgr, "Save1").data);

            var firstData = NewGameData(2f);
            var secondData = NewGameData(3f);
            AddLargeFixture(firstData, 20000);
            AddLargeFixture(secondData, 20000);
            var first = SaveWithoutWaiting(mgr, firstData);
            var second = SaveWithoutWaiting(mgr, secondData);
            while (!first.IsCompleted || !second.IsCompleted)
                yield return null;

            var successCount = new[] { first.Result, second.Result }.Count(result => result);
            Assert.That(successCount, Is.InRange(1, 2),
                "At least one save must commit; both may commit when the second capture begins after the first finishes.");
            var loaded = LoadDetailed(mgr, "Save1");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.IsNotNull(loaded.data);
            Assert.AreNotEqual("Conflict", loaded.status);
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
        public IEnumerator CorruptNewestGeneration_FallsBackWithoutDeletingEvidence()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");

            Assert.IsTrue(Save(mgr, NewGameData(100f)));
            Assert.IsTrue(Save(mgr, NewGameData(200f)));

            var generations = GetCompletedGenerations("Save3");
            Assert.AreEqual(2, generations.Length);
            var newest = generations[generations.Length - 1];
            File.AppendAllText(newest, "CORRUPT");
            yield return null;

            var r = Load(mgr);
            Assert.IsTrue(r.ok);
            Assert.AreEqual(100f, GetCompletion(r.data), 0.0001f);
            Assert.IsTrue(File.Exists(newest), "Corrupt evidence should remain untouched.");
        }

        [UnityTest]
        public IEnumerator SameLengthPayloadCorruption_FallsBackByChecksum()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(10f)));
            Assert.IsTrue(Save(mgr, NewGameData(20f)));

            var generations = GetCompletedGenerations("Save1");
            var newest = generations[generations.Length - 1];
            var bytes = File.ReadAllBytes(newest);
            bytes[bytes.Length - 1] ^= 0x5A;
            File.WriteAllBytes(newest, bytes);
            yield return null;

            var result = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Recovered", result.status);
            Assert.AreEqual(10f, GetCompletion(result.data), 0.0001f);
            Assert.IsTrue(Save(mgr, result.data),
                "A checksum-corrupt header-only head must not block the verified repair commit.");

            var stable = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Success", stable.status);
            Assert.AreEqual(10f, GetCompletion(stable.data), 0.0001f);
            Assert.IsTrue(File.Exists(newest), "The corrupt generation remains available as evidence.");
        }

        [UnityTest]
        public IEnumerator CompletedTempGeneration_IsRecoveredAfterInterruptedPublish()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(42f)));

            var completed = GetCompletedGenerations("Save2").Single();
            var temp = Path.ChangeExtension(completed, ".tmp");
            File.Move(completed, temp);
            yield return null;

            var result = Load(mgr);
            Assert.IsTrue(result.ok);
            Assert.AreEqual(42f, GetCompletion(result.data), 0.0001f);
            Assert.IsTrue(File.Exists(temp));
        }

        [UnityTest]
        public IEnumerator LegacySnapshot_LoadsAndUpgradesWithoutModifyingOriginalBytes()
        {
            var mgr = GetSaveManagerInstance();
            var sourceRoot = Path.Combine(testRoot, "LegacyFixtureSource");
            var legacyRoot = Path.Combine(testRoot, "LegacyFixtureTarget");

            SetRootPathOverride(sourceRoot);
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(73f)));
            var fixture = BuildLegacySnapshotFromCurrentGeneration(
                Directory.GetFiles(Path.Combine(sourceRoot, "Saves", "Save2"), "snapshot.g*.bin").Single());

            var legacySlot = Path.Combine(legacyRoot, "Saves", "Save2");
            Directory.CreateDirectory(legacySlot);
            var legacyPath = Path.Combine(legacySlot, "snapshot.bin");
            File.WriteAllBytes(legacyPath, fixture);

            SetRootPathOverride(legacyRoot);
            SetCurrentSlot(mgr, "Save2");
            var loaded = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Success", loaded.status);
            Assert.AreEqual(73f, GetCompletion(loaded.data), 0.0001f);

            var upgraded = InvokeTaskResult(
                mgr,
                "SaveDetailedAsync",
                loaded.data,
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(upgraded));
            yield return null;

            CollectionAssert.AreEqual(fixture, File.ReadAllBytes(legacyPath));
            Assert.AreEqual(1, Directory.GetFiles(legacySlot, "snapshot.g*.bin").Length);
            var reloaded = LoadDetailed(mgr, "Save2");
            Assert.IsTrue(reloaded.status == "Success" || reloaded.status == "Recovered");
            Assert.AreEqual(73f, GetCompletion(reloaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator RoutineSave_DoesNotSupersedeAnUnloadedLegacySnapshot()
        {
            var mgr = GetSaveManagerInstance();
            var sourceRoot = Path.Combine(testRoot, "UnloadedLegacySource");
            var targetRoot = Path.Combine(testRoot, "UnloadedLegacyTarget");

            SetRootPathOverride(sourceRoot);
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(31f)));
            var fixture = BuildLegacySnapshotFromCurrentGeneration(
                Directory.GetFiles(Path.Combine(sourceRoot, "Saves", "Save2"), "snapshot.g*.bin").Single());

            var slotDirectory = Path.Combine(targetRoot, "Saves", "Save2");
            Directory.CreateDirectory(slotDirectory);
            var legacyPath = Path.Combine(slotDirectory, "snapshot.bin");
            File.WriteAllBytes(legacyPath, fixture);

            SetRootPathOverride(targetRoot);
            SetCurrentSlot(mgr, "Save2");
            var rejected = InvokeTaskResult(
                mgr,
                "SaveDetailedAsync",
                NewGameData(99f),
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsFalse(ResultSucceeded(rejected));
            Assert.AreEqual("ReloadRequired", rejected.GetType().GetProperty("Status")?.GetValue(rejected)?.ToString());
            CollectionAssert.AreEqual(fixture, File.ReadAllBytes(legacyPath));
            Assert.IsEmpty(Directory.GetFiles(slotDirectory, "snapshot.g*.*"));
            Assert.IsEmpty(GetAuthorityStates(targetRoot, "Save2"));

            var loaded = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Success", loaded.status);
            Assert.AreEqual(31f, GetCompletion(loaded.data), 0.0001f);
            var upgraded = InvokeTaskResult(
                mgr,
                "SaveDetailedAsync",
                loaded.data,
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(upgraded));
            CollectionAssert.AreEqual(fixture, File.ReadAllBytes(legacyPath));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LegacyRotation_UsesPrimaryBeforeTimestamp()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(10f)));
            var olderPayload = GetCompletedGenerations("Save1").Single();
            Assert.IsTrue(Save(mgr, NewGameData(90f)));
            var newerPayload = GetCompletedGenerations("Save1").Last();

            var slotDir = Path.Combine(testRoot, "Saves", "Save1");
            var primary = BuildLegacySnapshotFromCurrentGeneration(
                newerPayload,
                DateTime.UtcNow.AddDays(-2),
                "historic-integrity-field");
            var previous = BuildLegacySnapshotFromCurrentGeneration(
                olderPayload,
                DateTime.UtcNow.AddDays(2),
                "historic-integrity-field");
            foreach (var generation in GetCompletedGenerations("Save1"))
                File.Delete(generation);
            foreach (var state in GetAuthorityStates(testRoot, "Save1"))
                File.Delete(state);
            File.WriteAllBytes(Path.Combine(slotDir, "snapshot.bin"), primary);
            File.WriteAllBytes(Path.Combine(slotDir, "snapshot.prev1.bin"), previous);
            yield return null;

            var loaded = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Success", loaded.status);
            Assert.AreEqual(90f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator LegacyFallback_UpgradesWhilePreservingCorruptPrimaryEvidence()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(12f)));
            var fallback = BuildLegacySnapshotFromCurrentGeneration(GetCompletedGenerations("Save1").Single());
            Assert.IsTrue(Save(mgr, NewGameData(99f)));
            var corruptPrimary = BuildLegacySnapshotFromCurrentGeneration(GetCompletedGenerations("Save1").Last())
                .Concat(Encoding.UTF8.GetBytes("CORRUPT"))
                .ToArray();

            var slotDirectory = Path.Combine(testRoot, "Saves", "Save1");
            foreach (var generation in GetCompletedGenerations("Save1"))
                File.Delete(generation);
            foreach (var state in GetAuthorityStates(testRoot, "Save1"))
                File.Delete(state);
            var primaryPath = Path.Combine(slotDirectory, "snapshot.bin");
            var fallbackPath = Path.Combine(slotDirectory, "snapshot.prev1.bin");
            File.WriteAllBytes(primaryPath, corruptPrimary);
            File.WriteAllBytes(fallbackPath, fallback);

            var loaded = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Recovered", loaded.status);
            Assert.AreEqual(12f, GetCompletion(loaded.data), 0.0001f);
            var upgraded = InvokeTaskResult(
                mgr,
                "SaveDetailedAsync",
                loaded.data,
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(upgraded));
            yield return null;

            CollectionAssert.AreEqual(corruptPrimary, File.ReadAllBytes(primaryPath));
            CollectionAssert.AreEqual(fallback, File.ReadAllBytes(fallbackPath));
            Assert.AreEqual(1, Directory.GetFiles(slotDirectory, "snapshot.g*.bin").Length);

            var stable = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Success", stable.status,
                "A verified repair descendant must not be rewritten on every launch.");
            Assert.AreEqual(12f, GetCompletion(stable.data), 0.0001f);
            CollectionAssert.AreEqual(corruptPrimary, File.ReadAllBytes(primaryPath));
        }

        [UnityTest]
        public IEnumerator MissingAuthority_PrefersVerifiedModernProgressOverPreservedLegacyCopy()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(15f)));
            var first = GetCompletedGenerations("Save2").Single();
            var legacy = BuildLegacySnapshotFromCurrentGeneration(first);
            Assert.IsTrue(Save(mgr, NewGameData(95f)));

            var slotDir = Path.Combine(testRoot, "Saves", "Save2");
            File.WriteAllBytes(Path.Combine(slotDir, "snapshot.bin"), legacy);
            foreach (var state in GetAuthorityStates(testRoot, "Save2"))
                File.Delete(state);
            yield return null;

            var loaded = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Recovered", loaded.status);
            Assert.AreEqual(95f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator MissingAuthority_RepairKeepsLineageWhenLateCloudStateArrives()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");
            Assert.IsTrue(Save(mgr, NewGameData(10f)));
            Assert.IsTrue(Save(mgr, NewGameData(20f)));

            var originalState = GetAuthorityStates(testRoot, "Save3").Single();
            var originalStateName = Path.GetFileName(originalState);
            var originalStateBytes = File.ReadAllBytes(originalState);
            File.Delete(originalState);

            var recovered = LoadDetailed(mgr, "Save3");
            Assert.AreEqual("Recovered", recovered.status);
            Assert.AreEqual(20f, GetCompletion(recovered.data), 0.0001f);
            Assert.IsTrue(Save(mgr, NewGameData(30f)));

            var stateDirectory = Path.Combine(testRoot, "Saves", "SlotState");
            File.WriteAllBytes(Path.Combine(stateDirectory, originalStateName), originalStateBytes);
            yield return null;

            var loaded = LoadDetailed(mgr, "Save3");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.AreEqual(30f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator AllCorruptGenerations_ReturnFailureAndRemainUntouched()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));
            Assert.IsTrue(Save(mgr, NewGameData(2f)));

            var generations = GetCompletedGenerations("Save3");
            foreach (var path in generations)
                File.AppendAllText(path, "CORRUPT");
            yield return null;

            var result = LoadDetailed(mgr, "Save3");
            Assert.AreEqual("Corrupt", result.status);
            Assert.IsNull(result.data);
            CollectionAssert.AreEquivalent(generations, GetCompletedGenerations("Save3"));
        }

        [UnityTest]
        public IEnumerator UnrecognizedSnapshotFile_IsNotTreatedAsFreshSlot()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            var slotDir = Path.Combine(testRoot, "Saves", "Save1");
            Directory.CreateDirectory(slotDir);
            var unknown = Path.Combine(slotDir, "snapshot.interrupted-rename");
            File.WriteAllText(unknown, "not a valid save");
            yield return null;

            var result = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Corrupt", result.status);
            Assert.IsNull(result.data);
            Assert.IsTrue(File.Exists(unknown));
        }

        [UnityTest]
        public IEnumerator Retention_NeverDeletesAnUnverifiedGeneration()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            for (var i = 0; i < 5; i++)
                Assert.IsTrue(Save(mgr, NewGameData(i)));

            var corrupt = GetCompletedGenerations("Save2")[0];
            File.AppendAllText(corrupt, "CORRUPT");
            for (var i = 5; i < 9; i++)
                Assert.IsTrue(Save(mgr, NewGameData(i)));
            yield return null;

            Assert.IsTrue(File.Exists(corrupt), "Retention must never delete unverified evidence.");
            Assert.AreEqual(6, GetCompletedGenerations("Save2").Length);
            var result = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Success", result.status,
                "Corrupt evidence older than a verified descendant is already repaired history.");
            Assert.AreEqual(8f, GetCompletion(result.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator HeaderVersionBitFlip_FallsBackInsteadOfMasqueradingAsFutureData()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));
            Assert.IsTrue(Save(mgr, NewGameData(2f)));

            var newest = GetCompletedGenerations("Save1").Last();
            var bytes = File.ReadAllBytes(newest);
            bytes[4] = 5;
            bytes[5] = 0;
            File.WriteAllBytes(newest, bytes);
            yield return null;

            var result = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Recovered", result.status);
            Assert.AreEqual(1f, GetCompletion(result.data), 0.0001f);
            Assert.IsTrue(Save(mgr, result.data));

            var stable = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Success", stable.status,
                "The verified repair descendant must settle recovery without deleting corrupt evidence.");
            Assert.AreEqual(1f, GetCompletion(stable.data), 0.0001f);
            Assert.IsTrue(File.Exists(newest));
        }

        [UnityTest]
        public IEnumerator DeletionTombstone_BlocksCloudRestoredSnapshot()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(25f)));
            var original = GetCompletedGenerations("Save2").Single();
            var filename = Path.GetFileName(original);
            var bytes = File.ReadAllBytes(original);

            var deletion = InvokeTaskResult(
                mgr,
                "DeleteSlotDetailedAsync",
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(deletion));

            var restoredDir = Path.Combine(testRoot, "Saves", "Save2");
            Directory.CreateDirectory(restoredDir);
            File.WriteAllBytes(Path.Combine(restoredDir, filename), bytes);
            yield return null;

            var load = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Deleted", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator ReplacementLineage_IgnoresHigherGenerationFromDeletedCloudBranch()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");
            for (var i = 1; i <= 6; i++)
                Assert.IsTrue(Save(mgr, NewGameData(i)));

            var oldNewest = GetCompletedGenerations("Save3").Last();
            var oldFilename = Path.GetFileName(oldNewest);
            var oldBytes = File.ReadAllBytes(oldNewest);
            var deletion = InvokeTaskResult(
                mgr,
                "DeleteSlotDetailedAsync",
                "Save3",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(deletion));

            var replacement = InvokeTaskResult(
                mgr,
                "ReplaceSlotDetailedAsync",
                NewGameData(0f),
                "Save3",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(replacement));

            var slotDir = Path.Combine(testRoot, "Saves", "Save3");
            File.WriteAllBytes(Path.Combine(slotDir, oldFilename), oldBytes);
            yield return null;

            var load = LoadDetailed(mgr, "Save3");
            Assert.IsTrue(load.status == "Success" || load.status == "Recovered");
            Assert.AreEqual(0f, GetCompletion(load.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator MalformedAuthorityState_NeverLooksLikeANewSlot()
        {
            var mgr = GetSaveManagerInstance();
            var stateDir = Path.Combine(testRoot, "Saves", "SlotState");
            Directory.CreateDirectory(stateDir);
            File.WriteAllText(
                Path.Combine(stateDir, "Save1.s00000000000000000001.0123456789abcdef0123456789abcdef.state"),
                "broken");
            yield return null;

            var load = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Corrupt", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator FutureAuthorityFormat_IsDowngradeProtectedFromForcedRecovery()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(42f)));
            var statePath = GetAuthorityStates(testRoot, "Save2").Single();
            RewriteAuthorityFormatVersion(statePath, 3);
            var protectedBytes = File.ReadAllBytes(statePath);
            yield return null;

            var load = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("UnsupportedNewer", load.status);
            Assert.IsNull(load.data);

            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(0f),
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsFalse(ResultSucceeded(recovery));
            CollectionAssert.AreEqual(protectedBytes, File.ReadAllBytes(statePath));
            Assert.AreEqual(1, GetCompletedGenerations("Save2").Length);
        }

        [UnityTest]
        public IEnumerator ForcedRecovery_ArchivesZeroOperationAuthorityEvidence()
        {
            var mgr = GetSaveManagerInstance();
            var stateDirectory = Path.Combine(testRoot, "Saves", "SlotState");
            Directory.CreateDirectory(stateDirectory);
            var malformedName =
                "Save1.s00000000000000000001.00000000000000000000000000000000.state";
            var malformedPath = Path.Combine(stateDirectory, malformedName);
            File.WriteAllText(malformedPath, "preserve this malformed authority evidence");
            Assert.AreEqual("Corrupt", LoadDetailed(mgr, "Save1").status);

            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(12f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(recovery));
            yield return null;

            Assert.IsFalse(File.Exists(malformedPath));
            Assert.AreEqual(
                1,
                Directory.GetFiles(
                    Path.Combine(testRoot, "SaveRecovery", "AuthorityEvidence"),
                    malformedName,
                    SearchOption.AllDirectories).Length);
            var loaded = LoadDetailed(mgr, "Save1");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.AreEqual(12f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator ForcedRecovery_ArchivesUnrecognizedSnapshotEvidence()
        {
            var mgr = GetSaveManagerInstance();
            var slotDirectory = Path.Combine(testRoot, "Saves", "Save2");
            Directory.CreateDirectory(slotDirectory);
            const string malformedName = "snapshot.future-format";
            var malformedPath = Path.Combine(slotDirectory, malformedName);
            File.WriteAllText(malformedPath, "preserve this unrecognized snapshot evidence");
            Assert.AreEqual("Corrupt", LoadDetailed(mgr, "Save2").status);

            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(21f),
                "Save2",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(recovery));
            yield return null;

            Assert.IsFalse(File.Exists(malformedPath));
            Assert.AreEqual(
                1,
                Directory.GetFiles(
                    Path.Combine(testRoot, "SaveRecovery", "SnapshotEvidence"),
                    malformedName,
                    SearchOption.AllDirectories).Length);
            var loaded = LoadDetailed(mgr, "Save2");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.AreEqual(21f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator EqualRevisionActiveLineages_FromCloudMerge_ReturnConflict()
        {
            var mgr = GetSaveManagerInstance();
            var branchA = CreateIndependentActiveBranch(mgr, "ActiveBranchA", "Save1", 10f);
            var branchB = CreateIndependentActiveBranch(mgr, "ActiveBranchB", "Save1", 20f);

            SetRootPathOverride(testRoot);
            MergeAuthorityStates(branchA, testRoot, "Save1");
            MergeAuthorityStates(branchB, testRoot, "Save1");
            yield return null;

            var load = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Conflict", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator SameLineageOfflineForks_WithUnequalRetainedGenerations_ReturnConflict()
        {
            var mgr = GetSaveManagerInstance();
            var baseRoot = Path.Combine(testRoot, "ForkBase");
            var branchA = Path.Combine(testRoot, "ForkA");
            var branchB = Path.Combine(testRoot, "ForkB");

            SetRootPathOverride(baseRoot);
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));
            CopyDirectory(baseRoot, branchA);
            CopyDirectory(baseRoot, branchB);

            SetRootPathOverride(branchA);
            Assert.IsTrue(LoadDetailed(mgr, "Save1").data != null);
            for (var index = 0; index < 8; index++)
                Assert.IsTrue(Save(mgr, NewGameData(100f + index)));

            SetRootPathOverride(branchB);
            Assert.IsTrue(LoadDetailed(mgr, "Save1").data != null);
            Assert.IsTrue(Save(mgr, NewGameData(200f)));

            SetRootPathOverride(testRoot);
            MergeAuthorityStates(baseRoot, testRoot, "Save1");
            MergeSnapshots(branchA, testRoot, "Save1");
            MergeSnapshots(branchB, testRoot, "Save1");
            yield return null;

            var load = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Conflict", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator SameLineageOfflineForks_WithIdenticalPayloads_StillReturnConflict()
        {
            var mgr = GetSaveManagerInstance();
            var baseRoot = Path.Combine(testRoot, "IdenticalForkBase");
            var branchA = Path.Combine(testRoot, "IdenticalForkA");
            var branchB = Path.Combine(testRoot, "IdenticalForkB");
            var destination = Path.Combine(testRoot, "IdenticalForkMerge");

            SetRootPathOverride(baseRoot);
            SetCurrentSlot(mgr, "Save1");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));
            CopyDirectory(baseRoot, branchA);
            CopyDirectory(baseRoot, branchB);

            var identicalData = NewGameData(2f);
            SetRootPathOverride(branchA);
            Assert.IsNotNull(LoadDetailed(mgr, "Save1").data);
            Assert.IsTrue(Save(mgr, identicalData));

            SetRootPathOverride(branchB);
            Assert.IsNotNull(LoadDetailed(mgr, "Save1").data);
            Assert.IsTrue(Save(mgr, identicalData));

            CopyDirectory(baseRoot, destination);
            MergeSnapshots(branchA, destination, "Save1");
            MergeSnapshots(branchB, destination, "Save1");
            SetRootPathOverride(destination);
            yield return null;

            var load = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Conflict", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator EqualRevisionDeletion_FromCloudMerge_WinsOverActiveLineage()
        {
            var mgr = GetSaveManagerInstance();
            var activeBranch = CreateIndependentActiveBranch(mgr, "ActiveBranch", "Save2", 30f);
            var deletedBranch = CreateIndependentDeletedBranch(mgr, "DeletedBranch", "Save2");

            SetRootPathOverride(testRoot);
            MergeAuthorityStates(activeBranch, testRoot, "Save2");
            MergeAuthorityStates(deletedBranch, testRoot, "Save2");
            yield return null;

            var load = LoadDetailed(mgr, "Save2");
            Assert.AreEqual("Deleted", load.status);
            Assert.IsNull(load.data);
        }

        [UnityTest]
        public IEnumerator CorruptHighestRevisionSibling_InvalidatesOtherwiseValidAuthorityState()
        {
            var mgr = GetSaveManagerInstance();
            var branchA = CreateIndependentActiveBranch(mgr, "ValidBranch", "Save3", 40f);
            var branchB = CreateIndependentActiveBranch(mgr, "CorruptBranch", "Save3", 50f);

            SetRootPathOverride(testRoot);
            MergeAuthorityStates(branchA, testRoot, "Save3");
            MergeAuthorityStates(branchB, testRoot, "Save3");
            var mergedStates = GetAuthorityStates(testRoot, "Save3");
            Assert.AreEqual(2, mergedStates.Length);
            var branchBFilename = Path.GetFileName(GetAuthorityStates(branchB, "Save3").Single());
            var corruptSibling = mergedStates.Single(path => Path.GetFileName(path) == branchBFilename);
            File.AppendAllText(corruptSibling, "CORRUPT");
            yield return null;

            var load = LoadDetailed(mgr, "Save3");
            Assert.AreEqual("Corrupt", load.status);
            Assert.IsNull(load.data);
            Assert.IsTrue(File.Exists(corruptSibling));
        }

        [UnityTest]
        public IEnumerator ForcedRecovery_PublishesNewAuthorityAboveMergedConflict()
        {
            var mgr = GetSaveManagerInstance();
            var branchA = CreateIndependentActiveBranch(mgr, "RecoveryBranchA", "Save1", 60f);
            var branchB = CreateIndependentActiveBranch(mgr, "RecoveryBranchB", "Save1", 70f);

            SetRootPathOverride(testRoot);
            MergeAuthorityStates(branchA, testRoot, "Save1");
            MergeAuthorityStates(branchB, testRoot, "Save1");
            var conflicted = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Conflict", conflicted.status);
            Assert.IsNull(conflicted.data);

            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(80f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(recovery));
            yield return null;

            var resolved = LoadDetailed(mgr, "Save1");
            Assert.IsTrue(resolved.status == "Success" || resolved.status == "Recovered");
            Assert.IsNotNull(resolved.data);
            Assert.AreEqual(80f, GetCompletion(resolved.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator ForcedRecovery_QuarantinesCyclicAuthorityAndSurvivesReplacement()
        {
            var mgr = GetSaveManagerInstance();
            var lineage = Guid.NewGuid();
            var operationA = Guid.NewGuid();
            var operationB = Guid.NewGuid();
            var evidenceA = WriteAuthorityState(testRoot, "Save1", 1, operationA, lineage, operationB);
            var evidenceB = WriteAuthorityState(testRoot, "Save1", 2, operationB, lineage, operationA);

            Assert.AreEqual("Conflict", LoadDetailed(mgr, "Save1").status);
            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(80f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(recovery));
            Assert.IsTrue(File.Exists(evidenceA));
            Assert.IsTrue(File.Exists(evidenceB));

            var recovered = LoadDetailed(mgr, "Save1");
            Assert.IsTrue(recovered.status == "Success" || recovered.status == "Recovered");
            Assert.AreEqual(80f, GetCompletion(recovered.data), 0.0001f);

            var replacement = InvokeTaskResult(
                mgr,
                "ReplaceSlotDetailedAsync",
                NewGameData(90f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(replacement));
            yield return null;

            var replaced = LoadDetailed(mgr, "Save1");
            Assert.IsTrue(replaced.status == "Success" || replaced.status == "Recovered");
            Assert.AreEqual(90f, GetCompletion(replaced.data), 0.0001f);
            Assert.IsTrue(File.Exists(evidenceA));
            Assert.IsTrue(File.Exists(evidenceB));
        }

        [UnityTest]
        public IEnumerator ConcurrentForcedRecoveries_RemainAConflictRegardlessOfLocalRevision()
        {
            var mgr = GetSaveManagerInstance();
            var branchA = CreateIndependentActiveBranch(mgr, "ForceSourceA", "Save1", 10f);
            var branchB = CreateIndependentActiveBranch(mgr, "ForceSourceB", "Save1", 20f);
            var mergedRoot = Path.Combine(testRoot, "ForceConflictBase");
            MergeAuthorityStates(branchA, mergedRoot, "Save1");
            MergeAuthorityStates(branchB, mergedRoot, "Save1");

            var recoveryA = Path.Combine(testRoot, "ForceRecoveryA");
            var recoveryB = Path.Combine(testRoot, "ForceRecoveryB");
            CopyDirectory(mergedRoot, recoveryA);
            CopyDirectory(mergedRoot, recoveryB);

            SetRootPathOverride(recoveryA);
            var firstRecovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(80f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(firstRecovery));

            SetRootPathOverride(recoveryB);
            var secondRecovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(90f),
                "Save1",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(secondRecovery));

            var destination = Path.Combine(testRoot, "ForceRecoveryMerge");
            MergeAuthorityStates(recoveryA, destination, "Save1");
            MergeAuthorityStates(recoveryB, destination, "Save1");
            MergeSnapshots(recoveryA, destination, "Save1");
            MergeSnapshots(recoveryB, destination, "Save1");
            SetRootPathOverride(destination);
            yield return null;

            var merged = LoadDetailed(mgr, "Save1");
            Assert.AreEqual("Conflict", merged.status);
            Assert.IsNull(merged.data);
        }

        [UnityTest]
        public IEnumerator AuthorityHistory_IsRetainedAcrossManyExplicitReplacements()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save2");
            Assert.IsTrue(Save(mgr, NewGameData(1f)));

            for (var index = 2; index <= 10; index++)
            {
                var replacement = InvokeTaskResult(
                    mgr,
                    "ReplaceSlotDetailedAsync",
                    NewGameData(index),
                    "Save2",
                    System.Threading.CancellationToken.None);
                Assert.IsTrue(ResultSucceeded(replacement));
            }
            yield return null;

            Assert.AreEqual(10, GetAuthorityStates(testRoot, "Save2").Length);
            var loaded = LoadDetailed(mgr, "Save2");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.AreEqual(10f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator ForcedRecovery_SupersedesNamedCorruptAuthorityWithoutDeletingEvidence()
        {
            var mgr = GetSaveManagerInstance();
            SetCurrentSlot(mgr, "Save3");
            Assert.IsTrue(Save(mgr, NewGameData(5f)));
            var corruptState = GetAuthorityStates(testRoot, "Save3").Single();
            File.AppendAllText(corruptState, "CORRUPT");
            Assert.AreEqual("Corrupt", LoadDetailed(mgr, "Save3").status);

            var recovery = InvokeTaskResult(
                mgr,
                "RecoverSlotWithFreshDataAsync",
                NewGameData(55f),
                "Save3",
                System.Threading.CancellationToken.None);
            Assert.IsTrue(ResultSucceeded(recovery));
            yield return null;

            Assert.IsTrue(File.Exists(corruptState));
            var loaded = LoadDetailed(mgr, "Save3");
            Assert.IsTrue(loaded.status == "Success" || loaded.status == "Recovered");
            Assert.AreEqual(55f, GetCompletion(loaded.data), 0.0001f);
        }

        [UnityTest]
        public IEnumerator ProductionMigration_PreservesCauldronTotalsAndIsIdempotent()
        {
            var source = NewGameData(25f);
            GameDataType.GetField("SchemaVersion")?.SetValue(source, 1);
            GameDataType.GetField("LastGameVersion")?.SetValue(source, "1.4.3");
            var countsField = GameDataType.GetField("CauldronCardCounts");
            Assert.IsNotNull(countsField);
            var sourceCounts = (System.Collections.IDictionary)Activator.CreateInstance(countsField.FieldType);
            sourceCounts.Add("RES:Ore", 750);
            sourceCounts.Add("BUFF:Haste", 400);
            countsField.SetValue(source, sourceCounts);

            var gearItemType = FindType("Blindsided.SaveData.GearItemRecord");
            var gearAffixType = FindType("Blindsided.SaveData.GearAffixRecord");
            Assert.IsNotNull(gearItemType);
            Assert.IsNotNull(gearAffixType);
            var gearItem = Activator.CreateInstance(gearItemType);
            gearItemType.GetField("slot")?.SetValue(gearItem, "Helmet");
            gearItemType.GetField("rarity")?.SetValue(gearItem, "TemporarilyMissingRarity");
            var affix = Activator.CreateInstance(gearAffixType);
            gearAffixType.GetField("statId")?.SetValue(affix, "temporarily-missing-stat");
            gearAffixType.GetField("value")?.SetValue(affix, 42f);
            gearAffixType.GetField("quality")?.SetValue(affix, 0.75d);
            var affixes = gearItemType.GetField("affixes")?.GetValue(gearItem) as IList;
            Assert.IsNotNull(affixes);
            affixes.Add(affix);
            var equipmentField = GameDataType.GetField("EquipmentBySlot");
            var equipment = equipmentField?.GetValue(source) as IDictionary;
            Assert.IsNotNull(equipment);
            equipment["Helmet"] = gearItem;

            var runner = FindType("Blindsided.SaveData.Migrations.SaveMigrationRunner");
            Assert.IsNotNull(runner);
            var migrate = runner.GetMethod("TryMigrate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(migrate);
            var first = migrate.Invoke(null, new[] { source, "1.4.3" });
            Assert.IsTrue((bool)first.GetType().GetProperty("Succeeded")?.GetValue(first));
            Assert.IsTrue((bool)first.GetType().GetProperty("Changed")?.GetValue(first));
            var migrated = first.GetType().GetProperty("Data")?.GetValue(first);
            Assert.IsNotNull(migrated);

            Assert.AreEqual(750, sourceCounts["RES:Ore"], "The source graph must remain untouched.");
            Assert.AreEqual(400, sourceCounts["BUFF:Haste"], "The source graph must remain untouched.");
            var migratedCounts = (System.Collections.IDictionary)countsField.GetValue(migrated);
            Assert.AreEqual(500, migratedCounts["RES:Ore"]);
            Assert.AreEqual(300, migratedCounts["BUFF:Haste"]);
            var total = migratedCounts.Values.Cast<int>().Sum(value => (long)value);
            Assert.AreEqual(1150L, total, "No cauldron cards may be discarded during migration.");
            var migratedEquipment = equipmentField.GetValue(migrated) as IDictionary;
            var migratedHelmet = migratedEquipment?["Helmet"];
            var migratedAffixes = gearItemType.GetField("affixes")?.GetValue(migratedHelmet) as IList;
            Assert.IsNotNull(migratedAffixes);
            Assert.AreEqual(1, migratedAffixes.Count);
            Assert.AreEqual(42f, (float)gearAffixType.GetField("value").GetValue(migratedAffixes[0]), 0.0001f);
            Assert.AreEqual(0.75d, (double)gearAffixType.GetField("quality").GetValue(migratedAffixes[0]), 0.000001d);
            var ledger = GameDataType.GetField("AppliedMigrationIds")?.GetValue(migrated) as IEnumerable;
            CollectionAssert.Contains(ledger?.Cast<object>().Select(value => value.ToString()).ToArray(), "GearAffixQuality_v1_2_17");
            var executed = first.GetType().GetProperty("AppliedIds")?.GetValue(first) as IEnumerable;
            CollectionAssert.DoesNotContain(
                executed?.Cast<object>().Select(value => value.ToString()).ToArray(),
                "GearAffixQuality_v1_2_17");

            var second = migrate.Invoke(null, new[] { migrated, "1.4.3" });
            Assert.IsTrue((bool)second.GetType().GetProperty("Succeeded")?.GetValue(second));
            Assert.IsFalse((bool)second.GetType().GetProperty("Changed")?.GetValue(second));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LedgeredSave_MissingHistoricalMigrationIdExecutesMigration()
        {
            var source = NewGameData(26f);
            GameDataType.GetField("LastGameVersion")?.SetValue(source, "1.4.3");
            var ledgerField = GameDataType.GetField("AppliedMigrationIds");
            var ledger = ledgerField?.GetValue(source);
            Assert.IsNotNull(ledger);
            var addMigrationId = ledger.GetType().GetMethod("Add");
            Assert.IsNotNull(addMigrationId);
            foreach (var migrationId in new[]
                     {
                         "SchemaV2Normalization",
                         "SchemaV2ZZCauldronOverflowRepair",
                         "DuckHelmetSanitation",
                         "GearAffixQuality_v1_2_17"
                     })
            {
                addMigrationId.Invoke(ledger, new object[] { migrationId });
            }

            var countsField = GameDataType.GetField("CauldronCardCounts");
            Assert.IsNotNull(countsField);
            var counts = countsField.GetValue(source) as IDictionary;
            Assert.IsNotNull(counts);
            counts["RES:Ore"] = 750;

            var runner = FindType("Blindsided.SaveData.Migrations.SaveMigrationRunner");
            Assert.IsNotNull(runner);
            var migrate = runner.GetMethod("TryMigrate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(migrate);
            var result = migrate.Invoke(null, new[] { source, "1.4.3" });
            Assert.IsTrue((bool)result.GetType().GetProperty("Succeeded")?.GetValue(result));
            var migrated = result.GetType().GetProperty("Data")?.GetValue(result);
            Assert.IsNotNull(migrated);

            var migratedCounts = countsField.GetValue(migrated) as IDictionary;
            Assert.IsNotNull(migratedCounts);
            Assert.AreEqual(750, Convert.ToInt32(counts["RES:Ore"]),
                "Migration must not mutate the loaded source object before commit.");
            Assert.AreEqual(500, Convert.ToInt32(migratedCounts["RES:Ore"]));
            long migratedTotal = 0;
            foreach (var value in migratedCounts.Values)
                migratedTotal += Convert.ToInt64(value);
            Assert.AreEqual(750L, migratedTotal, "The backfilled migration must preserve every card.");
            var executed = result.GetType().GetProperty("AppliedIds")?.GetValue(result) as IEnumerable;
            CollectionAssert.Contains(
                executed?.Cast<object>().Select(value => value.ToString()).ToArray(),
                "CauldronOverflowRedistribution");

            var second = migrate.Invoke(null, new[] { migrated, "1.4.3" });
            Assert.IsTrue((bool)second.GetType().GetProperty("Succeeded")?.GetValue(second));
            Assert.IsFalse((bool)second.GetType().GetProperty("Changed")?.GetValue(second));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThrowingMigration_RollsBackWithoutMutatingSource()
        {
            var source = NewGameData(7f);
            GameDataType.GetField("LastGameVersion")?.SetValue(source, "0.0.0");
            var preferences = GameDataType.GetField("SavedPreferences")?.GetValue(source);
            Assert.IsNotNull(preferences);
            var foldouts = preferences.GetType().GetField("Foldouts")?.GetValue(preferences) as IDictionary;
            Assert.IsNotNull(foldouts);
            foldouts["rollback-source-marker"] = true;
            var ingotCraftAmount = preferences.GetType().GetField("IngotCraftAmount");
            Assert.IsNotNull(ingotCraftAmount);
            ingotCraftAmount.SetValue(preferences, 0d);

            var resourceEntryType = GameDataType.GetNestedType("ResourceEntry", BindingFlags.Public);
            Assert.IsNotNull(resourceEntryType);
            var resourceEntry = Activator.CreateInstance(resourceEntryType);
            resourceEntryType.GetField("Amount")?.SetValue(resourceEntry, 123.75d);
            resourceEntryType.GetField("Earned")?.SetValue(resourceEntry, true);
            resourceEntryType.GetField("Tier")?.SetValue(resourceEntry, 0);
            var resources = GameDataType.GetField("Resources")?.GetValue(source) as IDictionary;
            Assert.IsNotNull(resources);
            resources["rollback-resource-marker"] = resourceEntry;

            var buffSlots = GameDataType.GetField("BuffSlots")?.GetValue(source) as IList;
            Assert.IsNotNull(buffSlots);
            buffSlots.Clear();
            buffSlots.Add("rollback-buff-marker");

            var runnerType = FindType("Blindsided.SaveData.Migrations.SaveMigrationRunner");
            Assert.IsNotNull(runnerType);
            var probe = runnerType.GetMethod(
                "RunRollbackProbeForTests",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(probe);

            var result = probe.Invoke(null, new[] { source });
            Assert.IsNotNull(result);
            var resultType = result.GetType();
            Assert.IsFalse((bool)resultType.GetProperty("Succeeded").GetValue(result));
            Assert.IsFalse((bool)resultType.GetProperty("Changed").GetValue(result));
            Assert.AreSame(source, resultType.GetProperty("Data").GetValue(result));
            StringAssert.Contains(
                "Intentional migration rollback probe.",
                (string)resultType.GetProperty("Error").GetValue(result));
            Assert.AreEqual(7f, GetCompletion(source), 0.0001f);
            Assert.AreEqual("0.0.0", GameDataType.GetField("LastGameVersion")?.GetValue(source));
            Assert.AreSame(preferences, GameDataType.GetField("SavedPreferences")?.GetValue(source));
            Assert.IsTrue((bool)foldouts["rollback-source-marker"]);
            Assert.AreEqual(0d, (double)ingotCraftAmount.GetValue(preferences), 0d);
            Assert.AreSame(resourceEntry, resources["rollback-resource-marker"]);
            Assert.AreEqual(
                123.75d,
                (double)resourceEntryType.GetField("Amount").GetValue(resourceEntry),
                0.000001d);
            Assert.IsTrue((bool)resourceEntryType.GetField("Earned").GetValue(resourceEntry));
            Assert.AreEqual(0, resourceEntryType.GetField("Tier").GetValue(resourceEntry));
            Assert.AreEqual(1, buffSlots.Count);
            Assert.AreEqual("rollback-buff-marker", buffSlots[0]);
            var appliedMigrationIds = GameDataType.GetField("AppliedMigrationIds")?.GetValue(source) as IEnumerable;
            Assert.IsNotNull(appliedMigrationIds);
            CollectionAssert.IsEmpty(appliedMigrationIds.Cast<object>().ToArray());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SlotLoadPreparation_RetainsNonNullTreeButRevokesSaveAuthority()
        {
            var oracleType = FindType("Blindsided.Oracle");
            Assert.IsNotNull(oracleType);
            DestroyExistingOracle(oracleType);

            var go = new GameObject("Oracle_LoadLifecycleTest");
            try
            {
                var oracle = go.AddComponent(oracleType);
                var liveData = NewGameData(42f);
                var saveDataField = oracleType.GetField("saveData", BindingFlags.Public | BindingFlags.Instance);
                var slotField = oracleType.GetField(
                    "_saveDataSlot",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var prepare = oracleType.GetMethod(
                    "PrepareForSlotLoad",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(saveDataField);
                Assert.IsNotNull(slotField);
                Assert.IsNotNull(prepare);

                saveDataField.SetValue(oracle, liveData);
                slotField.SetValue(oracle, 0);
                prepare.Invoke(oracle, null);

                Assert.AreSame(liveData, saveDataField.GetValue(oracle),
                    "A pending load must retain the last live tree until a candidate is verified.");
                Assert.IsNotNull(saveDataField.GetValue(oracle));
                Assert.AreEqual(-1, slotField.GetValue(oracle),
                    "Pending data must not remain authoritative for saving.");
                Assert.IsFalse((bool)oracleType.GetProperty("HasCurrentSlotData")?.GetValue(oracle));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadedCandidate_IsNormalizedBeforeAtomicPublication()
        {
            var oracleType = FindType("Blindsided.Oracle");
            Assert.IsNotNull(oracleType);
            DestroyExistingOracle(oracleType);

            var go = new GameObject("Oracle_CandidatePublicationTest");
            try
            {
                var oracle = go.AddComponent(oracleType);
                var candidate = NewGameData(73f);
                GameDataType.GetField("Resources")?.SetValue(candidate, null);
                GameDataType.GetField("Quests")?.SetValue(candidate, null);
                GameDataType.GetField("SavedPreferences")?.SetValue(candidate, null);
                var publish = oracleType.GetMethod(
                    "PublishLoadedData",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(publish);

                publish.Invoke(oracle, new[] { candidate, (object)true });

                var published = oracleType
                    .GetField("saveData", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(oracle);
                Assert.AreSame(candidate, published);
                Assert.IsNotNull(GameDataType.GetField("Resources")?.GetValue(candidate));
                Assert.IsNotNull(GameDataType.GetField("Quests")?.GetValue(candidate));
                Assert.IsNotNull(GameDataType.GetField("SavedPreferences")?.GetValue(candidate));
                Assert.IsTrue((bool)oracleType.GetProperty("HasCurrentSlotData")?.GetValue(oracle));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedExplicitEquip_PreservesPreviousRawAndRuntimeRecords()
        {
            var oracleType = FindType("Blindsided.Oracle");
            var equipmentType = FindType("TimelessEchoes.Gear.EquipmentController");
            var gearItemType = FindType("TimelessEchoes.Gear.GearItem");
            var gearRecordType = FindType("Blindsided.SaveData.GearItemRecord");
            Assert.IsNotNull(oracleType);
            Assert.IsNotNull(equipmentType);
            Assert.IsNotNull(gearItemType);
            Assert.IsNotNull(gearRecordType);
            DestroyExistingOracle(oracleType);
            var resetEquipment = equipmentType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            resetEquipment?.Invoke(null, null);

            var oracleGo = new GameObject("Oracle_EquipmentTransactionTest");
            var equipmentGo = new GameObject("Equipment_TransactionTest");
            try
            {
                var oracle = oracleGo.AddComponent(oracleType);
                var data = NewGameData(81f);
                oracleType.GetField("saveData", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(oracle, data);
                var recordsField = GameDataType.GetField("EquipmentBySlot");
                var records = recordsField?.GetValue(data) as IDictionary;
                Assert.IsNotNull(records);
                var previousRecord = Activator.CreateInstance(gearRecordType);
                gearRecordType.GetField("slot")?.SetValue(previousRecord, "Helmet");
                gearRecordType.GetField("rarity")?.SetValue(previousRecord, "TemporarilyMissingRarity");
                records["Helmet"] = previousRecord;

                var controller = equipmentGo.AddComponent(equipmentType);
                var loadState = equipmentType.GetMethod(
                    "LoadState",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var getEquipped = equipmentType.GetMethod("GetEquipped", BindingFlags.Public | BindingFlags.Instance);
                var equip = equipmentType.GetMethod("Equip", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(loadState);
                Assert.IsNotNull(getEquipped);
                Assert.IsNotNull(equip);
                loadState.Invoke(controller, null);
                var previousRuntime = getEquipped.Invoke(controller, new object[] { "Helmet" });
                Assert.IsNotNull(previousRuntime);

                var invalidReplacement = Activator.CreateInstance(gearItemType);
                gearItemType.GetField("slot")?.SetValue(invalidReplacement, "Helmet");
                gearItemType.GetField("affixes")?.SetValue(invalidReplacement, null);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("EquipmentController: Equip failed \\(slot='Helmet'.*", RegexOptions.CultureInvariant));
                Assert.IsFalse((bool)equip.Invoke(controller, new[] { invalidReplacement }));

                var persistedRecords = recordsField.GetValue(data) as IDictionary;
                Assert.IsNotNull(persistedRecords);
                Assert.AreSame(previousRecord, persistedRecords["Helmet"],
                    "A failed replacement must not delete the last complete raw record.");
                Assert.AreSame(previousRuntime, getEquipped.Invoke(controller, new object[] { "Helmet" }),
                    "A failed replacement must roll the live equipment view back as well.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(equipmentGo);
                UnityEngine.Object.DestroyImmediate(oracleGo);
                resetEquipment?.Invoke(null, null);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedEquipmentReconstruction_PreservesPreviousRuntimeState()
        {
            var oracleType = FindType("Blindsided.Oracle");
            var equipmentType = FindType("TimelessEchoes.Gear.EquipmentController");
            var gearRecordType = FindType("Blindsided.SaveData.GearItemRecord");
            Assert.IsNotNull(oracleType);
            Assert.IsNotNull(equipmentType);
            Assert.IsNotNull(gearRecordType);
            DestroyExistingOracle(oracleType);
            var resetEquipment = equipmentType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            resetEquipment?.Invoke(null, null);

            var oracleGo = new GameObject("Oracle_EquipmentLoadTransactionTest");
            var equipmentGo = new GameObject("Equipment_LoadTransactionTest");
            try
            {
                var oracle = oracleGo.AddComponent(oracleType);
                var data = NewGameData(84f);
                oracleType.GetField("saveData", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(oracle, data);
                var records = GameDataType.GetField("EquipmentBySlot")?.GetValue(data) as IDictionary;
                Assert.IsNotNull(records);
                var record = Activator.CreateInstance(gearRecordType);
                gearRecordType.GetField("slot")?.SetValue(record, "Helmet");
                records["Helmet"] = record;

                var controller = equipmentGo.AddComponent(equipmentType);
                var loadState = equipmentType.GetMethod(
                    "LoadState",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var getEquipped = equipmentType.GetMethod("GetEquipped", BindingFlags.Public | BindingFlags.Instance);
                var slotsField = equipmentType.GetField("slots", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(loadState);
                Assert.IsNotNull(getEquipped);
                Assert.IsNotNull(slotsField);

                loadState.Invoke(controller, null);
                var previousRuntime = getEquipped.Invoke(controller, new object[] { "Helmet" });
                Assert.IsNotNull(previousRuntime);

                slotsField.SetValue(controller, null);
                var exception = Assert.Throws<TargetInvocationException>(() => loadState.Invoke(controller, null));
                Assert.IsInstanceOf<InvalidOperationException>(exception?.InnerException);
                Assert.AreSame(previousRuntime, getEquipped.Invoke(controller, new object[] { "Helmet" }),
                    "A failed reconstruction must not publish a partial or empty runtime equipment view.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(equipmentGo);
                UnityEngine.Object.DestroyImmediate(oracleGo);
                resetEquipment?.Invoke(null, null);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeContributors_PreserveTemporarilyUnknownEnemyAndTaskRecords()
        {
            var oracleType = FindType("Blindsided.Oracle");
            var enemyTrackerType = FindType("TimelessEchoes.Stats.EnemyKillTracker");
            var gameplayTrackerType = FindType("TimelessEchoes.Stats.GameplayStatTracker");
            Assert.IsNotNull(oracleType);
            Assert.IsNotNull(enemyTrackerType);
            Assert.IsNotNull(gameplayTrackerType);
            DestroyExistingOracle(oracleType);
            var resetEnemy = enemyTrackerType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            var resetGameplay = gameplayTrackerType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            resetEnemy?.Invoke(null, null);
            resetGameplay?.Invoke(null, null);

            var oracleGo = new GameObject("Oracle_UnknownProgressionTest");
            var enemyGo = new GameObject("EnemyTracker_UnknownProgressionTest");
            var gameplayGo = new GameObject("GameplayTracker_UnknownProgressionTest");
            try
            {
                var oracle = oracleGo.AddComponent(oracleType);
                var data = NewGameData(82f);
                oracleType.GetField("saveData", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(oracle, data);

                var enemyKillsField = GameDataType.GetField("EnemyKills");
                var enemyKills = enemyKillsField?.GetValue(data) as IDictionary;
                Assert.IsNotNull(enemyKills);
                enemyKills["__temporarily_missing_enemy__"] = 1234d;

                var taskRecordsField = GameDataType.GetField("TaskRecords");
                var taskRecords = taskRecordsField?.GetValue(data) as IDictionary;
                Assert.IsNotNull(taskRecords);
                var taskRecordType = taskRecords.GetType().GetGenericArguments()[1];
                var unknownTaskRecord = Activator.CreateInstance(taskRecordType);
                taskRecords[int.MinValue] = unknownTaskRecord;

                var enemyTracker = enemyGo.AddComponent(enemyTrackerType);
                var gameplayTracker = gameplayGo.AddComponent(gameplayTrackerType);
                enemyTrackerType.GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(enemyTracker, null);
                gameplayTrackerType.GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(gameplayTracker, null);

                var savedEnemyKills = enemyKillsField.GetValue(data) as IDictionary;
                var savedTaskRecords = taskRecordsField.GetValue(data) as IDictionary;
                Assert.IsNotNull(savedEnemyKills);
                Assert.IsNotNull(savedTaskRecords);
                Assert.AreEqual(1234d, Convert.ToDouble(savedEnemyKills["__temporarily_missing_enemy__"]));
                Assert.AreSame(unknownTaskRecord, savedTaskRecords[int.MinValue]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameplayGo);
                UnityEngine.Object.DestroyImmediate(enemyGo);
                UnityEngine.Object.DestroyImmediate(oracleGo);
                resetGameplay?.Invoke(null, null);
                resetEnemy?.Invoke(null, null);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EquipmentSave_CanonicalizesDuplicateLegacySlotAliases()
        {
            var oracleType = FindType("Blindsided.Oracle");
            var equipmentType = FindType("TimelessEchoes.Gear.EquipmentController");
            var gearRecordType = FindType("Blindsided.SaveData.GearItemRecord");
            Assert.IsNotNull(oracleType);
            Assert.IsNotNull(equipmentType);
            Assert.IsNotNull(gearRecordType);
            DestroyExistingOracle(oracleType);
            var resetEquipment = equipmentType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            resetEquipment?.Invoke(null, null);

            var oracleGo = new GameObject("Oracle_EquipmentAliasSaveTest");
            var equipmentGo = new GameObject("Equipment_AliasSaveTest");
            try
            {
                var oracle = oracleGo.AddComponent(oracleType);
                var data = NewGameData(83f);
                oracleType.GetField("saveData", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(oracle, data);
                var recordsField = GameDataType.GetField("EquipmentBySlot");
                var records = recordsField?.GetValue(data) as IDictionary;
                Assert.IsNotNull(records);
                foreach (var key in new[] { "Helmet", "Helm" })
                {
                    var record = Activator.CreateInstance(gearRecordType);
                    gearRecordType.GetField("slot")?.SetValue(record, key);
                    records[key] = record;
                }

                var controller = equipmentGo.AddComponent(equipmentType);
                equipmentType.GetMethod("LoadState", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(controller, null);
                equipmentType.GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(controller, null);

                var saved = recordsField.GetValue(data) as IDictionary;
                Assert.IsNotNull(saved);
                Assert.IsTrue(saved.Contains("Helmet"));
                Assert.IsFalse(saved.Contains("Helm"));
                Assert.AreEqual(1, saved.Count,
                    "All aliases for a known equipment slot must collapse into one canonical record.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(equipmentGo);
                UnityEngine.Object.DestroyImmediate(oracleGo);
                resetEquipment?.Invoke(null, null);
                DestroyExistingOracle(oracleType);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedSaveContribution_CanRetryWithinTheSameFrame()
        {
            var eventHandlerType = FindType("Blindsided.EventHandler");
            Assert.IsNotNull(eventHandlerType);
            var reset = eventHandlerType.GetMethod(
                "ResetStatics",
                BindingFlags.NonPublic | BindingFlags.Static);
            var saveData = eventHandlerType.GetMethod("SaveData", BindingFlags.Public | BindingFlags.Static);
            var saveEvent = eventHandlerType.GetEvent("OnSaveData", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(reset);
            Assert.IsNotNull(saveData);
            Assert.IsNotNull(saveEvent);
            reset.Invoke(null, null);

            var calls = 0;
            Action failOnce = () =>
            {
                calls++;
                if (calls == 1)
                    throw new InvalidOperationException("Intentional first-attempt failure.");
            };
            saveEvent.AddEventHandler(null, failOnce);
            try
            {
                var first = Assert.Throws<TargetInvocationException>(() =>
                    saveData.Invoke(null, new object[] { false }));
                Assert.IsInstanceOf<AggregateException>(first?.InnerException);
                saveData.Invoke(null, new object[] { false });
                Assert.AreEqual(2, calls,
                    "A rejected contribution must not consume the frame's debounce slot.");
            }
            finally
            {
                saveEvent.RemoveEventHandler(null, failOnce);
                reset.Invoke(null, null);
            }

            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator EditorPlayAndBuildBootstrapThroughLoadingScene()
        {
            const string loadingScenePath = "Assets/Scenes/Loading.unity";
            Assert.AreEqual(
                loadingScenePath,
                UnityEditor.AssetDatabase.GetAssetPath(
                    UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene),
                "Editor Play must use the same verified-save entry scene as a player build.");

            var firstEnabledScene = UnityEditor.EditorBuildSettings.scenes
                .FirstOrDefault(scene => scene.enabled);
            Assert.IsNotNull(firstEnabledScene);
            Assert.AreEqual(loadingScenePath, firstEnabledScene.path);
            yield return null;
        }
#endif

        private static void DestroyExistingOracle(Type oracleType)
        {
            var existing = oracleType
                .GetField("oracle", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as Component;
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }
}
#endif
