using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blindsided
{
    public partial class Oracle
    {
        private void SaveToFile()
        {
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New save system only
            try
            {
                var slotName = $"Save{Mathf.Clamp(CurrentSlot, 0, 2) + 1}";
                SaveManager.Instance.SetCurrentSlot(slotName);
                SaveManager.Instance.SaveAsync(saveData).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"New save system Save failed: {ex}");
            }

            // Keep PlayerPrefs metadata in sync for UI which still reads playtime/completion
            PersistSlotMetadataToPlayerPrefs();

            // Clear deleted marker after first successful save
            try
            {
                var deletedKey = $"Slot{CurrentSlot}_Deleted";
                if (PlayerPrefs.GetInt(deletedKey, 0) == 1)
                {
                    PlayerPrefs.DeleteKey(deletedKey);
                    PlayerPrefs.Save();
                }
            }
            catch { }
        }

        private void Load()
        {
            loaded = false;
            saveData = new GameData();
            var deletedMarkerKey = $"Slot{Mathf.Clamp(CurrentSlot, 0, 2)}_Deleted";
            var wasIntentionallyDeleted = false;
            try { wasIntentionallyDeleted = PlayerPrefs.GetInt(deletedMarkerKey, 0) == 1; } catch { wasIntentionallyDeleted = false; }

            // Prefer new save system first
            try
            {
                var slotName = $"Save{Mathf.Clamp(CurrentSlot, 0, 2) + 1}";
                SaveManager.Instance.SetCurrentSlot(slotName);
                var result = SaveManager.Instance.LoadAsync().GetAwaiter().GetResult();
                if (result.ok && result.data != null)
                {
                    saveData = result.data;
                    NullCheckers();
                    loaded = true;
                    AwayForSeconds();
                    if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                        saveData.SavedPreferences.OfflineTimeActive = false;
                    PersistSlotMetadataToPlayerPrefs();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"New save system load failed or missing; trying ES3 fallback. {ex.Message}");
            }

            // Fallback: attempt to read ES3 and migrate to new system (read-only)
            try
            {
                // Try canonical ES3 key
                if (ES3.KeyExists(_dataName, _settings))
                {
                    saveData = ES3.Load<GameData>(_dataName, _settings);
                }
                else
                {
                    var deletedKey = $"Slot{CurrentSlot}_Deleted";
                    var suppressMigration = false;
                    try { suppressMigration = PlayerPrefs.GetInt(deletedKey, 0) == 1; } catch { suppressMigration = false; }

                    if (TryLoadWithKeyFallback(out var discoveredData, out _))
                    {
                        saveData = discoveredData;
                    }
                    else if (!suppressMigration && TryFindAndMigrateAnyEs3ForCurrentSlot(out var migratedSnapshot))
                    {
                        saveData = migratedSnapshot;
                        PersistSlotMetadataToPlayerPrefs();
                    }
                    else
                    {
                        throw new KeyNotFoundException($"No compatible ES3 save found for slot {CurrentSlot}.");
                    }
                }

                // After loading from ES3, immediately persist to new system
                try
                {
                    var slotName2 = $"Save{Mathf.Clamp(CurrentSlot, 0, 2) + 1}";
                    SaveManager.Instance.SetCurrentSlot(slotName2);
                    SaveManager.Instance.SaveAsync(saveData).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to persist migrated ES3 save to new system: {ex.Message}");
                }
            }
            catch (Exception e)
            {
                // If this slot was intentionally deleted, silently create a new game without prompting
                if (wasIntentionallyDeleted)
                {
                    Debug.Log($"No save found for intentionally deleted slot {CurrentSlot}; starting new game.");
                    saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Debug.LogError($"Load failed: {e}");
                    saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                    var message = BuildLoadFailureMessage();
                    if (string.IsNullOrEmpty(message))
                        message = $"Save data for File {CurrentSlot + 1} could not be loaded. A new game will be created.";
                    _pendingLoadFailureMessage = message;
                    _pendingLoadFailureNotice = true;
                    Debug.LogWarning(message);
                }
            }

            NullCheckers();
            loaded = true;
            AwayForSeconds();

            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }


        /// <summary>
        ///     One-time migration: In non-beta builds, copy any Beta-prefixed save files
        ///     (e.g., Beta5Sd0.es3) to live names (Sd0.es3) if the live files do not already exist.
        ///     Also migrates PlayerPrefs metadata from BetaXSlot{n}_* to Slot{n}_* if missing.
        /// </summary>
        private void TryMigrateFromBetaIfNeeded()
        {
            try
            {
                // Only run in non-beta builds and only once per device
                if (beta)
                    return;
                if (PlayerPrefs.GetInt(BetaMigrationPrefKey, 0) == 1)
                    return;

                var migratedAnyFile = false;

                // Migrate save files per slot if present under any Beta iteration
                for (var slotIndex = 0; slotIndex < 3; slotIndex++)
                {
                    var liveFileName = $"Sd{slotIndex}.es3";
                    var liveSettings = new ES3Settings(liveFileName, ES3.Location.File);
                    var livePath = liveSettings.FullPath;

                    if (ES3.FileExists(liveSettings))
                        continue; // Live file already exists; do not overwrite

                    // Prefer the highest Beta iteration if multiple are present
                    for (var b = 12; b >= 0; b--)
                    {
                        var betaFileName = $"Beta{b}Sd{slotIndex}.es3";
                        var betaSettings = new ES3Settings(betaFileName, ES3.Location.File);
                        if (!ES3.FileExists(betaSettings))
                            continue;

                        try
                        {
                            var betaPath = betaSettings.FullPath;
                            // Ensure destination directory exists
                            var liveDir = Path.GetDirectoryName(livePath);
                            if (!string.IsNullOrEmpty(liveDir))
                                Directory.CreateDirectory(liveDir);

                            File.Copy(betaPath, livePath, true);
                            migratedAnyFile = true;

                            // Copy Easy Save .bac backup if present
                            var betaBac = betaPath + ".bac";
                            var liveBac = livePath + ".bac";
                            if (File.Exists(betaBac) && !File.Exists(liveBac))
                                try
                                {
                                    File.Copy(betaBac, liveBac, true);
                                }
                                catch
                                {
                                }

                            Debug.Log($"Migrated Beta save '{betaFileName}' to '{liveFileName}'.");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Beta→Live file migration failed for slot {slotIndex}: {ex.Message}");
                        }

                        break; // Stop searching Beta iterations for this slot once one is migrated
                    }
                }

                // Migrate PlayerPrefs metadata per slot if live keys are missing
                for (var slotIndex = 0; slotIndex < 3; slotIndex++)
                {
                    var liveCompletionKey = $"Slot{slotIndex}_Completion";
                    var livePlaytimeKey = $"Slot{slotIndex}_Playtime";
                    var liveDateKey = $"Slot{slotIndex}_Date";

                    var liveHasAny = PlayerPrefs.HasKey(liveCompletionKey) ||
                                     PlayerPrefs.HasKey(livePlaytimeKey) ||
                                     PlayerPrefs.HasKey(liveDateKey);
                    if (liveHasAny)
                        continue;

                    // Prefer the highest Beta iteration values
                    for (var b = 12; b >= 0; b--)
                    {
                        var prefix = $"Beta{b}";
                        var betaCompletionKey = $"{prefix}{liveCompletionKey}";
                        var betaPlaytimeKey = $"{prefix}{livePlaytimeKey}";
                        var betaDateKey = $"{prefix}{liveDateKey}";

                        var betaHasAny = PlayerPrefs.HasKey(betaCompletionKey) ||
                                         PlayerPrefs.HasKey(betaPlaytimeKey) ||
                                         PlayerPrefs.HasKey(betaDateKey);
                        if (!betaHasAny)
                            continue;

                        try
                        {
                            if (PlayerPrefs.HasKey(betaCompletionKey))
                                PlayerPrefs.SetFloat(liveCompletionKey, PlayerPrefs.GetFloat(betaCompletionKey, 0f));
                            if (PlayerPrefs.HasKey(betaPlaytimeKey))
                                PlayerPrefs.SetFloat(livePlaytimeKey, PlayerPrefs.GetFloat(betaPlaytimeKey, 0f));
                            if (PlayerPrefs.HasKey(betaDateKey))
                                PlayerPrefs.SetString(liveDateKey, PlayerPrefs.GetString(betaDateKey, string.Empty));
                            PlayerPrefs.Save();
                            Debug.Log($"Migrated PlayerPrefs metadata from '{prefix}' for slot {slotIndex}.");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"PlayerPrefs migration failed for slot {slotIndex}: {ex.Message}");
                        }

                        break; // Stop after first Beta iteration found
                    }
                }

                if (migratedAnyFile)
                    Debug.Log(
                        "Beta→Live migration complete. Saves will be normalized to canonical keys on first load.");

                PlayerPrefs.SetInt(BetaMigrationPrefKey, 1);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Beta→Live migration encountered an error: {ex.Message}");
            }
        }

        /// <summary>
        ///     One-time migration: scans the persistent save directory for any top-level .es3 files
        ///     which are not already using our canonical naming. If a file contains a recognizable
        ///     GameData payload (matched via known keys), migrates it to the canonical filename for
        ///     the inferred slot (or first free slot) without overwriting existing canonical files.
        ///     Also copies any accompanying .bac backup and writes PlayerPrefs metadata.
        /// </summary>
        private void TryMigrateFromUnrecognizedEs3IfNeeded()
        {
            try
            {
                if (PlayerPrefs.GetInt(GenericMigrationPrefKey, 0) == 1)
                    return;

                // Resolve the persistent directory where ES3 saves our files
                var probe = new ES3Settings(_fileName, ES3.Location.File);
                var dir = Path.GetDirectoryName(probe.FullPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    return;

                // Build a map of canonical targets per slot and whether they already exist
                var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
                var slotToLivePath = new Dictionary<int, string>(3);
                var existingCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < 3; i++)
                {
                    var liveName = $"{prefix}Sd{i}.es3";
                    var livePath = Path.Combine(dir, liveName);
                    slotToLivePath[i] = livePath;
                    if (File.Exists(livePath))
                        existingCanonical.Add(liveName);
                }

                // Enumerate top-level .es3 files (exclude Backups/, previews, and canonical names)
                var candidates = Directory.GetFiles(dir, "*.es3", SearchOption.TopDirectoryOnly)
                    .Where(p =>
                    {
                        var name = Path.GetFileName(p);
                        if (name.IndexOf("_preview_", StringComparison.OrdinalIgnoreCase) >= 0)
                            return false;
                        if (existingCanonical.Contains(name))
                            return false;
                        return true;
                    })
                    .ToArray();

                if (candidates.Length == 0)
                {
                    PlayerPrefs.SetInt(GenericMigrationPrefKey, 1);
                    PlayerPrefs.Save();
                    return;
                }

                var migratedAny = false;

                foreach (var candidatePath in candidates)
                    try
                    {
                        var candidateName = Path.GetFileName(candidatePath);

                        // Validate contents: try to load with our known keys from this file
                        var settings = new ES3Settings(candidatePath, ES3.Location.File);
                        if (!TryLoadWithKeyFallback(settings, out var snapshot, out var usedKey) || snapshot == null)
                            continue;

                        // Infer target slot from the used key when possible
                        var targetSlot = -1;
                        if (!string.IsNullOrEmpty(usedKey) && TryParseSlotIndexFromKey(usedKey, out var parsed))
                            targetSlot = parsed;

                        // If we couldn't infer slot, pick the first free slot
                        if (targetSlot < 0)
                            for (var i = 0; i < 3; i++)
                                if (!File.Exists(slotToLivePath[i]))
                                {
                                    targetSlot = i;
                                    break;
                                }

                        if (targetSlot < 0 || targetSlot > 2)
                            continue; // no free slot

                        var livePath = slotToLivePath[targetSlot];
                        if (File.Exists(livePath))
                            continue; // don't overwrite existing canonical save

                        // Ensure directory exists and copy file
                        Directory.CreateDirectory(Path.GetDirectoryName(livePath) ?? string.Empty);
                        File.Copy(candidatePath, livePath, false);

                        // Copy Easy Save .bac backup if present alongside the candidate
                        var candidateBac = candidatePath + ".bac";
                        var liveBac = livePath + ".bac";
                        if (File.Exists(candidateBac) && !File.Exists(liveBac))
                            try
                            {
                                File.Copy(candidateBac, liveBac, false);
                            }
                            catch
                            {
                            }

                        // Persist metadata for this slot based on the migrated snapshot
                        PersistSlotMetadataToPlayerPrefs(targetSlot, snapshot);

                        Debug.Log(
                            $"Migrated unrecognized ES3 file '{candidateName}' to '{Path.GetFileName(livePath)}'.");
                        migratedAny = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Generic .es3 migration skipped for '{candidatePath}': {ex.Message}");
                    }

                // No global flag anymore; this path is now used as a targeted fallback.
                if (migratedAny)
                    Debug.Log(
                        "Generic .es3 migration complete. Any discovered saves were copied to canonical slot names.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Generic .es3 migration encountered an error: {ex.Message}");
            }
        }

        private static bool TryParseSlotIndexFromKey(string key, out int slotIndex)
        {
            slotIndex = -1;
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;
                var i = key.LastIndexOf("Data", StringComparison.OrdinalIgnoreCase);
                if (i < 0 || i + 4 >= key.Length)
                    return false;
                var j = i + 4;
                var sb = new StringBuilder();
                while (j < key.Length && char.IsDigit(key[j]))
                {
                    sb.Append(key[j]);
                    j++;
                }

                if (sb.Length == 0)
                    return false;
                if (!int.TryParse(sb.ToString(), out var parsed))
                    return false;
                if (parsed < 0 || parsed > 2)
                    return false;
                slotIndex = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PersistSlotMetadataToPlayerPrefs(int slotIndex, GameData snapshot)
        {
            try
            {
                var index = Mathf.Clamp(slotIndex, 0, 2);
                var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
                var completionKey = $"{prefix}Slot{index}_Completion";
                var playtimeKey = $"{prefix}Slot{index}_Playtime";
                var dateKey = $"{prefix}Slot{index}_Date";

                PlayerPrefs.SetFloat(completionKey, snapshot?.CompletionPercentage ?? 0f);
                PlayerPrefs.SetFloat(playtimeKey, (float)(snapshot?.PlayTime ?? 0));
                PlayerPrefs.SetString(dateKey,
                    snapshot?.DateQuitString ?? DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to persist migrated slot metadata: {ex.Message}");
            }
        }

        /// <summary>
        ///     Attempts to locate and load a <see cref="GameData" /> payload from the physical ES3 file
        ///     when the expected key is missing. Tries a set of historical/candidate keys.
        ///     On success, returns the loaded data and the key used.
        /// </summary>
        private bool TryLoadWithKeyFallback(out GameData loadedData, out string usedKey)
        {
            return TryLoadWithKeyFallback(new ES3Settings(_fileName, ES3.Location.File), out loadedData, out usedKey);
        }

        /// <summary>
        ///     Fallback loader for a specified ES3 settings target (e.g. preview file).
        /// </summary>
        private bool TryLoadWithKeyFallback(ES3Settings targetSettings, out GameData loadedData, out string usedKey)
        {
            loadedData = null;
            usedKey = null;
            try
            {
                if (!ES3.FileExists(targetSettings))
                    return false;
                foreach (var candidate in EnumerateCandidateKeys())
                    try
                    {
                        if (!ES3.KeyExists(candidate, targetSettings))
                            continue;
                        var data = ES3.Load<GameData>(candidate, targetSettings);
                        if (data != null)
                        {
                            loadedData = data;
                            usedKey = candidate;
                            return true;
                        }
                    }
                    catch
                    {
                    }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        ///     Yields a prioritized list of historical/candidate keys which may have been used
        ///     in older builds. Includes current canonical key, other slot indices, and Beta-prefixed variants.
        /// </summary>
        private IEnumerable<string> EnumerateCandidateKeys()
        {
            var index = Mathf.Clamp(CurrentSlot, 0, 2);
            var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;

            var results = new List<string>(40);

            // 1) Canonical for current version/slot
            results.Add($"{prefix}Data{index}");

            // 2) Same prefix, other slot indices (covers mismatched file/key bugs)
            for (var i = 0; i < 3; i++)
                results.Add($"{prefix}Data{i}");

            // 3) No prefix variants
            for (var i = 0; i < 3; i++)
                results.Add($"Data{i}");

            // 4) Beta-prefixed variants for a small historical range
            for (var b = 0; b <= 12; b++)
            for (var i = 0; i < 3; i++)
                results.Add($"Beta{b}Data{i}");

            // 5) Generic fallbacks seen in some early builds
            results.Add("Data");
            results.Add("SaveData");
            results.Add("GameData");

            // De-duplicate while preserving order
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in results)
                if (seen.Add(r))
                    yield return r;
        }

        /// <summary>
        ///     Probes all top-level .es3 files in the persistent save directory for a valid GameData snapshot
        ///     that belongs to the CURRENT slot (inferred via key). If found, copies that file to the canonical
        ///     filename for the current slot and returns the loaded snapshot.
        ///     Does not touch any other slot's canonical files.
        /// </summary>
        private bool TryFindAndMigrateAnyEs3ForCurrentSlot(out GameData migratedSnapshot)
        {
            migratedSnapshot = null;
            try
            {
                var fileSettings = new ES3Settings(_fileName, ES3.Location.File);
                var dir = Path.GetDirectoryName(fileSettings.FullPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    return false;

                var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
                var targetName = $"{prefix}Sd{CurrentSlot}.es3";
                var targetPath = Path.Combine(dir, targetName);

                // If target already exists, do nothing here
                if (File.Exists(targetPath))
                    return false;

                var candidates = Directory.GetFiles(dir, "*.es3", SearchOption.TopDirectoryOnly)
                    .Where(p => !string.Equals(Path.GetFileName(p), targetName, StringComparison.OrdinalIgnoreCase))
                    .Where(p => Path.GetFileName(p).IndexOf("_preview_", StringComparison.OrdinalIgnoreCase) < 0)
                    .ToArray();

                foreach (var candidate in candidates)
                    try
                    {
                        var settings = new ES3Settings(candidate, ES3.Location.File);
                        GameData snapshot = null;
                        string usedKey = null;
                        var loaded = TryLoadWithKeyFallback(settings, out snapshot, out usedKey) && snapshot != null;
                        if (!loaded)
                        {
                            // Some ES3 APIs require file name relative to save root; try a temp preview copy
                            var previewBaseFull = Path.Combine(dir, targetName);
                            var tempPreviewPath = MakeTempPreviewPath(previewBaseFull);
                            try
                            {
                                File.Copy(candidate, tempPreviewPath, true);
                                var tempPreviewName = Path.GetFileName(tempPreviewPath);
                                var previewSettings = new ES3Settings(tempPreviewName, ES3.Location.File);
                                loaded = TryLoadWithKeyFallback(previewSettings, out snapshot, out usedKey) &&
                                         snapshot != null;
                            }
                            catch
                            {
                                loaded = false;
                            }
                            finally
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(tempPreviewPath)) File.Delete(tempPreviewPath);
                                }
                                catch
                                {
                                }
                            }
                        }

                        if (!loaded)
                            continue;

                        // Ensure the snapshot belongs to the CURRENT slot by inspecting the key or filename
                        int inferredSlot;
                        var nameOnly = Path.GetFileName(candidate);
                        if (!string.IsNullOrEmpty(usedKey) && TryParseSlotIndexFromKey(usedKey, out inferredSlot))
                        {
                            if (inferredSlot != CurrentSlot)
                                continue;
                        }
                        else if (TryParseSlotIndexFromFileName(nameOnly, out inferredSlot))
                        {
                            if (inferredSlot != CurrentSlot)
                                continue;
                        }
                        else
                        {
                            // Unknown slot; to be safe, do not migrate
                            continue;
                        }

                        // Copy candidate to canonical name for current slot
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
                        File.Copy(candidate, targetPath, false);

                        // Copy .bac if present
                        var bacSrc = candidate + ".bac";
                        var bacDst = targetPath + ".bac";
                        if (File.Exists(bacSrc) && !File.Exists(bacDst))
                            try
                            {
                                File.Copy(bacSrc, bacDst, false);
                            }
                            catch
                            {
                            }

                        migratedSnapshot = snapshot;
                        return true;
                    }
                    catch
                    {
                    }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryParseSlotIndexFromFileName(string fileName, out int slotIndex)
        {
            slotIndex = -1;
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return false;
                var i = fileName.LastIndexOf("Sd", StringComparison.OrdinalIgnoreCase);
                if (i < 0 || i + 2 >= fileName.Length)
                    return false;
                var j = i + 2;
                var sb = new StringBuilder();
                while (j < fileName.Length && char.IsDigit(fileName[j]))
                {
                    sb.Append(fileName[j]);
                    j++;
                }

                if (sb.Length == 0)
                    return false;
                if (!int.TryParse(sb.ToString(), out var parsed))
                    return false;
                if (parsed < 0 || parsed > 2)
                    return false;
                slotIndex = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Compares the just-loaded save data with this device's last-known metadata in PlayerPrefs.
        ///     If a significant regression is detected, sets flags and stores a brief report for UI/logging.
        /// </summary>
        private static DateTime? ParseDateOrNull(string date)
        {
            if (string.IsNullOrEmpty(date)) return null;
            try
            {
                return DateTime.Parse(date, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static string MakeTempPreviewPath(string liveFullPath)
        {
            var dir = Path.GetDirectoryName(liveFullPath) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(liveFullPath);
            var tempName = $"{baseName}_preview_{Guid.NewGuid():N}.es3";
            return Path.Combine(dir, tempName);
        }

        // Regression prompt removed

        /// <summary>
        ///     Writes the specified slot's save metadata to PlayerPrefs so UI and other systems
        ///     relying on PlayerPrefs reflect the latest save/autosave.
        /// </summary>
        public void PersistSlotMetadataToPlayerPrefs(int slotIndex)
        {
            var index = Mathf.Clamp(slotIndex, 0, 2);
            var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
            var completionKey = $"{prefix}Slot{index}_Completion";
            var playtimeKey = $"{prefix}Slot{index}_Playtime";
            var dateKey = $"{prefix}Slot{index}_Date";

            PlayerPrefs.SetFloat(completionKey, saveData.CompletionPercentage);
            PlayerPrefs.SetFloat(playtimeKey, (float)saveData.PlayTime);
            PlayerPrefs.SetString(dateKey, saveData.DateQuitString);
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     Convenience overload for current slot.
        /// </summary>
        public void PersistSlotMetadataToPlayerPrefs()
        {
            PersistSlotMetadataToPlayerPrefs(CurrentSlot);
        }

        private void TryShowLoadFailureWindow(string message)
        {
            // Regression UI removed; no UI shown here anymore
            Debug.LogWarning(message);
        }

        private string GetLatestBackupHintText()
        {
            try
            {
                // New system manages backups internally; no legacy hints.
            }
            catch
            {
            }
            return "";
        }

        private string BuildLoadFailureMessage()
        {
            try
            {
                var slot = CurrentSlot + 1;
                return
                    $"Save data for File {slot} could not be loaded. A new game will be created. You may attempt to restore a backup.";
            }
            catch
            {
                return null;
            }
        }

        private bool TryGetLatestRotatingBackup(out string backupPath, out DateTime backupUtcTimestamp)
        {
            backupPath = null;
            backupUtcTimestamp = default;
            try
            {
                var targetSettings = new ES3Settings(_fileName, ES3.Location.File);
                var targetFullPath = targetSettings.FullPath;
                var baseName = Path.GetFileNameWithoutExtension(_fileName);
                var saveDir = Path.GetDirectoryName(targetFullPath);
                var backupDir = Path.Combine(saveDir ?? string.Empty, "Backups", baseName);

                if (!Directory.Exists(backupDir))
                    return false;

                var candidates = Directory.GetFiles(backupDir, $"{baseName}_*.es3", SearchOption.TopDirectoryOnly);
                if (candidates == null || candidates.Length == 0)
                    return false;

                var latest = candidates.OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal).First();

                // Parse UTC timestamp from filename pattern: <base>_yyyyMMdd-HHmmssfff(.suffix)
                var file = Path.GetFileNameWithoutExtension(latest);
                var idx = file.IndexOf('_');
                if (idx >= 0 && idx + 1 < file.Length)
                {
                    var tsPart = file.Substring(idx + 1);
                    if (DateTime.TryParseExact(tsPart, "yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts))
                    {
                        backupPath = latest;
                        backupUtcTimestamp = ts;
                        return true;
                    }
                }

                backupPath = latest;
                backupUtcTimestamp = DateTime.UtcNow;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Saves current game data into the specified slot index and updates PlayerPrefs for that slot.
        /// </summary>
        public void SaveToSlot(int slotIndex)
        {
            var index = Mathf.Clamp(slotIndex, 0, 2);
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New system write for the targeted slot (no ES3)
            try
            {
                var slotName = $"Save{index + 1}";
                SaveManager.Instance.SetCurrentSlot(slotName);
                SaveManager.Instance.SaveAsync(saveData).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"New save system SaveToSlot failed: {ex.Message}");
            }

            PersistSlotMetadataToPlayerPrefs(index);
        }

        private void NullCheckers()
        {
            saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            saveData.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            saveData.EnemyKills ??= new Dictionary<string, double>();
            saveData.CompletedNpcTasks ??= new HashSet<string>();
            saveData.PinnedQuests ??= new List<string>();
            // Gear system collections
            saveData.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
            saveData.BuffSlots ??= new List<string>(new string[5]);
            if (saveData.BuffSlots.Count < 5)
                while (saveData.BuffSlots.Count < 5)
                    saveData.BuffSlots.Add(null);
            saveData.AutoBuffSlots ??= new List<bool>(new bool[5]);
            if (saveData.AutoBuffSlots.Count < 5)
                while (saveData.AutoBuffSlots.Count < 5)
                    saveData.AutoBuffSlots.Add(false);
            if (saveData.UnlockedBuffSlots <= 0)
                saveData.UnlockedBuffSlots = 1;
            else if (saveData.UnlockedBuffSlots > 5)
                saveData.UnlockedBuffSlots = 5;
            if (saveData.UnlockedAutoBuffSlots < 0)
                saveData.UnlockedAutoBuffSlots = 0;
            else if (saveData.UnlockedAutoBuffSlots > 5)
                saveData.UnlockedAutoBuffSlots = 5;
            if (saveData.DisciplePercent <= 0f)
                saveData.DisciplePercent = 0.1f;
            saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
        }

        public static void AwayForSeconds()
        {
            if (string.IsNullOrEmpty(oracle.saveData.DateQuitString))
            {
                oracle.saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                return;
            }

            var quitTime = DateTime.Parse(oracle.saveData.DateQuitString, CultureInfo.InvariantCulture);
            var seconds = Mathf.Max(0f, (float)(DateTime.UtcNow - quitTime).TotalSeconds);
            EventHandler.AwayForTime(seconds);
        }

        // Easy Save backup removed; new system uses rolling prev files

        /// <summary>
        ///     Creates a timestamped backup copy of the persisted save file under
        ///     PersistentDataPath/Backups/
        ///     <baseName>
        ///         /
        ///         <baseName>
        ///             _yyyyMMdd-HHmmss.es3
        ///             and prunes older backups beyond <see cref="backupsToKeepPerSlot" />.
        /// </summary>
        // Easy Save rotating backups removed; new system does backups internally

        /// <summary>
        ///     Attempts to restore the latest timestamped rotating backup into the live save file.
        ///     Returns true if a backup was restored.
        /// </summary>
        // Easy Save backup restore removed

        // Easy Save backup pruning not used
    }
}