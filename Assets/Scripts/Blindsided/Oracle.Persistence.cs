using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using Blindsided.SaveData;
using Blindsided.Utilities;

namespace Blindsided
{
    public partial class Oracle
    {

        private void SaveToFile()
        {
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            ES3.Save(_dataName, saveData, _settings); // write to cache
            ES3.StoreCachedFile(_fileName);

            // Ensure PlayerPrefs metadata for the current slot is kept in sync for all saves/autosaves
            PersistSlotMetadataToPlayerPrefs();
        }

		private void Load()
        {
            loaded = false;
            saveData = new GameData();

			bool failedLoad = false;

			try
			{
				// Primary: try canonical key in cache/file per _settings
				if (ES3.KeyExists(_dataName, _settings))
				{
					saveData = ES3.Load<GameData>(_dataName, _settings);
				}
				else
				{
					// Backward-compat: discover the correct key inside the physical save file and migrate
					if (TryLoadWithKeyFallback(out var discoveredData, out var usedKey))
					{
						saveData = discoveredData;
						if (!string.Equals(usedKey, _dataName, StringComparison.Ordinal))
						{
							Debug.LogWarning($"Loaded save using fallback key '{usedKey}'. Migrating to '{_dataName}'.");
							// Re-save under canonical key to prevent future mismatches
							ES3.Save(_dataName, saveData, _settings);
							ES3.StoreCachedFile(_fileName);
							// Attempt to remove legacy key from both cache & file to avoid ambiguity
							try { ES3.DeleteKey(usedKey, _settings); } catch { }
							try { ES3.DeleteKey(usedKey, new ES3Settings(_fileName, ES3.Location.File)); } catch { }
						}
					}
					else
					{
						// Fallback: no save found in our canonical file. Probe any .es3 in the save dir and
						// migrate only for the CURRENT slot if a valid snapshot is discovered.
						if (TryFindAndMigrateAnyEs3ForCurrentSlot(out var migratedSnapshot))
						{
							// We have copied the physical file; ensure canonical key is written to cache and file
							saveData = migratedSnapshot;
							ES3.Save(_dataName, saveData, _settings);
							ES3.StoreCachedFile(_fileName);
							PersistSlotMetadataToPlayerPrefs();
						}
						else
						{
							throw new KeyNotFoundException($"No compatible save key found in '{_fileName}' or any other .es3 file.");
						}
					}
				}
			}
			catch (Exception e)
			{
				failedLoad = true;
				Debug.LogError($"Load failed: {e}");
				// Start a fresh save but inform the player and offer backup restore
				saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
				var message = BuildLoadFailureMessage();
				if (string.IsNullOrEmpty(message))
					message = $"Save data for File {CurrentSlot + 1} could not be loaded. A new game will be created.";
				_pendingLoadFailureMessage = message;
				_pendingLoadFailureNotice = true;
				Debug.LogWarning(message);
			}

            NullCheckers();

			// Check for potential regression relative to what this device previously recorded
			if (!failedLoad)
				DetectRegressionAgainstPlayerPrefs();

            loaded = true;
            AwayForSeconds();

            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }


        /// <summary>
        /// One-time migration: In non-beta builds, copy any Beta-prefixed save files
        /// (e.g., Beta5Sd0.es3) to live names (Sd0.es3) if the live files do not already exist.
        /// Also migrates PlayerPrefs metadata from BetaXSlot{n}_* to Slot{n}_* if missing.
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

                bool migratedAnyFile = false;

                // Migrate save files per slot if present under any Beta iteration
                for (int slotIndex = 0; slotIndex < 3; slotIndex++)
                {
                    var liveFileName = $"Sd{slotIndex}.es3";
                    var liveSettings = new ES3Settings(liveFileName, ES3.Location.File);
                    var livePath = liveSettings.FullPath;

                    if (ES3.FileExists(liveSettings))
                        continue; // Live file already exists; do not overwrite

                    // Prefer the highest Beta iteration if multiple are present
                    for (int b = 12; b >= 0; b--)
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
                            {
                                try { File.Copy(betaBac, liveBac, true); } catch { }
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
                for (int slotIndex = 0; slotIndex < 3; slotIndex++)
                {
                    var liveCompletionKey = $"Slot{slotIndex}_Completion";
                    var livePlaytimeKey = $"Slot{slotIndex}_Playtime";
                    var liveDateKey = $"Slot{slotIndex}_Date";

                    bool liveHasAny = PlayerPrefs.HasKey(liveCompletionKey) ||
                                      PlayerPrefs.HasKey(livePlaytimeKey) ||
                                      PlayerPrefs.HasKey(liveDateKey);
                    if (liveHasAny)
                        continue;

                    // Prefer the highest Beta iteration values
                    for (int b = 12; b >= 0; b--)
                    {
                        var prefix = $"Beta{b}";
                        var betaCompletionKey = $"{prefix}{liveCompletionKey}";
                        var betaPlaytimeKey = $"{prefix}{livePlaytimeKey}";
                        var betaDateKey = $"{prefix}{liveDateKey}";

                        bool betaHasAny = PlayerPrefs.HasKey(betaCompletionKey) ||
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
                {
                    Debug.Log("Beta→Live migration complete. Saves will be normalized to canonical keys on first load.");
                }

                PlayerPrefs.SetInt(BetaMigrationPrefKey, 1);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Beta→Live migration encountered an error: {ex.Message}");
            }
        }

        /// <summary>
        /// One-time migration: scans the persistent save directory for any top-level .es3 files
        /// which are not already using our canonical naming. If a file contains a recognizable
        /// GameData payload (matched via known keys), migrates it to the canonical filename for
        /// the inferred slot (or first free slot) without overwriting existing canonical files.
        /// Also copies any accompanying .bac backup and writes PlayerPrefs metadata.
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
                for (int i = 0; i < 3; i++)
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

                bool migratedAny = false;

                foreach (var candidatePath in candidates)
                {
                    try
                    {
                        var candidateName = Path.GetFileName(candidatePath);

                        // Validate contents: try to load with our known keys from this file
                        var settings = new ES3Settings(candidatePath, ES3.Location.File);
                        if (!TryLoadWithKeyFallback(settings, out var snapshot, out var usedKey) || snapshot == null)
                            continue;

                        // Infer target slot from the used key when possible
                        int targetSlot = -1;
                        if (!string.IsNullOrEmpty(usedKey) && TryParseSlotIndexFromKey(usedKey, out var parsed))
                            targetSlot = parsed;

                        // If we couldn't infer slot, pick the first free slot
                        if (targetSlot < 0)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                if (!File.Exists(slotToLivePath[i]))
                                {
                                    targetSlot = i;
                                    break;
                                }
                            }
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
                        {
                            try { File.Copy(candidateBac, liveBac, false); } catch { }
                        }

                        // Persist metadata for this slot based on the migrated snapshot
                        PersistSlotMetadataToPlayerPrefs(targetSlot, snapshot);

                        Debug.Log($"Migrated unrecognized ES3 file '{candidateName}' to '{Path.GetFileName(livePath)}'.");
                        migratedAny = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Generic .es3 migration skipped for '{candidatePath}': {ex.Message}");
                    }
                }

                // No global flag anymore; this path is now used as a targeted fallback.
                if (migratedAny)
                {
                    Debug.Log("Generic .es3 migration complete. Any discovered saves were copied to canonical slot names.");
                }
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
                PlayerPrefs.SetString(dateKey, snapshot?.DateQuitString ?? DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to persist migrated slot metadata: {ex.Message}");
            }
        }

		/// <summary>
		/// Attempts to locate and load a <see cref="GameData"/> payload from the physical ES3 file
		/// when the expected key is missing. Tries a set of historical/candidate keys.
		/// On success, returns the loaded data and the key used.
		/// </summary>
		private bool TryLoadWithKeyFallback(out GameData loadedData, out string usedKey)
		{
			return TryLoadWithKeyFallback(new ES3Settings(_fileName, ES3.Location.File), out loadedData, out usedKey);
		}

		/// <summary>
		/// Fallback loader for a specified ES3 settings target (e.g. preview file).
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
				{
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
					catch { }
				}
			}
			catch { }
			return false;
		}

		/// <summary>
		/// Yields a prioritized list of historical/candidate keys which may have been used
		/// in older builds. Includes current canonical key, other slot indices, and Beta-prefixed variants.
		/// </summary>
		private IEnumerable<string> EnumerateCandidateKeys()
		{
			var index = Mathf.Clamp(CurrentSlot, 0, 2);
			var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;

			var results = new List<string>(40);

			// 1) Canonical for current version/slot
			results.Add($"{prefix}Data{index}");

			// 2) Same prefix, other slot indices (covers mismatched file/key bugs)
			for (int i = 0; i < 3; i++)
				results.Add($"{prefix}Data{i}");

			// 3) No prefix variants
			for (int i = 0; i < 3; i++)
				results.Add($"Data{i}");

			// 4) Beta-prefixed variants for a small historical range
			for (int b = 0; b <= 12; b++)
				for (int i = 0; i < 3; i++)
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
		/// Probes all top-level .es3 files in the persistent save directory for a valid GameData snapshot
		/// that belongs to the CURRENT slot (inferred via key). If found, copies that file to the canonical
		/// filename for the current slot and returns the loaded snapshot.
		/// Does not touch any other slot's canonical files.
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
				{
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
								loaded = TryLoadWithKeyFallback(previewSettings, out snapshot, out usedKey) && snapshot != null;
							}
							catch { loaded = false; }
							finally
							{
								try { if (!string.IsNullOrEmpty(tempPreviewPath)) File.Delete(tempPreviewPath); } catch { }
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
						{
							try { File.Copy(bacSrc, bacDst, false); } catch { }
						}

						migratedSnapshot = snapshot;
						return true;
					}
					catch { }
				}
			}
			catch { }
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
        /// Compares the just-loaded save data with this device's last-known metadata in PlayerPrefs.
        /// If a significant regression is detected, sets flags and stores a brief report for UI/logging.
        /// </summary>
        private void DetectRegressionAgainstPlayerPrefs()
        {
            try
            {
                var index = Mathf.Clamp(CurrentSlot, 0, 2);
                var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
                var completionKey = $"{prefix}Slot{index}_Completion";
                var playtimeKey = $"{prefix}Slot{index}_Playtime";
                var dateKey = $"{prefix}Slot{index}_Date";

                var prevCompletion = PlayerPrefs.GetFloat(completionKey, -1f);
                var prevPlaytime = PlayerPrefs.GetFloat(playtimeKey, -1f);
                var prevDateString = PlayerPrefs.GetString(dateKey, string.Empty);

                var haveBaseline = prevCompletion >= 0f || prevPlaytime >= 0f || !string.IsNullOrEmpty(prevDateString);
                if (!haveBaseline) return; // nothing to compare on this device

                float loadedPlaytime = (float)saveData.PlayTime;
                float loadedCompletion = saveData.CompletionPercentage;

                var playtimeDrop = prevPlaytime - loadedPlaytime;
                var completionDrop = prevCompletion - loadedCompletion;

                bool regression = false;
                string reason = string.Empty;

                if (playtimeDrop > PlaytimeRegressionToleranceSeconds)
                {
                    regression = true;
                    reason = $"Playtime drop {playtimeDrop:0}s (> {PlaytimeRegressionToleranceSeconds:0}s)";
                    _lastPlaytimeDropSec = playtimeDrop;
                }
                else if (completionDrop > CompletionRegressionTolerance)
                {
                    regression = true;
                    reason = $"Completion drop {completionDrop:0.##}% (> {CompletionRegressionTolerance:0.##}%)";
                    _lastCompletionDropPct = completionDrop;
                }
                else if (!string.IsNullOrEmpty(prevDateString) && !string.IsNullOrEmpty(saveData.DateQuitString))
                {
                    // If stored date is substantially newer than loaded, consider it a regression
                    if (DateTime.TryParse(prevDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var prevDate)
                        && DateTime.TryParse(saveData.DateQuitString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loadedDate))
                    {
                        var minutesNewer = (prevDate - loadedDate).TotalMinutes;
                        if (minutesNewer > 10) // 10 minutes grace
                        {
                            regression = true;
                            reason = $"Last save time moved back by {minutesNewer:0} minutes";
                            _lastMinutesNewer = minutesNewer;
                        }
                    }
                }

                if (!regression) return;

                RegressionDetected = true;
                RegressionMessage =
                    $"Regression detected for slot {index}. Prev PT={prevPlaytime:0}s, Prev %={prevCompletion:0.##}; Loaded PT={loadedPlaytime:0}s, %={loadedCompletion:0.##}. Reason: {reason}";

                Debug.LogWarning(RegressionMessage);

                // Record a simple marker & message in PlayerPrefs so UI can surface it
                var regKey = $"{prefix}Slot{index}_RegressionDetected";
                var regInfoKey = $"{prefix}Slot{index}_RegressionInfo";
                PlayerPrefs.SetInt(regKey, 1);
                PlayerPrefs.SetString(regInfoKey, RegressionMessage);
                PlayerPrefs.Save();

                TryShowRegressionWindow(index, prevPlaytime, prevCompletion, loadedPlaytime, loadedCompletion);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Regression detection failed: {ex}");
            }
        }

		private void TryShowRegressionWindow(int slotIndex, float prevPlaytime, float prevCompletion,
			float loadedPlaytime, float loadedCompletion)
        {
            if (regressionConfirmWindow == null)
                return;

			// Gather current (loaded) info
			var loadedLastPlayed = ParseDateOrNull(saveData.DateQuitString);

			// Start with PlayerPrefs metadata as the backup candidate
			var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
			var completionKey = $"{prefix}Slot{slotIndex}_Completion";
			var playtimeKey = $"{prefix}Slot{slotIndex}_Playtime";
			var dateKey = $"{prefix}Slot{slotIndex}_Date";
			float backupPlaytime = PlayerPrefs.GetFloat(playtimeKey, prevPlaytime);
			float backupCompletion = PlayerPrefs.GetFloat(completionKey, prevCompletion);
			var backupDateString = PlayerPrefs.GetString(dateKey, string.Empty);
			DateTime? backupLastPlayed = ParseDateOrNull(backupDateString);
			string backupSource = "PlayerPrefs";

			// Prefer actual backup contents when available: try .bac first, then latest rotating backup
			try
			{
				var fileSettings = new ES3Settings(_fileName, ES3.Location.File);
				var liveFullPath = fileSettings.FullPath;
				var bacFullPath = liveFullPath + ".bac";
				GameData backupData = null;
				string tempPreviewPath = null;
				if (File.Exists(bacFullPath))
				{
					tempPreviewPath = MakeTempPreviewPath(liveFullPath);
					File.Copy(bacFullPath, tempPreviewPath, true);
					var tempPreviewName = Path.GetFileName(tempPreviewPath);
					var previewSettings = new ES3Settings(tempPreviewName, ES3.Location.File);
						if (!TryLoadWithKeyFallback(previewSettings, out backupData, out _))
							backupData = null;
					backupSource = "Backup (.bac)";
				}
				else
				{
					// Try rotating backups directory
					var baseName = Path.GetFileNameWithoutExtension(_fileName);
					var saveDir = Path.GetDirectoryName(liveFullPath);
					var backupDir = Path.Combine(saveDir ?? string.Empty, "Backups", baseName);
					if (Directory.Exists(backupDir))
					{
						var candidates = Directory.GetFiles(backupDir, $"{baseName}_*.es3", SearchOption.TopDirectoryOnly);
						if (candidates != null && candidates.Length > 0)
						{
							var latest = candidates.OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal).First();
							// Copy to root for easy ES3 loading by file name
							tempPreviewPath = MakeTempPreviewPath(liveFullPath);
							File.Copy(latest, tempPreviewPath, true);
							var tempPreviewName = Path.GetFileName(tempPreviewPath);
							var previewSettings = new ES3Settings(tempPreviewName, ES3.Location.File);
							if (!TryLoadWithKeyFallback(previewSettings, out backupData, out _))
								backupData = null;
							backupSource = "Backup (Rotating)";
						}
					}
				}

				if (backupData != null)
				{
					backupPlaytime = (float)backupData.PlayTime;
					backupCompletion = backupData.CompletionPercentage;
					backupLastPlayed = ParseDateOrNull(backupData.DateQuitString);
				}

				// Cleanup temp preview
				if (!string.IsNullOrEmpty(tempPreviewPath))
				{
					try { File.Delete(tempPreviewPath); } catch { /* ignore */ }
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Failed to preview backup contents: {ex.Message}");
			}

			var reasons = new List<string>(3);
			if (_lastPlaytimeDropSec > PlaytimeRegressionToleranceSeconds)
				reasons.Add($"Playtime: -{CalcUtils.FormatTime(_lastPlaytimeDropSec, shortForm: true)}");
			if (_lastCompletionDropPct > CompletionRegressionTolerance)
				reasons.Add($"Completion: -{_lastCompletionDropPct:0.##}%");
			if (_lastMinutesNewer > 10)
				reasons.Add($"Time: -{_lastMinutesNewer:0} min");

			var summary = reasons.Count > 0 ? string.Join(" • ", reasons) : "Progress appears lower.";

			if (regressionMessageText != null)
			{
				var localPlay = loadedPlaytime > 0 ? CalcUtils.FormatTime(loadedPlaytime, shortForm: true) : "None";
				var localComp = $"{loadedCompletion:0.##}%";
				var localLast = loadedLastPlayed.HasValue ? loadedLastPlayed.Value.ToLocalTime().ToString("g") : "Never";

				var backupPlay = backupPlaytime > 0 ? CalcUtils.FormatTime(backupPlaytime, shortForm: true) : "None";
				var backupCompStr = $"{backupCompletion:0.##}%";
				var backupLast = backupLastPlayed.HasValue ? backupLastPlayed.Value.ToLocalTime().ToString("g") : "Unknown";

				regressionMessageText.text =
					$"Progress mismatch detected for File {slotIndex + 1}.\n{summary}\n\n" +
					$"Local (Loaded)\n" +
					$"• Playtime: {localPlay}\n" +
					$"• Completion: {localComp}\n" +
					$"• Last Played: {localLast}\n\n" +
					$"Backup ({backupSource})\n" +
					$"• Playtime: {backupPlay}\n" +
					$"• Completion: {backupCompStr}\n" +
					$"• Last Played: {backupLast}\n\n" +
					$"Choose which save to keep: Keep Loaded or Restore Backup.";
			}

			// Always ensure buttons are labeled consistently when showing the window
			SetRegressionButtonsText("Keep Loaded", "Restore Backup");

			regressionConfirmWindow.SetActive(true);
        }

		private static DateTime? ParseDateOrNull(string date)
		{
			if (string.IsNullOrEmpty(date)) return null;
			try { return DateTime.Parse(date, CultureInfo.InvariantCulture); } catch { return null; }
		}

		private static string MakeTempPreviewPath(string liveFullPath)
		{
			var dir = Path.GetDirectoryName(liveFullPath) ?? string.Empty;
			var baseName = Path.GetFileNameWithoutExtension(liveFullPath);
			var tempName = $"{baseName}_preview_{Guid.NewGuid():N}.es3";
			return Path.Combine(dir, tempName);
		}

        private void ConfirmRegressionKeepLoaded()
        {
            // User accepts the loaded (possibly regressed) data; persist metadata so this prompt won't repeat.
            PersistSlotMetadataToPlayerPrefs();
            DismissRegressionWindow();

            if (_mainSceneLoadDeferred)
            {
                _mainSceneLoadDeferred = false;
                StartCoroutine(LoadMainScene());
            }
            // Resume autosave scheduling after the user confirms
            RestartAutosaveLoop(AutosaveIntervalSeconds);
        }

        private void DismissRegressionWindow()
        {
            if (regressionConfirmWindow != null)
                regressionConfirmWindow.SetActive(false);
        }

		private void SetRegressionButtonsText(string yes, string no)
		{
			if (regressionYesText != null)
				regressionYesText.text = yes;
			if (regressionNoText != null)
				regressionNoText.text = no;
		}

        /// <summary>
        /// Called by the "No" button: attempts to restore the Easy Save .bac backup for the current slot
        /// and reloads the scene so the restored data is applied everywhere.
        /// </summary>
        [Button]
        public void AttemptRestoreBackupAndReload()
        {
            try
            {
                DismissRegressionWindow();

                                // Prefer rotating backups; fall back to Easy Save's .bac
                                var restored = TryRestoreFromLatestRotatingBackup();
                                if (!restored)
                                {
                                        restored = ES3.RestoreBackup(_settings);
                                        if (!restored)
                                                Debug.LogWarning("No rotating or .bac backup found to restore.");
                                        else
                                                Debug.Log(".bac backup restored. Reloading save and scene.");
                                }
                                else
                                {
                                        Debug.Log("Rotating backup restored. Reloading save and scene.");
                                }

                // Reload save from disk regardless; if restore failed, this reloads the current file.
                Load();

                if (_pendingLoadFailureNotice)
                {
                    TryShowLoadFailureWindow(_pendingLoadFailureMessage);
                    _pendingLoadFailureNotice = false;
                    _pendingLoadFailureMessage = null;
                    _mainSceneLoadDeferred = true;
                    return;
                }
                if (RegressionDetected)
                {
                    _mainSceneLoadDeferred = true;
                    return;
                }

                // Reload the active scene(s) to ensure all systems pick up the new data
                // We already have a helper to load Main on boot, but here reload current.
                var active = SceneManager.GetActiveScene();
                SceneManager.LoadScene(active.name);
                _mainSceneLoadDeferred = false;
                StartCoroutine(LoadMainScene());
                // Resume autosave scheduling after reload begins
                RestartAutosaveLoop(FirstAutosaveDelaySeconds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Backup restore failed: {ex}");
                DismissRegressionWindow();
            }
        }

        /// <summary>
        /// Writes the specified slot's save metadata to PlayerPrefs so UI and other systems
        /// relying on PlayerPrefs reflect the latest save/autosave.
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
        /// Convenience overload for current slot.
        /// </summary>
        public void PersistSlotMetadataToPlayerPrefs()
        {
            PersistSlotMetadataToPlayerPrefs(CurrentSlot);
        }

		private void TryShowLoadFailureWindow(string message)
		{
			if (regressionConfirmWindow == null)
				return;

			var hint = GetLatestBackupHintText();
			var full = string.IsNullOrEmpty(hint) ? message : message + "\n" + hint;

			if (regressionMessageText != null)
				regressionMessageText.text = full + "\nKeep this new game, or try restoring from a backup?";

			SetRegressionButtonsText("Keep Loaded", "Restore Backup");
		
			regressionConfirmWindow.SetActive(true);
		}

		private string GetLatestBackupHintText()
		{
			try
			{
				if (TryGetLatestRotatingBackup(out var path, out var ts))
				{
					var local = ts.ToLocalTime();
					return $"Most recent rotating backup: {local:yyyy-MM-dd HH:mm}.";
				}

				var bacPath = _fileName + ".bac";
				if (ES3.FileExists(bacPath))
					return "An Easy Save backup (.bac) is available.";
			}
			catch { }
			return "No backups were found.";
		}

		private string BuildLoadFailureMessage()
		{
			try
			{
				var slot = CurrentSlot + 1;
				return $"Save data for File {slot} could not be loaded. A new game will be created. You may attempt to restore a backup.";
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
        /// Saves current game data into the specified slot index and updates PlayerPrefs for that slot.
        /// </summary>
		public void SaveToSlot(int slotIndex)
        {
            var index = Mathf.Clamp(slotIndex, 0, 2);
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
            var dataName = $"{prefix}Data{index}";
            var fileName = $"{prefix}Sd{index}.es3";
            var settings = new ES3Settings(fileName, ES3.Location.Cache);
            ES3.Save(dataName, saveData, settings);
            ES3.StoreCachedFile(fileName);
			CreateRotatingBackupForFile(fileName);

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
			saveData.CraftHistory ??= new List<GearItemRecord>();
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

        /// <summary>Deletes any existing .bac backup, then creates a fresh one.</summary>
		private void SafeCreateBackup()
        {
            var backupPath = _fileName + ".bac"; // Easy Save uses .bac
            if (ES3.FileExists(backupPath))
                ES3.DeleteFile(backupPath);

            try
            {
                ES3.CreateBackup(_fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Backup failure: {ex}");
            }
        }

		/// <summary>
		/// Creates a timestamped backup copy of the persisted save file under
		/// PersistentDataPath/Backups/<baseName>/<baseName>_yyyyMMdd-HHmmss.es3
		/// and prunes older backups beyond <see cref="backupsToKeepPerSlot"/>.
		/// </summary>
		private void CreateRotatingBackup()
		{
			CreateRotatingBackupForFile(_fileName);
		}

		private void CreateRotatingBackupForFile(string fileName)
		{
			try
			{
				// Ensure the persisted file exists (we copy from File location, not Cache)
				var sourceSettings = new ES3Settings(fileName, ES3.Location.File);
				if (!ES3.FileExists(sourceSettings))
					return;

				var sourceFullPath = sourceSettings.FullPath;
				var baseName = Path.GetFileNameWithoutExtension(fileName); // e.g., "Sd0" or "Beta3Sd0"
				var saveDir = Path.GetDirectoryName(sourceFullPath);
				var backupDir = Path.Combine(saveDir ?? string.Empty, "Backups", baseName);
				Directory.CreateDirectory(backupDir);

				var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
				var backupFileName = $"{baseName}_{timestamp}.es3";
				var backupFullPath = Path.Combine(backupDir, backupFileName);
				// Ensure uniqueness if multiple backups occur within the same millisecond
				int dupeIndex = 1;
				while (File.Exists(backupFullPath))
				{
					backupFileName = $"{baseName}_{timestamp}_{dupeIndex:00}.es3";
					backupFullPath = Path.Combine(backupDir, backupFileName);
					dupeIndex++;
				}

				File.Copy(sourceFullPath, backupFullPath, false);

				PruneOldBackups(backupDir, baseName, backupsToKeepPerSlot);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Rotating backup failed: {ex}");
			}
		}

		/// <summary>
		/// Attempts to restore the latest timestamped rotating backup into the live save file.
		/// Returns true if a backup was restored.
		/// </summary>
		private bool TryRestoreFromLatestRotatingBackup()
		{
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

				// Sort descending by name (timestamp in name ensures correct order)
				var latest = candidates.OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal).First();
				File.Copy(latest, targetFullPath, true);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to restore rotating backup: {ex}");
				return false;
			}
		}

		private void PruneOldBackups(string backupDir, string baseName, int keepCount)
		{
			try
			{
				var pattern = $"{baseName}_*.es3";
				var files = Directory.Exists(backupDir)
					? Directory.GetFiles(backupDir, pattern, SearchOption.TopDirectoryOnly)
					: Array.Empty<string>();

				if (files.Length <= keepCount)
					return;

				// Our filenames embed a UTC timestamp in sortable format, so sort by name
				var ordered = files.OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal).ToArray();
				var toDelete = ordered.Length - keepCount;
				for (int i = 0; i < toDelete; i++)
				{
					File.Delete(ordered[i]);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Pruning backups failed: {ex}");
			}
		}

    }
}

