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
            // Clear transient runtime meeting flags to avoid cross-file bleed
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Clear();
            EventHandler.LoadData();
            SaveToFile();
            wipeInProgress = false;
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

        // Regression prompt test removed

    }
}

