using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Sirenix.Serialization;
using UnityEngine;
using Blindsided.SaveData.Migrations;

namespace Blindsided.SaveData
{
    public static class SaveImportExport
    {
        private const string LegacyExportPrefix = "TE1:";
        private const string ExportPrefix = "TE2:";
        private const int MaxEncodedCharacters = 4 * 1024 * 1024;
        private const int MaxSaveBytes = 64 * 1024 * 1024;
        private static readonly byte[] EnvelopeMagic = { (byte)'T', (byte)'E', (byte)'2', 0 };

        public static string ExportCurrentSlot(bool copyToClipboard = false)
        {
            return ExportCurrentSlot(copyToClipboard, out _);
        }

        public static string ExportCurrentSlot(bool copyToClipboard, out bool durableSaveSucceeded)
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null || !oracle.HasCurrentSlotData)
            {
                durableSaveSucceeded = false;
                return null;
            }

            string durableSaveError = null;
            try
            {
                durableSaveSucceeded = oracle.SaveToSlot(oracle.CurrentSlot);
            }
            catch (Exception ex)
            {
                durableSaveSucceeded = false;
                durableSaveError = ex.Message;
            }

            var binary = CurrentSaveCodec.Serialize(oracle.saveData);
            if (binary == null || binary.Length > MaxSaveBytes)
                throw new InvalidDataException($"Save payload size is invalid ({binary?.Length ?? 0} bytes).");

            var envelope = BuildEnvelope(binary);
            var compressed = Deflate(envelope);
            var encodedLength = ExportPrefix.Length + ((long)compressed.Length * 4 + 2) / 3;
            if (encodedLength > MaxEncodedCharacters)
            {
                throw new InvalidDataException(
                    $"Encoded export is too large to import ({encodedLength:N0} characters; " +
                    $"maximum {MaxEncodedCharacters:N0}). Nothing was copied.");
            }

            var exportString = ExportPrefix + Base64UrlEncode(compressed);
            if (!durableSaveSucceeded)
            {
                Debug.LogWarning(
                    "The durable pre-export save failed. A rescue export was created from the current " +
                    "in-memory data instead; the disk save was not updated." +
                    (string.IsNullOrEmpty(durableSaveError) ? string.Empty : $" {durableSaveError}"));
            }

            if (copyToClipboard)
                GUIUtility.systemCopyBuffer = exportString;

            return exportString;
        }

        public static bool TryImportToCurrentSlot(string input, out string error)
        {
            return TryImportToCurrentSlot(input, out error, out _);
        }

        public static bool TryImportToCurrentSlot(
            string input,
            out string error,
            out bool committedToDisk)
        {
            error = null;
            committedToDisk = false;
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Empty input";
                return false;
            }
            if (input.Length > MaxEncodedCharacters)
            {
                error = "Input is too long";
                return false;
            }

            var isCurrentFormat = input.StartsWith(ExportPrefix, StringComparison.Ordinal);
            var isLegacyFormat = input.StartsWith(LegacyExportPrefix, StringComparison.Ordinal);
            var isEs3 = input.TrimStart().StartsWith("{", StringComparison.Ordinal);
            if (!isCurrentFormat && !isLegacyFormat && !isEs3)
            {
                error = "Invalid prefix";
                return false;
            }

            try
            {
                GameData decoded;
                if (isEs3)
                {
                    if (!LegacyEs3Adapter.TryDecode(input, out decoded, out error)) return false;
                }
                else
                {
                    var prefix = isCurrentFormat ? ExportPrefix : LegacyExportPrefix;
                    var compressed = Base64UrlDecode(input.Substring(prefix.Length));
                    var inflated = InflateBounded(compressed, isCurrentFormat ? MaxSaveBytes + 64 : MaxSaveBytes);
                    var binary = isCurrentFormat ? ReadEnvelope(inflated) : inflated;
                    decoded = SerializationUtility.DeserializeValue<GameData>(binary, DataFormat.Binary);
                }
                if (decoded == null)
                {
                    error = "Failed to decode save";
                    return false;
                }
                if (decoded.SchemaVersion > GameData.CurrentSchemaVersion)
                {
                    error =
                        $"This save uses newer schema {decoded.SchemaVersion}; this build supports {GameData.CurrentSchemaVersion}.";
                    return false;
                }

                var migration = SaveMigrationRunner.TryMigrate(decoded, Application.version);
                foreach (var warning in migration.Warnings) Debug.LogWarning(warning);
                if (!migration.Succeeded || migration.Data == null)
                {
                    error = migration.Error ?? "Save migration failed";
                    return false;
                }

                var oracle = Blindsided.Oracle.oracle;
                if (oracle == null)
                {
                    error = "Oracle missing";
                    return false;
                }

                if (!oracle.TryCommitImportedData(
                        migration.Data,
                        out var commitError,
                        out committedToDisk))
                {
                    error = commitError ?? "The imported data could not be activated safely.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "SaveImportExport.Import",
                    ex,
                    $"inputLength: {input?.Length ?? 0}");
                return false;
            }
        }

        internal static bool TryWriteRescuePayload(
            byte[] binary,
            string rootPath,
            string slotName,
            out string path,
            out string error)
        {
            path = null;
            error = null;
            try
            {
                if (binary == null || binary.Length > MaxSaveBytes)
                    throw new InvalidDataException($"Rescue payload size is invalid ({binary?.Length ?? 0} bytes).");

                var compressed = Deflate(BuildEnvelope(binary));
                var encodedLength = ExportPrefix.Length + ((long)compressed.Length * 4 + 2) / 3;
                if (encodedLength > MaxEncodedCharacters)
                    throw new InvalidDataException("Rescue export exceeds the supported import size.");
                var exportString = ExportPrefix + Base64UrlEncode(compressed);

                var directory = Path.Combine(rootPath, "SaveRecovery", "UncommittedExports");
                Directory.CreateDirectory(directory);
                var stem = $"{slotName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.te2.txt";
                var destination = Path.Combine(directory, stem);
                var temporary = destination + ".tmp";
                var bytes = new UTF8Encoding(false).GetBytes(exportString);
                using (var stream = new FileStream(
                           temporary,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           64 * 1024,
                           FileOptions.SequentialScan))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                File.Move(temporary, destination);
                path = destination;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                       ex is InvalidDataException || ex is CryptographicException)
            {
                error = ex.Message;
                return false;
            }
        }

        private static byte[] BuildEnvelope(byte[] payload)
        {
            using var output = new MemoryStream(payload.Length + 64);
            using var writer = new BinaryWriter(output);
            writer.Write(EnvelopeMagic);
            writer.Write(payload.Length);
            writer.Write(ComputeSha256(payload));
            writer.Write(payload);
            writer.Flush();
            return output.ToArray();
        }

        private static byte[] ReadEnvelope(byte[] envelope)
        {
            using var input = new MemoryStream(envelope, writable: false);
            using var reader = new BinaryReader(input);
            var magic = reader.ReadBytes(EnvelopeMagic.Length);
            if (magic.Length != EnvelopeMagic.Length ||
                magic[0] != EnvelopeMagic[0] || magic[1] != EnvelopeMagic[1] ||
                magic[2] != EnvelopeMagic[2] || magic[3] != EnvelopeMagic[3])
            {
                throw new InvalidDataException("Export envelope magic is invalid.");
            }

            var payloadLength = reader.ReadInt32();
            if (payloadLength < 0 || payloadLength > MaxSaveBytes)
                throw new InvalidDataException($"Export payload length {payloadLength} is invalid.");
            var expectedChecksum = reader.ReadBytes(32);
            var payload = reader.ReadBytes(payloadLength);
            if (expectedChecksum.Length != 32 || payload.Length != payloadLength || input.Position != input.Length)
                throw new InvalidDataException("Export envelope is truncated or has trailing data.");
            if (!ByteArraysEqual(expectedChecksum, ComputeSha256(payload)))
                throw new InvalidDataException("Export checksum mismatch.");
            return payload;
        }

        private static byte[] Deflate(byte[] input)
        {
            using var output = new MemoryStream();
            using (var stream = new DeflateStream(
                       output,
                       System.IO.Compression.CompressionLevel.Optimal,
                       leaveOpen: true))
                stream.Write(input, 0, input.Length);
            return output.ToArray();
        }

        private static byte[] InflateBounded(byte[] input, int maxOutputBytes)
        {
            using var compressed = new MemoryStream(input, writable: false);
            using var stream = new DeflateStream(compressed, CompressionMode.Decompress);
            using var output = new MemoryStream(Math.Min(input.Length * 4, maxOutputBytes));
            var buffer = new byte[64 * 1024];

            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    break;
                if (output.Length + read > maxOutputBytes)
                    throw new InvalidDataException("Decompressed save exceeds the supported size limit.");
                output.Write(buffer, 0, read);
            }

            return output.ToArray();
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var normalized = value.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight((normalized.Length + 3) / 4 * 4, '=');
            return Convert.FromBase64String(normalized);
        }

        private static byte[] ComputeSha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(bytes);
        }

        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
