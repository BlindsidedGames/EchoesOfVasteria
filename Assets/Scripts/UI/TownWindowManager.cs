using System;
using Sirenix.OdinInspector;
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
        [Serializable]
        [InlineProperty]
        public class WindowReference
        {
            [HorizontalGroup("Row")] public Button button;
            [HorizontalGroup("Row")] public GameObject window;
        }

        [Title("References")]
        [SerializeField] private WindowReference upgrades = new();
        [SerializeField] private WindowReference buffs = new();
        [SerializeField] private WindowReference quests = new();
        [SerializeField] private WindowReference credits = new();
        [SerializeField] private WindowReference inventory = new();

        [SerializeField]
        private Button startRunButton;

        private void Awake()
        {
            if (upgrades.button != null)
                upgrades.button.onClick.AddListener(OpenUpgrades);
            if (buffs.button != null)
                buffs.button.onClick.AddListener(OpenBuffs);
            if (quests.button != null)
                quests.button.onClick.AddListener(OpenQuests);
            if (credits.button != null)
                credits.button.onClick.AddListener(OpenCredits);
            if (inventory.button != null)
                inventory.button.onClick.AddListener(ToggleInventory);
            if (startRunButton != null)
                startRunButton.onClick.AddListener(CloseAllWindows);
        }

        private void OnDestroy()
        {
            if (upgrades.button != null)
                upgrades.button.onClick.RemoveListener(OpenUpgrades);
            if (buffs.button != null)
                buffs.button.onClick.RemoveListener(OpenBuffs);
            if (quests.button != null)
                quests.button.onClick.RemoveListener(OpenQuests);
            if (credits.button != null)
                credits.button.onClick.RemoveListener(OpenCredits);
            if (inventory.button != null)
                inventory.button.onClick.RemoveListener(ToggleInventory);
            if (startRunButton != null)
                startRunButton.onClick.RemoveListener(CloseAllWindows);
        }

        private void OpenUpgrades() => OpenWindow(upgrades.window);
        private void OpenBuffs() => OpenWindow(buffs.window);
        private void OpenQuests() => OpenWindow(quests.window);
        private void OpenCredits() => OpenWindow(credits.window);
        private void ToggleInventory()
        {
            if (inventory.window != null)
                inventory.window.SetActive(!inventory.window.activeSelf);
        }

        private void OpenWindow(GameObject window)
        {
            CloseAllWindows();
            if (window != null)
                window.SetActive(true);
        }

        private void CloseAllWindows()
        {
            if (upgrades.window != null)
                upgrades.window.SetActive(false);
            if (buffs.window != null)
                buffs.window.SetActive(false);
            if (quests.window != null)
                quests.window.SetActive(false);
            if (credits.window != null)
                credits.window.SetActive(false);
            if (inventory.window != null)
                inventory.window.SetActive(false);
        }
    }
}
