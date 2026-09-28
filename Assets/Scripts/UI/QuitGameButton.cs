using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Handles quitting the game with a confirmation window.
    ///     The final exit action commits the required verified checkpoint(s) before quitting.
    /// </summary>
    public class QuitGameButton : MonoBehaviour
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject confirmWindow;
        [SerializeField] private Button exitButton;
        private readonly GameQuitRequest request = new();

        private void Awake()
        {
            if (!enabled) return; // Retired presentation must not subscribe or build hidden UI.
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
            if (confirmWindow != null)
                confirmWindow.SetActive(true);
        }

        private void OnExitClicked()
        {
            if (request.InProgress) return;
            if (exitButton != null) exitButton.interactable = false;
            request.TryQuit(GameQuitRequest.Prepare, GameQuitRequest.Exit);
            if (!request.InProgress && exitButton != null) exitButton.interactable = true;
        }

    }
}
