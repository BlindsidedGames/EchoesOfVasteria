using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Manages the town UI windows. Clicking a button closes all windows except
    /// the inventory and opens the associated one. Right-click closes all
    /// windows.
    /// </summary>
    public class TownWindowManager : MonoBehaviour
    {
        public static TownWindowManager Instance { get; private set; }
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
        [SerializeField] private WindowReference disciples = new();
        [SerializeField] private WindowReference stats = new();
        [SerializeField] private WindowReference wiki = new();
        [SerializeField] private WindowReference inventory = new();
        [SerializeField] private WindowReference options = new();



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
            if (disciples.button != null)
                disciples.button.onClick.AddListener(OpenDisciples);
            if (stats.button != null)
                stats.button.onClick.AddListener(OpenStats);
            if (wiki.button != null)
                wiki.button.onClick.AddListener(OpenWiki);
            if (options.button != null)
                options.button.onClick.AddListener(OpenOptions);
            if (inventory.button != null)
                inventory.button.onClick.AddListener(ToggleInventory);
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
            if (disciples.button != null)
                disciples.button.onClick.RemoveListener(OpenDisciples);
            if (stats.button != null)
                stats.button.onClick.RemoveListener(OpenStats);
            if (wiki.button != null)
                wiki.button.onClick.RemoveListener(OpenWiki);
            if (options.button != null)
                options.button.onClick.RemoveListener(OpenOptions);
            if (inventory.button != null)
                inventory.button.onClick.RemoveListener(ToggleInventory);
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                CloseAllWindows();
        }

        private void OpenUpgrades() => OpenWindow(upgrades.window);
        private void OpenBuffs() => OpenWindow(buffs.window);
        private void OpenQuests() => OpenWindow(quests.window);
        private void OpenCredits() => OpenWindow(credits.window);
        private void OpenDisciples() => OpenWindow(disciples.window);
        private void OpenStats() => OpenWindow(stats.window);
        private void OpenWiki() => OpenWindow(wiki.window);
        private void OpenOptions() => OpenWindow(options.window);
        private void ToggleInventory()
        {
            if (inventory.window != null)
                inventory.window.SetActive(!inventory.window.activeSelf);
        }

        private void OpenWindow(GameObject window)
        {
            CloseAllWindowsExceptInventory();
            if (window != null)
                window.SetActive(true);
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
        }
    }
}
