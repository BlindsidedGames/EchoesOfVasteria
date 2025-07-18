using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TimelessEchoes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using Easy Save 3 with
    ///     • direct file writes   • 8 KB buffer   • periodic cloud uploads   • one backup / session
    /// </summary>
    public class Oracle : SerializedMonoBehaviour
    {
        #region Singleton

        public static Oracle oracle;

        private void Awake()
        {
            if (oracle == null)
            {
                oracle = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _settings = new ES3Settings(_fileName, ES3.Location.File)
            {
                bufferSize = 8192
            };
            SteamCloudSync.Instance.SetFileName(_fileName);
            //SteamCloudSync.Instance.Download(); // ensure local copy is up-to-date
            wipeInProgress = false;
        }

        #endregion

        #region Inspector fields

        [TabGroup("SaveData", "Beta")] public bool beta;
        [TabGroup("SaveData", "Beta")] public int betaSaveIteration;

        private string _dataName => (beta ? $"Beta{betaSaveIteration}" : "") + "Data";
        private string _fileName => (beta ? $"Beta{betaSaveIteration}" : "") + "Sd.es3";

        [TabGroup("SaveData")] public GameData saveData = new();

        #endregion

        #region Private fields

        private ES3Settings _settings;
        private bool loaded;
        private bool wipeInProgress;

        private float _lastFlush;
        private const float FlushInterval = 120f; // disk write every 2 min

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
            Load();
            InvokeRepeating(nameof(SaveToFile), 10, 10);
        }

        private void Update()
        {
            if (loaded) saveData.PlayTime += Time.deltaTime;
        }

        private void OnApplicationQuit()
        {
            SaveToFile(false);
            SteamCloudSync.Instance.Upload();
            SafeCreateBackup();
        }

        private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying && !wipeInProgress) SaveToFile(); // save the latest state immediately
        }


#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
                SaveToFile();
        }
#endif

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
            SteamCloudSync.Instance.SkipNextDownload();
            SteamCloudSync.Instance.QueueUploadAfterSceneLoad();
            var prefs = saveData.SavedPreferences;
            saveData = new GameData();
            saveData.SavedPreferences = prefs;
            EventHandler.ResetData();
            EventHandler.LoadData();
            SaveToFile(false);
            SceneManager.LoadScene(0);
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipeCloudData()
        {
            wipeInProgress = true;
            SteamCloudSync.Instance.SkipNextDownload();
            SteamCloudSync.Instance.QueueUploadAfterSceneLoad();
            var prefs = saveData.SavedPreferences;
            saveData = new GameData();
            saveData.SavedPreferences = prefs;
            EventHandler.ResetData();
            EventHandler.LoadData();
            SaveToFile(false);
            SceneManager.LoadScene(0);
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

        #endregion

        #region Core save / load

        private void SaveToFile()
        {
            SaveToFile(true);
        }

        private void SaveToFile(bool allowUpload)
        {
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            ES3.Save(_dataName, saveData, _settings); // direct file write

            if (allowUpload && Time.unscaledTime - _lastFlush > FlushInterval)
            {
                SteamCloudSync.Instance.Upload();
                _lastFlush = Time.unscaledTime;
            }
        }

        private void Load()
        {
            loaded = false;
            saveData = new GameData();

            try
            {
                saveData = ES3.Load<GameData>(_dataName, _settings);
            }
            catch
            {
                if (ES3.RestoreBackup(_fileName))
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
            loaded = true;
            EventHandler.LoadData();
            AwayForSeconds();

            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }

        #endregion

        #region Helpers

        private void NullCheckers()
        {
            saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            saveData.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            saveData.EnemyKills ??= new Dictionary<string, double>();
            saveData.CompletedNpcTasks ??= new HashSet<string>();
            saveData.BuffSlots ??= new List<string>(new string[5]);
            if (saveData.BuffSlots.Count < 5)
                while (saveData.BuffSlots.Count < 5)
                    saveData.BuffSlots.Add(null);
            if (saveData.UnlockedBuffSlots <= 0)
                saveData.UnlockedBuffSlots = 1;
            else if (saveData.UnlockedBuffSlots > 5)
                saveData.UnlockedBuffSlots = 5;
            saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            saveData.FishDonations ??= new Dictionary<string, double>();
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
                Debug.LogError($"Backup failure: {ex.Message}");
            }
        }

        #endregion

        #region Static colour tags

        [HideInInspector] public static string colorHighlight = "<color=#00FFFD>";
        [HideInInspector] public static string colorBlack = "<color=#030008>";
        [HideInInspector] public static string colourWhite = "<color=#DBDBDB>";
        [HideInInspector] public static string colorOrange = "<color=#DF9500>";
        [HideInInspector] public static string colorRed = "<color=#FF7D7D>";
        [HideInInspector] public static string naniteHighlight = "<sprite=0 color=#00FFFD>";

        #endregion
    }
}