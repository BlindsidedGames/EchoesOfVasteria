using System;
using System.Globalization;
using Blindsided.SaveData;
using UnityEngine;

namespace Blindsided
{
    public partial class Oracle
    {
        [Serializable]
        private sealed class PendingHardcoreDeath
        {
            public string lineage;
            public string started;
            public GameData.HardcoreDeathRecord death;
        }

        private string PendingHardcoreDeathKey => GetSlotPlayerPrefsKey(CurrentSlot, "PendingHardcoreDeath");

        /// <summary>Commit the terminal state before another frame or run can proceed.</summary>
        public bool FinishHardcoreRun(string map, float distance)
        {
            if (!HasLoadedCurrentSlotData || !saveData.Hardcore || saveData.HardcoreFallen) return false;
            saveData.HardcoreDeath = new GameData.HardcoreDeathRecord
            {
                Map = map, Distance = Math.Max(0, distance),
                DiedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            RememberHardcoreDeath();
            saveData.ActiveRun = null;
            if (SaveCheckpointBlocking(CurrentSlot, false, "hardcore-death-save"))
            {
                ClearPendingHardcoreDeath();
                return true;
            }
            if (!_recoveryRequired)
                RequireRuntimeReloadRecovery("The hardcore death could not be saved",
                    new InvalidOperationException("The terminal checkpoint was rejected"), checkpointCommitted: false,
                    phase: "hardcore-death-save");
            return false;
        }

        private void RememberHardcoreDeath()
        {
            var lineage = SaveManager.Instance.GetKnownLineage(GetSlotDirectoryName(CurrentSlot));
            var pending = new PendingHardcoreDeath
            {
                lineage = lineage == Guid.Empty ? null : lineage.ToString("N"),
                started = saveData.DateStarted,
                death = saveData.HardcoreDeath
            };
            try
            {
                PlayerPrefs.SetString(PendingHardcoreDeathKey, JsonUtility.ToJson(pending));
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException ex) { Debug.LogWarning($"Could not record pending Hardcore death: {ex.GetType().Name}"); }
        }

        private bool RestorePendingHardcoreDeath(GameData candidate)
        {
            if (!candidate.Hardcore || !PlayerPrefs.HasKey(PendingHardcoreDeathKey)) return false;
            PendingHardcoreDeath pending;
            try { pending = JsonUtility.FromJson<PendingHardcoreDeath>(PlayerPrefs.GetString(PendingHardcoreDeathKey)); }
            catch (ArgumentException) { return false; }
            if (pending?.death == null) return false;
            var sameCharacter = string.IsNullOrEmpty(pending.lineage)
                ? !string.IsNullOrEmpty(pending.started) && pending.started == candidate.DateStarted
                : pending.lineage == SaveManager.Instance.GetKnownLineage(GetSlotDirectoryName(CurrentSlot)).ToString("N");
            if (!sameCharacter) return false;

            if (candidate.HardcoreFallen)
            {
                ClearPendingHardcoreDeath();
                return false;
            }
            candidate.HardcoreDeath = pending.death;
            candidate.ActiveRun = null;
            return true;
        }

        private void ClearPendingHardcoreDeath()
        {
            try
            {
                PlayerPrefs.DeleteKey(PendingHardcoreDeathKey);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException ex) { Debug.LogWarning($"Could not clear pending Hardcore death: {ex.GetType().Name}"); }
        }
    }
}
