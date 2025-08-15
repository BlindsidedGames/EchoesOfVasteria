using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TimelessEchoes;
using TimelessEchoes.Stats;

namespace Blindsided
{
    public partial class Oracle
    {

        private void Start()
        {
            Load();
            if (StaticReferences.TargetFps <= 0)
                StaticReferences.TargetFps = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = StaticReferences.TargetFps;
                        InvokeRepeating(nameof(SaveToFile), 1, 30);

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

