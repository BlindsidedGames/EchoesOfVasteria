using System.Collections;
using Blindsided.SaveData;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blindsided
{
    public partial class Oracle
    {
        // Autosave management
        private Coroutine _autosaveRoutine;
        private const float FirstAutosaveDelaySeconds = 30f;
        private const float AutosaveIntervalSeconds = 30f;
        private void Start()
        {
            Load();
            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;

            // Wire up regression confirmation UI if present
            if (regressionYesButton != null)
            {
                regressionYesButton.onClick.RemoveAllListeners();
                regressionYesButton.onClick.AddListener(ConfirmRegressionKeepLoaded);
            }

            if (regressionNoButton != null)
            {
                regressionNoButton.onClick.RemoveAllListeners();
                regressionNoButton.onClick.AddListener(AttemptRestoreBackupAndReload);
            }

            if (_pendingLoadFailureNotice)
            {
                TryShowLoadFailureWindow(_pendingLoadFailureMessage);
                _pendingLoadFailureNotice = false;
                _pendingLoadFailureMessage = null;
                _mainSceneLoadDeferred = true;
            }
            else if (regressionConfirmWindow != null && regressionConfirmWindow.activeSelf)
            {
                _mainSceneLoadDeferred = true;
            }
            else
            {
                StartCoroutine(LoadMainScene());
            }
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
            ES3.StoreCachedFile(_fileName);
            SafeCreateBackup();
            CreateRotatingBackup();
        }

        private void OnDisable()
        {
            // This is called when you exit Play Mode in the Editor
            if (Application.isPlaying && !wipeInProgress && oracle == this && _settings != null)
            {
                SaveToFile(); // save the latest state immediately
                ES3.StoreCachedFile(_fileName);
                CreateRotatingBackup();
                StopAutosaveLoop();
            }
        }


#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                SaveToFile();
                ES3.StoreCachedFile(_fileName);
            }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveToFile();
                ES3.StoreCachedFile(_fileName);
            }
        }
#endif
    }
}

// Autosave helpers for Oracle
namespace Blindsided
{
    public partial class Oracle
    {
        private System.Collections.IEnumerator AutosaveRoutine(float initialDelay, float interval)
        {
            if (initialDelay > 0)
                yield return new WaitForSecondsRealtime(initialDelay);

            while (true)
            {
                // Skip autosave while wiping, when a regression or load-failure prompt is active
                var regressionActive = regressionConfirmWindow != null && regressionConfirmWindow.activeSelf;
                if (!wipeInProgress && !regressionActive && !_pendingLoadFailureNotice)
                {
                    try
                    {
                        SaveToFile();
                        ES3.StoreCachedFile(_fileName);
                        CreateRotatingBackup();
                    }
                    catch (System.Exception ex)
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
    }
}
