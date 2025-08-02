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

namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using Easy Save 3 with
    ///     • caching enabled   • 8 KB buffer   • periodic cloud uploads   • one backup / session
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
            SteamCloudSync.Instance.SetFileName(_fileName);
            //SteamCloudSync.Instance.Download(); // ensure local copy is up-to-date
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

        private float _lastFlush;
        private const float FlushInterval = 120f; // disk write every 2 min

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
            Load();
            StartCoroutine(LoadMainScene());
            InvokeRepeating(nameof(SaveToFile), 10, 10);
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
            SaveToFile(false);
            ES3.StoreCachedFile(_fileName);
            SteamCloudSync.Instance.Upload();
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
            SteamCloudSync.Instance.SetFileName(_fileName);
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
            SteamCloudSync.Instance.SkipNextDownload();
            SteamCloudSync.Instance.QueueUploadAfterSceneLoad();
            var prefs = saveData.SavedPreferences;
            saveData = new GameData();
            saveData.SavedPreferences = prefs;
            EventHandler.ResetData();
            EventHandler.LoadData();
            SaveToFile(false);
            ES3.StoreCachedFile(_fileName);
            SceneManager.LoadScene(0);
            StartCoroutine(LoadMainScene());
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

            ES3.Save(_dataName, saveData, _settings); // write to cache

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
            loaded = true;
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
            saveData.PinnedQuests ??= new HashSet<string>();
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