using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Blindsided.SaveData
{
    public sealed partial class SaveManager
    {
        public sealed class RecoveryCandidate
        {
            internal string Root, Slot, Path, ViewHash, FileHash;
            internal RecoveryCandidate() { }
            public Guid WriteId { get; internal set; }
            public long Generation { get; internal set; }
            public DateTime SavedAtUtc { get; internal set; }
            public string CharacterName { get; internal set; }
            public double PlayTime { get; internal set; }
            public float Completion { get; internal set; }
        }

        public Task<IReadOnlyList<RecoveryCandidate>> GetRecoveryCandidatesAsync(string slotName)
        {
            ValidateSlotName(slotName);
            var root = GetRootPath();
            return Task.Run<IReadOnlyList<RecoveryCandidate>>(() =>
            {
                var viewHash = RecoveryViewHash(root, slotName);
                if (LoadSnapshot(root, slotName, CancellationToken.None).Status != SaveLoadStatus.Conflict)
                    return Array.Empty<RecoveryCandidate>();
                var state = ReadSlotState(root, slotName);
                var reads = new List<GenerationReadCandidate>();
                foreach (var candidate in GetGenerationCandidates(System.IO.Path.Combine(root, "Saves", slotName)))
                {
                    // Superseded lineages stay on disk and may have been written by a newer build.
                    // Match normal loading: inspect their envelopes without interpreting their payloads.
                    var header = ReadSnapshot(candidate.Path, candidate.Generation, deserialize: false, verifyPayload: false);
                    if (header.State == SnapshotReadState.Valid && state.Record != null &&
                        state.Record.LineageId != header.LineageId)
                        continue;
                    var read = ReadSnapshot(candidate.Path, candidate.Generation, deserialize: true);
                    if (read.State == SnapshotReadState.Unsupported || read.State == SnapshotReadState.Unavailable)
                        return Array.Empty<RecoveryCandidate>();
                    if (read.State == SnapshotReadState.Valid && read.IntegrityVerified &&
                        read.WriteId == candidate.WriteId &&
                        (state.Record == null || state.Record.LineageId == read.LineageId))
                        reads.Add(new GenerationReadCandidate(candidate, read));
                }
                var choices = new List<RecoveryCandidate>();
                foreach (var lineage in reads.GroupBy(r => r.Read.LineageId))
                {
                    if (!TryGetSnapshotHeads(lineage.ToList(), out var heads, out _)) continue;
                    foreach (var head in heads)
                        choices.Add(new RecoveryCandidate
                        {
                            Root = root, Slot = slotName, Path = head.Candidate.Path,
                            ViewHash = viewHash, FileHash = HashFile(head.Candidate.Path),
                            WriteId = head.Read.WriteId, Generation = head.Read.Generation,
                            SavedAtUtc = head.Read.TimestampUtc,
                            CharacterName = head.Read.Data.Character?.Name,
                            PlayTime = head.Read.Data.PlayTime,
                            Completion = head.Read.Data.CompletionPercentage
                        });
                }
                if (viewHash != RecoveryViewHash(root, slotName)) return Array.Empty<RecoveryCandidate>();
                return choices.OrderByDescending(c => c.SavedAtUtc).ToArray();
            });
        }

        /// <summary>Explicitly preserves one verified branch as a new authority; never deletes the originals.</summary>
        public async Task<SaveWriteResult> RecoverSnapshotAsync(RecoveryCandidate choice, CancellationToken ct = default)
        {
            if (choice == null) throw new ArgumentNullException(nameof(choice));
            var root = GetRootPath();
            var buildId = Application.version ?? string.Empty;
            var epoch = ReserveAuthorityEpoch(choice.Slot, changesAuthority: true);
            try { await writeGate.WaitAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return new SaveWriteResult(SaveWriteStatus.Cancelled, choice.Slot); }
            try
            {
                return await Task.Run(() =>
                {
                    if (!string.Equals(root, choice.Root, StringComparison.Ordinal) || !RecoveryViewMatches(choice))
                        return new SaveWriteResult(SaveWriteStatus.ReloadRequired, choice.Slot,
                            error: "The available saves changed. Refresh the recovery choices.", recoveryStatus: SaveLoadStatus.Conflict);
                    var read = ReadSnapshot(choice.Path, choice.Generation, deserialize: true);
                    var bytes = File.ReadAllBytes(choice.Path);
                    if (read.State != SnapshotReadState.Valid || !read.IntegrityVerified || read.Data == null ||
                        read.WriteId != choice.WriteId || Convert.ToBase64String(ComputeSha256(bytes)) != choice.FileHash)
                        return new SaveWriteResult(SaveWriteStatus.ReloadRequired, choice.Slot,
                            error: "The selected save changed or failed verification.", recoveryStatus: SaveLoadStatus.Conflict);
                    var headerSize = BitConverter.ToUInt16(bytes, 6);
                    var payload = bytes.Skip(headerSize).ToArray();
                    var meta = JsonUtility.ToJson(new SlotMeta
                    {
                        schemaVersion = read.Data.SchemaVersion, timestampUtc = DateTime.UtcNow.ToString("o"),
                        buildId = buildId, sizeBytes = payload.Length, integrity = "sha256",
                        createdVersion = read.Data.GameVersionCreated, lastVersion = read.Data.LastGameVersion,
                        completion = read.Data.CompletionPercentage, playTime = read.Data.PlayTime,
                        dateQuit = read.Data.DateQuitString
                    });
                    // Keep the original bytes: projecting an older schema before migration can erase legacy fields.
                    var request = new SaveWriteRequest(root, choice.Slot, payload, read.Data.SchemaVersion,
                        buildId, DateTime.UtcNow, meta, true, true, null, Guid.Empty, Guid.Empty, null, epoch);
                    return WriteSnapshot(request, choice);
                }).ConfigureAwait(false);
            }
            catch (Exception ex) { return new SaveWriteResult(SaveWriteStatus.Failed, choice.Slot, error: ex.Message); }
            finally { writeGate.Release(); }
        }

        private static string HashFile(string path) => Convert.ToBase64String(ComputeSha256(File.ReadAllBytes(path)));

        private static string RecoveryViewHash(string root, string slot, string exclude = null)
        {
            var directory = System.IO.Path.Combine(root, "Saves", slot);
            var stateDirectory = System.IO.Path.Combine(root, "Saves", "SlotState");
            var files = (Directory.Exists(directory) ? Directory.GetFiles(directory, "snapshot*") : Array.Empty<string>())
                .Concat(Directory.Exists(stateDirectory) ? Directory.GetFiles(stateDirectory, slot + ".*") : Array.Empty<string>())
                .Where(p => !string.Equals(p, exclude, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.Ordinal);
            return Convert.ToBase64String(ComputeSha256(System.Text.Encoding.UTF8.GetBytes(
                string.Join("\n", files.Select(p => p + ":" + HashFile(p))))));
        }

        private static bool RecoveryViewMatches(RecoveryCandidate choice, string exclude = null) =>
            choice.ViewHash == RecoveryViewHash(choice.Root, choice.Slot, exclude);
    }
}
