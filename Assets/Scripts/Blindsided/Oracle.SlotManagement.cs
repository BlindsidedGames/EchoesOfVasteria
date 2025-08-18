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
			// Stop autosave while switching to avoid snapshotting a half-applied state
			StopAutosaveLoop();
            Load();
			// Ensure all systems reload their state for the new slot
			EventHandler.LoadData();
			// Restart autosave with the standard interval after a slot switch
			RestartAutosaveLoop(FirstAutosaveDelaySeconds);
        }

    }
}

