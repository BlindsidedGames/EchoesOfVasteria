using System;
using Blindsided;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>Existing verified slot operations, independent of the UI renderer.</summary>
    public static class SaveSlotActions
    {
        public static string SlotKey(int index, string field)
        {
            var oracle = Oracle.oracle;
            if (oracle == null)
                return $"Slot{index}_{field}";
            return oracle.GetSlotPlayerPrefsKey(index, field);
        }

        public static bool SaveSlot(int index)
        {
            try
            {
                var oracle = Oracle.oracle;
                if (oracle == null || !oracle.SaveToSlot(index))
                {
                    Debug.LogError($"Failed to save File {index + 1} safely.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save slot {index}: {ex}");
                return false;
            }
        }

        public static bool DeleteSlot(int index)
        {
            try
            {
                var oracle = Oracle.oracle;
                if (oracle == null || !oracle.CanBeginSaveMutation)
                {
                    Debug.LogWarning("A save-slot operation is already in progress.");
                    return false;
                }

                var slotName = oracle.GetSlotDirectoryName(index);
                var result = SaveManager.Instance
                    .DeleteSlotDetailedAsync(slotName)
                    .GetAwaiter()
                    .GetResult();
                if (!result.Succeeded)
                {
                    Debug.LogError($"Failed to delete File {index + 1} safely: {result.Error}");
                    return false;
                }
                if (!string.IsNullOrEmpty(result.Error))
                    Debug.LogWarning(result.Error);

                try
                {
                    var deletedKey = oracle.GetSlotDeletedKey(index);
                    PlayerPrefs.SetInt(deletedKey, 1);
                    PlayerPrefs.DeleteKey(SlotKey(index, "Completion"));
                    PlayerPrefs.DeleteKey(SlotKey(index, "Playtime"));
                    PlayerPrefs.DeleteKey(SlotKey(index, "Date"));
                    PlayerPrefs.Save();
                }
                catch (Exception ex)
                {
                    // The tombstone is already authoritative. Stale UI metadata must not prevent
                    // the current slot from completing its fresh replacement transaction.
                    Debug.LogWarning($"File {index + 1} was deleted safely, but its UI metadata could not be cleared: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete File {index + 1} safely; no UI metadata was changed: {ex}");
                return false;
            }
        }
    }
}
