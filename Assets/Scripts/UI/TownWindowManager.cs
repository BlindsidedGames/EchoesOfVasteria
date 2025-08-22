using System;
using Blindsided;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EventHandler = Blindsided.EventHandler;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Manages the town UI windows. Clicking a button closes all windows,
    ///     opens the associated one, and shows a global close button. Selecting the
    ///     same button again closes its window. Right-click or the close button
    ///     closes all windows.
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
            [HorizontalGroup("Row3")] public bool openInventory;
        }

        [Title("References")] [SerializeField] private WindowReference upgrades = new();
        [SerializeField] [Space] private WindowReference buffs = new();
        [SerializeField] [Space] private WindowReference quests = new();
        [SerializeField] [Space] private WindowReference credits = new();
        [SerializeField] [Space] private WindowReference alterEchoes = new();
        [SerializeField] [Space] private WindowReference stats = new();
        [SerializeField] [Space] private WindowReference skills = new();
        [SerializeField] [Space] private WindowReference wiki = new();
        [SerializeField] [Space] private WindowReference cauldron = new();
        [SerializeField] [Space] private WindowReference forge = new();
        [SerializeField] [Space] private WindowReference inventory = new();
        [SerializeField] [Space] private GameObject forgeInfo;
        [SerializeField] [Space] private Button forgeInfoButton;
        [SerializeField] [Space] private TMP_Text forgeInfoButtonText;
        [SerializeField] [Space] private WindowReference options = new();
        [SerializeField] [Space] private GameObject discord;
        [SerializeField] [Space] private GameObject autoPin;
        [SerializeField] [Space] private GameObject stopOnVastium;
        [SerializeField] [Space] private GameObject townButtons;
        [SerializeField] [Space] private GameObject windowsOpenIndicator;
        [SerializeField] [Space] private Button closeButton;
        [SerializeField] [Space] private Button townsfolkButton;
        [SerializeField] [Space] private GameObject townsfolkDropdown;

        private bool _rightMouseWasDown;


        private void Awake()
        {
            Instance = this;
            if (upgrades.button != null)
                upgrades.button.onClick.AddListener(OpenUpgrades);
            if (buffs.button != null)
                buffs.button.onClick.AddListener(OpenBuffs);
            if (quests.button != null)
                quests.button.onClick.AddListener(OpenQuests);
            if (credits.button != null)
                credits.button.onClick.AddListener(OpenCredits);
            if (alterEchoes.button != null)
                alterEchoes.button.onClick.AddListener(OpenAlterEchoes);
            if (stats.button != null)
                stats.button.onClick.AddListener(OpenStats);
            if (skills.button != null)
                skills.button.onClick.AddListener(OpenSkills);
            if (wiki.button != null)
                wiki.button.onClick.AddListener(OpenWiki);
            if (cauldron.button != null)
                cauldron.button.onClick.AddListener(OpenCauldron);
            if (forge.button != null)
                forge.button.onClick.AddListener(OpenForge);
            if (options.button != null)
                options.button.onClick.AddListener(OpenOptions);
            if (inventory.button != null)
                inventory.button.onClick.AddListener(OpenInventory);
            if (forgeInfoButton != null)
                forgeInfoButton.onClick.AddListener(ToggleForgeInfo);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseAllWindows);
            if (townsfolkButton != null)
                townsfolkButton.onClick.AddListener(ToggleTownsfolkDropdown);
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
            if (buffs.button != null)
                buffs.button.onClick.RemoveListener(OpenBuffs);
            if (quests.button != null)
                quests.button.onClick.RemoveListener(OpenQuests);
            if (credits.button != null)
                credits.button.onClick.RemoveListener(OpenCredits);
            if (alterEchoes.button != null)
                alterEchoes.button.onClick.RemoveListener(OpenAlterEchoes);
            if (stats.button != null)
                stats.button.onClick.RemoveListener(OpenStats);
            if (skills.button != null)
                skills.button.onClick.RemoveListener(OpenSkills);
            if (wiki.button != null)
                wiki.button.onClick.RemoveListener(OpenWiki);
            if (cauldron.button != null)
                cauldron.button.onClick.RemoveListener(OpenCauldron);
            if (forge.button != null)
                forge.button.onClick.RemoveListener(OpenForge);
            if (options.button != null)
                options.button.onClick.RemoveListener(OpenOptions);
            if (inventory.button != null)
                inventory.button.onClick.RemoveListener(OpenInventory);
            if (forgeInfoButton != null)
                forgeInfoButton.onClick.RemoveListener(ToggleForgeInfo);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseAllWindows);
            if (townsfolkButton != null)
                townsfolkButton.onClick.RemoveListener(ToggleTownsfolkDropdown);
            if (Instance == this)
                Instance = null;
        }

        private void PollCloseAllWindows()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var isDown = mouse.rightButton.isPressed;
            if (isDown && !_rightMouseWasDown) CloseAllWindows();
            _rightMouseWasDown = isDown;
        }

        private void HandleLoadData()
        {
            if (Oracle.oracle == null)
                return;

            if (!Oracle.oracle.saveData.SavedPreferences.Tutorial)
            {
                CloseAllWindows();
                if (quests.window != null)
                {
                    quests.window.SetActive(true);
                    if (autoPin != null)
                        autoPin.SetActive(true);
                    if (quests.button != null)
                        quests.button.interactable = false;
                }

                if (inventory.window != null)
                {
                    inventory.window.SetActive(true);
                    if (inventory.button != null)
                        inventory.button.interactable = false;
                }

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
            var wasActive = buffs.window != null && buffs.window.activeSelf;
            ToggleWindow(buffs);
            if (wasActive) buffs.window.SetActive(false);
        }

        private void OpenQuests()
        {
            var wasActive = quests.window != null && quests.window.activeSelf;
            ToggleWindow(quests);
            if (autoPin != null)
                autoPin.SetActive(!wasActive);
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

        private void OpenSkills()
        {
            ToggleWindow(skills);
        }

        private void OpenWiki()
        {
            ToggleWindow(wiki);
        }

        private void OpenCauldron()
        {
            ToggleWindow(cauldron);
        }

        private void OpenOptions()
        {
            ToggleWindow(options);
            if (discord != null)
                discord.SetActive(true);
        }

        private void OpenForge()
        {
            var wasActive = forge.window != null && forge.window.activeSelf;
            ToggleWindow(forge);
            var isActive = forge.window != null && forge.window.activeSelf;

            if (isActive)
            {
                if (stopOnVastium != null)
                    stopOnVastium.SetActive(true);
                if (inventory.window != null) inventory.window.SetActive(false);
                if (forgeInfo != null)
                    forgeInfo.SetActive(true);
                if (forgeInfoButtonText != null)
                    forgeInfoButtonText.text = "Inventory";
            }
            else if (wasActive)
            {
                if (forgeInfo != null)
                    forgeInfo.SetActive(false);
                if (forgeInfoButtonText != null)
                    forgeInfoButtonText.text = "Info";
            }
        }

        private void OpenInventory()
        {
            ToggleWindow(inventory);
        }

        private void ToggleForgeInfo()
        {
            if (forgeInfo == null || inventory.window == null)
                return;

            var inventoryActive = inventory.window.activeSelf;
            var showInventory = !inventoryActive;

            inventory.window.SetActive(showInventory);

            forgeInfo.SetActive(!showInventory);

            if (forgeInfoButtonText != null)
                forgeInfoButtonText.text = showInventory ? "Info" : "Inventory";
        }

        private void ToggleTownsfolkDropdown()
        {
            if (townsfolkDropdown == null)
                return;

            var newActive = !townsfolkDropdown.activeSelf;
            townsfolkDropdown.SetActive(newActive);
        }

        private void CloseTownsfolkDropdown()
        {
            if (townsfolkDropdown != null && townsfolkDropdown.activeSelf)
                townsfolkDropdown.SetActive(false);
        }

        public void CloseForgeInfo()
        {
            if (forgeInfo != null)
                forgeInfo.SetActive(false);
        }

        private void ToggleWindow(WindowReference reference)
        {
            if (reference.window == null || reference.button == null)
                return;

            var windowWasActive = reference.window.activeSelf;

            // Close the townsfolk dropdown when any other button is pressed
            CloseTownsfolkDropdown();

            CloseAllWindows();

            if (!windowWasActive)
            {
                reference.window.SetActive(true);

                if (reference.openInventory)
                    if (inventory.window != null)
                        inventory.window.SetActive(true);
            }

            UpdateTownButtonsVisibility();
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
            if (skills.window != null)
                skills.window.SetActive(false);
            if (wiki.window != null)
                wiki.window.SetActive(false);
            if (cauldron.window != null)
                cauldron.window.SetActive(false);
            if (options.window != null)
                options.window.SetActive(false);
            if (forge.window != null)
                forge.window.SetActive(false);
            if (inventory.window != null)
                inventory.window.SetActive(false);
            if (forgeInfo != null)
                forgeInfo.SetActive(false);
            if (discord != null)
                discord.SetActive(false);
            if (autoPin != null)
                autoPin.SetActive(false);
            if (stopOnVastium != null)
                stopOnVastium.SetActive(false);
            CloseTownsfolkDropdown();

            EnableAllWindowButtons();
            UpdateTownButtonsVisibility();
        }

        private void EnableAllWindowButtons()
        {
            if (upgrades.button != null)
                upgrades.button.interactable = true;
            if (buffs.button != null)
                buffs.button.interactable = true;
            if (quests.button != null)
                quests.button.interactable = true;
            if (credits.button != null)
                credits.button.interactable = true;
            if (alterEchoes.button != null)
                alterEchoes.button.interactable = true;
            if (stats.button != null)
                stats.button.interactable = true;
            if (skills.button != null)
                skills.button.interactable = true;
            if (wiki.button != null)
                wiki.button.interactable = true;
            if (cauldron.button != null)
                cauldron.button.interactable = true;
            if (forge.button != null)
                forge.button.interactable = true;
            if (options.button != null)
                options.button.interactable = true;
            if (inventory.button != null)
                inventory.button.interactable = true;
        }

        private bool AnyWindowOpen()
        {
            return (upgrades.window != null && upgrades.window.activeSelf)
                   || (buffs.window != null && buffs.window.activeSelf)
                   || (quests.window != null && quests.window.activeSelf)
                   || (credits.window != null && credits.window.activeSelf)
                   || (alterEchoes.window != null && alterEchoes.window.activeSelf)
                   || (stats.window != null && stats.window.activeSelf)
                   || (skills.window != null && skills.window.activeSelf)
                   || (wiki.window != null && wiki.window.activeSelf)
                   || (cauldron.window != null && cauldron.window.activeSelf)
                   || (options.window != null && options.window.activeSelf)
                   || (forge.window != null && forge.window.activeSelf)
                   || (inventory.window != null && inventory.window.activeSelf);
        }

        private void UpdateTownButtonsVisibility()
        {
            if (townButtons != null)
                townButtons.SetActive(true);
            if (windowsOpenIndicator != null)
                windowsOpenIndicator.SetActive(AnyWindowOpen());
            if (closeButton != null)
                closeButton.gameObject.SetActive(ShouldShowGlobalCloseButton());
        }

        /// <summary>
        ///     Determines whether the global close button should be shown.
        ///     Show it whenever any window is open.
        /// </summary>
        private bool ShouldShowGlobalCloseButton()
        {
            return AnyWindowOpen();
        }
    }
}