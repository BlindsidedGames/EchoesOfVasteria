using System;
using System.Globalization;
using Blindsided.SaveData;

namespace Blindsided
{
    public partial class Oracle
    {
        /// <summary>Commit the terminal state before another frame or run can proceed.</summary>
        public bool FinishHardcoreRun(string map, float distance)
        {
            if (!HasLoadedCurrentSlotData || !saveData.Hardcore || saveData.HardcoreFallen) return false;
            EventHandler.SaveData();
            saveData.HardcoreDeath = new GameData.HardcoreDeathRecord
            {
                Map = map, Distance = Math.Max(0, distance),
                DiedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            saveData.ActiveRun = null;
            if (SaveNowBlocking(CurrentSlot)) return true;
            RequireRuntimeReloadRecovery("The hardcore death could not be saved",
                new InvalidOperationException("The terminal checkpoint was rejected"), checkpointCommitted: false);
            return false;
        }
    }
}
