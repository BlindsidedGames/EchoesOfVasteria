using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;

namespace Blindsided.SaveData
{
    public enum SaveWriteStatus
    {
        Success,
        Cancelled,
        ReloadRequired,
        Failed
    }

    public sealed class SaveWriteResult
    {
        internal SaveWriteResult(
            SaveWriteStatus status,
            string slotName,
            string path = null,
            long generation = 0,
            string error = null,
            SaveLoadStatus? recoveryStatus = null)
        {
            Status = status;
            SlotName = slotName;
            Path = path;
            Generation = generation;
            Error = error;
            RecoveryStatus = recoveryStatus;
        }

        public SaveWriteStatus Status { get; }
        public string SlotName { get; }
        public string Path { get; }
        public long Generation { get; }
        public string Error { get; }
        public SaveLoadStatus? RecoveryStatus { get; }
        public bool Succeeded => Status == SaveWriteStatus.Success;
    }

    public enum SaveLoadStatus
    {
        Success,
        Recovered,
        NotFound,
        Deleted,
        Corrupt,
        Unavailable,
        Conflict,
        UnsupportedNewer,
        Cancelled,
        Failed
    }

    public sealed class SaveLoadResult
    {
        internal SaveLoadResult(
            SaveLoadStatus status,
            string slotName,
            GameData data = null,
            string sourcePath = null,
            bool integrityVerified = false,
            string diagnostic = null)
        {
            Status = status;
            SlotName = slotName;
            Data = data;
            SourcePath = sourcePath;
            IntegrityVerified = integrityVerified;
            Diagnostic = diagnostic;
        }

        public SaveLoadStatus Status { get; }
        public string SlotName { get; }
        public GameData Data { get; }
        public string SourcePath { get; }
        public bool IntegrityVerified { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Status == SaveLoadStatus.Success || Status == SaveLoadStatus.Recovered;
    }

    public sealed class SaveDeleteResult
    {
        internal SaveDeleteResult(bool succeeded, string slotName, string recoveryPath = null, string error = null)
        {
            Succeeded = succeeded;
            SlotName = slotName;
            RecoveryPath = recoveryPath;
            Error = error;
        }

        public bool Succeeded { get; }
        public string SlotName { get; }
        public string RecoveryPath { get; }
        public string Error { get; }
    }

    /// <summary>
    /// Stores verified, immutable save generations and reads both the new format and legacy snapshots.
    /// GameData capture occurs synchronously so callers never serialize a live mutable graph on a worker.
    /// Checksum, durable disk I/O, verification, publication, and retention run off the calling thread.
    /// </summary>
    public sealed class SaveManager
    {
        private const int RetainedGenerationCount = 5;
        private const int MaxGenerationDeletesPerCommit = 2;
        private const int MaxPayloadBytes = 256 * 1024 * 1024;
        private const int MaxHeaderBytes = 4096;

        private static readonly Lazy<SaveManager> InstanceFactory =
            new(() => new SaveManager());

        private static readonly Regex SlotNamePattern =
            new(@"^(?:Beta\d+)?Save[1-3]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GenerationFilePattern =
            new(
                @"^snapshot\.g(?<generation>\d{20})\.(?<writeId>[0-9a-fA-F]{32})\.(?<extension>bin|tmp)$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SlotStateFilePattern =
            new(
                @"^(?<slot>(?:Beta\d+)?Save[1-3])\.s(?<revision>\d{20})\.(?<operationId>[0-9a-fA-F]{32})\.state$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly SemaphoreSlim writeGate = new(1, 1);
        private readonly object serializationLock = new();
        private readonly object authorityEpochLock = new();
        private readonly object slotContextLock = new();
        private readonly Dictionary<string, long> authorityEpochs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CachedSlotContext> slotContexts = new(StringComparer.Ordinal);
        private static string rootPathOverride;

        private SaveManager()
        {
        }

        public static SaveManager Instance => InstanceFactory.Value;

        /// <summary>
        /// Compatibility property for editor tools. Save and load transactions capture an explicit slot name.
        /// </summary>
        public string CurrentSlotName { get; private set; } = "Save1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            rootPathOverride = null;
            if (InstanceFactory.IsValueCreated)
            {
                lock (InstanceFactory.Value.authorityEpochLock)
                    InstanceFactory.Value.authorityEpochs.Clear();
                lock (InstanceFactory.Value.slotContextLock)
                    InstanceFactory.Value.slotContexts.Clear();
            }
        }

        public void SetCurrentSlot(string slotName)
        {
            ValidateSlotName(slotName);
            CurrentSlotName = slotName;
        }

#if UNITY_INCLUDE_TESTS
        public static void SetRootPathForTests(string path)
        {
            rootPathOverride = path;
            if (!InstanceFactory.IsValueCreated)
                return;

            lock (InstanceFactory.Value.slotContextLock)
                InstanceFactory.Value.slotContexts.Clear();
            lock (InstanceFactory.Value.authorityEpochLock)
                InstanceFactory.Value.authorityEpochs.Clear();
        }
#endif

        public Task<SaveWriteResult> SaveDetailedAsync(
            GameData data,
            string slotName,
            CancellationToken ct = default)
        {
            return CaptureAndWriteAsync(data, slotName, replaceLineage: false, forceAuthorityReplacement: false, ct);
        }

        /// <summary>
        /// Commits data as a new authoritative slot lineage. Use this for an explicit reset or import,
        /// never for routine autosaves. The lineage state is published only after the snapshot verifies.
        /// </summary>
        public Task<SaveWriteResult> ReplaceSlotDetailedAsync(
            GameData data,
            string slotName,
            CancellationToken ct = default)
        {
            return CaptureAndWriteAsync(data, slotName, replaceLineage: true, forceAuthorityReplacement: false, ct);
        }

        /// <summary>
        /// Explicit recovery path for a player-approved fresh start. Existing snapshots and state files
        /// remain untouched, but a higher authority revision makes the new lineage active.
        /// </summary>
        public Task<SaveWriteResult> RecoverSlotWithFreshDataAsync(
            GameData data,
            string slotName,
            CancellationToken ct = default)
        {
            return CaptureAndWriteAsync(data, slotName, replaceLineage: true, forceAuthorityReplacement: true, ct);
        }

        private Task<SaveWriteResult> CaptureAndWriteAsync(
            GameData data,
            string slotName,
            bool replaceLineage,
            bool forceAuthorityReplacement,
            CancellationToken ct)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ValidateSlotName(slotName);

            if (data.SchemaVersion < 0 || data.SchemaVersion > GameData.CurrentSchemaVersion)
            {
                return Task.FromResult(
                    new SaveWriteResult(
                        SaveWriteStatus.Failed,
                        slotName,
                        error:
                        $"Schema {data.SchemaVersion} cannot be written by a build supporting schema {GameData.CurrentSchemaVersion}."));
            }

            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(
                    new SaveWriteResult(SaveWriteStatus.Cancelled, slotName, error: "Save cancelled before capture."));
            }

            byte[] payload;
            string metaJson;
            var root = GetRootPath();
            var context = GetCachedSlotContext(root, slotName);
            var buildId = Application.version ?? string.Empty;
            var timestampUtc = DateTime.UtcNow;

            try
            {
                // Game systems mutate the same dictionaries and lists throughout a frame. Capture the
                // complete graph here, before returning a Task, so background work sees immutable bytes.
                lock (serializationLock)
                {
                    payload = SerializationUtility.SerializeValue(data, DataFormat.Binary);
                }

                if (payload == null || payload.Length > MaxPayloadBytes)
                {
                    return Task.FromResult(
                        new SaveWriteResult(
                            SaveWriteStatus.Failed,
                            slotName,
                            error: $"Serialized payload size was invalid ({payload?.Length ?? 0} bytes)."));
                }

                metaJson = JsonUtility.ToJson(new SlotMeta
                {
                    schemaVersion = data.SchemaVersion,
                    timestampUtc = timestampUtc.ToString("o"),
                    buildId = buildId,
                    sizeBytes = payload.Length,
                    integrity = "sha256",
                    createdVersion = data.GameVersionCreated ?? string.Empty,
                    lastVersion = data.LastGameVersion ?? string.Empty,
                    completion = data.CompletionPercentage,
                    playTime = data.PlayTime,
                    dateQuit = data.DateQuitString ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    new SaveWriteResult(SaveWriteStatus.Failed, slotName, error: $"Snapshot capture failed: {ex}"));
            }

            var request = new SaveWriteRequest(
                root,
                slotName,
                payload,
                data.SchemaVersion,
                buildId,
                timestampUtc,
                metaJson,
                replaceLineage,
                forceAuthorityReplacement,
                context?.Authority,
                context?.HeadWriteId ?? Guid.Empty,
                context?.LineageId ?? Guid.Empty,
                context?.LegacySnapshot,
                ReserveAuthorityEpoch(slotName, replaceLineage));

            return WriteSnapshotAsync(request, ct);
        }

        /// <summary>
        /// Publishes a durable deletion tombstone while leaving every snapshot generation in place.
        /// Keeping the immutable generations avoids a cross-process move race and preserves recovery evidence.
        /// </summary>
        public async Task<SaveDeleteResult> DeleteSlotDetailedAsync(
            string slotName,
            CancellationToken ct = default)
        {
            ValidateSlotName(slotName);
            var root = GetRootPath();
            var authorityEpoch = ReserveAuthorityEpoch(slotName, changesAuthority: true);

            try
            {
                await writeGate.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new SaveDeleteResult(false, slotName, error: "Deletion cancelled before it started.");
            }

            try
            {
                if (!IsCurrentAuthorityEpoch(slotName, authorityEpoch))
                {
                    return new SaveDeleteResult(
                        false,
                        slotName,
                        error: "Deletion was superseded by a later slot replacement request.");
                }
                return await Task.Run(() => DeleteSlot(root, slotName)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new SaveDeleteResult(false, slotName, error: ex.ToString());
            }
            finally
            {
                writeGate.Release();
            }
        }

        public async Task<bool> SaveAsync(GameData data, CancellationToken ct = default)
        {
            var slotName = CurrentSlotName;
            var result = await SaveDetailedAsync(data, slotName, ct).ConfigureAwait(false);
            return result.Succeeded;
        }

        public Task<SaveLoadResult> LoadDetailedAsync(string slotName, CancellationToken ct = default)
        {
            ValidateSlotName(slotName);
            var root = GetRootPath();

            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(
                    new SaveLoadResult(SaveLoadStatus.Cancelled, slotName, diagnostic: "Load cancelled."));
            }

            return Task.Run(() => LoadSnapshot(root, slotName, ct), ct);
        }

        public async Task<(bool ok, GameData data)> LoadAsync(CancellationToken ct = default)
        {
            var slotName = CurrentSlotName;
            var result = await LoadDetailedAsync(slotName, ct).ConfigureAwait(false);
            return (result.Succeeded, result.Data);
        }

        private async Task<SaveWriteResult> WriteSnapshotAsync(SaveWriteRequest request, CancellationToken ct)
        {
            try
            {
                await writeGate.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Cancelled,
                    request.SlotName,
                    error: "Save cancelled before the commit started.");
            }

            try
            {
                // Once a commit starts it runs to completion. Cancelling midway would deliberately leave
                // a partial temp file; a process crash is already handled by the same recovery path.
                return await Task.Run(() => WriteSnapshot(request)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new SaveWriteResult(SaveWriteStatus.Failed, request.SlotName, error: ex.ToString());
            }
            finally
            {
                writeGate.Release();
            }
        }

        private SaveWriteResult WriteSnapshot(SaveWriteRequest request)
        {
            if (!IsCurrentAuthorityEpoch(request.SlotName, request.AuthorityEpoch))
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Cancelled,
                    request.SlotName,
                    error: "This captured snapshot was superseded by a later delete or replacement request.");
            }

            var slotDir = Path.Combine(request.RootPath, "Saves", request.SlotName);
            if (request.ForceAuthorityReplacement &&
                (!TryArchiveUnrecognizedSlotStateFiles(
                     request.RootPath,
                     request.SlotName,
                     out var archiveError) ||
                 !TryArchiveUnrecognizedSnapshotFiles(
                     request.RootPath,
                     request.SlotName,
                     out archiveError)))
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Failed,
                    request.SlotName,
                    error: archiveError);
            }

            var stateRead = ReadSlotState(request.RootPath, request.SlotName);
            if (stateRead.State == SlotStateReadState.Unavailable)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Failed,
                    request.SlotName,
                    error: $"Slot authority state could not be read safely: {stateRead.Error}");
            }
            if (stateRead.State == SlotStateReadState.Unsupported)
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.UnsupportedNewer,
                    $"Slot authority state was written by a newer version: {stateRead.Error}");
            }
            if (!request.ForceAuthorityReplacement &&
                (stateRead.State == SlotStateReadState.Invalid ||
                 stateRead.State == SlotStateReadState.Conflict))
            {
                return CreateReloadRequiredResult(
                    request,
                    stateRead.State == SlotStateReadState.Conflict
                        ? SaveLoadStatus.Conflict
                        : SaveLoadStatus.Corrupt,
                    $"Slot authority state changed or became invalid: {stateRead.Error}");
            }

            if (!request.ReplaceLineage &&
                request.ExpectedAuthority != null &&
                !request.ExpectedAuthority.Matches(stateRead))
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    "Slot authority changed after this data was loaded. Reload before saving again.");
            }

            if (!request.ReplaceLineage &&
                request.ExpectedAuthority != null &&
                !IsCachedSlotContextCurrent(
                    request.RootPath,
                    request.SlotName,
                    request.ExpectedAuthority,
                    request.ExpectedHeadWriteId,
                    request.ExpectedLegacySnapshot))
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Cancelled,
                    request.SlotName,
                    error: "A newer in-process save already advanced this slot. Capture a fresh snapshot before saving again.");
            }

            if (!request.ReplaceLineage && stateRead.Record?.Mode == SlotStateMode.Deleted)
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Deleted,
                    "The slot is intentionally deleted and requires an explicit replacement commit.");
            }

            var hasInferredLineage = !request.ReplaceLineage &&
                                     stateRead.Record == null &&
                                     request.ExpectedLineageId != Guid.Empty &&
                                     request.ExpectedHeadWriteId != Guid.Empty;
            if (!request.ReplaceLineage && stateRead.State == SlotStateReadState.Missing &&
                !hasInferredLineage && Directory.Exists(slotDir))
            {
                if (GetGenerationCandidates(slotDir).Count > 0)
                {
                    return CreateReloadRequiredResult(
                        request,
                        SaveLoadStatus.Conflict,
                        "Modern snapshots exist without authority state. Load and verify the slot before saving it.");
                }

                if (GetLegacyCandidates(slotDir).Count > 0 &&
                    !IsLegacySnapshotCurrent(slotDir, request.ExpectedLegacySnapshot, out var legacyError))
                {
                    return CreateReloadRequiredResult(
                        request,
                        SaveLoadStatus.Conflict,
                        legacyError);
                }
            }

            // The first successful write upgrades a legacy or brand-new slot into an explicit
            // lineage. A verified modern lineage whose state file is temporarily absent keeps its
            // existing identity so late cloud state cannot turn the repair into a new save fork.
            var publishActiveState = request.ReplaceLineage || stateRead.Record == null;
            var lineageId = request.ReplaceLineage
                ? Guid.NewGuid()
                : stateRead.Record?.LineageId ??
                  (hasInferredLineage ? request.ExpectedLineageId : Guid.NewGuid());
            if (!request.ReplaceLineage &&
                stateRead.Record != null &&
                request.ExpectedHeadWriteId == Guid.Empty)
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    "The active save head is unknown. Load the slot before attempting a routine save.");
            }

            if (!request.ReplaceLineage &&
                request.ExpectedHeadWriteId != Guid.Empty &&
                !IsSnapshotHeadCurrent(
                    slotDir,
                    lineageId,
                    request.ExpectedHeadWriteId,
                    out var headError))
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    headError);
            }
            Directory.CreateDirectory(slotDir);

            var generation = GetNextGeneration(slotDir);
            var writeId = Guid.NewGuid();
            var stem = $"snapshot.g{generation:D20}.{writeId:N}";
            var tempPath = Path.Combine(slotDir, stem + ".tmp");
            var finalPath = Path.Combine(slotDir, stem + ".bin");
            var checksum = ComputeSha256(request.Payload);
            var header = new SaveHeaderV2
            {
                SchemaVersion = request.SchemaVersion,
                Generation = generation,
                TimestampUtc = request.TimestampUtc,
                BuildId = request.BuildId,
                PayloadSize = request.Payload.Length,
                PayloadSha256 = checksum,
                WriteId = writeId,
                LineageId = lineageId,
                ParentWriteId = request.ReplaceLineage ? Guid.Empty : request.ExpectedHeadWriteId
            };
            var headerBytes = header.ToBytes();

            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       64 * 1024,
                       FileOptions.SequentialScan))
            {
                stream.Write(headerBytes, 0, headerBytes.Length);
                stream.Write(request.Payload, 0, request.Payload.Length);
                stream.Flush(true);
            }

            var verification = ReadSnapshot(tempPath, generation, deserialize: false);
            if (verification.State != SnapshotReadState.Valid ||
                !verification.IntegrityVerified ||
                verification.WriteId != writeId ||
                verification.LineageId != lineageId)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Failed,
                    request.SlotName,
                    tempPath,
                    generation,
                    $"Durable temp verification failed: {verification.Error ?? "unknown verification error"}");
            }

            if (request.ExpectedLegacySnapshot != null &&
                !IsLegacySnapshotCurrent(slotDir, request.ExpectedLegacySnapshot, out var legacyCommitError))
            {
                var preservedPath = TryMoveRejectedSnapshotToRecovery(
                    request.RootPath,
                    request.SlotName,
                    tempPath);
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    legacyCommitError,
                    preservedPath ?? tempPath,
                    generation);
            }

            // The destination is unique, so a same-directory rename publishes the complete generation
            // without replacing or deleting any older recoverable save.
            File.Move(tempPath, finalPath);

            SlotStateRecord committedState = stateRead.Record;
            if (publishActiveState)
            {
                var activeState = WriteNextSlotState(
                    request.RootPath,
                    request.SlotName,
                    request.ForceAuthorityReplacement
                        ? SlotStateMode.RecoveredActive
                        : SlotStateMode.Active,
                    lineageId,
                    stateRead.RecoveryParents);
                if (!activeState.Succeeded)
                {
                    return CreateReloadRequiredResult(
                        request,
                        SaveLoadStatus.Unavailable,
                        "The replacement snapshot is verified but was not made authoritative: " + activeState.Error,
                        finalPath,
                        generation);
                }

                committedState = activeState.Record;
            }

            if (!IsSlotAuthorityUnchanged(request.RootPath, request.SlotName, committedState))
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    "The snapshot is verified, but slot authority changed before the commit could be confirmed. Reload before saving again.",
                    finalPath,
                    generation);
            }

            if (!IsSnapshotHeadCurrent(slotDir, lineageId, writeId, out var committedHeadError))
            {
                return CreateReloadRequiredResult(
                    request,
                    SaveLoadStatus.Conflict,
                    committedHeadError,
                    finalPath,
                    generation);
            }

            TryWriteMeta(Path.Combine(slotDir, "meta.json"), request.MetaJson);
            PruneOldGenerations(slotDir, lineageId);
            RememberSlotContext(request.RootPath, request.SlotName, committedState, writeId, lineageId);

            return new SaveWriteResult(SaveWriteStatus.Success, request.SlotName, finalPath, generation);
        }

        private static SaveWriteResult CreateReloadRequiredResult(
            SaveWriteRequest request,
            SaveLoadStatus recoveryStatus,
            string error,
            string preservedPath = null,
            long generation = 0)
        {
            var detail = error;
            if (string.IsNullOrEmpty(preservedPath))
            {
                if (SaveImportExport.TryWriteRescuePayload(
                        request.Payload,
                        request.RootPath,
                        request.SlotName,
                        out var rescuePath,
                        out var rescueError))
                {
                    preservedPath = rescuePath;
                    detail += $" The rejected in-memory snapshot was preserved as an importable rescue at '{rescuePath}'.";
                }
                else
                {
                    detail +=
                        " The current in-memory data is still loaded, but its automatic rescue export failed: " +
                        rescueError;
                }
            }
            else
            {
                detail += $" The rejected in-memory snapshot remains preserved at '{preservedPath}'.";
            }

            return new SaveWriteResult(
                SaveWriteStatus.ReloadRequired,
                request.SlotName,
                preservedPath,
                generation,
                detail,
                recoveryStatus);
        }

        private static string TryMoveRejectedSnapshotToRecovery(
            string root,
            string slotName,
            string snapshotPath)
        {
            try
            {
                var recoveryDirectory = Path.Combine(root, "SaveRecovery", "RejectedCommits");
                Directory.CreateDirectory(recoveryDirectory);
                var destination = Path.Combine(
                    recoveryDirectory,
                    $"{slotName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.bin");
                File.Move(snapshotPath, destination);
                return destination;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }
        }

        private SaveDeleteResult DeleteSlot(string root, string slotName)
        {
            var stateRead = ReadSlotState(root, slotName);
            if (stateRead.State == SlotStateReadState.Unavailable ||
                stateRead.State == SlotStateReadState.Unsupported ||
                stateRead.State == SlotStateReadState.Invalid ||
                stateRead.State == SlotStateReadState.Conflict)
            {
                return new SaveDeleteResult(
                    false,
                    slotName,
                    error: $"Slot authority state could not be read safely: {stateRead.Error}");
            }

            var deletedRecord = stateRead.Record;
            if (deletedRecord?.Mode != SlotStateMode.Deleted)
            {
                var deletedState = WriteNextSlotState(
                    root,
                    slotName,
                    SlotStateMode.Deleted,
                    Guid.Empty,
                    stateRead.RecoveryParents);
                if (!deletedState.Succeeded)
                    return new SaveDeleteResult(false, slotName, error: deletedState.Error);
                deletedRecord = deletedState.Record;
            }

            var slotDir = Path.Combine(root, "Saves", slotName);
            RememberSlotContext(root, slotName, deletedRecord, Guid.Empty, Guid.Empty);

            // Do not move or erase immutable snapshots after publishing the tombstone. Another process
            // or cloud client could publish a replacement between those operations; leaving the files in
            // place makes deletion atomic at the authority layer and keeps every generation recoverable.
            return new SaveDeleteResult(
                true,
                slotName,
                Directory.Exists(slotDir) ? slotDir : null);
        }

        private SaveLoadResult LoadSnapshot(string root, string slotName, CancellationToken ct)
        {
            try
            {
                var slotState = ReadSlotState(root, slotName);
                switch (slotState.State)
                {
                    case SlotStateReadState.Unavailable:
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            diagnostic: slotState.Error);
                    case SlotStateReadState.Invalid:
                        return new SaveLoadResult(
                            SaveLoadStatus.Corrupt,
                            slotName,
                            diagnostic: slotState.Error);
                    case SlotStateReadState.Unsupported:
                        return new SaveLoadResult(
                            SaveLoadStatus.UnsupportedNewer,
                            slotName,
                            diagnostic: slotState.Error);
                    case SlotStateReadState.Conflict:
                        return new SaveLoadResult(
                            SaveLoadStatus.Conflict,
                            slotName,
                            diagnostic: slotState.Error);
                }

                if (slotState.Record?.Mode == SlotStateMode.Deleted)
                {
                    RememberSlotContext(root, slotName, slotState.Record, Guid.Empty, Guid.Empty);
                    return new SaveLoadResult(
                        SaveLoadStatus.Deleted,
                        slotName,
                        diagnostic: "The slot has an authoritative deletion tombstone.");
                }

                var requiredLineage = slotState.Record?.LineageId ?? Guid.Empty;
                var slotDir = Path.Combine(root, "Saves", slotName);
                if (!Directory.Exists(slotDir))
                {
                    if (slotState.Record == null)
                        RememberSlotContext(root, slotName, null, Guid.Empty, Guid.Empty);
                    return slotState.Record == null
                        ? new SaveLoadResult(SaveLoadStatus.NotFound, slotName)
                        : new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            diagnostic: "The active slot state exists, but its snapshot directory is not available yet.");
                }

                ct.ThrowIfCancellationRequested();

                var generationCandidates = GetGenerationCandidates(slotDir);
                var legacyCandidates = GetLegacyCandidates(slotDir);
                var diagnostics = new List<string>();
                var snapshotFiles = Directory.GetFiles(slotDir, "snapshot*", SearchOption.TopDirectoryOnly);
                var inspectedAny = snapshotFiles.Length > 0;
                var recognizedPaths = new HashSet<string>(
                    generationCandidates.Select(candidate => candidate.Path)
                        .Concat(legacyCandidates.Select(candidate => candidate.Path)),
                    StringComparer.OrdinalIgnoreCase);
                var unrecognizedPaths = snapshotFiles
                    .Where(path => !recognizedPaths.Contains(path))
                    .ToList();
                if (unrecognizedPaths.Count > 0)
                {
                    foreach (var path in unrecognizedPaths)
                        diagnostics.Add($"{Path.GetFileName(path)}: unrecognized save filename");
                    return new SaveLoadResult(
                        SaveLoadStatus.Corrupt,
                        slotName,
                        diagnostic: BuildDiagnostic(diagnostics));
                }

                var generationReads = new List<GenerationReadCandidate>(generationCandidates.Count);
                foreach (var candidate in generationCandidates)
                {
                    ct.ThrowIfCancellationRequested();
                    var headerRead = ReadSnapshot(
                        candidate.Path,
                        candidate.Generation,
                        deserialize: false,
                        verifyPayload: false);
                    var read = headerRead.State == SnapshotReadState.Valid &&
                               slotState.Record != null &&
                               headerRead.LineageId != requiredLineage
                        ? headerRead
                        : ReadSnapshot(candidate.Path, candidate.Generation, deserialize: false);
                    if (read.State == SnapshotReadState.Valid && read.WriteId != candidate.WriteId)
                    {
                        read = SnapshotReadResult.Invalid(
                            $"Filename write ID {candidate.WriteId:N} does not match header write ID {read.WriteId:N}.");
                    }
                    generationReads.Add(new GenerationReadCandidate(
                        candidate,
                        read));
                }

                var unavailable = generationReads.FirstOrDefault(item => item.Read.State == SnapshotReadState.Unavailable);
                if (unavailable.Read != null)
                {
                    return new SaveLoadResult(
                        SaveLoadStatus.Unavailable,
                        slotName,
                        sourcePath: unavailable.Candidate.Path,
                        diagnostic: unavailable.Read.Error);
                }

                var unsupported = generationReads.FirstOrDefault(item => item.Read.State == SnapshotReadState.Unsupported);
                if (unsupported.Read != null)
                {
                    return new SaveLoadResult(
                        SaveLoadStatus.UnsupportedNewer,
                        slotName,
                        sourcePath: unsupported.Candidate.Path,
                        integrityVerified: unsupported.Read.IntegrityVerified,
                        diagnostic: unsupported.Read.Error);
                }

                foreach (var item in generationReads.Where(item => item.Read.State == SnapshotReadState.Invalid))
                    diagnostics.Add($"{Path.GetFileName(item.Candidate.Path)}: {item.Read.Error}");

                if (slotState.Record == null)
                {
                    var modernLineages = generationReads
                        .Where(item => item.Read.State == SnapshotReadState.Valid &&
                                       item.Read.LineageId != Guid.Empty)
                        .GroupBy(item => item.Read.LineageId)
                        .ToList();
                    if (modernLineages.Count > 0)
                    {
                        var orphanHeads = new List<GenerationReadCandidate>();
                        foreach (var lineage in modernLineages)
                        {
                            if (!TryGetSnapshotHeads(lineage.ToList(), out var lineageHeads, out var ancestryError))
                            {
                                return new SaveLoadResult(
                                    SaveLoadStatus.Corrupt,
                                    slotName,
                                    diagnostic: ancestryError);
                            }
                            orphanHeads.AddRange(lineageHeads);
                        }

                        if (orphanHeads.Count != 1)
                        {
                            return new SaveLoadResult(
                                SaveLoadStatus.Conflict,
                                slotName,
                                diagnostic:
                                "Multiple verified modern save heads exist without their authority state. " +
                                "No lineage was selected or deleted, even where payloads happen to match.");
                        }

                        var inferred = orphanHeads
                            .OrderByDescending(item => item.Candidate.Generation)
                            .ThenByDescending(item => item.Read.TimestampUtc)
                            .ThenBy(item => item.Candidate.IsTemporary)
                            .ThenByDescending(item => item.Read.WriteId)
                            .First();
                        requiredLineage = inferred.Read.LineageId;
                        diagnostics.Add(
                            "Slot authority state is missing; recovered the newest verified modern lineage");
                    }
                }

                foreach (var item in generationReads.Where(item =>
                             item.Read.State == SnapshotReadState.Valid &&
                             item.Read.LineageId != requiredLineage))
                {
                    diagnostics.Add(
                        $"{Path.GetFileName(item.Candidate.Path)}: belongs to a superseded save lineage");
                }

                var matching = generationReads
                    .Where(item => item.Read.State == SnapshotReadState.Valid &&
                                   item.Read.LineageId == requiredLineage)
                    .ToList();
                if (matching.Count > 0)
                {
                    if (!TryGetSnapshotHeads(matching, out var heads, out var ancestryError))
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Corrupt,
                            slotName,
                            diagnostic: ancestryError);
                    }

                    if (heads.Count != 1)
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Conflict,
                            slotName,
                            diagnostic:
                            "Multiple valid save branches were found. No branch was selected or deleted: " +
                            string.Join(", ", heads.Select(item => Path.GetFileName(item.Candidate.Path))));
                    }

                    var chosen = heads
                        .OrderByDescending(item => item.Candidate.Generation)
                        .ThenByDescending(item => item.Read.TimestampUtc)
                        .ThenBy(item => item.Candidate.IsTemporary)
                        .ThenByDescending(item => item.Read.WriteId)
                        .First();
                    var detailed = ReadSnapshot(
                        chosen.Candidate.Path,
                        chosen.Candidate.Generation,
                        deserialize: true);
                    if (detailed.State == SnapshotReadState.Unavailable)
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            sourcePath: chosen.Candidate.Path,
                            diagnostic: detailed.Error);
                    }
                    if (detailed.State != SnapshotReadState.Valid || detailed.Data == null)
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Corrupt,
                            slotName,
                            sourcePath: chosen.Candidate.Path,
                            diagnostic: detailed.Error ?? "The selected save head could not be deserialized.");
                    }
                    if (!SnapshotIdentityMatches(chosen.Read, detailed))
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            sourcePath: chosen.Candidate.Path,
                            diagnostic:
                            "The selected snapshot changed while it was being loaded. Retrying is safe; no data was written.");
                    }
                    if (!IsSnapshotHeadCurrent(
                            slotDir,
                            detailed.LineageId,
                            detailed.WriteId,
                            out var currentHeadError))
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            sourcePath: chosen.Candidate.Path,
                            diagnostic: currentHeadError);
                    }
                    if (!IsSlotAuthorityUnchanged(root, slotName, slotState.Record))
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            diagnostic: "Slot authority changed while the snapshot was being loaded; retrying is safe.");
                    }

                    RememberSlotContext(
                        root,
                        slotName,
                        slotState.Record,
                        detailed.WriteId,
                        detailed.LineageId);
                    // A corrupt generation at or beyond the selected head means we fell back and
                    // need one repair commit. Once that verified descendant has a greater generation,
                    // the retained corrupt file is historical evidence and must not trigger another
                    // rewrite on every launch.
                    var hasUnrepairedInvalidGeneration = generationReads.Any(item =>
                        item.Read.State == SnapshotReadState.Invalid &&
                        item.Candidate.Generation >= chosen.Candidate.Generation);
                    var recovered = slotState.Record == null ||
                                    hasUnrepairedInvalidGeneration ||
                                    chosen.Candidate.IsTemporary;
                    return new SaveLoadResult(
                        recovered ? SaveLoadStatus.Recovered : SaveLoadStatus.Success,
                        slotName,
                        detailed.Data,
                        chosen.Candidate.Path,
                        detailed.IntegrityVerified,
                        recovered ? BuildDiagnostic(diagnostics) : null);
                }

                var readableLegacy = new List<LegacyReadCandidate>();

                foreach (var candidate in legacyCandidates)
                {
                    ct.ThrowIfCancellationRequested();
                    var read = ReadSnapshot(candidate.Path, expectedGeneration: null, deserialize: true);
                    if (read.State == SnapshotReadState.Unavailable)
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            sourcePath: candidate.Path,
                            diagnostic: read.Error);
                    }
                    if (read.State == SnapshotReadState.Valid || read.State == SnapshotReadState.Unsupported)
                    {
                        if (requiredLineage == Guid.Empty)
                            readableLegacy.Add(new LegacyReadCandidate(candidate, read));
                        else
                            diagnostics.Add($"{Path.GetFileName(candidate.Path)}: belongs to a superseded legacy lineage");
                    }
                    else
                    {
                        diagnostics.Add($"{Path.GetFileName(candidate.Path)}: {read.Error}");
                    }
                }

                if (readableLegacy.Count > 0)
                {
                    var chosen = readableLegacy
                        // The historic writer's contract was primary, prev1, then prev2. Wall-clock
                        // timestamps are diagnostic only and can move backwards after a clock change.
                        .OrderBy(item => item.Candidate.Priority)
                        .First();

                    if (chosen.Read.State == SnapshotReadState.Unsupported)
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.UnsupportedNewer,
                            slotName,
                            sourcePath: chosen.Candidate.Path,
                            integrityVerified: chosen.Read.IntegrityVerified,
                            diagnostic: chosen.Read.Error);
                    }

                    var isPrimary = string.Equals(
                        Path.GetFileName(chosen.Candidate.Path),
                        "snapshot.bin",
                        StringComparison.OrdinalIgnoreCase);
                    var recovered = generationCandidates.Count > 0 || !isPrimary;
                    if (!IsSlotAuthorityUnchanged(root, slotName, slotState.Record))
                    {
                        return new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            diagnostic: "Slot authority changed while the legacy snapshot was being loaded; retrying is safe.");
                    }
                    RememberSlotContext(
                        root,
                        slotName,
                        slotState.Record,
                        chosen.Read.WriteId,
                        Guid.Empty,
                        LegacySnapshotToken.From(chosen.Candidate.Path, chosen.Read));
                    return new SaveLoadResult(
                        recovered ? SaveLoadStatus.Recovered : SaveLoadStatus.Success,
                        slotName,
                        chosen.Read.Data,
                        chosen.Candidate.Path,
                        chosen.Read.IntegrityVerified,
                        recovered ? BuildDiagnostic(diagnostics) : null);
                }

                if (!inspectedAny)
                {
                    if (slotState.Record == null)
                        RememberSlotContext(root, slotName, null, Guid.Empty, Guid.Empty);
                    return slotState.Record == null
                        ? new SaveLoadResult(SaveLoadStatus.NotFound, slotName)
                        : new SaveLoadResult(
                            SaveLoadStatus.Unavailable,
                            slotName,
                            diagnostic: "The active slot state has no available snapshot yet.");
                }

                return new SaveLoadResult(
                    SaveLoadStatus.Corrupt,
                    slotName,
                    diagnostic: BuildDiagnostic(diagnostics) ?? "Save files exist, but none passed validation.");
            }
            catch (OperationCanceledException)
            {
                return new SaveLoadResult(SaveLoadStatus.Cancelled, slotName, diagnostic: "Load cancelled.");
            }
            catch (Exception ex)
            {
                return new SaveLoadResult(SaveLoadStatus.Failed, slotName, diagnostic: ex.ToString());
            }
        }

        private bool IsSlotAuthorityUnchanged(string root, string slotName, SlotStateRecord initial)
        {
            var current = ReadSlotState(root, slotName);
            if (current.State == SlotStateReadState.Missing)
                return initial == null;
            if (current.State != SlotStateReadState.Valid || current.Record == null || initial == null)
                return false;
            return current.Record.Revision == initial.Revision &&
                   current.Record.OperationId == initial.OperationId &&
                   current.Record.Mode == initial.Mode &&
                   current.Record.LineageId == initial.LineageId;
        }

        private SnapshotReadResult ReadSnapshot(
            string path,
            long? expectedGeneration,
            bool deserialize,
            bool verifyPayload = true)
        {
            try
            {
                using var stream = OpenReadWithRetry(path);

                if (stream.Length < SaveHeader.MinimumSize ||
                    stream.Length > (long)MaxPayloadBytes + MaxHeaderBytes)
                {
                    return SnapshotReadResult.Invalid($"File size {stream.Length} is outside supported bounds.");
                }

                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                var prefix = reader.ReadBytes(SaveHeaderV2.Magic.Length);
                stream.Position = 0;

                int schemaVersion;
                int payloadSize;
                DateTime timestampUtc;
                long generation;
                Guid writeId;
                Guid lineageId;
                Guid parentWriteId;
                byte[] expectedChecksum;
                bool integrityVerified;
                bool hasAncestry;

                if (SaveHeaderV2.HasMagic(prefix))
                {
                    var headerRead = SaveHeaderV2.Read(reader, stream.Length, MaxHeaderBytes, MaxPayloadBytes);
                    if (headerRead.Unsupported)
                        return SnapshotReadResult.UnsupportedResult(headerRead.Error);
                    if (headerRead.Header == null)
                        return SnapshotReadResult.Invalid(headerRead.Error);

                    var header = headerRead.Header;
                    schemaVersion = header.SchemaVersion;
                    payloadSize = header.PayloadSize;
                    timestampUtc = header.TimestampUtc;
                    generation = header.Generation;
                    writeId = header.WriteId;
                    lineageId = header.LineageId;
                    parentWriteId = header.ParentWriteId;
                    expectedChecksum = header.PayloadSha256;
                    integrityVerified = false;
                    hasAncestry = header.HasAncestry;

                    if (expectedGeneration.HasValue && generation != expectedGeneration.Value)
                    {
                        return SnapshotReadResult.Invalid(
                            $"Filename generation {expectedGeneration.Value} does not match header generation {generation}.");
                    }
                }
                else
                {
                    var headerRead = SaveHeader.Read(reader, stream.Length, MaxHeaderBytes, MaxPayloadBytes);
                    if (headerRead.Header == null)
                        return SnapshotReadResult.Invalid(headerRead.Error);

                    var header = headerRead.Header;
                    schemaVersion = header.SchemaVersion;
                    payloadSize = header.PayloadSize;
                    timestampUtc = header.TimestampUtc;
                    generation = 0;
                    writeId = Guid.Empty;
                    lineageId = Guid.Empty;
                    parentWriteId = Guid.Empty;
                    expectedChecksum = null;
                    integrityVerified = false;
                    hasAncestry = false;
                }

                if (!verifyPayload)
                {
                    return SnapshotReadResult.ValidResult(
                        null,
                        timestampUtc,
                        false,
                        generation,
                        writeId,
                        lineageId,
                        parentWriteId,
                        expectedChecksum,
                        hasAncestry);
                }

                var payload = reader.ReadBytes(payloadSize);
                if (payload.Length != payloadSize || stream.Position != stream.Length)
                    return SnapshotReadResult.Invalid("Payload length does not exactly match the declared size.");

                var actualChecksum = ComputeSha256(payload);
                if (expectedChecksum != null)
                {
                    if (!ByteArraysEqual(expectedChecksum, actualChecksum))
                        return SnapshotReadResult.Invalid("Payload checksum mismatch.");
                    integrityVerified = true;
                }

                if (schemaVersion > GameData.CurrentSchemaVersion)
                {
                    return SnapshotReadResult.UnsupportedResult(
                        $"Save schema {schemaVersion} is newer than supported schema {GameData.CurrentSchemaVersion}.",
                        timestampUtc,
                        integrityVerified);
                }

                if (!deserialize)
                {
                    return SnapshotReadResult.ValidResult(
                        null,
                        timestampUtc,
                        integrityVerified,
                        generation,
                        writeId,
                        lineageId,
                        parentWriteId,
                        actualChecksum,
                        hasAncestry);
                }

                GameData data;
                lock (serializationLock)
                {
                    data = SerializationUtility.DeserializeValue<GameData>(payload, DataFormat.Binary);
                }

                if (data == null)
                    return SnapshotReadResult.Invalid("Odin deserialization returned null.");

                if (data.SchemaVersion > GameData.CurrentSchemaVersion)
                {
                    return SnapshotReadResult.UnsupportedResult(
                        $"Save data schema {data.SchemaVersion} is newer than supported schema {GameData.CurrentSchemaVersion}.",
                        timestampUtc,
                        integrityVerified);
                }

                if (data.SchemaVersion != schemaVersion)
                {
                    return SnapshotReadResult.Invalid(
                        $"Header schema {schemaVersion} does not match payload schema {data.SchemaVersion}.");
                }

                return SnapshotReadResult.ValidResult(
                    data,
                    timestampUtc,
                    integrityVerified,
                    generation,
                    writeId,
                    lineageId,
                    parentWriteId,
                    actualChecksum,
                    hasAncestry);
            }
            catch (SnapshotUnavailableException ex)
            {
                return SnapshotReadResult.Unavailable(ex.Message);
            }
            catch (EndOfStreamException ex)
            {
                return SnapshotReadResult.Invalid(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return SnapshotReadResult.Unavailable(ex.Message);
            }
            catch (IOException ex)
            {
                return SnapshotReadResult.Unavailable(ex.Message);
            }
            catch (Exception ex)
            {
                return SnapshotReadResult.Invalid(ex.Message);
            }
        }

        private static FileStream OpenReadWithRetry(string path)
        {
            Exception lastError = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        64 * 1024,
                        FileOptions.SequentialScan);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    lastError = ex;
                    if (attempt < 2)
                        Thread.Sleep(25 * (attempt + 1));
                }
            }

            throw new SnapshotUnavailableException(
                $"Save file is temporarily unavailable after retries: {lastError?.Message}");
        }

        private static List<GenerationCandidate> GetGenerationCandidates(string slotDir)
        {
            var candidates = new List<GenerationCandidate>();
            foreach (var path in Directory.GetFiles(slotDir, "snapshot.g*.*", SearchOption.TopDirectoryOnly))
            {
                var match = GenerationFilePattern.Match(Path.GetFileName(path));
                if (!match.Success ||
                    !long.TryParse(match.Groups["generation"].Value, out var generation) ||
                    !Guid.TryParseExact(match.Groups["writeId"].Value, "N", out var writeId))
                {
                    continue;
                }

                candidates.Add(new GenerationCandidate(
                    path,
                    generation,
                    writeId,
                    string.Equals(match.Groups["extension"].Value, "tmp", StringComparison.OrdinalIgnoreCase)));
            }

            return candidates
                .OrderByDescending(candidate => candidate.Generation)
                .ThenBy(candidate => candidate.IsTemporary)
                .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<LegacyCandidate> GetLegacyCandidates(string slotDir)
        {
            var paths = new[]
            {
                new LegacyCandidate(Path.Combine(slotDir, "snapshot.bin"), 0),
                new LegacyCandidate(Path.Combine(slotDir, "snapshot.tmp"), 1),
                new LegacyCandidate(Path.Combine(slotDir, "snapshot.prev1.bin"), 2),
                new LegacyCandidate(Path.Combine(slotDir, "snapshot.prev2.bin"), 3)
            };

            return paths.Where(candidate => File.Exists(candidate.Path)).ToList();
        }

        private bool IsLegacySnapshotCurrent(
            string slotDir,
            LegacySnapshotToken expected,
            out string error)
        {
            error = null;
            if (expected == null)
            {
                error =
                    "Legacy snapshots exist in a slot that has not been loaded and verified. " +
                    "Load the slot before saving it.";
                return false;
            }

            LegacyCandidate current = default;
            SnapshotReadResult currentRead = null;
            foreach (var candidate in GetLegacyCandidates(slotDir).OrderBy(item => item.Priority))
            {
                var read = ReadSnapshot(candidate.Path, expectedGeneration: null, deserialize: false);
                if (read.State == SnapshotReadState.Unavailable)
                {
                    error = "The loaded legacy snapshot is temporarily unavailable. Retry without replacing it.";
                    return false;
                }

                if (read.State != SnapshotReadState.Valid)
                    continue;
                current = candidate;
                currentRead = read;
                break;
            }

            if (currentRead == null || string.IsNullOrEmpty(current.Path) ||
                !string.Equals(
                    Path.GetFullPath(current.Path),
                    expected.Path,
                    StringComparison.OrdinalIgnoreCase))
            {
                error =
                    "The authoritative legacy snapshot changed after it was loaded. Reload before saving again.";
                return false;
            }

            var actual = LegacySnapshotToken.From(current.Path, currentRead);
            if (!LegacySnapshotTokensMatch(actual, expected))
            {
                error = "The loaded legacy snapshot changed after it was loaded. Reload before saving again.";
                return false;
            }

            return true;
        }

        private static long GetNextGeneration(string slotDir)
        {
            var maxGeneration = 0L;
            foreach (var path in Directory.GetFiles(slotDir, "snapshot.g*.*", SearchOption.TopDirectoryOnly))
            {
                var match = GenerationFilePattern.Match(Path.GetFileName(path));
                if (match.Success &&
                    long.TryParse(match.Groups["generation"].Value, out var generation) &&
                    generation > maxGeneration)
                {
                    maxGeneration = generation;
                }
            }

            if (maxGeneration == long.MaxValue)
                throw new InvalidOperationException("Save generation counter is exhausted.");

            return maxGeneration + 1;
        }

        private void PruneOldGenerations(string slotDir, Guid activeLineage)
        {
            try
            {
                var candidates = GetGenerationCandidates(slotDir);
                if (candidates.Count <= RetainedGenerationCount)
                    return;

                // Header checks are bounded and cheap. Full payload hashing is reserved for the
                // exact old files being considered for deletion, rather than rereading every
                // retained save after every autosave.
                var active = new List<GenerationReadCandidate>();
                foreach (var candidate in candidates)
                {
                    var read = ReadSnapshot(
                        candidate.Path,
                        candidate.Generation,
                        deserialize: false,
                        verifyPayload: false);
                    if (read.State == SnapshotReadState.Valid &&
                        read.LineageId == activeLineage)
                        active.Add(new GenerationReadCandidate(candidate, read));
                }

                if (active.Count <= RetainedGenerationCount)
                    return;

                if (!TryGetSnapshotHeads(active, out var heads, out _) || heads.Count != 1)
                    return;

                foreach (var item in active
                             .OrderByDescending(candidate => candidate.Candidate.Generation)
                             .ThenByDescending(candidate => candidate.Read.TimestampUtc)
                             .ThenBy(candidate => candidate.Candidate.IsTemporary)
                             .ThenByDescending(candidate => candidate.Read.WriteId)
                             .Skip(RetainedGenerationCount)
                             .Take(MaxGenerationDeletesPerCommit)
                             .ToList())
                {
                    try
                    {
                        var verified = ReadSnapshot(
                            item.Candidate.Path,
                            item.Candidate.Generation,
                            deserialize: false);
                        if (verified.State == SnapshotReadState.Valid &&
                            verified.IntegrityVerified &&
                            verified.LineageId == activeLineage)
                        {
                            File.Delete(item.Candidate.Path);
                        }
                    }
                    catch
                    {
                        // Retention is best-effort. Extra verified generations are safe to keep.
                    }
                }
            }
            catch
            {
                // A successful commit is never downgraded because cleanup failed.
            }
        }

        private static bool TryArchiveUnrecognizedSlotStateFiles(
            string root,
            string slotName,
            out string error)
        {
            error = null;
            var stateRoot = Path.Combine(root, "Saves", "SlotState");
            if (!Directory.Exists(stateRoot))
                return true;

            try
            {
                var unrecognized = Directory
                    .GetFiles(stateRoot, slotName + "*.state", SearchOption.TopDirectoryOnly)
                    .Where(path =>
                    {
                        var match = SlotStateFilePattern.Match(Path.GetFileName(path));
                        return !match.Success ||
                               !string.Equals(match.Groups["slot"].Value, slotName, StringComparison.Ordinal) ||
                               !long.TryParse(match.Groups["revision"].Value, out var revision) ||
                               revision <= 0 ||
                               !Guid.TryParseExact(
                                   match.Groups["operationId"].Value,
                                   "N",
                                   out var operationId) ||
                               operationId == Guid.Empty;
                    })
                    .ToList();
                if (unrecognized.Count == 0)
                    return true;

                var archiveRoot = Path.Combine(
                    root,
                    "SaveRecovery",
                    "AuthorityEvidence",
                    $"{slotName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}");
                Directory.CreateDirectory(archiveRoot);
                foreach (var path in unrecognized)
                    File.Move(path, Path.Combine(archiveRoot, Path.GetFileName(path)));
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error =
                    "Fresh recovery could not preserve malformed authority evidence in SaveRecovery. " +
                    "No snapshot was written: " + ex.Message;
                return false;
            }
        }

        private static bool TryArchiveUnrecognizedSnapshotFiles(
            string root,
            string slotName,
            out string error)
        {
            error = null;
            var slotDir = Path.Combine(root, "Saves", slotName);
            if (!Directory.Exists(slotDir))
                return true;

            try
            {
                var recognizedLegacyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "snapshot.bin",
                    "snapshot.tmp",
                    "snapshot.prev1.bin",
                    "snapshot.prev2.bin"
                };
                var unrecognized = Directory
                    .GetFiles(slotDir, "snapshot*", SearchOption.TopDirectoryOnly)
                    .Where(path =>
                    {
                        var fileName = Path.GetFileName(path);
                        return !recognizedLegacyNames.Contains(fileName) &&
                               !GenerationFilePattern.IsMatch(fileName);
                    })
                    .ToList();
                if (unrecognized.Count == 0)
                    return true;

                var archiveRoot = Path.Combine(
                    root,
                    "SaveRecovery",
                    "SnapshotEvidence",
                    $"{slotName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}");
                Directory.CreateDirectory(archiveRoot);
                foreach (var path in unrecognized)
                    File.Move(path, Path.Combine(archiveRoot, Path.GetFileName(path)));
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error =
                    "Fresh recovery could not preserve unrecognized snapshot evidence in SaveRecovery. " +
                    "No snapshot was written: " + ex.Message;
                return false;
            }
        }

        private SlotStateReadResult ReadSlotState(string root, string slotName)
        {
            var stateRoot = Path.Combine(root, "Saves", "SlotState");
            if (!Directory.Exists(stateRoot))
                return SlotStateReadResult.Missing();

            string[] slotFiles;
            try
            {
                slotFiles = Directory.GetFiles(stateRoot, slotName + "*.state", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return SlotStateReadResult.Unavailable(ex.Message);
            }

            if (slotFiles.Length == 0)
                return SlotStateReadResult.Missing();

            var candidates = new List<SlotStateCandidate>();
            var filenameErrors = new List<string>();
            foreach (var path in slotFiles)
            {
                var match = SlotStateFilePattern.Match(Path.GetFileName(path));
                if (!match.Success ||
                    !string.Equals(match.Groups["slot"].Value, slotName, StringComparison.Ordinal) ||
                    !long.TryParse(match.Groups["revision"].Value, out var revision) ||
                    revision <= 0 ||
                    !Guid.TryParseExact(match.Groups["operationId"].Value, "N", out var operationId) ||
                    operationId == Guid.Empty)
                {
                    filenameErrors.Add($"{Path.GetFileName(path)}: unrecognized slot authority filename");
                    continue;
                }

                candidates.Add(new SlotStateCandidate(path, revision, operationId));
            }

            if (candidates.Count == 0)
                return SlotStateReadResult.Invalid("Slot authority state files exist but none have valid names.");

            var recoveryParents = candidates
                .Select(candidate => candidate.OperationId)
                .Distinct()
                .ToArray();
            if (filenameErrors.Count > 0)
            {
                return SlotStateReadResult.Invalid(
                    string.Join(" | ", filenameErrors.Take(8)),
                    recoveryParents);
            }

            var valid = new List<SlotStateRecord>();
            var failures = new List<(SlotStateCandidate candidate, string error)>();

            foreach (var candidate in candidates)
            {
                var read = ReadSlotStateFile(candidate.Path);
                if (read.State == SlotStateReadState.Unavailable ||
                    read.State == SlotStateReadState.Unsupported)
                    return read;
                if (read.State != SlotStateReadState.Valid || read.Record == null)
                {
                    failures.Add((candidate, $"{Path.GetFileName(candidate.Path)}: {read.Error}"));
                    continue;
                }
                if (read.Record.Revision != candidate.Revision || read.Record.OperationId != candidate.OperationId)
                {
                    failures.Add((candidate, $"{Path.GetFileName(candidate.Path)}: filename does not match state contents"));
                    continue;
                }

                valid.Add(read.Record);
            }

            if (valid.Count == 0)
            {
                return SlotStateReadResult.Invalid(
                    failures.Count > 0
                        ? string.Join(" | ", failures.Select(item => item.error).Take(8))
                        : "No valid slot authority state remains.",
                    recoveryParents);
            }

            var duplicateOperation = valid
                .GroupBy(record => record.OperationId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateOperation != null)
            {
                return SlotStateReadResult.Invalid(
                    "Duplicate slot authority operation identifiers were found.",
                    recoveryParents);
            }

            var byOperation = valid.ToDictionary(record => record.OperationId);
            var supersededOperations = new HashSet<Guid>(
                valid.SelectMany(record => record.SupersedesOperationIds));
            var relevantFailures = failures
                .Where(item => !supersededOperations.Contains(item.candidate.OperationId))
                .Select(item => item.error)
                .ToList();
            if (relevantFailures.Count > 0)
            {
                return SlotStateReadResult.Invalid(
                    string.Join(" | ", relevantFailures.Take(8)),
                    recoveryParents);
            }

            var heads = valid
                .Where(record => !supersededOperations.Contains(record.OperationId))
                .ToList();
            if (heads.Count == 0)
            {
                return SlotStateReadResult.Conflict(
                    "Slot authority history has no current operation.",
                    recoveryParents);
            }

            // Every historical operation must be reachable from a current head and the reachable
            // graph must be acyclic. Unknown parent IDs are retained as recovery evidence, but a
            // known parent must always predate its child revision. A player-approved recovery state
            // explicitly covers every older operation it observed; that boundary quarantines a
            // malformed historical subgraph without deleting it or making competing recovery heads
            // silently agree.
            var visitState = new Dictionary<Guid, byte>();
            bool IsRecoveryBoundary(SlotStateRecord record)
            {
                if (record.Mode != SlotStateMode.RecoveredActive)
                    return false;

                var covered = new HashSet<Guid>(record.SupersedesOperationIds);
                foreach (var older in valid.Where(item => item.Revision < record.Revision))
                {
                    if (!covered.Contains(older.OperationId))
                        return false;
                }

                return record.SupersedesOperationIds.All(parentId =>
                    !byOperation.TryGetValue(parentId, out var parent) ||
                    parent.Revision < record.Revision);
            }

            bool Visit(SlotStateRecord record)
            {
                if (visitState.TryGetValue(record.OperationId, out var state))
                    return state == 2;

                visitState[record.OperationId] = 1;
                if (IsRecoveryBoundary(record))
                {
                    foreach (var older in valid.Where(item => item.Revision < record.Revision))
                        visitState[older.OperationId] = 2;
                    visitState[record.OperationId] = 2;
                    return true;
                }

                foreach (var parentId in record.SupersedesOperationIds)
                {
                    if (!byOperation.TryGetValue(parentId, out var parent))
                        continue;
                    if (parent.Revision >= record.Revision || !Visit(parent))
                        return false;
                }

                visitState[record.OperationId] = 2;
                return true;
            }

            if (heads.Any(head => !Visit(head)) || visitState.Count != valid.Count)
            {
                return SlotStateReadResult.Conflict(
                    "Slot authority history contains a disconnected or cyclic branch.",
                    recoveryParents);
            }

            var frontier = heads.Select(record => record.OperationId).ToArray();

            // Deletion wins every concurrent frontier. A later explicit replacement only wins by
            // superseding every observed head, in which case the tombstones are no longer heads.
            var deleted = heads
                .Where(record => record.Mode == SlotStateMode.Deleted)
                .OrderByDescending(record => record.Revision)
                .ThenByDescending(record => record.WrittenUtcTicks)
                .FirstOrDefault();
            if (deleted != null)
                return SlotStateReadResult.Valid(deleted, frontier);

            var activeLineages = heads
                .Select(record => record.LineageId)
                .Distinct()
                .ToList();
            if (activeLineages.Count != 1)
            {
                return SlotStateReadResult.Conflict(
                    "Cloud data contains competing active save authority branches.",
                    frontier);
            }

            var active = heads
                .OrderByDescending(record => record.Revision)
                .ThenByDescending(record => record.WrittenUtcTicks)
                .ThenByDescending(record => record.OperationId)
                .First();
            return SlotStateReadResult.Valid(active, frontier);
        }

        private SlotStateReadResult ReadSlotStateFile(string path)
        {
            try
            {
                using var stream = OpenReadWithRetry(path);
                if (stream.Length > SlotStateRecord.MaximumContainerSize)
                {
                    return SlotStateReadResult.Unsupported(
                        $"State size {stream.Length} exceeds the largest authority container supported by this build.");
                }
                if (stream.Length < SlotStateRecord.MinimumContainerSize)
                    return SlotStateReadResult.Invalid($"State size {stream.Length} is invalid.");

                var bytes = new byte[(int)stream.Length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0)
                        return SlotStateReadResult.Invalid("Slot authority state is truncated.");
                    offset += read;
                }

                if (SlotStateRecord.TryRead(bytes, out var record, out var error, out var unsupported))
                    return SlotStateReadResult.Valid(record);
                return unsupported
                    ? SlotStateReadResult.Unsupported(error)
                    : SlotStateReadResult.Invalid(error);
            }
            catch (SnapshotUnavailableException ex)
            {
                return SlotStateReadResult.Unavailable(ex.Message);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return SlotStateReadResult.Unavailable(ex.Message);
            }
            catch (Exception ex)
            {
                return SlotStateReadResult.Invalid(ex.Message);
            }
        }

        private SlotStateWriteResult WriteNextSlotState(
            string root,
            string slotName,
            SlotStateMode mode,
            Guid lineageId,
            IReadOnlyCollection<Guid> supersedesOperationIds)
        {
            try
            {
                var stateRoot = Path.Combine(root, "Saves", "SlotState");
                Directory.CreateDirectory(stateRoot);
                var revision = GetNextSlotStateRevision(stateRoot, slotName);
                var operationId = Guid.NewGuid();
                var record = new SlotStateRecord
                {
                    Revision = revision,
                    OperationId = operationId,
                    Mode = mode,
                    LineageId = lineageId,
                    SupersedesOperationIds = supersedesOperationIds?
                        .Where(parent => parent != Guid.Empty)
                        .Distinct()
                        .ToList() ?? new List<Guid>(),
                    WrittenUtcTicks = DateTime.UtcNow.Ticks
                };
                var stem = $"{slotName}.s{revision:D20}.{operationId:N}.state";
                var finalPath = Path.Combine(stateRoot, stem);
                var tempPath = finalPath + ".tmp";
                var bytes = record.ToBytes();

                using (var stream = new FileStream(
                           tempPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.SequentialScan))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                var verification = ReadSlotStateFile(tempPath);
                if (verification.State != SlotStateReadState.Valid ||
                    verification.Record == null ||
                    verification.Record.OperationId != operationId ||
                    verification.Record.Revision != revision ||
                    verification.Record.Mode != mode ||
                    verification.Record.LineageId != lineageId ||
                    !verification.Record.SupersedesOperationIds
                        .OrderBy(parent => parent)
                        .SequenceEqual(record.SupersedesOperationIds.OrderBy(parent => parent)))
                {
                    return SlotStateWriteResult.Failed(
                        $"Durable slot state verification failed: {verification.Error ?? "content mismatch"}");
                }

                File.Move(tempPath, finalPath);
                return SlotStateWriteResult.Success(record);
            }
            catch (Exception ex)
            {
                return SlotStateWriteResult.Failed(ex.ToString());
            }
        }

        private static long GetNextSlotStateRevision(string stateRoot, string slotName)
        {
            var maxRevision = 0L;
            foreach (var path in Directory.GetFiles(stateRoot, slotName + ".s*.state", SearchOption.TopDirectoryOnly))
            {
                var match = SlotStateFilePattern.Match(Path.GetFileName(path));
                if (match.Success &&
                    long.TryParse(match.Groups["revision"].Value, out var revision) &&
                    revision > maxRevision)
                {
                    maxRevision = revision;
                }
            }

            if (maxRevision == long.MaxValue)
                throw new InvalidOperationException("Slot authority revision counter is exhausted.");
            return maxRevision + 1;
        }

        private static void TryWriteMeta(string path, string json)
        {
            try
            {
                File.WriteAllText(path, json ?? string.Empty, new UTF8Encoding(false));
            }
            catch
            {
                // meta.json is a UI cache, never the authority for save recovery.
            }
        }

        private static string BuildDiagnostic(List<string> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
                return null;

            return string.Join(" | ", diagnostics.Take(8));
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

        private bool IsSnapshotHeadCurrent(
            string slotDir,
            Guid lineageId,
            Guid expectedWriteId,
            out string error)
        {
            error = null;
            if (lineageId == Guid.Empty || expectedWriteId == Guid.Empty)
            {
                error = "The expected save lineage or head identifier is missing.";
                return false;
            }
            if (!Directory.Exists(slotDir))
            {
                error = "The snapshot directory changed while the save transaction was running. Retrying is safe.";
                return false;
            }

            var active = new List<GenerationReadCandidate>();
            foreach (var candidate in GetGenerationCandidates(slotDir))
            {
                var read = ReadSnapshot(
                    candidate.Path,
                    candidate.Generation,
                    deserialize: false,
                    verifyPayload: false);
                if (read.State == SnapshotReadState.Unavailable ||
                    read.State == SnapshotReadState.Unsupported)
                {
                    error =
                        $"Snapshot files changed or became unavailable during the save transaction: {read.Error}";
                    return false;
                }
                if (read.State != SnapshotReadState.Valid ||
                    read.WriteId != candidate.WriteId ||
                    read.LineageId != lineageId)
                    continue;

                active.Add(new GenerationReadCandidate(candidate, read));
            }

            if (!TryGetSnapshotHeads(active, out var heads, out var ancestryError))
            {
                error = ancestryError ?? "The active save ancestry could not be verified.";
                return false;
            }
            while (heads.Count != 1 || heads[0].Read.WriteId != expectedWriteId)
            {
                var competingHeads = heads
                    .Where(head => head.Read.WriteId != expectedWriteId)
                    .ToList();
                var removedInvalidHead = false;
                foreach (var head in competingHeads)
                {
                    // Normal autosaves stay header-only. A competing head is exceptional, so hash
                    // just that payload before deciding it represents a real cloud/concurrent branch.
                    var verified = ReadSnapshot(
                        head.Candidate.Path,
                        head.Candidate.Generation,
                        deserialize: false);
                    if (verified.State == SnapshotReadState.Unavailable ||
                        verified.State == SnapshotReadState.Unsupported)
                    {
                        error =
                            $"A competing snapshot could not be verified safely: {verified.Error}";
                        return false;
                    }

                    if (verified.State == SnapshotReadState.Invalid)
                    {
                        active.RemoveAll(item => item.Read.WriteId == head.Read.WriteId);
                        removedInvalidHead = true;
                        continue;
                    }

                    if (!SnapshotIdentityMatches(head.Read, verified))
                    {
                        error = "A competing snapshot changed while the save transaction was being verified.";
                        return false;
                    }

                    error =
                        "The active snapshot head changed or forked while the save transaction was running. " +
                        "Reload before saving again; every branch remains preserved.";
                    return false;
                }

                if (!removedInvalidHead ||
                    !TryGetSnapshotHeads(active, out heads, out ancestryError))
                {
                    error = ancestryError ??
                            "The active snapshot head changed or forked while the save transaction was running.";
                    return false;
                }
            }

            return true;
        }

        private static bool SnapshotIdentityMatches(SnapshotReadResult expected, SnapshotReadResult actual)
        {
            return expected != null &&
                   actual != null &&
                   expected.Generation == actual.Generation &&
                   expected.WriteId == actual.WriteId &&
                   expected.LineageId == actual.LineageId &&
                   expected.ParentWriteId == actual.ParentWriteId &&
                   ByteArraysEqual(expected.PayloadSha256, actual.PayloadSha256);
        }

        private static bool TryGetSnapshotHeads(
            IReadOnlyList<GenerationReadCandidate> candidates,
            out List<GenerationReadCandidate> heads,
            out string error)
        {
            heads = new List<GenerationReadCandidate>();
            error = null;
            if (candidates == null || candidates.Count == 0)
            {
                error = "No verified save candidates were available.";
                return false;
            }

            var duplicateWriteId = candidates
                .GroupBy(item => item.Read.WriteId)
                .FirstOrDefault(group => group.Key == Guid.Empty || group.Count() > 1);
            if (duplicateWriteId != null)
            {
                error = "Save ancestry contains duplicate or empty write identifiers.";
                return false;
            }

            var ancestryCandidates = candidates.Where(item => item.Read.HasAncestry).ToList();
            if (ancestryCandidates.Count == 0)
            {
                var highestGeneration = candidates.Max(item => item.Candidate.Generation);
                heads = candidates
                    .Where(item => item.Candidate.Generation == highestGeneration)
                    .ToList();
                return true;
            }

            var ancestryByWriteId = ancestryCandidates.ToDictionary(item => item.Read.WriteId);
            var visitState = new Dictionary<Guid, byte>();
            bool Visit(GenerationReadCandidate candidate)
            {
                if (visitState.TryGetValue(candidate.Read.WriteId, out var state))
                    return state != 1;

                visitState[candidate.Read.WriteId] = 1;
                if (candidate.Read.ParentWriteId != Guid.Empty &&
                    ancestryByWriteId.TryGetValue(candidate.Read.ParentWriteId, out var parent) &&
                    !Visit(parent))
                    return false;
                visitState[candidate.Read.WriteId] = 2;
                return true;
            }

            if (ancestryCandidates.Any(candidate => !Visit(candidate)))
            {
                error = "Save ancestry contains a cycle.";
                return false;
            }

            var referencedParents = new HashSet<Guid>(
                ancestryCandidates
                    .Select(item => item.Read.ParentWriteId)
                    .Where(parent => parent != Guid.Empty));
            heads.AddRange(ancestryCandidates.Where(item => !referencedParents.Contains(item.Read.WriteId)));

            // Formats before v4 did not record parents. Their highest generation is the only
            // possible pre-ancestry head; a v4 child can still explicitly reference its write ID.
            var nonAncestry = candidates.Where(item => !item.Read.HasAncestry).ToList();
            if (nonAncestry.Count > 0)
            {
                var highestNonAncestryGeneration = nonAncestry.Max(item => item.Candidate.Generation);
                heads.AddRange(nonAncestry.Where(item =>
                    item.Candidate.Generation == highestNonAncestryGeneration &&
                    !referencedParents.Contains(item.Read.WriteId)));
            }

            if (heads.Count == 0)
            {
                error = "Save ancestry has no authoritative head.";
                return false;
            }

            return true;
        }

        private static string GetRootPath()
        {
            return string.IsNullOrEmpty(rootPathOverride) ? Application.persistentDataPath : rootPathOverride;
        }

        private CachedSlotContext GetCachedSlotContext(string root, string slotName)
        {
            lock (slotContextLock)
            {
                slotContexts.TryGetValue(GetSlotContextKey(root, slotName), out var context);
                return context;
            }
        }

        private void RememberSlotContext(
            string root,
            string slotName,
            SlotStateRecord authority,
            Guid headWriteId,
            Guid lineageId,
            LegacySnapshotToken legacySnapshot = null)
        {
            lock (slotContextLock)
            {
                slotContexts[GetSlotContextKey(root, slotName)] = new CachedSlotContext(
                    SlotAuthorityToken.FromRecord(authority),
                    headWriteId,
                    lineageId,
                    legacySnapshot);
            }
        }

        private bool IsCachedSlotContextCurrent(
            string root,
            string slotName,
            SlotAuthorityToken expectedAuthority,
            Guid expectedHeadWriteId,
            LegacySnapshotToken expectedLegacySnapshot)
        {
            lock (slotContextLock)
            {
                return slotContexts.TryGetValue(GetSlotContextKey(root, slotName), out var current) &&
                       current.HeadWriteId == expectedHeadWriteId &&
                       current.Authority.IsEquivalentTo(expectedAuthority) &&
                       LegacySnapshotTokensMatch(current.LegacySnapshot, expectedLegacySnapshot);
            }
        }

        private static bool LegacySnapshotTokensMatch(
            LegacySnapshotToken left,
            LegacySnapshotToken right)
        {
            return left == null ? right == null : left.IsEquivalentTo(right);
        }

        private static string GetSlotContextKey(string root, string slotName)
        {
            return Path.GetFullPath(root) + "\0" + slotName;
        }

        private long ReserveAuthorityEpoch(string slotName, bool changesAuthority)
        {
            lock (authorityEpochLock)
            {
                authorityEpochs.TryGetValue(slotName, out var epoch);
                if (changesAuthority)
                {
                    if (epoch == long.MaxValue)
                        throw new InvalidOperationException("In-process slot authority counter is exhausted.");
                    epoch++;
                    authorityEpochs[slotName] = epoch;
                }

                return epoch;
            }
        }

        private bool IsCurrentAuthorityEpoch(string slotName, long expectedEpoch)
        {
            lock (authorityEpochLock)
            {
                authorityEpochs.TryGetValue(slotName, out var currentEpoch);
                return currentEpoch == expectedEpoch;
            }
        }

        private static void ValidateSlotName(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName) || !SlotNamePattern.IsMatch(slotName))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotName),
                    "Slot must be Save1-3 or a Beta iteration variant such as Beta1Save2.");
            }
        }

        [Serializable]
        private struct SlotMeta
        {
            public int schemaVersion;
            public string timestampUtc;
            public string buildId;
            public int sizeBytes;
            public string integrity;
            public string createdVersion;
            public string lastVersion;
            public float completion;
            public double playTime;
            public string dateQuit;
        }

        private enum SlotStateMode : byte
        {
            Active = 1,
            Deleted = 2,
            RecoveredActive = 3
        }

        private enum SlotStateReadState
        {
            Missing,
            Valid,
            Invalid,
            Unavailable,
            Unsupported,
            Conflict
        }

        private sealed class SlotStateRecord
        {
            private const ushort FormatVersion = 2;
            private static readonly byte[] Magic = { (byte)'T', (byte)'E', (byte)'S', (byte)'2' };
            private const int ChecksumSize = 32;
            private const int FixedContentSize = 59;
            public const int MaxParentCount = 1024;
            public const int MinimumContainerSize = 8 + ChecksumSize;
            public const int MaximumContainerSize = ushort.MaxValue;
            public const int MinimumSerializedSize = FixedContentSize + ChecksumSize;
            public const int MaximumSerializedSize = FixedContentSize + MaxParentCount * 16 + ChecksumSize;

            public long Revision;
            public Guid OperationId;
            public SlotStateMode Mode;
            public Guid LineageId;
            public List<Guid> SupersedesOperationIds = new();
            public long WrittenUtcTicks;

            public byte[] ToBytes()
            {
                if (Revision <= 0 || OperationId == Guid.Empty)
                    throw new InvalidOperationException("Slot authority revision and operation ID are required.");
                if (Mode != SlotStateMode.Active && Mode != SlotStateMode.Deleted &&
                    Mode != SlotStateMode.RecoveredActive)
                    throw new InvalidOperationException("Slot authority mode is invalid.");
                if ((Mode == SlotStateMode.Active || Mode == SlotStateMode.RecoveredActive) &&
                    LineageId == Guid.Empty)
                    throw new InvalidOperationException("An active slot authority state requires a lineage ID.");
                if (Mode == SlotStateMode.Deleted && LineageId != Guid.Empty)
                    throw new InvalidOperationException("A deleted slot authority state cannot carry a lineage ID.");

                var parents = (SupersedesOperationIds ?? new List<Guid>())
                    .Where(parent => parent != Guid.Empty)
                    .Distinct()
                    .OrderBy(parent => parent)
                    .ToList();
                if (parents.Count > MaxParentCount || parents.Contains(OperationId))
                    throw new InvalidOperationException("Slot authority parent identifiers are invalid.");

                var serializedSize = checked((ushort)(FixedContentSize + parents.Count * 16 + ChecksumSize));

                using var stream = new MemoryStream(serializedSize);
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(serializedSize);
                writer.Write(Revision);
                writer.Write((byte)Mode);
                writer.Write(OperationId.ToByteArray());
                writer.Write(LineageId.ToByteArray());
                writer.Write(WrittenUtcTicks);
                writer.Write((ushort)parents.Count);
                foreach (var parent in parents)
                    writer.Write(parent.ToByteArray());
                writer.Flush();

                var content = stream.ToArray();
                if (content.Length != serializedSize - ChecksumSize)
                    throw new InvalidOperationException("Slot authority state layout is invalid.");
                writer.Write(ComputeSha256(content));
                writer.Flush();
                return stream.ToArray();
            }

            public static bool TryRead(
                byte[] bytes,
                out SlotStateRecord record,
                out string error,
                out bool unsupported)
            {
                record = null;
                error = null;
                unsupported = false;
                try
                {
                    if (bytes == null ||
                        bytes.Length < MinimumContainerSize ||
                        bytes.Length > MaximumContainerSize)
                    {
                        error = "Slot authority state size is invalid.";
                        return false;
                    }

                    var contentSize = bytes.Length - ChecksumSize;
                    var content = new byte[contentSize];
                    Buffer.BlockCopy(bytes, 0, content, 0, contentSize);
                    var checksum = ComputeSha256(content);
                    var difference = 0;
                    for (var i = 0; i < checksum.Length; i++)
                        difference |= checksum[i] ^ bytes[contentSize + i];
                    if (difference != 0)
                    {
                        error = "Slot authority state checksum mismatch.";
                        return false;
                    }

                    using var stream = new MemoryStream(content, writable: false);
                    using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                    var magic = reader.ReadBytes(Magic.Length);
                    if (!ByteArraysEqual(magic, Magic))
                    {
                        unsupported = true;
                        error = "Slot authority state uses an unrecognized checksummed container format.";
                        return false;
                    }
                    var formatVersion = reader.ReadUInt16();
                    var serializedSize = reader.ReadUInt16();
                    if (serializedSize != bytes.Length)
                    {
                        error = "Slot authority state declared size does not match its file size.";
                        return false;
                    }
                    if (formatVersion > FormatVersion)
                    {
                        unsupported = true;
                        error =
                            $"Slot authority format {formatVersion} is newer than supported format {FormatVersion}.";
                        return false;
                    }
                    if (formatVersion != FormatVersion ||
                        bytes.Length < MinimumSerializedSize ||
                        bytes.Length > MaximumSerializedSize)
                    {
                        unsupported = true;
                        error = $"Slot authority format {formatVersion} is not supported by this build.";
                        return false;
                    }

                    var revision = reader.ReadInt64();
                    var mode = (SlotStateMode)reader.ReadByte();
                    var operationId = new Guid(reader.ReadBytes(16));
                    var lineageId = new Guid(reader.ReadBytes(16));
                    var writtenUtcTicks = reader.ReadInt64();
                    var parentCount = reader.ReadUInt16();
                    if (parentCount > MaxParentCount ||
                        stream.Length - stream.Position != parentCount * 16L)
                    {
                        error = "Slot authority parent list is invalid.";
                        return false;
                    }

                    var parents = new List<Guid>(parentCount);
                    for (var index = 0; index < parentCount; index++)
                        parents.Add(new Guid(reader.ReadBytes(16)));
                    if (revision <= 0 || operationId == Guid.Empty ||
                        (mode != SlotStateMode.Active && mode != SlotStateMode.Deleted &&
                         mode != SlotStateMode.RecoveredActive) ||
                        ((mode == SlotStateMode.Active || mode == SlotStateMode.RecoveredActive) &&
                         lineageId == Guid.Empty) ||
                        (mode == SlotStateMode.Deleted && lineageId != Guid.Empty) ||
                        writtenUtcTicks <= 0 ||
                        parents.Any(parent => parent == Guid.Empty || parent == operationId) ||
                        parents.Distinct().Count() != parents.Count)
                    {
                        error = "Slot authority state fields are invalid.";
                        return false;
                    }

                    record = new SlotStateRecord
                    {
                        Revision = revision,
                        OperationId = operationId,
                        Mode = mode,
                        LineageId = lineageId,
                        SupersedesOperationIds = parents,
                        WrittenUtcTicks = writtenUtcTicks
                    };
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
        }

        private readonly struct SlotStateCandidate
        {
            public SlotStateCandidate(string path, long revision, Guid operationId)
            {
                Path = path;
                Revision = revision;
                OperationId = operationId;
            }

            public string Path { get; }
            public long Revision { get; }
            public Guid OperationId { get; }
        }

        private sealed class SlotStateReadResult
        {
            private SlotStateReadResult(
                SlotStateReadState state,
                SlotStateRecord record,
                string error,
                IReadOnlyCollection<Guid> recoveryParents = null)
            {
                State = state;
                Record = record;
                Error = error;
                RecoveryParents = recoveryParents?
                    .Where(parent => parent != Guid.Empty)
                    .Distinct()
                    .ToArray() ?? Array.Empty<Guid>();
            }

            public SlotStateReadState State { get; }
            public SlotStateRecord Record { get; }
            public string Error { get; }
            public IReadOnlyCollection<Guid> RecoveryParents { get; }

            public static SlotStateReadResult Missing() =>
                new(SlotStateReadState.Missing, null, null);

            public static SlotStateReadResult Valid(
                SlotStateRecord record,
                IReadOnlyCollection<Guid> recoveryParents = null) =>
                new(
                    SlotStateReadState.Valid,
                    record,
                    null,
                    recoveryParents ?? (record == null
                        ? Array.Empty<Guid>()
                        : new[] { record.OperationId }));

            public static SlotStateReadResult Invalid(
                string error,
                IReadOnlyCollection<Guid> recoveryParents = null) =>
                new(SlotStateReadState.Invalid, null, error, recoveryParents);

            public static SlotStateReadResult Unavailable(string error) =>
                new(SlotStateReadState.Unavailable, null, error);

            public static SlotStateReadResult Unsupported(string error) =>
                new(SlotStateReadState.Unsupported, null, error);

            public static SlotStateReadResult Conflict(
                string error,
                IReadOnlyCollection<Guid> recoveryParents = null) =>
                new(SlotStateReadState.Conflict, null, error, recoveryParents);
        }

        private sealed class SlotStateWriteResult
        {
            private SlotStateWriteResult(bool succeeded, SlotStateRecord record, string error)
            {
                Succeeded = succeeded;
                Record = record;
                Error = error;
            }

            public bool Succeeded { get; }
            public SlotStateRecord Record { get; }
            public string Error { get; }

            public static SlotStateWriteResult Success(SlotStateRecord record) => new(true, record, null);
            public static SlotStateWriteResult Failed(string error) => new(false, null, error);
        }

        private sealed class SaveWriteRequest
        {
            public SaveWriteRequest(
                string rootPath,
                string slotName,
                byte[] payload,
                int schemaVersion,
                string buildId,
                DateTime timestampUtc,
                string metaJson,
                bool replaceLineage,
                bool forceAuthorityReplacement,
                SlotAuthorityToken expectedAuthority,
                Guid expectedHeadWriteId,
                Guid expectedLineageId,
                LegacySnapshotToken expectedLegacySnapshot,
                long authorityEpoch)
            {
                RootPath = rootPath;
                SlotName = slotName;
                Payload = payload;
                SchemaVersion = schemaVersion;
                BuildId = buildId;
                TimestampUtc = timestampUtc;
                MetaJson = metaJson;
                ReplaceLineage = replaceLineage;
                ForceAuthorityReplacement = forceAuthorityReplacement;
                ExpectedAuthority = expectedAuthority;
                ExpectedHeadWriteId = expectedHeadWriteId;
                ExpectedLineageId = expectedLineageId;
                ExpectedLegacySnapshot = expectedLegacySnapshot;
                AuthorityEpoch = authorityEpoch;
            }

            public string RootPath { get; }
            public string SlotName { get; }
            public byte[] Payload { get; }
            public int SchemaVersion { get; }
            public string BuildId { get; }
            public DateTime TimestampUtc { get; }
            public string MetaJson { get; }
            public bool ReplaceLineage { get; }
            public bool ForceAuthorityReplacement { get; }
            public SlotAuthorityToken ExpectedAuthority { get; }
            public Guid ExpectedHeadWriteId { get; }
            public Guid ExpectedLineageId { get; }
            public LegacySnapshotToken ExpectedLegacySnapshot { get; }
            public long AuthorityEpoch { get; }
        }

        private sealed class CachedSlotContext
        {
            public CachedSlotContext(
                SlotAuthorityToken authority,
                Guid headWriteId,
                Guid lineageId,
                LegacySnapshotToken legacySnapshot)
            {
                Authority = authority;
                HeadWriteId = headWriteId;
                LineageId = lineageId;
                LegacySnapshot = legacySnapshot;
            }

            public SlotAuthorityToken Authority { get; }
            public Guid HeadWriteId { get; }
            public Guid LineageId { get; }
            public LegacySnapshotToken LegacySnapshot { get; }
        }

        private sealed class LegacySnapshotToken
        {
            private LegacySnapshotToken(string path, DateTime timestampUtc, byte[] payloadSha256)
            {
                Path = path;
                TimestampUtc = timestampUtc;
                PayloadSha256 = payloadSha256;
            }

            public string Path { get; }
            public DateTime TimestampUtc { get; }
            public byte[] PayloadSha256 { get; }

            public static LegacySnapshotToken From(string path, SnapshotReadResult read)
            {
                return read?.PayloadSha256 == null
                    ? null
                    : new LegacySnapshotToken(
                        System.IO.Path.GetFullPath(path),
                        read.TimestampUtc,
                        (byte[])read.PayloadSha256.Clone());
            }

            public bool IsEquivalentTo(LegacySnapshotToken other)
            {
                return other != null &&
                       string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase) &&
                       TimestampUtc == other.TimestampUtc &&
                       ByteArraysEqual(PayloadSha256, other.PayloadSha256);
            }
        }

        private sealed class SlotAuthorityToken
        {
            private SlotAuthorityToken(
                bool missing,
                long revision,
                Guid operationId,
                SlotStateMode mode,
                Guid lineageId)
            {
                Missing = missing;
                Revision = revision;
                OperationId = operationId;
                Mode = mode;
                LineageId = lineageId;
            }

            private bool Missing { get; }
            private long Revision { get; }
            private Guid OperationId { get; }
            private SlotStateMode Mode { get; }
            private Guid LineageId { get; }

            public static SlotAuthorityToken FromRecord(SlotStateRecord record)
            {
                return record == null
                    ? new SlotAuthorityToken(true, 0, Guid.Empty, default, Guid.Empty)
                    : new SlotAuthorityToken(
                        false,
                        record.Revision,
                        record.OperationId,
                        record.Mode,
                        record.LineageId);
            }

            public bool Matches(SlotStateReadResult current)
            {
                if (Missing)
                    return current != null && current.State == SlotStateReadState.Missing;

                var record = current?.State == SlotStateReadState.Valid ? current.Record : null;
                return record != null &&
                       record.Revision == Revision &&
                       record.OperationId == OperationId &&
                       record.Mode == Mode &&
                       record.LineageId == LineageId;
            }

            public bool IsEquivalentTo(SlotAuthorityToken other)
            {
                return other != null &&
                       other.Missing == Missing &&
                       other.Revision == Revision &&
                       other.OperationId == OperationId &&
                       other.Mode == Mode &&
                       other.LineageId == LineageId;
            }
        }

        private readonly struct GenerationCandidate
        {
            public GenerationCandidate(string path, long generation, Guid writeId, bool isTemporary)
            {
                Path = path;
                Generation = generation;
                WriteId = writeId;
                IsTemporary = isTemporary;
            }

            public string Path { get; }
            public long Generation { get; }
            public Guid WriteId { get; }
            public bool IsTemporary { get; }
        }

        private readonly struct LegacyCandidate
        {
            public LegacyCandidate(string path, int priority)
            {
                Path = path;
                Priority = priority;
            }

            public string Path { get; }
            public int Priority { get; }
        }

        private readonly struct LegacyReadCandidate
        {
            public LegacyReadCandidate(LegacyCandidate candidate, SnapshotReadResult read)
            {
                Candidate = candidate;
                Read = read;
            }

            public LegacyCandidate Candidate { get; }
            public SnapshotReadResult Read { get; }
        }

        private readonly struct GenerationReadCandidate
        {
            public GenerationReadCandidate(GenerationCandidate candidate, SnapshotReadResult read)
            {
                Candidate = candidate;
                Read = read;
            }

            public GenerationCandidate Candidate { get; }
            public SnapshotReadResult Read { get; }
        }

        private enum SnapshotReadState
        {
            Valid,
            Invalid,
            Unavailable,
            Unsupported
        }

        private sealed class SnapshotReadResult
        {
            private SnapshotReadResult(
                SnapshotReadState state,
                GameData data,
                DateTime timestampUtc,
                bool integrityVerified,
                long generation,
                Guid writeId,
                Guid lineageId,
                Guid parentWriteId,
                byte[] payloadSha256,
                bool hasAncestry,
                string error)
            {
                State = state;
                Data = data;
                TimestampUtc = timestampUtc;
                IntegrityVerified = integrityVerified;
                Generation = generation;
                WriteId = writeId;
                LineageId = lineageId;
                ParentWriteId = parentWriteId;
                PayloadSha256 = payloadSha256;
                HasAncestry = hasAncestry;
                Error = error;
            }

            public SnapshotReadState State { get; }
            public GameData Data { get; }
            public DateTime TimestampUtc { get; }
            public bool IntegrityVerified { get; }
            public long Generation { get; }
            public Guid WriteId { get; }
            public Guid LineageId { get; }
            public Guid ParentWriteId { get; }
            public byte[] PayloadSha256 { get; }
            public bool HasAncestry { get; }
            public string Error { get; }

            public static SnapshotReadResult ValidResult(
                GameData data,
                DateTime timestampUtc,
                bool integrityVerified,
                long generation,
                Guid writeId,
                Guid lineageId,
                Guid parentWriteId,
                byte[] payloadSha256,
                bool hasAncestry)
            {
                return new SnapshotReadResult(
                    SnapshotReadState.Valid,
                    data,
                    timestampUtc,
                    integrityVerified,
                    generation,
                    writeId,
                    lineageId,
                    parentWriteId,
                    payloadSha256,
                    hasAncestry,
                    null);
            }

            public static SnapshotReadResult Invalid(string error)
            {
                return new SnapshotReadResult(
                    SnapshotReadState.Invalid,
                    null,
                    default,
                    false,
                    0,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    false,
                    error);
            }

            public static SnapshotReadResult Unavailable(string error)
            {
                return new SnapshotReadResult(
                    SnapshotReadState.Unavailable,
                    null,
                    default,
                    false,
                    0,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    false,
                    error);
            }

            public static SnapshotReadResult UnsupportedResult(
                string error,
                DateTime timestampUtc = default,
                bool integrityVerified = false)
            {
                return new SnapshotReadResult(
                    SnapshotReadState.Unsupported,
                    null,
                    timestampUtc,
                    integrityVerified,
                    0,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    false,
                    error);
            }
        }

        private sealed class SnapshotUnavailableException : IOException
        {
            public SnapshotUnavailableException(string message) : base(message)
            {
            }
        }
    }

    internal sealed class SaveHeaderV2
    {
        private const ushort LegacyFormatVersion = 2;
        private const ushort FirstChecksummedFormatVersion = 3;
        public const ushort CurrentFormatVersion = 4;
        public static readonly byte[] Magic = { (byte)'T', (byte)'E', (byte)'V', (byte)'2' };
        private static readonly byte[] V3HeaderChecksumMarker = { (byte)'H', (byte)'3', (byte)'C', (byte)'!' };
        private static readonly byte[] HeaderChecksumMarker = { (byte)'H', (byte)'4', (byte)'C', (byte)'!' };

        public int SchemaVersion;
        public long Generation;
        public DateTime TimestampUtc;
        public string BuildId;
        public int PayloadSize;
        public byte[] PayloadSha256;
        public Guid WriteId;
        public Guid LineageId;
        public Guid ParentWriteId;
        public bool HasAncestry;

        public static bool HasMagic(byte[] bytes)
        {
            return bytes != null && bytes.Length == Magic.Length &&
                   bytes[0] == Magic[0] && bytes[1] == Magic[1] &&
                   bytes[2] == Magic[2] && bytes[3] == Magic[3];
        }

        public byte[] ToBytes()
        {
            var buildBytes = Encoding.UTF8.GetBytes(BuildId ?? string.Empty);
            if (buildBytes.Length > 512)
                throw new InvalidOperationException("Build identifier is too long for the save header.");
            if (PayloadSha256 == null || PayloadSha256.Length != 32)
                throw new InvalidOperationException("A 32-byte SHA-256 checksum is required.");

            if (WriteId == Guid.Empty || LineageId == Guid.Empty || ParentWriteId == WriteId)
                throw new InvalidOperationException("Save lineage and write identifiers are invalid.");

            var headerSize = checked((ushort)(151 + buildBytes.Length));
            using var stream = new MemoryStream(headerSize);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(Magic);
            writer.Write(CurrentFormatVersion);
            writer.Write(headerSize);
            writer.Write(SchemaVersion);
            writer.Write(Generation);
            writer.Write(TimestampUtc.ToBinary());
            writer.Write(PayloadSize);
            writer.Write((ushort)buildBytes.Length);
            writer.Write(buildBytes);
            writer.Write((byte)PayloadSha256.Length);
            writer.Write(PayloadSha256);
            writer.Write(WriteId.ToByteArray());
            writer.Write(HeaderChecksumMarker);
            writer.Write(LineageId.ToByteArray());
            writer.Write(ParentWriteId.ToByteArray());
            writer.Flush();

            var unsignedHeader = stream.ToArray();
            using var sha = SHA256.Create();
            writer.Write(sha.ComputeHash(unsignedHeader));
            writer.Flush();
            return stream.ToArray();
        }

        public static HeaderReadResult<SaveHeaderV2> Read(
            BinaryReader reader,
            long fileLength,
            int maxHeaderBytes,
            int maxPayloadBytes)
        {
            try
            {
                var magic = reader.ReadBytes(Magic.Length);
                if (!HasMagic(magic))
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save header magic is invalid.");

                var formatVersion = reader.ReadUInt16();
                var headerSize = reader.ReadUInt16();
                var minimumHeaderSize = formatVersion switch
                {
                    LegacyFormatVersion => 83,
                    FirstChecksummedFormatVersion => 135,
                    _ => 151
                };
                if (headerSize < minimumHeaderSize || headerSize > maxHeaderBytes || headerSize > fileLength)
                    return HeaderReadResult<SaveHeaderV2>.Invalid($"Header size {headerSize} is invalid.");

                if (formatVersion > CurrentFormatVersion)
                {
                    reader.BaseStream.Position = 0;
                    var futureHeader = reader.ReadBytes(headerSize);
                    if (!HasValidHeaderChecksum(futureHeader))
                    {
                        return HeaderReadResult<SaveHeaderV2>.Invalid(
                            "The apparent newer-format header failed its integrity check.");
                    }
                    return HeaderReadResult<SaveHeaderV2>.UnsupportedFormat(
                        $"Save format {formatVersion} is newer than supported format {CurrentFormatVersion}.");
                }
                if (formatVersion != CurrentFormatVersion &&
                    formatVersion != FirstChecksummedFormatVersion &&
                    formatVersion != LegacyFormatVersion)
                    return HeaderReadResult<SaveHeaderV2>.Invalid($"Unsupported save format {formatVersion}.");

                reader.BaseStream.Position = 0;
                var headerBytes = reader.ReadBytes(headerSize);
                if (headerBytes.Length != headerSize)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save header is truncated.");
                if (formatVersion >= FirstChecksummedFormatVersion && !HasValidHeaderChecksum(headerBytes))
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save header checksum mismatch.");

                using var headerStream = new MemoryStream(headerBytes, writable: false);
                using var headerReader = new BinaryReader(headerStream, Encoding.UTF8, leaveOpen: true);
                headerReader.ReadBytes(Magic.Length);
                headerReader.ReadUInt16();
                headerReader.ReadUInt16();
                var schemaVersion = headerReader.ReadInt32();
                var generation = headerReader.ReadInt64();
                var timestampUtc = DateTime.FromBinary(headerReader.ReadInt64()).ToUniversalTime();
                var payloadSize = headerReader.ReadInt32();
                var buildLength = headerReader.ReadUInt16();
                if (generation <= 0)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save generation must be positive.");
                if (schemaVersion < 0)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save schema cannot be negative.");
                if (payloadSize < 0 || payloadSize > maxPayloadBytes)
                    return HeaderReadResult<SaveHeaderV2>.Invalid($"Payload size {payloadSize} is invalid.");
                if (buildLength > 512)
                    return HeaderReadResult<SaveHeaderV2>.Invalid($"Build identifier length {buildLength} is invalid.");

                var buildBytes = headerReader.ReadBytes(buildLength);
                if (buildBytes.Length != buildLength)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Build identifier is truncated.");

                var checksumLength = headerReader.ReadByte();
                if (checksumLength != 32)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save checksum length is invalid.");
                var checksum = headerReader.ReadBytes(checksumLength);
                var writeIdBytes = headerReader.ReadBytes(16);
                if (checksum.Length != checksumLength || writeIdBytes.Length != 16)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save header is truncated.");

                if (formatVersion == LegacyFormatVersion &&
                    headerSize - headerStream.Position >= HeaderChecksumMarker.Length + 16 + 32)
                {
                    var markerPosition = headerStream.Position;
                    var possibleMarker = headerReader.ReadBytes(HeaderChecksumMarker.Length);
                    headerStream.Position = markerPosition;
                    if (HasMarker(possibleMarker, HeaderChecksumMarker) ||
                        HasMarker(possibleMarker, V3HeaderChecksumMarker))
                    {
                        return HeaderReadResult<SaveHeaderV2>.Invalid(
                            "A checksummed save header has a damaged format version.");
                    }
                }

                if (formatVersion >= FirstChecksummedFormatVersion)
                {
                    var marker = headerReader.ReadBytes(HeaderChecksumMarker.Length);
                    var expectedMarker = formatVersion == CurrentFormatVersion
                        ? HeaderChecksumMarker
                        : V3HeaderChecksumMarker;
                    if (!HasMarker(marker, expectedMarker))
                        return HeaderReadResult<SaveHeaderV2>.Invalid("Save header checksum marker is invalid.");
                }

                var lineageBytes = formatVersion >= FirstChecksummedFormatVersion
                    ? headerReader.ReadBytes(16)
                    : Array.Empty<byte>();
                if (formatVersion >= FirstChecksummedFormatVersion && lineageBytes.Length != 16)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save lineage is truncated.");

                var parentWriteIdBytes = formatVersion == CurrentFormatVersion
                    ? headerReader.ReadBytes(16)
                    : Array.Empty<byte>();
                if (formatVersion == CurrentFormatVersion && parentWriteIdBytes.Length != 16)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save parent linkage is truncated.");

                var expectedFieldsEnd = formatVersion >= FirstChecksummedFormatVersion ? headerSize - 32 : headerSize;
                if (headerStream.Position != expectedFieldsEnd)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Header fields do not match the declared header size.");
                reader.BaseStream.Position = headerSize;

                if (fileLength - headerSize != payloadSize)
                    return HeaderReadResult<SaveHeaderV2>.Invalid("File length does not match the declared payload size.");

                var writeId = new Guid(writeIdBytes);
                var lineageId = formatVersion >= FirstChecksummedFormatVersion
                    ? new Guid(lineageBytes)
                    : Guid.Empty;
                var parentWriteId = formatVersion == CurrentFormatVersion
                    ? new Guid(parentWriteIdBytes)
                    : Guid.Empty;
                if (writeId == Guid.Empty ||
                    (formatVersion >= FirstChecksummedFormatVersion && lineageId == Guid.Empty) ||
                    parentWriteId == writeId)
                {
                    return HeaderReadResult<SaveHeaderV2>.Invalid("Save lineage identifiers are invalid.");
                }

                return HeaderReadResult<SaveHeaderV2>.Valid(new SaveHeaderV2
                {
                    SchemaVersion = schemaVersion,
                    Generation = generation,
                    TimestampUtc = timestampUtc,
                    BuildId = Encoding.UTF8.GetString(buildBytes),
                    PayloadSize = payloadSize,
                    PayloadSha256 = checksum,
                    WriteId = writeId,
                    LineageId = lineageId,
                    ParentWriteId = parentWriteId,
                    HasAncestry = formatVersion == CurrentFormatVersion
                });
            }
            catch (Exception ex)
            {
                return HeaderReadResult<SaveHeaderV2>.Invalid($"Failed to parse save header: {ex.Message}");
            }
        }

        private static bool HasValidHeaderChecksum(byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length < 40)
                return false;

            var contentLength = headerBytes.Length - 32;
            var content = new byte[contentLength];
            Buffer.BlockCopy(headerBytes, 0, content, 0, contentLength);
            using var sha = SHA256.Create();
            var actual = sha.ComputeHash(content);
            var difference = 0;
            for (var i = 0; i < actual.Length; i++)
                difference |= actual[i] ^ headerBytes[contentLength + i];
            return difference == 0;
        }

        private static bool HasMarker(byte[] marker, byte[] expected)
        {
            return marker != null && expected != null && marker.Length == expected.Length &&
                   marker[0] == expected[0] && marker[1] == expected[1] &&
                   marker[2] == expected[2] && marker[3] == expected[3];
        }
    }

    /// <summary>
    /// Reader for the schema/timestamp/build/size/HMAC header used by snapshot.bin and its rotations.
    /// The historic HMAC is intentionally treated as unverified because its per-install key was removed.
    /// </summary>
    internal sealed class SaveHeader
    {
        public int SchemaVersion;
        public DateTime TimestampUtc;
        public string BuildId;
        public int PayloadSize;
        public string HmacBase64;

        public static int MinimumSize => 4 + 8 + 2 + 4 + 2;

        public static HeaderReadResult<SaveHeader> Read(
            BinaryReader reader,
            long fileLength,
            int maxHeaderBytes,
            int maxPayloadBytes)
        {
            try
            {
                var schemaVersion = reader.ReadInt32();
                var timestampUtc = DateTime.FromBinary(reader.ReadInt64()).ToUniversalTime();
                var buildLength = reader.ReadUInt16();
                if (schemaVersion < 0)
                    return HeaderReadResult<SaveHeader>.Invalid("Legacy save schema cannot be negative.");
                if (buildLength > 512)
                    return HeaderReadResult<SaveHeader>.Invalid($"Legacy build identifier length {buildLength} is invalid.");

                var buildBytes = reader.ReadBytes(buildLength);
                if (buildBytes.Length != buildLength)
                    return HeaderReadResult<SaveHeader>.Invalid("Legacy build identifier is truncated.");

                var payloadSize = reader.ReadInt32();
                if (payloadSize < 0 || payloadSize > maxPayloadBytes)
                    return HeaderReadResult<SaveHeader>.Invalid($"Legacy payload size {payloadSize} is invalid.");

                var hmacLength = reader.ReadUInt16();
                if (hmacLength > 1024)
                    return HeaderReadResult<SaveHeader>.Invalid($"Legacy integrity length {hmacLength} is invalid.");
                var hmacBytes = reader.ReadBytes(hmacLength);
                if (hmacBytes.Length != hmacLength)
                    return HeaderReadResult<SaveHeader>.Invalid("Legacy integrity field is truncated.");

                var headerSize = reader.BaseStream.Position;
                if (headerSize > maxHeaderBytes || fileLength - headerSize != payloadSize)
                    return HeaderReadResult<SaveHeader>.Invalid("Legacy file length does not match its declared payload size.");

                return HeaderReadResult<SaveHeader>.Valid(new SaveHeader
                {
                    SchemaVersion = schemaVersion,
                    TimestampUtc = timestampUtc,
                    BuildId = Encoding.UTF8.GetString(buildBytes),
                    PayloadSize = payloadSize,
                    HmacBase64 = Encoding.UTF8.GetString(hmacBytes)
                });
            }
            catch (Exception ex)
            {
                return HeaderReadResult<SaveHeader>.Invalid($"Failed to parse legacy save header: {ex.Message}");
            }
        }
    }

    internal sealed class HeaderReadResult<T> where T : class
    {
        private HeaderReadResult(T header, string error, bool unsupported)
        {
            Header = header;
            Error = error;
            Unsupported = unsupported;
        }

        public T Header { get; }
        public string Error { get; }
        public bool Unsupported { get; }

        public static HeaderReadResult<T> Valid(T header)
        {
            return new HeaderReadResult<T>(header, null, false);
        }

        public static HeaderReadResult<T> Invalid(string error)
        {
            return new HeaderReadResult<T>(null, error, false);
        }

        public static HeaderReadResult<T> UnsupportedFormat(string error)
        {
            return new HeaderReadResult<T>(null, error, true);
        }
    }
}
