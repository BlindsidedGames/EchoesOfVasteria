using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Blindsided.SaveData;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
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

        [Header("Seasonal Leaderboard")]
        [SerializeField] private string minimumSeasonalGameVersion = "0.0.0"; // configurable threshold
        public string MinimumSeasonalGameVersion => minimumSeasonalGameVersion;

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

        /// <summary>
        /// Returns true if this save qualifies for the Seasonal leaderboard based on GameVersionCreated.
        /// </summary>
        public bool IsSeasonalEligible()
        {
            try
            {
                var created = saveData != null ? saveData.GameVersionCreated : null;
                if (string.IsNullOrEmpty(created) || string.IsNullOrEmpty(minimumSeasonalGameVersion))
                    return false;
                return CompareVersionStrings(created, minimumSeasonalGameVersion) >= 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Compares two version strings in the form of dotted numeric segments, ignoring non-numeric suffixes.
        /// Returns -1, 0, 1 for a &lt; b, ==, &gt;.
        /// </summary>
        private static int CompareVersionStrings(string a, string b)
        {
            if (a == b) return 0;
            if (string.IsNullOrEmpty(a)) return -1;
            if (string.IsNullOrEmpty(b)) return 1;

            int[] pa = ParseVersionToIntArray(a);
            int[] pb = ParseVersionToIntArray(b);
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int ai = i < pa.Length ? pa[i] : 0;
                int bi = i < pb.Length ? pb[i] : 0;
                if (ai < bi) return -1;
                if (ai > bi) return 1;
            }
            return 0;
        }

        private static int[] ParseVersionToIntArray(string v)
        {
            // Split by dots; for each segment, parse leading digits (ignore letters like 'f1')
            var parts = v.Split('.');
            var list = new System.Collections.Generic.List<int>(parts.Length);
            foreach (var p in parts)
            {
                int val = 0;
                bool any = false;
                // parse from start; stop at first non-digit
                for (int i = 0; i < p.Length; i++)
                {
                    if (char.IsDigit(p[i]))
                    {
                        any = true;
                        val = val * 10 + (p[i] - '0');
                    }
                    else break;
                }
                list.Add(any ? val : 0);
            }
            return list.ToArray();
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
                        Blindsided.Utilities.FeedbackForm.SubmitException(
                            "Save.Autosave",
                            ex,
                            $"slot: {Mathf.Clamp(CurrentSlot, 0, 2) + 1}");
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
            // Prevent overwriting an existing save with an uninitialized/blank file
            // if the game has not completed a successful Load yet. Still allow
            // saves during an intentional wipe operation.
            if (!wipeInProgress && !loaded)
            {
                Debug.LogWarning("Skipping save: data has not been loaded yet.");
                Blindsided.Utilities.FeedbackForm.Submit(
                    "Save.SkipBeforeLoad",
                    $"slot: {Mathf.Clamp(CurrentSlot, 0, 2) + 1}\n" +
                    "Reason: Save attempted before any successful Load.\n" +
                    $"AppVersion: {Application.version}\nPlatform: {Application.platform}\nUnity: {Application.unityVersion}");
                return;
            }
            if (!wipeInProgress)
                EventHandler.SaveData();
            // Update version metadata on each save (creation is only set if absent)
            if (string.IsNullOrEmpty(saveData.GameVersionCreated))
                saveData.GameVersionCreated = Application.version;
            saveData.LastGameVersion = Application.version;
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New save system only
            try
            {
                SaveInternal(Mathf.Clamp(CurrentSlot, 0, 2));
            }
            catch (Exception ex)
            {
                Debug.LogError($"New save system Save failed: {ex}");
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.SaveToFile",
                    ex,
                    $"slot: {Mathf.Clamp(CurrentSlot, 0, 2) + 1}");
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
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.Load",
                    ex,
                    $"slot: {Mathf.Clamp(CurrentSlot, 0, 2) + 1}");
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
            // Guard against saving before a successful load has occurred
            if (!wipeInProgress && !loaded)
            {
                Debug.LogWarning("Skipping SaveToSlot: data has not been loaded yet.");
                Blindsided.Utilities.FeedbackForm.Submit(
                    "Save.SkipBeforeLoad.SaveToSlot",
                    $"slot: {Mathf.Clamp(slotIndex, 0, 2) + 1}\n" +
                    "Reason: SaveToSlot attempted before any successful Load.\n" +
                    $"AppVersion: {Application.version}\nPlatform: {Application.platform}\nUnity: {Application.unityVersion}");
                return;
            }
            var index = Mathf.Clamp(slotIndex, 0, 2);
            if (!wipeInProgress)
                EventHandler.SaveData();
            // Update version metadata on each save (creation is only set if absent)
            if (string.IsNullOrEmpty(saveData.GameVersionCreated))
                saveData.GameVersionCreated = Application.version;
            saveData.LastGameVersion = Application.version;
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            // New system write for the targeted slot
            try
            {
                SaveInternal(index);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"New save system SaveToSlot failed: {ex.Message}");
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.SaveToSlot",
                    ex,
                    $"slot: {Mathf.Clamp(index, 0, 2) + 1}");
            }

            PersistSlotMetadataToPlayerPrefs(index);
        }

        private void ApplyPostLoadCommon()
        {
            NullCheckers();
            // Backfill created version if missing (legacy saves or fresh new games)
            if (string.IsNullOrEmpty(saveData.GameVersionCreated))
                saveData.GameVersionCreated = Application.version;
            // Disable any legacy stat->gear migration and sanitize the migrated duck helmet once
            try { EnsureMigrationDisabledAndSanitizeDuckHelmet(); } catch { }
            loaded = true;
            AwayForSeconds();
            if (saveData.SavedPreferences.OfflineTimeAutoDisable)
                saveData.SavedPreferences.OfflineTimeActive = false;
        }

        private void EnsureMigrationDisabledAndSanitizeDuckHelmet()
        {
            if (saveData == null) return;
            // Hard-disable any migration logic by marking it done
            saveData.StatUpgradesMigratedToGear = true;

            // One-time check; if already sanitized, do nothing further
            if (saveData.DuckHelmetSanitized)
                return;

            try
            {
                var equipment = saveData.EquipmentBySlot;
                if (equipment == null)
                {
                    saveData.DuckHelmetSanitized = true;
                    return;
                }

                if (!equipment.TryGetValue("Helmet", out var helm) || helm == null)
                {
                    saveData.DuckHelmetSanitized = true;
                    return;
                }

                // Only operate on the migrated duck helmet: rarity must be null/empty
                if (!string.IsNullOrWhiteSpace(helm.rarity))
                {
                    saveData.DuckHelmetSanitized = true;
                    return;
                }

                // Resolve stat definitions by hero mapping for robust ID/name matches
                var allStats = AssetCache.GetAll<StatDefSO>(string.Empty) ?? System.Array.Empty<StatDefSO>();
                var moveDef = allStats.FirstOrDefault(s => s != null && s.heroMapping == HeroStatMapping.MoveSpeed);
                var atkDef = allStats.FirstOrDefault(s => s != null && s.heroMapping == HeroStatMapping.AttackRate);

                bool Matches(string statId, StatDefSO def)
                {
                    if (def == null || string.IsNullOrWhiteSpace(statId)) return false;
                    if (!string.IsNullOrWhiteSpace(def.id) && statId.Equals(def.id, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                    return statId.Equals(def.name, System.StringComparison.OrdinalIgnoreCase);
                }

                // No affixes to inspect
                if (helm.affixes == null)
                {
                    saveData.DuckHelmetSanitized = true;
                    return;
                }

                // Remove Move Speed affix from the migrated helmet
                if (moveDef != null)
                {
                    helm.affixes.RemoveAll(a => a != null && Matches(a.statId, moveDef));
                }

                // If Attack Rate value > 1.5, reduce to 1.2 (not a clamp). Apply to all matching affixes.
                if (atkDef != null)
                {
                    for (int i = 0; i < helm.affixes.Count; i++)
                    {
                        var a = helm.affixes[i];
                        if (a == null) continue;
                        if (Matches(a.statId, atkDef) && a.value > 1.5f)
                        {
                            a.value = 1.2f;
                            helm.affixes[i] = a;
                        }
                    }
                }

                // Persist sanitized record back (dictionary contains reference, but do it explicitly for clarity)
                equipment["Helmet"] = helm;
            }
            finally
            {
                // Mark as sanitized regardless to keep it a one-time check
                saveData.DuckHelmetSanitized = true;
            }
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
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.PreSwitchBackup",
                    ex,
                    $"fromSlot: {Mathf.Clamp(CurrentSlot, 0, 2) + 1}");
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
