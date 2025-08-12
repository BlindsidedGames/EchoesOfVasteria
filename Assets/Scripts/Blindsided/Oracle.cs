using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        #endregion

        #region Private fields

        private ES3Settings _settings;
        private bool loaded;
        private bool wipeInProgress;
        private const string SlotPrefKey = "SaveSlot";

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

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            Load();
            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;
            StartCoroutine(LoadMainScene());
            InvokeRepeating(nameof(SaveToFile), 10, 10);

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
        }

        private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying && !wipeInProgress && oracle == this && _settings != null)
            {
                SaveToFile(); // save the latest state immediately
                ES3.StoreCachedFile(_fileName);
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
#endif

        #endregion

        #region Slot management

        public void SelectSlot(int slot)
        {
            CurrentSlot = Mathf.Clamp(slot, 0, 2);
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

            // Ensure PlayerPrefs metadata for the current slot is kept in sync for all saves/autosaves
            PersistSlotMetadataToPlayerPrefs();
        }

        private void Load()
        {
            loaded = false;
            saveData = new GameData();

            var backupPath = _fileName + ".bac";

            if (!ES3.FileExists(_fileName) && ES3.FileExists(backupPath))
                ES3.RestoreBackup(_fileName);

            try
            {
                saveData = ES3.Load<GameData>(_dataName, _settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e}");
                if (ES3.FileExists(backupPath) && ES3.RestoreBackup(_fileName))
                {
                    Debug.LogWarning("Backup restored; re-loading.");
                    saveData = ES3.Load<GameData>(_dataName, _settings);
                }
                else
                {
                    saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                    Debug.LogWarning($"Save not found; new game @ {saveData.DateStarted}");
                }
            }

            NullCheckers();

            // Check for potential regression relative to what this device previously recorded
            DetectRegressionAgainstPlayerPrefs();

            loaded = true;
            AwayForSeconds();

            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }

        #endregion

        #region Helpers

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
                regressionMessageText.text =
                    $"Detected older progress for File {slotIndex + 1}.\n{summary}\nKeep currently loaded data?";
            }

            regressionConfirmWindow.SetActive(true);
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

        /// <summary>
        /// Called by the "No" button: attempts to restore the Easy Save .bac backup for the current slot
        /// and reloads the scene so the restored data is applied everywhere.
        /// </summary>
        [Button]
        public void AttemptRestoreBackupAndReload()
        {
            try
            {
                var restored = ES3.RestoreBackup(_settings);
                if (!restored)
                {
                    Debug.LogWarning("No backup found to restore.");
                }
                else
                {
                    Debug.Log("Backup restored. Reloading save and scene.");
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

            PersistSlotMetadataToPlayerPrefs(index);
        }

        private void NullCheckers()
        {
            saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            saveData.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            saveData.EnemyKills ??= new Dictionary<string, double>();
            saveData.CompletedNpcTasks ??= new HashSet<string>();
            saveData.PinnedQuests ??= new List<string>();
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

        #endregion
    }
}