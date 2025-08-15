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
            Load();
        }

    }
}

