using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.UI.Toolkit;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TimelessEchoes.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using the new save system.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public partial class Oracle : SerializedMonoBehaviour
    {
        public static Oracle oracle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            oracle = null;
        }
        // Autosave management
        private Coroutine _autosaveRoutine;
        private Coroutine _saveCoordinatorRoutine;
        private Coroutine _recoverySceneTransitionRoutine;
        private Task<SaveWriteResult> _activeSaveTask;
        private int _activeSaveSlot = -1;
        private bool _activeSaveIsReplacement;
        private bool _savePending;
        private bool _isQuitting;
        private bool _finalSaveCompleted;
        private bool _slotTransitionInProgress;
        private bool _loadingSceneTransitionInProgress;
        private int _saveDataSlot = -1;
        private bool _recoveryRequired;
        private SaveLoadStatus _recoveryStatus = SaveLoadStatus.Failed;
        private string _recoveryDiagnostic;
        private GameObject _recoveryCanvas;
        [SerializeField] private TimelessEchoes.UI.Toolkit.ToolkitDialogScreen recoveryViewPrefab;
        private TimelessEchoes.UI.Toolkit.ToolkitDialogScreen _nativeRecoveryView;
        private TMP_Text _localizedRecoveryTitle;
        private bool _recoveryActionInProgress;
        private const int MaxLoadAttempts = 3;
        private const float LoadRetryDelaySeconds = 0.35f;
        private const float FirstAutosaveDelaySeconds = 30f;
        private const float AutosaveIntervalSeconds = 60f;
        private const string LoadingSceneName = "Loading";
        private const string MainSceneName = "Main";

        private void Awake()
        {
            if (oracle == null)
            {
                oracle = this;
                DontDestroyOnLoad(gameObject);
#if !UNITY_ANDROID && !UNITY_IOS
                Application.wantsToQuit += HandleWantsToQuit;
#endif
#if UNITY_ANDROID || UNITY_IOS
                ConfigureMobileSleepTimeout();
#endif
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // The live save tree is a stable public boundary. Loading state is represented by
            // loaded/_saveDataSlot, never by exposing a transient null to gameplay systems.
            saveData ??= new GameData();
            betaSaveIteration = Mathf.Max(MinBetaIteration, betaSaveIteration);
            CurrentSlot = Mathf.Clamp(PlayerPrefs.GetInt(GetCurrentSlotPrefKey(), 0), 0, 2);
            wipeInProgress = false;
        }

        [TabGroup("SaveData")] [InlineEditor] [SerializeField] private BuildModeConfig buildModeConfig;
        [TabGroup("SaveData")] [ShowInInspector] public bool demo => ActiveBuildModeConfig != null && ActiveBuildModeConfig.Demo;
        [TabGroup("SaveData")] [HorizontalGroup("SaveData/BetaRow")] [ShowInInspector] [LabelText("Beta")] public bool beta => ActiveBuildModeConfig != null && ActiveBuildModeConfig.Beta;
        [TabGroup("SaveData")] [HorizontalGroup("SaveData/BetaRow")] [LabelText("Iteration")] [MinValue(1)] [EnableIf(nameof(beta))] public int betaSaveIteration = 1;

        [TabGroup("SaveData")] [ShowInInspector] public int CurrentSlot { get; private set; }

        public bool HasCurrentSlotData => saveData != null && _saveDataSlot == CurrentSlot;
        public bool CanBeginSaveMutation =>
            !_slotTransitionInProgress && !_loadingSceneTransitionInProgress &&
            !_recoveryActionInProgress && !wipeInProgress && !_economicTransactionActive;

        [TabGroup("SaveData")] [NonSerialized, OdinSerialize] public GameData saveData = new();

        [Header("Seasonal Leaderboard")]

        private bool loaded;
        public bool HasLoadedCurrentSlotData => loaded && !_recoveryRequired && HasCurrentSlotData;
        private bool wipeInProgress;
        private const string SlotPrefKey = "SaveSlot";
        private const int MinBetaIteration = 1;
#if UNITY_ANDROID || UNITY_IOS
        [SerializeField] private bool preventMobileSleep = true;
        private int _originalSleepTimeout;
        private bool _sleepTimeoutOverridden;
        private bool _applicationInBackground;
#endif

        private int GetSafeBetaIteration()
        {
            return Mathf.Max(MinBetaIteration, betaSaveIteration);
        }

        private BuildModeConfig ActiveBuildModeConfig
        {
            get
            {
                if (buildModeConfig == null)
                {
                    buildModeConfig = BuildModeConfig.Load();
                }

                return buildModeConfig;
            }
        }

        private string GetBetaPrefix()
        {
            if (!beta) return string.Empty;
            return $"Beta{GetSafeBetaIteration()}";
        }

        private string GetCurrentSlotPrefKey()
        {
            var prefix = GetBetaPrefix();
            return string.IsNullOrEmpty(prefix) ? SlotPrefKey : $"{prefix}_{SlotPrefKey}";
        }

        public string GetSlotDirectoryName(int slotIndex)
        {
            var clamped = Mathf.Clamp(slotIndex, 0, 2);
            var baseName = $"Save{clamped + 1}";
            var prefix = GetBetaPrefix();
            return string.IsNullOrEmpty(prefix) ? baseName : $"{prefix}{baseName}";
        }

        public string GetSlotPlayerPrefsPrefix(int slotIndex)
        {
            var clamped = Mathf.Clamp(slotIndex, 0, 2);
            var prefix = GetBetaPrefix();
            return string.IsNullOrEmpty(prefix) ? $"Slot{clamped}" : $"{prefix}Slot{clamped}";
        }

        public string GetSlotPlayerPrefsKey(int slotIndex, string suffix)
        {
            return $"{GetSlotPlayerPrefsPrefix(slotIndex)}_{suffix}";
        }

        public string GetSlotDeletedKey(int slotIndex)
        {
            return GetSlotPlayerPrefsKey(slotIndex, "Deleted");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            betaSaveIteration = Mathf.Max(MinBetaIteration, betaSaveIteration);
        }
#endif

#if UNITY_INCLUDE_TESTS
        public void SetBuildModeConfig(BuildModeConfig overrideConfig)
        {
            buildModeConfig = overrideConfig;
        }
#endif

        // Defer showing load-failure notice until UI is ready

        private IEnumerator Start()
        {
            // Normal builds and Editor Play both start in Loading. This fallback protects a
            // misordered build or direct scene launch without allowing Main to reach a null tree.
            List<GameObject> suspendedRoots = null;
            if (!IsLoadingSceneActive())
            {
                suspendedRoots = SuspendActiveSceneRoots();
                yield return EnterLoadingScene();
                if (!IsLoadingSceneActive())
                {
                    RestoreSceneRoots(suspendedRoots);
                    RequireRecovery(
                        SaveLoadStatus.Failed,
                        "The Loading scene could not be opened before save verification began.");
                    yield break;
                }
            }

            yield return LoadCurrentSlot();
            if (!loaded)
                yield break;

            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;
            yield return LoadMainScene();
        }

        private IEnumerator LoadMainScene(string durableOperation = null)
        {
            // This set refers to scene objects, so it must be empty before Main's Awake/Start
            // methods begin spawning NPC tasks after any Loading -> Main transition.
            StaticReferences.ActiveNpcMeetings.Clear();
            var async = SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
            while (!async.isDone)
                yield return null;

            yield return null; // wait one frame for scene initialization
            if (!TryReloadRuntimeData(
                    resetFirst: false,
                    clearTransientState: false,
                    durableOperation ?? "The loaded save remains intact on disk"))
                yield break;

            // Start autosave only after data is loaded and applied in the main scene.
            // First autosave occurs shortly after load, then at a modest interval. Lifecycle saves
            // cover focus/pause/quit without continually forcing durable writes to the user's SSD.
            StartAutosaveLoop(FirstAutosaveDelaySeconds);
        }

        private static bool IsLoadingSceneActive()
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                LoadingSceneName,
                StringComparison.Ordinal);
        }

        private IEnumerator EnterLoadingScene()
        {
            if (IsLoadingSceneActive())
                yield break;

            if (_loadingSceneTransitionInProgress)
            {
                while (_loadingSceneTransitionInProgress)
                    yield return null;
                yield break;
            }

            AsyncOperation operation;
            _loadingSceneTransitionInProgress = true;
            try
            {
                operation = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                _loadingSceneTransitionInProgress = false;
                Debug.LogError($"Could not open the Loading scene: {ex}");
                yield break;
            }

            if (operation == null)
            {
                _loadingSceneTransitionInProgress = false;
                Debug.LogError("Could not open the Loading scene: Unity returned no scene operation.");
                yield break;
            }

            try
            {
                while (!operation.isDone)
                    yield return null;

                // Allow deferred destruction and the duplicate scene Oracle's cleanup to settle.
                yield return null;
            }
            finally
            {
                _loadingSceneTransitionInProgress = false;
            }
        }

        private List<GameObject> SuspendActiveSceneRoots()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || IsLoadingSceneActive())
                return new List<GameObject>();

            var roots = activeScene
                .GetRootGameObjects()
                .Where(root => root != null && root != gameObject && root.activeSelf)
                .ToList();
            foreach (var root in roots)
                root.SetActive(false);
            return roots;
        }

        private static void RestoreSceneRoots(IEnumerable<GameObject> roots)
        {
            if (roots == null)
                return;

            foreach (var root in roots)
                if (root != null)
                    root.SetActive(true);
        }

        private void Update()
        {
            if (!loaded) return;

            AccumulatePlayTime(Time.unscaledDeltaTime);
        }

        internal void AccumulatePlayTime(float unscaledDeltaSeconds)
        {
            if (unscaledDeltaSeconds <= 0f || saveData == null) return;
            saveData.PlayTime += unscaledDeltaSeconds;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            StopAutosaveLoop();
#if UNITY_ANDROID || UNITY_IOS
            if (oracle == this)
                RestoreMobileSleepTimeout();
#endif

            if (!_finalSaveCompleted)
                PrepareForQuit();
        }

        private void OnDestroy()
        {
            ToolkitLocalization.Changed -= RefreshRecoveryLanguage;
            if (oracle != this)
                return;

#if UNITY_ANDROID || UNITY_IOS
            RestoreMobileSleepTimeout();
#else
            Application.wantsToQuit -= HandleWantsToQuit;
#endif

            oracle = null;
        }

        private void OnDisable()
        {
            ToolkitLocalization.Changed -= RefreshRecoveryLanguage;
            if (oracle != this)
                return;

            StopAutosaveLoop();
#if UNITY_EDITOR
            // Exiting Play Mode does not always follow the same lifecycle as a player build.
            if (Application.isPlaying && !_isQuitting && loaded && !wipeInProgress)
                SaveNowBlocking(CurrentSlot);
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        private void ConfigureMobileSleepTimeout()
        {
            if (!preventMobileSleep)
                return;

            _originalSleepTimeout = Screen.sleepTimeout;
            if (_originalSleepTimeout == SleepTimeout.NeverSleep)
                return;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _sleepTimeoutOverridden = true;
        }

        private void RestoreMobileSleepTimeout()
        {
            if (!_sleepTimeoutOverridden)
                return;

            Screen.sleepTimeout = _originalSleepTimeout;
            _sleepTimeoutOverridden = false;
        }
#endif

#if !UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
                RequestSave();
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        private void OnApplicationPause(bool paused)
        {
            if (oracle != this)
                return;

            if (paused)
                EnterMobileBackground();
            else
                ExitMobileBackground();
        }

        private void EnterMobileBackground()
        {
            if (_applicationInBackground)
                return;

            _applicationInBackground = true;
            try
            {
                EventHandler.ApplicationBackground();
            }
            catch (Exception ex)
            {
                Debug.LogError($"One or more systems failed to enter the mobile background: {ex}");
            }

            // Always attempt the complete checkpoint even if a background notification failed.
            // SaveData's own invoke-all validation will still reject an incomplete snapshot.
            SaveNowBlocking(CurrentSlot);
        }

        private void ExitMobileBackground()
        {
            if (!_applicationInBackground)
                return;

            _applicationInBackground = false;
            AwayForSeconds();
            try
            {
                EventHandler.ApplicationForeground();
            }
            catch (Exception ex)
            {
                Debug.LogError($"One or more systems failed to return from the mobile background: {ex}");
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
                        RequestSave();
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
            if (_recoveryRequired || !loaded)
                return;
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

        private void RequestSave()
        {
            if (_economicTransactionActive) { _savePending = true; return; }
            if (!CanSave())
                return;

            if (_activeSaveTask != null)
            {
                _savePending = true;
                return;
            }

            if (!TryStartTrackedSave(CurrentSlot))
                return;

            if (_saveCoordinatorRoutine == null)
                _saveCoordinatorRoutine = StartCoroutine(SaveCoordinator());
        }

        private IEnumerator SaveCoordinator()
        {
            try
            {
                while (_activeSaveTask != null)
                {
                    while (!_activeSaveTask.IsCompleted)
                        yield return null;

                    var completedTask = _activeSaveTask;
                    var completedSlot = _activeSaveSlot;
                    var wasReplacement = _activeSaveIsReplacement;
                    _activeSaveTask = null;
                    _activeSaveSlot = -1;
                    _activeSaveIsReplacement = false;
                    HandleSaveResult(
                        GetSaveResult(completedTask, completedSlot),
                        completedSlot,
                        wasReplacement);

                    if (_savePending && CanSave())
                    {
                        _savePending = false;
                        if (!TryStartTrackedSave(CurrentSlot))
                            break;
                    }
                }
            }
            finally
            {
                _saveCoordinatorRoutine = null;
            }
        }

        private bool TryStartTrackedSave(
            int slotIndex,
            bool replaceLineage = false,
            bool forceAuthorityReplacement = false)
        {
            if (!TryPrepareSnapshot(out var error))
            {
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning(error);
                return false;
            }

            var index = Mathf.Clamp(slotIndex, 0, 2);
            var slotName = GetSlotDirectoryName(index);
            SaveManager.Instance.SetCurrentSlot(slotName);
            _activeSaveSlot = index;
            _activeSaveIsReplacement = replaceLineage;
            _activeSaveTask = forceAuthorityReplacement
                ? SaveManager.Instance.RecoverSlotWithFreshDataAsync(saveData, slotName)
                : replaceLineage
                    ? SaveManager.Instance.ReplaceSlotDetailedAsync(saveData, slotName)
                    : SaveManager.Instance.SaveDetailedAsync(saveData, slotName);
            return true;
        }

        private bool TryPrepareSnapshot(out string error)
        {
            error = null;
            if (!CanSave())
            {
                error = _recoveryRequired
                    ? "Skipping save because this slot requires recovery. Existing files remain untouched."
                    : "Skipping save because data has not completed loading.";
                return false;
            }

            if (!wipeInProgress)
            {
                try
                {
                    EventHandler.SaveData(force: true);
                }
                catch (Exception ex)
                {
                    error =
                        "Save cancelled because one or more runtime systems could not contribute a complete snapshot: " +
                        ex;
                    return false;
                }
            }

            if (saveData == null)
            {
                error = "Skipping save because the in-memory save tree is null.";
                return false;
            }

            if (string.IsNullOrEmpty(saveData.GameVersionCreated))
                saveData.GameVersionCreated = Application.version;
            saveData.LastGameVersion = Application.version;
            saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private bool CanSave()
        {
            return !_recoveryRequired && HasCurrentSlotData && (loaded || wipeInProgress);
        }

        private bool SaveNowBlocking(int slotIndex, bool replaceLineage = false)
        {
            if (_economicTransactionActive) { _savePending = true; return false; }
            if (!CanSave())
                return false;

            StopSaveCoordinatorMonitoring();
            CompleteActiveSaveBlocking();
            _savePending = false;

            if (!TryStartTrackedSave(slotIndex, replaceLineage))
                return false;

            var task = _activeSaveTask;
            var index = _activeSaveSlot;
            var wasReplacement = _activeSaveIsReplacement;
            var result = GetSaveResult(task, index, waitForCompletion: true);
            _activeSaveTask = null;
            _activeSaveSlot = -1;
            _activeSaveIsReplacement = false;
            HandleSaveResult(result, index, wasReplacement);
            return result.Succeeded;
        }

        private void CompleteActiveSaveBlocking()
        {
            if (_activeSaveTask == null)
                return;

            var task = _activeSaveTask;
            var index = _activeSaveSlot;
            var wasReplacement = _activeSaveIsReplacement;
            var result = GetSaveResult(task, index, waitForCompletion: true);
            _activeSaveTask = null;
            _activeSaveSlot = -1;
            _activeSaveIsReplacement = false;
            HandleSaveResult(result, index, wasReplacement);
        }

        private static SaveWriteResult GetSaveResult(
            Task<SaveWriteResult> task,
            int slotIndex,
            bool waitForCompletion = false)
        {
            try
            {
                return waitForCompletion ? task.GetAwaiter().GetResult() : task.Result;
            }
            catch (Exception ex)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.Failed,
                    $"Save{Mathf.Clamp(slotIndex, 0, 2) + 1}",
                    error: ex.ToString());
            }
        }

        private void HandleSaveResult(SaveWriteResult result, int slotIndex, bool replacedLineage = false)
        {
            if (result != null && result.Succeeded)
            {
                try
                {
                    PersistSlotMetadataToPlayerPrefs(slotIndex);
                    if (replacedLineage)
                        ClearDeletedMarker(slotIndex);
                }
                catch (Exception ex)
                {
                    // Snapshot authority is already durable. UI metadata and telemetry are best-effort.
                    Debug.LogWarning($"Save committed, but slot metadata could not be refreshed: {ex.Message}");
                }
                return;
            }

            var detail = result?.Error ?? "Unknown save failure.";
            Debug.LogError($"Save failed for File {slotIndex + 1}: {detail}");
            if (result?.Status == SaveWriteStatus.ReloadRequired && slotIndex == CurrentSlot)
                RequireSaveReloadRecovery(result, slotIndex);
            try
            {
                FeedbackForm.Submit(
                    "Save.CommitFailed",
                    $"slot: {slotIndex + 1}\nstatus: {result?.Status}\ndetail: {detail}");
            }
            catch
            {
                // Reporting must never wedge the save coordinator.
            }
        }

        private void RequireSaveReloadRecovery(SaveWriteResult result, int slotIndex)
        {
            loaded = false;
            _recoveryRequired = true;
            _recoveryStatus = result.RecoveryStatus ?? SaveLoadStatus.Conflict;
            _savePending = false;
            StopAutosaveLoop();
            _recoveryDiagnostic =
                $"File {slotIndex + 1} changed outside the running save session. Autosave stopped before " +
                "the external authority was overwritten. The current in-memory state is retained, and an " +
                "importable rescue was attempted before offering a reload. " + result.Error;
            try { FeedbackForm.Submit("Save.ReloadRequired", _recoveryDiagnostic); }
            catch { }
            ShowRecoveryUiAtLoadingBoundary();
        }

        private void ClearDeletedMarker(int slotIndex)
        {
            try
            {
                var deletedKey = GetSlotDeletedKey(slotIndex);
                if (PlayerPrefs.GetInt(deletedKey, 0) != 1)
                    return;

                PlayerPrefs.DeleteKey(deletedKey);
                PlayerPrefs.Save();
            }
            catch
            {
                // The snapshot is already durable; a stale UI marker is non-authoritative.
            }
        }

        private void StopSaveCoordinatorMonitoring()
        {
            if (_saveCoordinatorRoutine == null)
                return;

            StopCoroutine(_saveCoordinatorRoutine);
            _saveCoordinatorRoutine = null;
        }

        private IEnumerator LoadCurrentSlot()
        {
            PrepareForSlotLoad();

            var index = Mathf.Clamp(CurrentSlot, 0, 2);
            var slotName = GetSlotDirectoryName(index);
            SaveManager.Instance.SetCurrentSlot(slotName);
            SaveLoadResult loadResult = null;
            for (var attempt = 1; attempt <= MaxLoadAttempts; attempt++)
            {
                var loadTask = SaveManager.Instance.LoadDetailedAsync(slotName);
                while (!loadTask.IsCompleted)
                    yield return null;

                try
                {
                    loadResult = loadTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    loadResult = new SaveLoadResult(
                        SaveLoadStatus.Failed,
                        slotName,
                        diagnostic: ex.ToString());
                }

                if (!IsTransientLoadFailure(loadResult.Status) || attempt >= MaxLoadAttempts)
                    break;

                Debug.LogWarning(
                    $"File {index + 1} load attempt {attempt} was temporarily unavailable. " +
                    "Retrying without changing any save files.");
                yield return new WaitForSecondsRealtime(LoadRetryDelaySeconds * attempt);
            }

            if (loadResult == null)
            {
                RequireRecovery(SaveLoadStatus.Failed, "The save load ended without a result.");
                yield break;
            }

            if (loadResult.Status == SaveLoadStatus.NotFound)
            {
                PublishLoadedData(new GameData
                {
                    DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                });
                yield break;
            }

            if (loadResult.Status == SaveLoadStatus.Deleted)
            {
                var freshData = new GameData
                {
                    DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                };
                var replacementTask = SaveManager.Instance.ReplaceSlotDetailedAsync(freshData, slotName);
                while (!replacementTask.IsCompleted)
                    yield return null;

                var replacement = GetSaveResult(replacementTask, index);
                if (!replacement.Succeeded)
                {
                    RequireRecovery(
                        SaveLoadStatus.Failed,
                        $"The deleted slot could not be recreated safely: {replacement.Error}");
                    yield break;
                }

                PublishLoadedData(freshData);
                HandleSaveResult(replacement, index, replacedLineage: true);
                yield break;
            }

            if (!loadResult.Succeeded || loadResult.Data == null)
            {
                RequireRecovery(loadResult.Status, loadResult.Diagnostic);
                yield break;
            }

            var migration = SaveMigrationRunner.TryMigrate(loadResult.Data, Application.version);
            foreach (var warning in migration.Warnings) Debug.LogWarning(warning);
            if (!migration.Succeeded || migration.Data == null)
            {
                RequireRecovery(
                    SaveLoadStatus.Failed,
                    migration.Error ?? "A required save migration failed.");
                yield break;
            }

            var candidate = migration.Data;
            var mustCommit = migration.Changed ||
                             loadResult.Status == SaveLoadStatus.Recovered ||
                             !loadResult.IntegrityVerified;
            if (mustCommit)
            {
                var migrationSave = SaveManager.Instance.SaveDetailedAsync(candidate, slotName);
                while (!migrationSave.IsCompleted)
                    yield return null;

                var migrationWrite = GetSaveResult(migrationSave, index);
                if (!migrationWrite.Succeeded)
                {
                    RequireRecovery(
                        SaveLoadStatus.Failed,
                        $"Loaded data was preserved, but its verified migration could not be committed: {migrationWrite.Error}");
                    yield break;
                }
            }

            PublishLoadedData(candidate);
            PersistSlotMetadataToPlayerPrefs();

            if (loadResult.Status == SaveLoadStatus.Recovered)
            {
                Debug.LogWarning(
                    $"Recovered File {index + 1} from '{loadResult.SourcePath}'. " +
                    "The failed newer files were left untouched for diagnosis.");
            }
        }

        private void PrepareForSlotLoad()
        {
            loaded = false;
            _recoveryRequired = false;
            _recoveryStatus = SaveLoadStatus.Failed;
            _recoveryDiagnostic = null;
            saveData ??= new GameData();
            _saveDataSlot = -1;
        }

        private static bool IsTransientLoadFailure(SaveLoadStatus status)
        {
            return status == SaveLoadStatus.Unavailable || status == SaveLoadStatus.Failed;
        }

        private void RequireRecovery(SaveLoadStatus status, string diagnostic)
        {
            loaded = false;
            _recoveryRequired = true;
            _recoveryStatus = status;
            saveData ??= new GameData();
            _saveDataSlot = -1;
            StopAutosaveLoop();
            _recoveryDiagnostic =
                $"File {CurrentSlot + 1} requires save recovery ({status}). " +
                "No existing snapshot generation was overwritten or deleted by this failure. " +
                $"{diagnostic}";
            Debug.LogError(_recoveryDiagnostic);
            try { FeedbackForm.Submit("Save.RecoveryRequired", _recoveryDiagnostic); }
            catch { }
            ShowRecoveryUiAtLoadingBoundary();
        }

        private void ShowRecoveryUiAtLoadingBoundary()
        {
            ShowRecoveryUi();
            if (IsLoadingSceneActive() || _recoverySceneTransitionRoutine != null)
                return;

            _recoverySceneTransitionRoutine = StartCoroutine(MoveRecoveryToLoadingScene());
        }

        private IEnumerator MoveRecoveryToLoadingScene()
        {
            var suspendedRoots = SuspendActiveSceneRoots();
            yield return EnterLoadingScene();
            if (!IsLoadingSceneActive())
            {
                RestoreSceneRoots(suspendedRoots);
                _recoveryDiagnostic +=
                    "\nThe Loading scene could not be opened; the previous scene was restored with saving disabled.";
            }

            _recoverySceneTransitionRoutine = null;
            ShowRecoveryUi();
        }

        private void RefreshRecoveryLanguage()
        {
            if (_recoveryRequired && isActiveAndEnabled) ShowRecoveryUi();
        }

        private void ShowRecoveryUi()
        {
            ToolkitLocalization.Changed -= RefreshRecoveryLanguage;
            ToolkitLocalization.Changed += RefreshRecoveryLanguage;
            var canStartFresh = CanOfferFreshRecovery(_recoveryStatus);
            var unsupportedNewer = _recoveryStatus == SaveLoadStatus.UnsupportedNewer;
            var titleText = unsupportedNewer ? ToolkitLocalization.Text("save.recovery.newer-title", "Save From Newer Version") :
                canStartFresh ? ToolkitLocalization.Text("save.recovery.required-title", "Save Recovery Required") : ToolkitLocalization.Text("save.recovery.unavailable-title", "Save Temporarily Unavailable");
            var bodyText = GetRecoveryBody(canStartFresh, unsupportedNewer);
            if (recoveryViewPrefab != null)
            {
                if (_nativeRecoveryView == null)
                    _nativeRecoveryView = Instantiate(recoveryViewPrefab, transform);
                _recoveryCanvas ??= transform.Find("Canvas")?.gameObject;
                if (_recoveryCanvas != null) _recoveryCanvas.SetActive(false);
                _nativeRecoveryView.Show(titleText, bodyText, ToolkitLocalization.Text("save.recovery.retry", "Retry"),
                    canStartFresh ? ToolkitLocalization.Text("save.recovery.fresh", "Keep Old Files & Start Fresh") : ToolkitLocalization.Text("save.recovery.folder", "Open Save Folder"),
                    BeginRecoveryRetry, canStartFresh ? BeginFreshRecovery : OpenSaveFolder,
                    !_recoveryActionInProgress);
                return;
            }

            _recoveryCanvas ??= transform.Find("Canvas")?.gameObject;
            if (_recoveryCanvas == null)
            {
                Debug.LogError("Save recovery UI is missing from the loading Oracle hierarchy.");
                return;
            }

            var texts = _recoveryCanvas.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            var title = _localizedRecoveryTitle != null ? _localizedRecoveryTitle : texts.FirstOrDefault(text =>
                text != null && text.text != null &&
                (text.text.Contains("Potential Regression") ||
                 text.text.Contains("Save Recovery Required") ||
                 text.text.Contains("Save Temporarily Unavailable") ||
                 text.text.Contains("Save From Newer Version")));
            var body = texts
                .Where(text => text != null && text != title && text.GetComponentInParent<Button>() == null)
                .OrderByDescending(text => text.text?.Length ?? 0)
                .FirstOrDefault();

            if (title != null)
            {
                _localizedRecoveryTitle = title;
                title.text = titleText;
            }

            if (body != null)
                body.text = bodyText;

            Button primaryButton = null;
            Button secondaryButton = null;
            foreach (var button in _recoveryCanvas.GetComponentsInChildren<Button>(includeInactive: true))
            {
                if (string.Equals(button.name, "Yes", StringComparison.Ordinal))
                    primaryButton = button;
                else
                    secondaryButton ??= button;
            }

            if (primaryButton != null)
            {
                var label = primaryButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
                if (label != null) label.text = ToolkitLocalization.Text("save.recovery.retry", "Retry");
                primaryButton.onClick.RemoveAllListeners();
                primaryButton.onClick.AddListener(BeginRecoveryRetry);
            }

            if (secondaryButton != null)
            {
                var label = secondaryButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
                if (label != null)
                    label.text = canStartFresh ? ToolkitLocalization.Text("save.recovery.fresh", "Keep Old Files & Start Fresh") : ToolkitLocalization.Text("save.recovery.folder", "Open Save Folder");
                secondaryButton.onClick.RemoveAllListeners();
                if (canStartFresh)
                    secondaryButton.onClick.AddListener(BeginFreshRecovery);
                else
                    secondaryButton.onClick.AddListener(OpenSaveFolder);
            }

            _recoveryCanvas.transform.localScale = Vector3.one;
            _recoveryCanvas.SetActive(true);
        }

        private void HideRecoveryUi()
        {
            ToolkitLocalization.Changed -= RefreshRecoveryLanguage;
            if (_nativeRecoveryView != null) _nativeRecoveryView.Hide();
            if (_recoveryCanvas != null)
                _recoveryCanvas.SetActive(false);
        }

        private string GetRecoveryBody(bool canStartFresh, bool unsupportedNewer)
        {
            if (unsupportedNewer)
                return ToolkitLocalization.Text("save.recovery.newer-body", "File {0} was written by a newer game version. It has not been changed. Install the newer version, then Retry, or inspect the save folder. Starting fresh is disabled to protect that progress.\n\n{1}", CurrentSlot + 1, _recoveryDiagnostic);
            if (canStartFresh)
                return ToolkitLocalization.Text("save.recovery.conflict-body", "File {0} has a confirmed integrity or cloud-authority conflict. Retry is the safest first action and every existing snapshot remains preserved. If you explicitly start fresh, a new cloud lineage is created without deleting the old files.\n\n{1}", CurrentSlot + 1, _recoveryDiagnostic);
            return ToolkitLocalization.Text("save.recovery.unavailable-body", "File {0} is temporarily unavailable or could not be applied to the running game. Autosave is disabled and existing snapshot evidence remains preserved. Retry after closing any sync or backup tool that may be using the files.\n\n{1}", CurrentSlot + 1, _recoveryDiagnostic);
        }

        private static bool CanOfferFreshRecovery(SaveLoadStatus status)
        {
            return status == SaveLoadStatus.Corrupt || status == SaveLoadStatus.Conflict;
        }

        private void BeginRecoveryRetry()
        {
            if (!_recoveryActionInProgress && !_loadingSceneTransitionInProgress)
                StartCoroutine(RetryRecoveryRoutine());
        }

        private IEnumerator RetryRecoveryRoutine()
        {
            _recoveryActionInProgress = true;
            var buttons = GetRecoveryButtons();
            SetButtonsInteractable(buttons, false);

            try
            {
                if (!IsLoadingSceneActive())
                {
                    var suspendedRoots = SuspendActiveSceneRoots();
                    yield return EnterLoadingScene();
                    if (!IsLoadingSceneActive())
                    {
                        RestoreSceneRoots(suspendedRoots);
                        _recoveryDiagnostic +=
                            "\nRetry could not open the Loading scene, so the live scene was restored unchanged.";
                        yield break;
                    }
                }

                yield return LoadCurrentSlot();
                if (!loaded)
                    yield break;

                PersistCurrentSlotSelection();
                HideRecoveryUi();
                yield return LoadMainScene("The retried save was loaded from a verified disk snapshot");
            }
            finally
            {
                _recoveryActionInProgress = false;
                SetButtonsInteractable(buttons, true);
                if (_recoveryRequired)
                    ShowRecoveryUi();
            }
        }

        private void BeginFreshRecovery()
        {
            if (!_recoveryActionInProgress && !_loadingSceneTransitionInProgress &&
                CanOfferFreshRecovery(_recoveryStatus))
                StartCoroutine(RecoverWithFreshDataRoutine());
        }

        private IEnumerator RecoverWithFreshDataRoutine()
        {
            _recoveryActionInProgress = true;
            var buttons = GetRecoveryButtons();
            SetButtonsInteractable(buttons, false);
            try
            {
                if (!IsLoadingSceneActive())
                {
                    var suspendedRoots = SuspendActiveSceneRoots();
                    yield return EnterLoadingScene();
                    if (!IsLoadingSceneActive())
                    {
                        RestoreSceneRoots(suspendedRoots);
                        _recoveryDiagnostic +=
                            "\nFresh recovery could not open the Loading scene. No save files were changed.";
                        yield break;
                    }
                }

                var index = Mathf.Clamp(CurrentSlot, 0, 2);
                var slotName = GetSlotDirectoryName(index);
                var candidate = CreateFreshGameData();
                var task = SaveManager.Instance.RecoverSlotWithFreshDataAsync(candidate, slotName);
                while (!task.IsCompleted)
                    yield return null;

                var result = GetSaveResult(task, index);
                if (!result.Succeeded)
                {
                    HandleSaveResult(result, index, replacedLineage: true);
                    _recoveryDiagnostic =
                        "Fresh recovery could not be committed. Existing snapshot evidence remains preserved. " +
                        result.Error;
                    yield break;
                }

                HandleSaveResult(result, index, replacedLineage: true);
                if (!TryActivateCommittedData(
                        candidate,
                        resetRuntime: false,
                        clearTransientState: false,
                        "Fresh recovery was committed successfully to disk"))
                    yield break;

                PersistCurrentSlotSelection();
                HideRecoveryUi();
                yield return LoadMainScene("Fresh recovery was committed successfully to disk");
            }
            finally
            {
                _recoveryActionInProgress = false;
                SetButtonsInteractable(buttons, true);
                if (_recoveryRequired)
                    ShowRecoveryUi();
            }
        }

        private Button[] GetRecoveryButtons()
        {
            return _recoveryCanvas != null
                ? _recoveryCanvas.GetComponentsInChildren<Button>(includeInactive: true)
                : Array.Empty<Button>();
        }

        private void SetButtonsInteractable(IEnumerable<Button> buttons, bool interactable)
        {
            if (_nativeRecoveryView != null) _nativeRecoveryView.SetInteractable(interactable);
            foreach (var button in buttons)
                button.interactable = interactable;
        }

        private bool TryActivateCommittedData(
            GameData candidate,
            bool resetRuntime,
            bool clearTransientState,
            string durableOperation)
        {
            try
            {
                _recoveryRequired = false;
                _recoveryDiagnostic = null;
                if (resetRuntime)
                {
                    EventHandler.ResetData();
                    if (clearTransientState)
                        StaticReferences.ActiveNpcMeetings.Clear();
                }

                PublishLoadedData(candidate, markLoaded: false);
                if (resetRuntime)
                    EventHandler.LoadData();
                loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                RequireRuntimeReloadRecovery(durableOperation, ex);
                return false;
            }
        }

        private bool TryReloadRuntimeData(
            bool resetFirst,
            bool clearTransientState,
            string durableOperation)
        {
            try
            {
                if (resetFirst)
                    EventHandler.ResetData();
                if (clearTransientState)
                    StaticReferences.ActiveNpcMeetings.Clear();
                EventHandler.LoadData();
                return true;
            }
            catch (Exception ex)
            {
                RequireRuntimeReloadRecovery(durableOperation, ex);
                return false;
            }
        }

        private void RequireRuntimeReloadRecovery(string durableOperation, Exception exception)
        {
            loaded = false;
            _recoveryRequired = true;
            _recoveryStatus = SaveLoadStatus.Failed;
            StopAutosaveLoop();
            _recoveryDiagnostic =
                $"{durableOperation}, but reloading it into the running game failed. " +
                "The disk commit is safe and no recovery files were removed. Restart the game or Retry. " +
                exception;
            Debug.LogError(_recoveryDiagnostic);
            try { FeedbackForm.Submit("Save.RuntimeReloadFailed", _recoveryDiagnostic); }
            catch { }
            ShowRecoveryUiAtLoadingBoundary();
        }

        private static void OpenSaveFolder()
        {
            Application.OpenURL($"file://{Application.persistentDataPath}");
        }

        public bool SaveToSlot(int slotIndex)
        {
            if (!CanBeginSaveMutation || !HasCurrentSlotData)
                return false;

            var index = Mathf.Clamp(slotIndex, 0, 2);
            return SaveNowBlocking(index, replaceLineage: index != CurrentSlot);
        }

        public bool PrepareForQuit()
        {
            if (_finalSaveCompleted)
                return true;

            _isQuitting = true;
            StopAutosaveLoop();

            if (_recoveryRequired || !loaded)
            {
                _finalSaveCompleted = true;
                TryFlushPlayerPrefs();
                return true;
            }

            var hasActiveRun = HasActiveRun();
            if (hasActiveRun && !SaveNowBlocking(CurrentSlot))
            {
                _isQuitting = false;
                RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                return false;
            }

            if (hasActiveRun)
            {
                try
                {
                    AbandonActiveRun();
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"Could not finalize the active run during quit. The preceding full checkpoint is safe: {ex}");
                    _finalSaveCompleted = true;
                    TryFlushPlayerPrefs();
                    return true;
                }
            }

            var finalCommitSucceeded = SaveNowBlocking(CurrentSlot);
            if (!finalCommitSucceeded && !hasActiveRun)
            {
                _isQuitting = false;
                RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                return false;
            }

            if (!finalCommitSucceeded)
            {
                Debug.LogError(
                    "The abandoned-run update could not be committed, but the immediately preceding full " +
                    "checkpoint is durable. Quitting with that verified checkpoint.");
            }

            _finalSaveCompleted = true;
            TryFlushPlayerPrefs();
            return true;
        }

#if !UNITY_ANDROID && !UNITY_IOS
        private bool HandleWantsToQuit()
        {
            return PrepareForQuit();
        }
#endif

        private static bool HasActiveRun()
        {
            var tracker = GameplayStatTracker.Instance ?? FindAnyObjectByType<GameplayStatTracker>();
            return tracker != null && tracker.RunInProgress;
        }

        private static void AbandonActiveRun()
        {
            var tracker = GameplayStatTracker.Instance ??
                          FindAnyObjectByType<GameplayStatTracker>();
            if (tracker != null && tracker.RunInProgress)
                tracker.AbandonRun();
        }

        public bool TryCommitImportedData(GameData importedData, out string error)
        {
            return TryCommitImportedData(importedData, out error, out _);
        }

        public bool TryCommitImportedData(
            GameData importedData,
            out string error,
            out bool committedToDisk)
        {
            if (importedData == null)
                throw new ArgumentNullException(nameof(importedData));

            error = null;
            committedToDisk = false;
            if (!CanBeginSaveMutation)
            {
                error = "Another save-slot transition is already in progress.";
                return false;
            }

            var forceNewAuthority = _recoveryRequired;
            StopAutosaveLoop();
            StopSaveCoordinatorMonitoring();
            CompleteActiveSaveBlocking();
            _savePending = false;

            var index = Mathf.Clamp(CurrentSlot, 0, 2);
            var slotName = GetSlotDirectoryName(index);
            var task = forceNewAuthority
                ? SaveManager.Instance.RecoverSlotWithFreshDataAsync(importedData, slotName)
                : SaveManager.Instance.ReplaceSlotDetailedAsync(importedData, slotName);
            var result = GetSaveResult(task, index, waitForCompletion: true);
            if (!result.Succeeded)
            {
                error = result.Error ?? "The imported save could not be committed.";
                HandleSaveResult(result, index, replacedLineage: true);
                if (loaded && !_recoveryRequired)
                    RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                return false;
            }

            committedToDisk = true;
            HandleSaveResult(result, index, replacedLineage: true);
            var runtimeReloadSucceeded = false;
            try
            {
                runtimeReloadSucceeded = TryActivateCommittedData(
                    importedData,
                    resetRuntime: true,
                    clearTransientState: true,
                    "The imported save was committed successfully to disk");
                if (!runtimeReloadSucceeded)
                {
                    error =
                        "The imported save was committed successfully to disk, but the running game could not " +
                        "reload it. Retry from the recovery screen or restart the game.";
                }

                return runtimeReloadSucceeded;
            }
            finally
            {
                if (runtimeReloadSucceeded)
                {
                    RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                    HideRecoveryUi();
                }
            }
        }

        private void PublishLoadedData(GameData candidate, bool markLoaded = true)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            NormalizeLoadedData(candidate);
            // Backfill created version if missing (legacy saves or fresh new games)
            if (string.IsNullOrEmpty(candidate.GameVersionCreated))
                candidate.GameVersionCreated = Application.version;
            candidate.SavedPreferences.OfflineTimeAutoDisable = false;
            candidate.SavedPreferences.OfflineTimeActive = true;

            // Publish the complete tree and its ownership together. No partially normalized
            // candidate is ever visible to runtime consumers.
            saveData = candidate;
            _saveDataSlot = CurrentSlot;
            loaded = markLoaded;
        }

        private static void NormalizeLoadedData(GameData data)
        {
            data.Farm ??= new TimelessEchoes.Farming.FarmState();
            data.UpgradeLevels ??= new Dictionary<string, int>();
            data.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            data.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            data.EnemyKills ??= new Dictionary<string, double>();
            data.CompletedNpcTasks ??= new HashSet<string>();
            data.PinnedQuests ??= new List<string>();
            // Gear system collections
            data.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
            data.BuffSlots ??= new List<string>(new string[5]);
            if (data.BuffSlots.Count < 5)
                while (data.BuffSlots.Count < 5)
                    data.BuffSlots.Add(null);
            data.AutoBuffSlots ??= new List<bool>(new bool[5]);
            if (data.AutoBuffSlots.Count < 5)
                while (data.AutoBuffSlots.Count < 5)
                    data.AutoBuffSlots.Add(false);
            if (data.UnlockedBuffSlots <= 0)
                data.UnlockedBuffSlots = 1;
            else if (data.UnlockedBuffSlots > 5)
                data.UnlockedBuffSlots = 5;
            if (data.UnlockedAutoBuffSlots < 0)
                data.UnlockedAutoBuffSlots = 0;
            else if (data.UnlockedAutoBuffSlots > 5)
                data.UnlockedAutoBuffSlots = 5;
            if (data.DisciplePercent <= 0f)
                data.DisciplePercent = 0.01f;
            // Cauldron system collections and totals
            data.CauldronCardCounts ??= new Dictionary<string, int>();
            data.CauldronTotals ??= new GameData.CauldronTotalsRecord();
            data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            data.SavedPreferences ??= new GameData.Preferences();
        }

        public static void AwayForSeconds()
        {
            if (oracle?.saveData == null)
                return;

            if (string.IsNullOrEmpty(oracle.saveData.DateQuitString))
            {
                oracle.saveData.DateStarted = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (!DateTime.TryParse(
                    oracle.saveData.DateQuitString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                    out var quitTime))
            {
                Debug.LogWarning(
                    $"Ignoring invalid saved quit timestamp '{oracle.saveData.DateQuitString}'. " +
                    "The original value is preserved for diagnostics.");
                return;
            }

            quitTime = quitTime.ToUniversalTime();
            var seconds = Mathf.Max(0f, (float)(DateTime.UtcNow - quitTime).TotalSeconds);
            EventHandler.AwayForTime(seconds);
        }

        public void SelectSlot(int slot)
        {
            var clamped = Mathf.Clamp(slot, 0, 2);
            if (clamped == CurrentSlot || !CanBeginSaveMutation)
                return;

            StartCoroutine(SelectSlotRoutine(clamped));
        }

        private IEnumerator SelectSlotRoutine(int slot)
        {
            _slotTransitionInProgress = true;
            StopAutosaveLoop();
            var sourceSlot = CurrentSlot;
            var sourceData = saveData;
            var sourceWasRecoveryBlocked = _recoveryRequired;
            try
            {
                // Never abandon unsaved changes in a healthy slot. A recovery-blocked slot has no
                // writable runtime state, so it can be left without touching its files.
                List<GameObject> suspendedRoots = null;
                if (!sourceWasRecoveryBlocked)
                {
                    StopSaveCoordinatorMonitoring();
                    if (_activeSaveTask != null)
                    {
                        while (!_activeSaveTask.IsCompleted)
                            yield return null;
                        CompleteActiveSaveBlocking();
                    }

                    _savePending = false;
                    suspendedRoots = SuspendActiveSceneRoots();
                    if (!TryStartTrackedSave(CurrentSlot))
                    {
                        RestoreSceneRoots(suspendedRoots);
                        Debug.LogError("Slot switch cancelled because a complete snapshot could not be prepared.");
                        RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                        yield break;
                    }

                    var saveTask = _activeSaveTask;
                    var saveIndex = _activeSaveSlot;
                    yield return EnterLoadingScene();
                    if (!IsLoadingSceneActive())
                    {
                        RestoreSceneRoots(suspendedRoots);
                        while (!saveTask.IsCompleted)
                            yield return null;

                        var transitionFailureSave = GetSaveResult(saveTask, saveIndex);
                        _activeSaveTask = null;
                        _activeSaveSlot = -1;
                        _activeSaveIsReplacement = false;
                        HandleSaveResult(transitionFailureSave, saveIndex);
                        if (transitionFailureSave.Succeeded && !_recoveryRequired)
                            RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                        Debug.LogError("Slot switch cancelled because the Loading scene could not be opened.");
                        yield break;
                    }

                    while (!saveTask.IsCompleted)
                        yield return null;

                    var result = GetSaveResult(saveTask, saveIndex);
                    _activeSaveTask = null;
                    _activeSaveSlot = -1;
                    _activeSaveIsReplacement = false;
                    HandleSaveResult(result, saveIndex);
                    if (!result.Succeeded)
                    {
                        Debug.LogError("Slot switch cancelled because the current slot could not be saved safely.");
                        if (!_recoveryRequired)
                            yield return LoadMainScene(
                                "The slot switch was cancelled and the current in-memory slot was retained");
                        yield break;
                    }
                }
                else if (!IsLoadingSceneActive())
                {
                    suspendedRoots = SuspendActiveSceneRoots();
                    yield return EnterLoadingScene();
                    if (!IsLoadingSceneActive())
                    {
                        RestoreSceneRoots(suspendedRoots);
                        Debug.LogError("Slot switch cancelled because the Loading scene could not be opened.");
                        yield break;
                    }
                }

                // Main has now been unloaded, so no old runtime subscriber can observe or mutate
                // the target slot while it is being verified.
                CurrentSlot = Mathf.Clamp(slot, 0, 2);
                StaticReferences.ActiveNpcMeetings.Clear();

                yield return LoadCurrentSlot();
                if (loaded)
                {
                    PersistCurrentSlotSelection();
                    HideRecoveryUi();
                    yield return LoadMainScene(
                        "The previous slot was checkpointed and the selected slot was loaded from a verified snapshot");
                }
                else if (!sourceWasRecoveryBlocked)
                {
                    // Selecting a damaged, unavailable, or newer-version slot must not strand the
                    // player there. The source was checkpointed before Main closed, so restore that
                    // still-live tree and leave the failed target untouched for a later retry.
                    CurrentSlot = sourceSlot;
                    SaveManager.Instance.SetCurrentSlot(GetSlotDirectoryName(sourceSlot));
                    _recoveryRequired = false;
                    _recoveryStatus = SaveLoadStatus.Failed;
                    _recoveryDiagnostic = null;
                    PublishLoadedData(sourceData);
                    PersistCurrentSlotSelection();
                    HideRecoveryUi();
                    yield return LoadMainScene(
                        $"File {slot + 1} could not be verified, so File {sourceSlot + 1} was restored unchanged");
                }
            }
            finally
            {
                _slotTransitionInProgress = false;
            }
        }

        private void PersistCurrentSlotSelection()
        {
            try
            {
                PlayerPrefs.SetInt(GetCurrentSlotPrefKey(), CurrentSlot);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                // The verified snapshot remains authoritative even if this convenience preference
                // cannot be flushed. The previous slot will be selected safely on the next launch.
                Debug.LogWarning($"Could not persist the selected save slot: {ex.Message}");
            }
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public void WipePreferences()
        {
            if (!HasCurrentSlotData)
                return;

            saveData.SavedPreferences = new GameData.Preferences();
        }

        [TabGroup("SaveData", "Buttons")]
        [Button]
        public bool WipeAllData(bool replacingDeletedSlot = false)
        {
            if (!CanBeginSaveMutation)
                return false;

            StopAutosaveLoop();
            StopSaveCoordinatorMonitoring();
            CompleteActiveSaveBlocking();
            _savePending = false;
            wipeInProgress = true;
            var wasRecoveryRequired = _recoveryRequired;
            var prefs = HasCurrentSlotData
                ? saveData.SavedPreferences ?? new GameData.Preferences()
                : new GameData.Preferences();
            var candidate = CreateFreshGameData(prefs);
            var index = Mathf.Clamp(CurrentSlot, 0, 2);
            var slotName = GetSlotDirectoryName(index);
            var task = SaveManager.Instance.ReplaceSlotDetailedAsync(candidate, slotName);
            var result = GetSaveResult(task, index, waitForCompletion: true);
            var runtimeReloadSucceeded = false;
            try
            {
                if (!result.Succeeded)
                {
                    HandleSaveResult(result, index, replacedLineage: true);
                    if (wasRecoveryRequired || replacingDeletedSlot)
                    {
                        RequireRecovery(
                            SaveLoadStatus.Failed,
                            $"A fresh replacement could not be committed: {result.Error}");
                    }
                    else
                    {
                        RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                    }
                    return false;
                }

                HandleSaveResult(result, index, replacedLineage: true);
                runtimeReloadSucceeded = TryActivateCommittedData(
                    candidate,
                    resetRuntime: true,
                    clearTransientState: true,
                    "The fresh replacement was committed successfully to disk");
                return runtimeReloadSucceeded;
            }
            finally
            {
                wipeInProgress = false;
                if (runtimeReloadSucceeded)
                {
                    RestartAutosaveLoop(FirstAutosaveDelaySeconds);
                    HideRecoveryUi();
                }
            }
        }

        public void PersistSlotMetadataToPlayerPrefs(int? slotIndex = null, bool flush = false)
        {
            if (saveData == null)
                return;

            try
            {
                var index = Mathf.Clamp(slotIndex ?? CurrentSlot, 0, 2);
                var completionKey = GetSlotPlayerPrefsKey(index, "Completion");
                var playtimeKey = GetSlotPlayerPrefsKey(index, "Playtime");
                var dateKey = GetSlotPlayerPrefsKey(index, "Date");

                PlayerPrefs.SetFloat(completionKey, saveData.CompletionPercentage);
                PlayerPrefs.SetFloat(playtimeKey, (float)saveData.PlayTime);
                PlayerPrefs.SetString(dateKey, saveData.DateQuitString);
                if (flush)
                    PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                // Slot cards are a UI cache. Failure to refresh them must never interrupt a
                // verified load or downgrade the authoritative snapshot.
                Debug.LogWarning($"Could not refresh save-slot metadata: {ex.Message}");
            }
        }

        private static GameData CreateFreshGameData(GameData.Preferences preferences = null)
        {
            var now = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            return new GameData
            {
                SavedPreferences = preferences ?? new GameData.Preferences(),
                DateStarted = now,
                DateQuitString = now,
                GameVersionCreated = Application.version,
                LastGameVersion = Application.version
            };
        }

        private static void TryFlushPlayerPrefs()
        {
            try { PlayerPrefs.Save(); }
            catch (Exception ex) { Debug.LogWarning($"Could not flush UI preferences: {ex.Message}"); }
        }



    }
}
