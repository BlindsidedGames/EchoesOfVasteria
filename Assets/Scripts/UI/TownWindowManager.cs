using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Manages the town UI windows. Clicking a button closes all windows and
    /// opens the associated one. The Start Run button only closes the windows.
    /// </summary>
    public class TownWindowManager : MonoBehaviour
    {
        [SerializeField] private Button upgradesButton;
        [SerializeField] private GameObject upgradesWindow;

        [SerializeField] private Button buffsButton;
        [SerializeField] private GameObject buffsWindow;

        [SerializeField] private Button questsButton;
        [SerializeField] private GameObject questsWindow;

        [SerializeField] private Button creditsButton;
        [SerializeField] private GameObject creditsWindow;

        [SerializeField] private Button inventoryButton;
        [SerializeField] private GameObject inventoryWindow;

        [SerializeField] private Button startRunButton;

        private void Awake()
        {
            if (upgradesButton != null)
                upgradesButton.onClick.AddListener(OpenUpgrades);
            if (buffsButton != null)
                buffsButton.onClick.AddListener(OpenBuffs);
            if (questsButton != null)
                questsButton.onClick.AddListener(OpenQuests);
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OpenCredits);
            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OpenInventory);
            if (startRunButton != null)
                startRunButton.onClick.AddListener(CloseAllWindows);
        }

        private void OnDestroy()
        {
            if (upgradesButton != null)
                upgradesButton.onClick.RemoveListener(OpenUpgrades);
            if (buffsButton != null)
                buffsButton.onClick.RemoveListener(OpenBuffs);
            if (questsButton != null)
                questsButton.onClick.RemoveListener(OpenQuests);
            if (creditsButton != null)
                creditsButton.onClick.RemoveListener(OpenCredits);
            if (inventoryButton != null)
                inventoryButton.onClick.RemoveListener(OpenInventory);
            if (startRunButton != null)
                startRunButton.onClick.RemoveListener(CloseAllWindows);
        }

        private void OpenUpgrades() => OpenWindow(upgradesWindow);
        private void OpenBuffs() => OpenWindow(buffsWindow);
        private void OpenQuests() => OpenWindow(questsWindow);
        private void OpenCredits() => OpenWindow(creditsWindow);
        private void OpenInventory() => OpenWindow(inventoryWindow);

        private void OpenWindow(GameObject window)
        {
            CloseAllWindows();
            if (window != null)
                window.SetActive(true);
        }

        private void CloseAllWindows()
        {
            if (upgradesWindow != null)
                upgradesWindow.SetActive(false);
            if (buffsWindow != null)
                buffsWindow.SetActive(false);
            if (questsWindow != null)
                questsWindow.SetActive(false);
            if (creditsWindow != null)
                creditsWindow.SetActive(false);
            if (inventoryWindow != null)
                inventoryWindow.SetActive(false);
        }
    }
}
