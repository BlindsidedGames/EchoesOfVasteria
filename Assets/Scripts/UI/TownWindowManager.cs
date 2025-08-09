using System;
using Blindsided;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EventHandler = Blindsided.EventHandler;

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
            [HorizontalGroup("Row4")] public bool openInventory;
        }

        [Title("References")] [SerializeField] private WindowReference upgrades = new();
        [SerializeField] [Space] private WindowReference buffs = new();
        [SerializeField] [Space] private WindowReference quests = new();
        [SerializeField] [Space] private WindowReference credits = new();
        [SerializeField] [Space] private WindowReference alterEchoes = new();
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
            if (alterEchoes.button != null)
                alterEchoes.button.onClick.AddListener(OpenAlterEchoes);
            if (alterEchoes.closeButton != null)
                alterEchoes.closeButton.onClick.AddListener(() => CloseWindow(alterEchoes.window));
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
                inventory.closeButton.onClick.AddListener(CloseAllWindows);
        }

        private void OnEnable()
        {
            EventHandler.OnLoadData += HandleLoadData;
            UITicker.Instance?.Subscribe(PollCloseAllWindows, 0.05f);
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= HandleLoadData;
            UITicker.Instance?.Unsubscribe(PollCloseAllWindows);
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
            if (alterEchoes.button != null)
                alterEchoes.button.onClick.RemoveListener(OpenAlterEchoes);
            if (alterEchoes.closeButton != null)
                alterEchoes.closeButton.onClick.RemoveAllListeners();
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
                inventory.closeButton.onClick.RemoveListener(CloseAllWindows);
            if (Instance == this)
                Instance = null;
        }

        private void PollCloseAllWindows()
        {
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                CloseAllWindows();
        }

        private void HandleLoadData()
        {
            if (Oracle.oracle == null)
                return;

            if (!Oracle.oracle.saveData.SavedPreferences.Tutorial)
            {
                CloseAllWindows();
                if (quests.window != null)
                    quests.window.SetActive(true);
                if (inventory.window != null)
                    inventory.window.SetActive(true);
                UpdateTownButtonsVisibility();
                Oracle.oracle.saveData.SavedPreferences.Tutorial = true;
                EventHandler.SaveData();
            }
        }

        private void OpenUpgrades()
        {
            ToggleWindow(upgrades);
        }

        private void OpenBuffs()
        {
            ToggleWindow(buffs);
        }

        private void OpenQuests()
        {
            ToggleWindow(quests);
        }

        private void OpenCredits()
        {
            ToggleWindow(credits);
        }

        private void OpenAlterEchoes()
        {
            ToggleWindow(alterEchoes);
        }

        private void OpenStats()
        {
            ToggleWindow(stats);
        }

        private void OpenWiki()
        {
            ToggleWindow(wiki);
        }

        private void OpenOptions()
        {
            ToggleWindow(options);
        }

        private void ToggleInventory()
        {
            if (inventory.window != null)
                inventory.window.SetActive(!inventory.window.activeSelf);
            UpdateTownButtonsVisibility();
        }

        private void ToggleWindow(WindowReference reference)
        {
            if (reference.window == null)
                return;

            var wasActive = reference.window.activeSelf;
            if (!wasActive) CloseAllWindowsExceptInventory();
            reference.window.SetActive(!wasActive);
            if (reference.openInventory && reference.window.activeSelf && inventory.window != null)
                inventory.window.SetActive(true);
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
            if (alterEchoes.window != null)
                alterEchoes.window.SetActive(false);
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
            if (alterEchoes.window != null)
                alterEchoes.window.SetActive(false);
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
                   || (alterEchoes.window != null && alterEchoes.window.activeSelf)
                   || (stats.window != null && stats.window.activeSelf)
                   || (wiki.window != null && wiki.window.activeSelf)
                   || (options.window != null && options.window.activeSelf)
                   || (inventory.window != null && inventory.window.activeSelf);
        }

        /// <summary>
        ///     Determines whether any window other than the stats window is open.
        ///     Used for the windows open indicator so that opening the stats
        ///     screen does not activate it.
        /// </summary>
        private bool AnyWindowOpenForIndicator()
        {
            return (upgrades.window != null && upgrades.window.activeSelf)
                   || (buffs.window != null && buffs.window.activeSelf)
                   || (quests.window != null && quests.window.activeSelf)
                   || (credits.window != null && credits.window.activeSelf)
                   || (alterEchoes.window != null && alterEchoes.window.activeSelf)
                   || (wiki.window != null && wiki.window.activeSelf)
                   || (options.window != null && options.window.activeSelf)
                   || (inventory.window != null && inventory.window.activeSelf);
        }

        private void UpdateTownButtonsVisibility()
        {
            if (townButtons != null)
                townButtons.SetActive(!AnyWindowOpen());
            if (windowsOpenIndicator != null)
                windowsOpenIndicator.SetActive(AnyWindowOpenForIndicator());
        }
    }
}