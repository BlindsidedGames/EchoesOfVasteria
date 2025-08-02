using System;
using System.Globalization;
using Blindsided;
using UnityEngine;
using UnityEngine.UI;
using EventHandler = Blindsided.EventHandler;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Handles quitting the game with a confirmation window.
    ///     First button saves progress and shows the confirm window.
    ///     Second button exits the application.
    /// </summary>
    public class QuitGameButton : MonoBehaviour
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject confirmWindow;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (quitButton == null)
                quitButton = GetComponent<Button>();

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDestroy()
        {
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnQuitClicked()
        {
            SaveGame();
            if (confirmWindow != null)
                confirmWindow.SetActive(true);
        }

        private void OnExitClicked()
        {
            SaveGame();
            Application.Quit();
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
#endif
        }

        private static void SaveGame()
        {
            var oracle = Oracle.oracle;
            if (oracle == null)
                return;

            EventHandler.SaveData();
            oracle.saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            ES3.Save(oracle.DataName, oracle.saveData, oracle.Settings);
            ES3.StoreCachedFile(oracle.FileName);
        }
    }
}