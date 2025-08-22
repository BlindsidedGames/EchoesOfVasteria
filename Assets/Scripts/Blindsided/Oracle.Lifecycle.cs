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
            if (Application.isPlaying && !wipeInProgress && oracle == this && _settings != null)
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
                // Skip autosave while wiping or when a load-failure notice is pending
                if (!wipeInProgress && !_pendingLoadFailureNotice)
                {
                    try
                    {
                        SaveToFile();
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
