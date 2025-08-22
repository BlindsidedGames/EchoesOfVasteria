using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;

namespace Blindsided.SaveData
{
    public sealed class SaveManager
    {
        private static readonly Lazy<SaveManager> _instance = new Lazy<SaveManager>(() => new SaveManager());
        public static SaveManager Instance => _instance.Value;

        private readonly byte[] hmacKey;
        private readonly object fileLock = new object();
        private static string rootPathOverride;

        private SaveManager()
        {
            hmacKey = SaveSecretManager.GetOrCreateSecret();
        }

        public string CurrentSlotName { get; private set; } = "Save1";

        public void SetCurrentSlot(string slotName)
        {
            if (slotName != "Save1" && slotName != "Save2" && slotName != "Save3")
                throw new ArgumentOutOfRangeException(nameof(slotName), "Slot must be Save1, Save2, or Save3");
            CurrentSlotName = slotName;
        }

        public static void SetRootPathForTests(string path)
        {
            rootPathOverride = path;
            SaveSecretManager.SetRootPathForTests(path);
        }

        private static string GetRootPath()
        {
            return string.IsNullOrEmpty(rootPathOverride) ? Application.persistentDataPath : rootPathOverride;
        }

        public Task<bool> SaveAsync(GameData data, CancellationToken ct = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var root = GetRootPath();
            var slotDir = Path.Combine(root, "Saves", CurrentSlotName);
            var tmpPath = Path.Combine(slotDir, "snapshot.tmp");
            var finalPath = Path.Combine(slotDir, "snapshot.bin");
            var prev1Path = Path.Combine(slotDir, "snapshot.prev1.bin");
            var prev2Path = Path.Combine(slotDir, "snapshot.prev2.bin");
            var metaPath = Path.Combine(slotDir, "meta.json");

            Directory.CreateDirectory(slotDir);

            // Serialize with Odin Binary
            byte[] payload = SerializationUtility.SerializeValue(data, DataFormat.Binary);

            // Build header and HMAC
            var header = new SaveHeader
            {
                SchemaVersion = data.SchemaVersion,
                TimestampUtc = DateTime.UtcNow,
                BuildId = Application.version,
                PayloadSize = payload.Length
            };
            var headerBytes = header.ToBytes();
            var hmac = ComputeHmac(header.RawHeaderWithoutHmac, payload);
            header.HmacBase64 = Convert.ToBase64String(hmac);
            headerBytes = header.ToBytes();

            // Write to temp
            lock (fileLock)
            {
                ct.ThrowIfCancellationRequested();
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(headerBytes, 0, headerBytes.Length);
                    fs.Write(payload, 0, payload.Length);
                    fs.Flush(true);
                }
            }

            // Verify temp by re-reading
            if (!TryReadSnapshot(tmpPath, out var _))
            {
                TrySafeDelete(tmpPath);
                return Task.FromResult(false);
            }

            // Rotate backups and replace
            lock (fileLock)
            {
                try { if (File.Exists(prev2Path)) File.Delete(prev2Path); } catch { }
                try { if (File.Exists(prev1Path)) File.Move(prev1Path, prev2Path); } catch { }
                try { if (File.Exists(finalPath)) File.Move(finalPath, prev1Path); } catch { }
                try { if (File.Exists(finalPath)) File.Delete(finalPath); } catch { }
                File.Move(tmpPath, finalPath);

                var meta = new SlotMeta
                {
                    schemaVersion = header.SchemaVersion,
                    timestampUtc = header.TimestampUtc.ToString("o"),
                    buildId = header.BuildId,
                    sizeBytes = header.PayloadSize,
                    integrity = "ok"
                };
                TryWriteAllText(metaPath, JsonUtility.ToJson(meta));
            }

            return Task.FromResult(true);
        }

        public Task<(bool ok, GameData data)> LoadAsync(CancellationToken ct = default)
        {
            var root = GetRootPath();
            var slotDir = Path.Combine(root, "Saves", CurrentSlotName);
            var finalPath = Path.Combine(slotDir, "snapshot.bin");
            var prev1Path = Path.Combine(slotDir, "snapshot.prev1.bin");
            var prev2Path = Path.Combine(slotDir, "snapshot.prev2.bin");

            if (TryReadSnapshot(finalPath, out var data)) return Task.FromResult((true, data));
            if (TryReadSnapshot(prev1Path, out data)) return Task.FromResult((true, data));
            if (TryReadSnapshot(prev2Path, out data)) return Task.FromResult((true, data));
            return Task.FromResult((false, (GameData)null));
        }

        private bool TryReadSnapshot(string path, out GameData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                byte[] fileBytes = File.ReadAllBytes(path);
                if (fileBytes.Length < SaveHeader.MinimumSize) return false;

                var header = SaveHeader.FromBytes(fileBytes, out var headerSize);
                if (header == null) return false;

                var payload = new byte[fileBytes.Length - headerSize];
                Buffer.BlockCopy(fileBytes, headerSize, payload, 0, payload.Length);

                var computed = ComputeHmac(header.RawHeaderWithoutHmac, payload);
                if (!ConstantTimeEquals(computed, Convert.FromBase64String(header.HmacBase64)))
                    return false;

                data = SerializationUtility.DeserializeValue<GameData>(payload, DataFormat.Binary);
                return data != null;
            }
            catch
            {
                return false;
            }
        }

        private byte[] ComputeHmac(byte[] headerBytes, byte[] payload)
        {
            using (var hmac = new HMACSHA256(hmacKey))
            {
                hmac.TransformBlock(headerBytes, 0, headerBytes.Length, null, 0);
                hmac.TransformFinalBlock(payload, 0, payload.Length);
                return hmac.Hash;
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void TrySafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void TryWriteAllText(string path, string contents)
        {
            try { File.WriteAllText(path, contents); } catch { }
        }

        [Serializable]
        private struct SlotMeta
        {
            public int schemaVersion;
            public string timestampUtc;
            public string buildId;
            public int sizeBytes;
            public string integrity;
        }
    }

    internal sealed class SaveHeader
    {
        public int SchemaVersion;
        public DateTime TimestampUtc;
        public string BuildId;
        public int PayloadSize;
        public string HmacBase64;

        public byte[] RawHeaderWithoutHmac { get; private set; }

        public static int MinimumSize => 4 + 8 + 2 + 4 + 2;

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                using (var pre = new MemoryStream())
                using (var preBw = new BinaryWriter(pre, Encoding.UTF8, true))
                {
                    preBw.Write(SchemaVersion);
                    preBw.Write(TimestampUtc.ToBinary());
                    var build = BuildId ?? string.Empty;
                    preBw.Write((ushort)build.Length);
                    if (build.Length > 0)
                        preBw.Write(Encoding.UTF8.GetBytes(build));
                    preBw.Write(PayloadSize);
                    preBw.Flush();
                    RawHeaderWithoutHmac = pre.ToArray();
                }

                bw.Write(RawHeaderWithoutHmac);
                var hmac = HmacBase64 ?? string.Empty;
                bw.Write((ushort)hmac.Length);
                if (hmac.Length > 0)
                    bw.Write(Encoding.UTF8.GetBytes(hmac));
                bw.Flush();
                return ms.ToArray();
            }
        }

        public static SaveHeader FromBytes(byte[] buffer, out int headerSize)
        {
            headerSize = 0;
            try
            {
                using (var ms = new MemoryStream(buffer, false))
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var schema = br.ReadInt32();
                    var ts = DateTime.FromBinary(br.ReadInt64());
                    var buildLen = br.ReadUInt16();
                    var buildBytes = buildLen > 0 ? br.ReadBytes(buildLen) : Array.Empty<byte>();
                    var build = buildLen > 0 ? Encoding.UTF8.GetString(buildBytes) : string.Empty;
                    var size = br.ReadInt32();

                    var preLen = (int)ms.Position;
                    var raw = new byte[preLen];
                    Buffer.BlockCopy(buffer, 0, raw, 0, preLen);

                    var hmacLen = br.ReadUInt16();
                    var hmacBytes = hmacLen > 0 ? br.ReadBytes(hmacLen) : Array.Empty<byte>();
                    var hmac = hmacLen > 0 ? Encoding.UTF8.GetString(hmacBytes) : string.Empty;

                    headerSize = (int)ms.Position;
                    return new SaveHeader
                    {
                        SchemaVersion = schema,
                        TimestampUtc = ts,
                        BuildId = build,
                        PayloadSize = size,
                        HmacBase64 = hmac,
                        RawHeaderWithoutHmac = raw
                    };
                }
            }
            catch
            {
                return null;
            }
        }
    }
}


