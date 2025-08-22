using System;
using UnityEngine;

namespace Blindsided
{
    public partial class Oracle
    {

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
					// Save current slot using new system only
					SaveToFile();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Pre-switch backup failed: {ex}");
			}

			// Stop autosave BEFORE switching slots so no autosave can write the old slot's data
			// into the new slot's file due to _settings changing mid-cycle.
			StopAutosaveLoop();

			CurrentSlot = clamped;
            PlayerPrefs.SetInt(SlotPrefKey, CurrentSlot);
            PlayerPrefs.Save();
            _settings = new ES3Settings(_fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };
            EventHandler.ResetData();
            // Clear transient runtime meeting flags to avoid cross-file bleed
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Clear();
            Load();
			// Ensure all systems reload their state for the new slot
			EventHandler.LoadData();
			// Restart autosave with the standard interval after a slot switch
			RestartAutosaveLoop(FirstAutosaveDelaySeconds);
        }

    }
}

