using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using the new save system.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public class Oracle : SerializedMonoBehaviour
    {
        public static Oracle oracle;
        // Autosave management
        private Coroutine _autosaveRoutine;
        private const float FirstAutosaveDelaySeconds = 30f;
        private const float AutosaveIntervalSeconds = 30f;

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
            wipeInProgress = false;
        }

        [TabGroup("SaveData")] public bool demo;
        [TabGroup("SaveData", "Beta")] public bool beta;
        [TabGroup("SaveData", "Beta")] public int betaSaveIteration;

		[TabGroup("SaveData")] [ShowInInspector] public int CurrentSlot { get; private set; }

		[TabGroup("SaveData")] public GameData saveData = new();

        private bool loaded;
        private bool wipeInProgress;
        private const string SlotPrefKey = "SaveSlot";
        

        // Defer showing load-failure notice until UI is ready

        private void Start()
        {
            Load();
            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;
            StartCoroutine(LoadMainScene());
        }

        private IEnumerator LoadMainScene()
        {
            var async = SceneManager.LoadSceneAsync("Main");
            while (!async.isDone)
                yield return null;

            yield return null; // wait one frame for scene initialization
            EventHandler.LoadData();
            // Start autosave only after data is loaded and applied in the main scene.
            // First autosave should occur 30 seconds after load, then every 30 seconds.
            StartAutosaveLoop(FirstAutosaveDelaySeconds);
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
            SaveToFile();
        }

        private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying && !wipeInProgress && oracle == this)
            {
                SaveToFile(); // save the latest state immediately (new system only)
                StopAutosaveLoop();
            }
        }

#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                SaveToFile();
            }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveToFile();
            }
        }
#endif

        private IEnumerator AutosaveRoutine(float initialDelay, float interval)
        {
            if (initialDelay > 0)
                yield return new WaitForSecondsRealtime(initialDelay);

            while (true)
            {
                // Skip autosave only during explicit wipes
                if (!wipeInProgress)
                {
                    try
                    {
                        SaveToFile();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Autosave failed: {ex}");
                    }
                }

                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private void StartAutosaveLoop(float initialDelaySeconds)
        {
            StopAutosaveLoop();
            _autosaveRoutine = StartCoroutine(AutosaveRoutine(initialDelaySeconds, AutosaveIntervalSeconds));
        }

        private void StopAutosaveLoop()
        {
            if (_autosaveRoutine != null)
            {
                try { StopCoroutine(_autosaveRoutine); } catch { }
                _autosaveRoutine = null;
            }
        }

        private void RestartAutosaveLoop(float initialDelaySeconds)
        {
            StartAutosaveLoop(initialDelaySeconds);
        }

        private void SaveToFile()
        {
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New save system only
            try
            {
                SaveInternal(Mathf.Clamp(CurrentSlot, 0, 2));
            }
            catch (Exception ex)
            {
                Debug.LogError($"New save system Save failed: {ex}");
            }

            // Keep PlayerPrefs metadata in sync for UI which still reads playtime/completion
            PersistSlotMetadataToPlayerPrefs();

            // Clear deleted marker after first successful save
            try
            {
                var deletedKey = $"Slot{CurrentSlot}_Deleted";
                if (PlayerPrefs.GetInt(deletedKey, 0) == 1)
                {
                    PlayerPrefs.DeleteKey(deletedKey);
                    PlayerPrefs.Save();
                }
            }
            catch { }
        }

        private void Load()
        {
            loaded = false;
            saveData = new GameData();
            var deletedMarkerKey = $"Slot{Mathf.Clamp(CurrentSlot, 0, 2)}_Deleted";
            var wasIntentionallyDeleted = false;
            try { wasIntentionallyDeleted = PlayerPrefs.GetInt(deletedMarkerKey, 0) == 1; } catch { wasIntentionallyDeleted = false; }

            // Prefer new save system first
            try
            {
                var slotName = $"Save{Mathf.Clamp(CurrentSlot, 0, 2) + 1}";
                SaveManager.Instance.SetCurrentSlot(slotName);
                var result = SaveManager.Instance.LoadAsync().GetAwaiter().GetResult();
                if (result.ok && result.data != null)
                {
                    saveData = result.data;
                    ApplyPostLoadCommon();
                    PersistSlotMetadataToPlayerPrefs();
                    return;
                }
                else
                {
                    // Load failed (not an exception), back up existing files if present before creating a new game
                    if (!wasIntentionallyDeleted)
                        BackupSlotDirectoryIfExists(CurrentSlot, "load_fail");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"New save system load failed or missing. {ex.Message}");
                // Exception trying to read the slot — back it up if it exists so we don't overwrite evidence
                if (!wasIntentionallyDeleted)
                    BackupSlotDirectoryIfExists(CurrentSlot, "load_exception");
            }

            // No legacy fallback: create a new game
            if (wasIntentionallyDeleted)
            {
                Debug.Log($"No save found for intentionally deleted slot {CurrentSlot}; starting new game.");
                saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                Debug.LogError($"Load failed or missing save for slot {CurrentSlot}.");
                saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                var message = BuildLoadFailureMessage();
                if (string.IsNullOrEmpty(message))
                    message = $"Save data for File {CurrentSlot + 1} could not be loaded. A new game will be created.";
                Debug.LogWarning(message);
            }
            ApplyPostLoadCommon();
        }

        private void BackupSlotDirectoryIfExists(int index, string reason = null)
        {
            try
            {
                var clamped = Mathf.Clamp(index, 0, 2);
                var slotName = $"Save{clamped + 1}";
                var savesDir = Path.Combine(Application.persistentDataPath, "Saves");
                var slotDir = Path.Combine(savesDir, slotName);
                if (!Directory.Exists(slotDir)) return;

                var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                var suffix = string.IsNullOrWhiteSpace(reason) ? "backup" : reason;
                // Place full-slot backups inside the slot's own Archive/ folder to keep things tidy
                var archiveRoot = Path.Combine(slotDir, "Archive");
                Directory.CreateDirectory(archiveRoot);
                var backupBaseName = $"{suffix}_{stamp}";
                var destDir = Path.Combine(archiveRoot, backupBaseName);
                var attempt = 0;
                while (Directory.Exists(destDir))
                {
                    attempt++;
                    destDir = Path.Combine(archiveRoot, backupBaseName + "_" + attempt);
                }
                Directory.CreateDirectory(destDir);

                // Move all top-level files and folders (except the Archive folder itself) into the backup subfolder
                foreach (var file in Directory.GetFiles(slotDir))
                {
                    var name = Path.GetFileName(file);
                    // Skip files already under Archive (shouldn't appear here) and skip temp file if it's currently in-use
                    var target = Path.Combine(destDir, name);
                    try { File.Move(file, target); } catch { }
                }
                foreach (var dir in Directory.GetDirectories(slotDir))
                {
                    var name = Path.GetFileName(dir);
                    if (string.Equals(name, "Archive", StringComparison.OrdinalIgnoreCase)) continue;
                    var target = Path.Combine(destDir, name);
                    try { Directory.Move(dir, target); } catch { }
                }

                Debug.LogWarning($"Backed up save slot '{slotName}' to '{slotName}/Archive/{Path.GetFileName(destDir)}' due to {suffix}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to back up slot directory: {ex.Message}");
            }
        }

        private string BuildLoadFailureMessage()
        {
            try
            {
                var slot = CurrentSlot + 1;
                return
                    $"Save data for File {slot} could not be loaded. A new game will be created. You may attempt to restore a backup.";
            }
            catch
            {
                return null;
            }
        }

        public void SaveToSlot(int slotIndex)
        {
            var index = Mathf.Clamp(slotIndex, 0, 2);
            if (!wipeInProgress)
                EventHandler.SaveData();
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New system write for the targeted slot
            try
            {
                SaveInternal(index);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"New save system SaveToSlot failed: {ex.Message}");
            }

            PersistSlotMetadataToPlayerPrefs(index);
        }

        private void ApplyPostLoadCommon()
        {
            NullCheckers();
            loaded = true;
            AwayForSeconds();
            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }

        private void SaveInternal(int index)
        {
            var slotName = $"Save{index + 1}";
            SaveManager.Instance.SetCurrentSlot(slotName);
            SaveManager.Instance.SaveAsync(saveData).GetAwaiter().GetResult();
        }

        private void NullCheckers()
        {
            saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            saveData.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            saveData.EnemyKills ??= new Dictionary<string, double>();
            saveData.CompletedNpcTasks ??= new HashSet<string>();
            saveData.PinnedQuests ??= new List<string>();
            // Gear system collections
            saveData.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
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
            if (saveData.DisciplePercent <= 0f)
                saveData.DisciplePercent = 0.1f;
            // Cauldron system collections and totals
            saveData.CauldronCardCounts ??= new Dictionary<string, int>();
            saveData.CauldronTotals ??= new GameData.CauldronTotalsRecord();
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

        public void SelectSlot(int slot)
        {
            var clamped = Mathf.Clamp(slot, 0, 2);
            if (clamped == CurrentSlot)
                return;

            // Save and backup the current slot before switching
            try
            {
                // Save current slot using new system only
                SaveToFile();
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
            EventHandler.ResetData();
            // Clear transient runtime meeting flags to avoid cross-file bleed
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Clear();
            Load();
            // Ensure all systems reload their state for the new slot
            EventHandler.LoadData();
            // Restart autosave with the standard interval after a slot switch
            RestartAutosaveLoop(FirstAutosaveDelaySeconds);
        }

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

        

        public void PersistSlotMetadataToPlayerPrefs(int? slotIndex = null)
        {
            var index = Mathf.Clamp(slotIndex ?? CurrentSlot, 0, 2);
            var prefix = beta ? $"Beta{betaSaveIteration}" : string.Empty;
            var completionKey = $"{prefix}Slot{index}_Completion";
            var playtimeKey = $"{prefix}Slot{index}_Playtime";
            var dateKey = $"{prefix}Slot{index}_Date";

            PlayerPrefs.SetFloat(completionKey, saveData.CompletionPercentage);
            PlayerPrefs.SetFloat(playtimeKey, (float)saveData.PlayTime);
            PlayerPrefs.SetString(dateKey, saveData.DateQuitString);
            PlayerPrefs.Save();
        }

        

    }
}
