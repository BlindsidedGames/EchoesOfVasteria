using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using TimelessEchoes.Upgrades;

namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using Easy Save 3 with
    ///     • RAM-cached writes   • 8 KB buffer   • periodic flush   • one backup / session
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

            _settings = new ES3Settings(_fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };
            ES3.CacheFile(_fileName); // pull existing save into RAM
        }

        #endregion

        #region Inspector fields

        [TabGroup("SaveData", "Beta")] public bool beta;
        [TabGroup("SaveData", "Beta")] public int betaSaveIteration;

        private string _dataName => (beta ? $"Beta{betaSaveIteration}" : "") + "Data";
        private string _fileName => (beta ? $"Beta{betaSaveIteration}" : "") + "Sd.es3";

        [TabGroup("SaveData")] public SaveData.SaveData saveData = new();

        #endregion

        #region Private fields

        private ES3Settings _settings;
        private bool loaded;

        private float _lastFlush;
        private const float FlushInterval = 120f; // disk write every 2 min

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
            Load();
            InvokeRepeating(nameof(SaveToCache), 10, 10);
        }

        private void Update()
        {
            if (loaded) saveData.PlayTime += Time.deltaTime;
        }

        private void OnApplicationQuit()
        {
            FlushToDisk();
            SafeCreateBackup();
        }

        private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying)
            {
                SaveToCache(); // write the latest state into the cache
                ES3.StoreCachedFile(_fileName); // copy RAM ➜ disk immediately
            }
        }


#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
                FlushToDisk();
        }
#endif

        #endregion

        #region Editor buttons

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipePreferences()
        {
            saveData.SavedPreferences = new SaveData.SaveData.Preferences();
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipeAllData()
        {
            EventHandler.ResetData();
            saveData = new SaveData.SaveData();
            FlushToDisk();
            SceneManager.LoadScene(0);
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void LoadFromClipboard()
        {
            var bytes = beta
                ? Encoding.ASCII.GetBytes(GUIUtility.systemCopyBuffer)
                : Convert.FromBase64String(GUIUtility.systemCopyBuffer);

            saveData = SerializationUtility.DeserializeValue<SaveData.SaveData>(bytes, DataFormat.JSON);
            SaveToCache();
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

        private void SaveToCache()
        {
            EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            ES3.Save(_dataName, saveData, _settings); // RAM-only

            if (Time.unscaledTime - _lastFlush > FlushInterval)
                FlushToDisk();
        }

        private void FlushToDisk()
        {
            ES3.StoreCachedFile(_fileName); // RAM ➜ file
            _lastFlush = Time.unscaledTime;
        }

        private void Load()
        {
            loaded = false;
            saveData = new SaveData.SaveData();

            try
            {
                saveData = ES3.Load<SaveData.SaveData>(_dataName, _settings);
            }
            catch
            {
                if (ES3.RestoreBackup(_fileName))
                {
                    Debug.LogWarning("Backup restored; re-loading.");
                    ES3.CacheFile(_fileName);
                    saveData = ES3.Load<SaveData.SaveData>(_dataName, _settings);
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
            saveData.HeroStates ??= new Dictionary<string, SaveData.SaveData.HeroState>();
            saveData.GlobalKillCounts ??= new Dictionary<string, int>();
            saveData.ResourceAmounts ??= new Dictionary<Resource, int>();
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