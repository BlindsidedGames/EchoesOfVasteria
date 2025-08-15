using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Blindsided.SaveData;

namespace Blindsided
{
    public partial class Oracle
    {

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

    }
}

