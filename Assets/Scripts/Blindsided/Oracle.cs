using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TimelessEchoes;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Blindsided.Utilities;

namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using Easy Save 3 with
    ///     • caching enabled   • 8 KB buffer   • one backup / session
    /// </summary>
    [DefaultExecutionOrder(0)]
    public class Oracle : SerializedMonoBehaviour
    {
        #region Singleton

        public static Oracle oracle;

        private void Awake()
        {
            if (oracle == null)
            {
                oracle = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CurrentSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPrefKey, 0), 0, 2);
            _settings = new ES3Settings(_fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };
            wipeInProgress = false;
            TryMigrateFromBetaIfNeeded();
        }

        #endregion

        #region Inspector fields

        [TabGroup("SaveData", "Beta")] public bool beta;
        [TabGroup("SaveData", "Beta")] public int betaSaveIteration;

		[TabGroup("SaveData")] [ShowInInspector] public int CurrentSlot { get; private set; }

        private string _dataName => (beta ? $"Beta{betaSaveIteration}" : "") + $"Data{CurrentSlot}";
        private string _fileName => (beta ? $"Beta{betaSaveIteration}" : "") + $"Sd{CurrentSlot}.es3";

        public string DataName => _dataName;
        public string FileName => _fileName;
        public ES3Settings Settings => _settings;

		[TabGroup("SaveData")] public GameData saveData = new();

		[Header("Backups")] 
		[Tooltip("How many timestamped session backups to keep per slot in PersistentDataPath/Backups/<FileNameWithoutExtension>.")]
		[Range(1, 50)] public int backupsToKeepPerSlot = 10;

        #endregion

        #region Private fields

        private ES3Settings _settings;
        private bool loaded;
        private bool wipeInProgress;
        private const string SlotPrefKey = "SaveSlot";
        private const string BetaMigrationPrefKey = "BetaToLiveMigrationDone";

        // Regression detection thresholds and state
        private const float PlaytimeRegressionToleranceSeconds = 60f; // allow minor discrepancies
        private const float CompletionRegressionTolerance = 0.25f;    // percent points
        [ShowInInspector, ReadOnly] public bool RegressionDetected { get; private set; }
        [ShowInInspector, ReadOnly] public string RegressionMessage { get; private set; }

        // Detailed deltas for the regression prompt
        private float _lastPlaytimeDropSec;
        private float _lastCompletionDropPct;
        private double _lastMinutesNewer;

        [Header("Regression Confirmation UI")] [TabGroup("SaveData")]
        [SerializeField] private GameObject regressionConfirmWindow;
        [SerializeField] private Button regressionYesButton;
        [SerializeField] private Button regressionNoButton;
        [SerializeField] private TMP_Text regressionMessageText;
		[SerializeField] private TMP_Text regressionYesText;
		[SerializeField] private TMP_Text regressionNoText;

		// Defer showing load-failure notice until UI is ready
		private bool _pendingLoadFailureNotice;
		private string _pendingLoadFailureMessage;

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            Load();
            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;
            StartCoroutine(LoadMainScene());
			InvokeRepeating(nameof(SaveToFile), 1, 30);

            // Wire up regression confirmation UI if present
            if (regressionYesButton != null)
            {
                regressionYesButton.onClick.RemoveAllListeners();
                regressionYesButton.onClick.AddListener(ConfirmRegressionKeepLoaded);
            }
            if (regressionNoButton != null)
            {
                regressionNoButton.onClick.RemoveAllListeners();
                regressionNoButton.onClick.AddListener(AttemptRestoreBackupAndReload);
            }
        }

		private IEnumerator LoadMainScene()
        {
            var async = SceneManager.LoadSceneAsync("Main");
            while (!async.isDone)
                yield return null;

            yield return null; // wait one frame for scene initialization
            EventHandler.LoadData();

			// If a load failure occurred before UI existed, show the notice now
			if (_pendingLoadFailureNotice)
			{
				TryShowLoadFailureWindow(_pendingLoadFailureMessage);
				_pendingLoadFailureNotice = false;
				_pendingLoadFailureMessage = null;
			}
        }

        private void Update()
        {
            if (loaded) saveData.PlayTime += Time.deltaTime;
        }

		private void OnApplicationQuit()
        {
            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            if (tracker != null && tracker.RunInProgress)
                tracker.AbandonRun();
			SaveToFile();
			ES3.StoreCachedFile(_fileName);
			SafeCreateBackup();
			CreateRotatingBackup();
        }

		private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying && !wipeInProgress && oracle == this && _settings != null)
            {
                SaveToFile(); // save the latest state immediately
                ES3.StoreCachedFile(_fileName);
				CreateRotatingBackup();
            }
        }


#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                SaveToFile();
                ES3.StoreCachedFile(_fileName);
            }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveToFile();
                ES3.StoreCachedFile(_fileName);
            }
        }
#endif

        #endregion

        #region Slot management

		public void SelectSlot(int slot)
        {
			var clamped = Mathf.Clamp(slot, 0, 2);
			if (clamped == CurrentSlot)
				return;

			// Save and backup the current slot before switching
			try
			{
				if (_settings != null)
				{
					SaveToFile();
					ES3.StoreCachedFile(_fileName);
					CreateRotatingBackup();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Pre-switch backup failed: {ex}");
			}

			CurrentSlot = clamped;
            PlayerPrefs.SetInt(SlotPrefKey, CurrentSlot);
            PlayerPrefs.Save();
            _settings = new ES3Settings(_fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };
            Load();
        }

        #endregion

        #region Editor buttons

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipePreferences()
        {
            saveData.SavedPreferences = new GameData.Preferences();
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipeAllData()
        {
            wipeInProgress = true;
            var prefs = saveData.SavedPreferences;
            saveData = new GameData();
            saveData.SavedPreferences = prefs;
            EventHandler.ResetData();
            EventHandler.LoadData();
            SaveToFile();
            ES3.StoreCachedFile(_fileName);
            SceneManager.LoadScene(0);
            StartCoroutine(LoadMainScene());
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void LoadFromClipboard()
        {
            var bytes = beta
                ? Encoding.ASCII.GetBytes(GUIUtility.systemCopyBuffer)
                : Convert.FromBase64String(GUIUtility.systemCopyBuffer);

            saveData = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.JSON);
            SaveToFile();
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void SaveToClipboard()
        {
            var bytes = SerializationUtility.SerializeValue(saveData, DataFormat.JSON);
            GUIUtility.systemCopyBuffer = beta ? Encoding.UTF8.GetString(bytes) : Convert.ToBase64String(bytes);
        }

        [TabGroup("SaveData", "Buttons")]
        [Button("Test Regression Prompt")]
        public void TestRegressionPrompt()
        {
            // Seed some dummy deltas
            _lastPlaytimeDropSec = 600f; // 10 minutes
            _lastCompletionDropPct = 2.5f; // 2.5%
            _lastMinutesNewer = 45.0; // 45 minutes

            RegressionDetected = true;
            RegressionMessage = "[TEST] Simulated regression for preview";

            float loadedPt = (float)saveData.PlayTime;
            float loadedComp = saveData.CompletionPercentage;
            float prevPt = loadedPt + _lastPlaytimeDropSec;
            float prevComp = loadedComp + _lastCompletionDropPct;

            TryShowRegressionWindow(CurrentSlot, prevPt, prevComp, loadedPt, loadedComp);
        }

        #endregion

        #region Core save / load

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
						throw new KeyNotFoundException($"No compatible save key found in '{_fileName}'.");
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

        #endregion

        #region Helpers

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
                DismissRegressionWindow();

                // Reload the active scene(s) to ensure all systems pick up the new data
                // We already have a helper to load Main on boot, but here reload current.
                var active = SceneManager.GetActiveScene();
                SceneManager.LoadScene(active.name);
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

        #endregion
    }
}