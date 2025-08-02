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
            if (Oracle.oracle == null)
                return;

            EventHandler.SaveData();
            Oracle.oracle.saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            var beta = Oracle.oracle.beta;
            var iteration = Oracle.oracle.betaSaveIteration;
            var slot = Oracle.oracle.CurrentSlot;
            var dataName = (beta ? $"Beta{iteration}" : "") + $"Data{slot}";
            var fileName = (beta ? $"Beta{iteration}" : "") + $"Sd{slot}.es3";
            var settings = new ES3Settings(fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };

            ES3.Save(dataName, Oracle.oracle.saveData, settings);
            ES3.StoreCachedFile(fileName);
        }
    }
}