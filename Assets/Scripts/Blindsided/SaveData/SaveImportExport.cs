using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Sirenix.Serialization;
using UnityEngine;
using Blindsided.SaveData.Migrations;
using TimelessEchoes.UI.Toolkit;

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
                throw new InvalidDataException(ToolkitLocalization.Text("save.export.invalid-size", "The save is too large or could not be prepared for export."));

            var envelope = BuildEnvelope(binary);
            var compressed = Deflate(envelope);
            var encodedLength = ExportPrefix.Length + ((long)compressed.Length * 4 + 2) / 3;
            if (encodedLength > MaxEncodedCharacters)
            {
                throw new InvalidDataException(
                    ToolkitLocalization.Text("save.export.too-large", "The export exceeds the limit of {0:N0} characters. Nothing was copied.", MaxEncodedCharacters));
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
                error = ToolkitLocalization.Text("save.import.empty", "Enter a save to import.");
                return false;
            }
            if (input.Length > MaxEncodedCharacters)
            {
                error = ToolkitLocalization.Text("save.import.too-long", "The import exceeds the limit of {0:N0} characters.", MaxEncodedCharacters);
                return false;
            }

            var isCurrentFormat = input.StartsWith(ExportPrefix, StringComparison.Ordinal);
            var isLegacyFormat = input.StartsWith(LegacyExportPrefix, StringComparison.Ordinal);
            var isEs3 = input.TrimStart().StartsWith("{", StringComparison.Ordinal);
            if (!isCurrentFormat && !isLegacyFormat && !isEs3)
            {
                error = ToolkitLocalization.Text("save.import.invalid-format", "This text is not a supported save export.");
                return false;
            }

            try
            {
                GameData decoded;
                try
                {
                    if (isEs3)
                    {
                        if (!LegacyEs3Adapter.TryDecode(input, out decoded, out var legacyError))
                        {
                            Debug.LogWarning("Legacy save import failed: " + legacyError);
                            error = ToolkitLocalization.Text("save.import.invalid-data", "The save data is incomplete or damaged. Paste the complete export and try again.");
                            return false;
                        }
                    }
                    else
                    {
                        var prefix = isCurrentFormat ? ExportPrefix : LegacyExportPrefix;
                        var compressed = Base64UrlDecode(input.Substring(prefix.Length));
                        var inflated = InflateBounded(compressed, isCurrentFormat ? MaxSaveBytes + 64 : MaxSaveBytes);
                        var binary = isCurrentFormat ? ReadEnvelope(inflated) : inflated;
                        decoded = SerializationUtility.DeserializeValue<GameData>(binary, DataFormat.Binary);
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidDataException ||
                                           ex is EndOfStreamException)
                {
                    // Malformed pasted data is an expected input failure. Keep unexpected
                    // migration and activation failures on the existing reporting path.
                    Debug.LogWarning("Save import data could not be decoded: " + ex.Message);
                    error = ToolkitLocalization.Text("save.import.invalid-data", "The save data is incomplete or damaged. Paste the complete export and try again.");
                    return false;
                }
                if (decoded == null)
                {
                    error = ToolkitLocalization.Text("save.import.decode-failed", "The save could not be read.");
                    return false;
                }
                if (decoded.SchemaVersion > GameData.CurrentSchemaVersion)
                {
                    error =
                        ToolkitLocalization.Text("save.import.newer-version", "This save uses format {0}; this game supports format {1}. Update the game before importing.", decoded.SchemaVersion, GameData.CurrentSchemaVersion);
                    return false;
                }

                var migration = SaveMigrationRunner.TryMigrate(decoded, Application.version);
                foreach (var warning in migration.Warnings) Debug.LogWarning(warning);
                if (!migration.Succeeded || migration.Data == null)
                {
                    Debug.LogWarning("Save import migration failed: " + migration.Error);
                    error = ToolkitLocalization.Text("save.import.migration-failed", "The save could not be upgraded to this game version. Your existing progress was not replaced.");
                    return false;
                }

                var oracle = Blindsided.Oracle.oracle;
                if (oracle == null)
                {
                    error = ToolkitLocalization.Text("save.import.not-ready", "The game is not ready to import a save. Try again after loading finishes.");
                    return false;
                }

                if (!oracle.TryCommitImportedData(
                        migration.Data,
                        out var commitError,
                        out committedToDisk))
                {
                    Debug.LogWarning("Imported save could not be activated: " + commitError);
                    error = ToolkitLocalization.Text("save.import.activation-failed", "The imported save could not be activated safely. Retry after loading finishes.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ToolkitLocalization.Text("save.import.unexpected-error", "The import could not be completed. Please try again. Technical details were recorded for support.");
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
