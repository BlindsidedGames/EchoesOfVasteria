using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Manages the town UI windows. Clicking a button closes all windows except
    ///     the inventory and opens the associated one. Right-click closes all
    ///     windows.
    /// </summary>
    public class TownWindowManager : MonoBehaviour
    {
        public static TownWindowManager Instance { get; private set; }

        [Serializable]
        [InlineProperty]
        public class WindowReference
        {
            [HorizontalGroup("Row")] public Button button;
            [HorizontalGroup("Row2")] public GameObject window;
            [HorizontalGroup("Row3")] public Button closeButton;
        }

        [Title("References")] [SerializeField] private WindowReference upgrades = new();
        [SerializeField] [Space] private WindowReference buffs = new();
        [SerializeField] [Space] private WindowReference quests = new();
        [SerializeField] [Space] private WindowReference credits = new();
        [SerializeField] [Space] private WindowReference disciples = new();
        [SerializeField] [Space] private WindowReference stats = new();
        [SerializeField] [Space] private WindowReference wiki = new();
        [SerializeField] [Space] private WindowReference inventory = new();
        [SerializeField] [Space] private WindowReference options = new();
        [SerializeField] [Space] private GameObject townButtons;
        [SerializeField] [Space] private GameObject windowsOpenIndicator;


        private void Awake()
        {
            Instance = this;
            if (upgrades.button != null)
                upgrades.button.onClick.AddListener(OpenUpgrades);
            if (upgrades.closeButton != null)
                upgrades.closeButton.onClick.AddListener(() => CloseWindow(upgrades.window));
            if (buffs.button != null)
                buffs.button.onClick.AddListener(OpenBuffs);
            if (buffs.closeButton != null)
                buffs.closeButton.onClick.AddListener(() => CloseWindow(buffs.window));
            if (quests.button != null)
                quests.button.onClick.AddListener(OpenQuests);
            if (quests.closeButton != null)
                quests.closeButton.onClick.AddListener(() => CloseWindow(quests.window));
            if (credits.button != null)
                credits.button.onClick.AddListener(OpenCredits);
            if (credits.closeButton != null)
                credits.closeButton.onClick.AddListener(() => CloseWindow(credits.window));
            if (disciples.button != null)
                disciples.button.onClick.AddListener(OpenDisciples);
            if (disciples.closeButton != null)
                disciples.closeButton.onClick.AddListener(() => CloseWindow(disciples.window));
            if (stats.button != null)
                stats.button.onClick.AddListener(OpenStats);
            if (stats.closeButton != null)
                stats.closeButton.onClick.AddListener(() => CloseWindow(stats.window));
            if (wiki.button != null)
                wiki.button.onClick.AddListener(OpenWiki);
            if (wiki.closeButton != null)
                wiki.closeButton.onClick.AddListener(() => CloseWindow(wiki.window));
            if (options.button != null)
                options.button.onClick.AddListener(OpenOptions);
            if (options.closeButton != null)
                options.closeButton.onClick.AddListener(() => CloseWindow(options.window));
            if (inventory.button != null)
                inventory.button.onClick.AddListener(ToggleInventory);
            if (inventory.closeButton != null)
                inventory.closeButton.onClick.AddListener(() => CloseWindow(inventory.window));
        }

        private void Start()
        {
            CloseAllWindows();
        }

        private void OnDestroy()
        {
            if (upgrades.button != null)
                upgrades.button.onClick.RemoveListener(OpenUpgrades);
            if (upgrades.closeButton != null)
                upgrades.closeButton.onClick.RemoveAllListeners();
            if (buffs.button != null)
                buffs.button.onClick.RemoveListener(OpenBuffs);
            if (buffs.closeButton != null)
                buffs.closeButton.onClick.RemoveAllListeners();
            if (quests.button != null)
                quests.button.onClick.RemoveListener(OpenQuests);
            if (quests.closeButton != null)
                quests.closeButton.onClick.RemoveAllListeners();
            if (credits.button != null)
                credits.button.onClick.RemoveListener(OpenCredits);
            if (credits.closeButton != null)
                credits.closeButton.onClick.RemoveAllListeners();
            if (disciples.button != null)
                disciples.button.onClick.RemoveListener(OpenDisciples);
            if (disciples.closeButton != null)
                disciples.closeButton.onClick.RemoveAllListeners();
            if (stats.button != null)
                stats.button.onClick.RemoveListener(OpenStats);
            if (stats.closeButton != null)
                stats.closeButton.onClick.RemoveAllListeners();
            if (wiki.button != null)
                wiki.button.onClick.RemoveListener(OpenWiki);
            if (wiki.closeButton != null)
                wiki.closeButton.onClick.RemoveAllListeners();
            if (options.button != null)
                options.button.onClick.RemoveListener(OpenOptions);
            if (options.closeButton != null)
                options.closeButton.onClick.RemoveAllListeners();
            if (inventory.button != null)
                inventory.button.onClick.RemoveListener(ToggleInventory);
            if (inventory.closeButton != null)
                inventory.closeButton.onClick.RemoveAllListeners();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                CloseAllWindows();
        }

        private void OpenUpgrades()
        {
            ToggleWindow(upgrades.window);
        }

        private void OpenBuffs()
        {
            ToggleWindow(buffs.window);
        }

        private void OpenQuests()
        {
            ToggleWindow(quests.window);
        }

        private void OpenCredits()
        {
            ToggleWindow(credits.window);
        }

        private void OpenDisciples()
        {
            ToggleWindow(disciples.window);
        }

        private void OpenStats()
        {
            ToggleWindow(stats.window);
        }

        private void OpenWiki()
        {
            ToggleWindow(wiki.window);
        }

        private void OpenOptions()
        {
            ToggleWindow(options.window);
        }

        private void ToggleInventory()
        {
            if (inventory.window != null)
                inventory.window.SetActive(!inventory.window.activeSelf);
            UpdateTownButtonsVisibility();
        }

        private void ToggleWindow(GameObject window)
        {
            if (window == null)
                return;

            var wasActive = window.activeSelf;
            if (!wasActive) CloseAllWindowsExceptInventory();
            window.SetActive(!wasActive);
            UpdateTownButtonsVisibility();
        }

        private void CloseWindow(GameObject window)
        {
            if (window != null)
                window.SetActive(false);
            UpdateTownButtonsVisibility();
        }

        private void CloseAllWindowsExceptInventory()
        {
            if (upgrades.window != null)
                upgrades.window.SetActive(false);
            if (buffs.window != null)
                buffs.window.SetActive(false);
            if (quests.window != null)
                quests.window.SetActive(false);
            if (credits.window != null)
                credits.window.SetActive(false);
            if (disciples.window != null)
                disciples.window.SetActive(false);
            if (stats.window != null)
                stats.window.SetActive(false);
            if (wiki.window != null)
                wiki.window.SetActive(false);
            if (options.window != null)
                options.window.SetActive(false);
        }

        public void CloseAllWindows()
        {
            if (upgrades.window != null)
                upgrades.window.SetActive(false);
            if (buffs.window != null)
                buffs.window.SetActive(false);
            if (quests.window != null)
                quests.window.SetActive(false);
            if (credits.window != null)
                credits.window.SetActive(false);
            if (disciples.window != null)
                disciples.window.SetActive(false);
            if (stats.window != null)
                stats.window.SetActive(false);
            if (wiki.window != null)
                wiki.window.SetActive(false);
            if (options.window != null)
                options.window.SetActive(false);
            if (inventory.window != null)
                inventory.window.SetActive(false);
            UpdateTownButtonsVisibility();
        }

        private bool AnyWindowOpen()
        {
            return (upgrades.window != null && upgrades.window.activeSelf)
                   || (buffs.window != null && buffs.window.activeSelf)
                   || (quests.window != null && quests.window.activeSelf)
                   || (credits.window != null && credits.window.activeSelf)
                   || (disciples.window != null && disciples.window.activeSelf)
                   || (stats.window != null && stats.window.activeSelf)
                   || (wiki.window != null && wiki.window.activeSelf)
                   || (options.window != null && options.window.activeSelf)
                   || (inventory.window != null && inventory.window.activeSelf);
        }

        private void UpdateTownButtonsVisibility()
        {
            if (townButtons != null)
                townButtons.SetActive(!AnyWindowOpen());
            if (windowsOpenIndicator != null)
                windowsOpenIndicator.SetActive(AnyWindowOpen());
        }
    }
}