using System;
using Blindsided;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using EventHandler = Blindsided.EventHandler;
using TimelessEchoes.Upgrades;
using System.Collections;

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

        public enum Window { Upgrades, Buffs, Quests, Credits, AlterEchoes, Stats, Skills, Library, Cauldron, Forge, Inventory, Options, Farm }
        public event Action CloseRequested;
        public event Action WindowsChanged;
        public bool HasOpenWindow => AnyWindowOpen();
        public bool ForgeNeedsAttention { get; private set; }
        public bool CauldronNeedsAttention { get; private set; }
        private readonly System.Collections.Generic.HashSet<Window> tutorialLockedRoutes = new();
        public bool CanOpenWindow(Window window) => !tutorialLockedRoutes.Contains(window);

        /// <summary>UI-independent routes shared by native and legacy navigation during migration.</summary>
        public void OpenWindow(Window window)
        {
            if (!CanOpenWindow(window)) return;
            switch (window)
            {
                case Window.Upgrades: OpenUpgrades(); break;
                case Window.Buffs: OpenBuffs(); break;
                case Window.Quests:
                    // This was a persistent event on the original Quests button.
                    var scroll = quests.window != null ? quests.window.GetComponentInChildren<ScrollRect>(true) : null;
                    if (scroll != null) scroll.verticalNormalizedPosition = 1;
                    OpenQuests(); break;
                case Window.Credits: OpenCredits(); break;
                case Window.AlterEchoes: OpenAlterEchoes(); break;
                case Window.Stats: OpenStats(); break;
                case Window.Skills: OpenSkills(); break;
                case Window.Library: OpenWiki(); break;
                case Window.Cauldron: OpenCauldron(); break;
                case Window.Forge: OpenForge(); break;
                case Window.Inventory: OpenInventory(); break;
                case Window.Options: OpenOptions(); break;
                case Window.Farm: OpenFarm(); break;
            }
        }

        public bool IsWindowOpen(Window window)
        {
            if (window == Window.Farm && toolkitFarm != null) return toolkitFarm.IsOpen;
            if (window == Window.Forge && toolkitForge != null && toolkitForge.IsConfigured) return toolkitForge.IsOpen;
            if (window == Window.Stats && toolkitStatistics != null && toolkitStatistics.IsConfigured) return toolkitStatistics.IsOpen;
            if (window == Window.AlterEchoes && toolkitAlterEchoes != null && toolkitAlterEchoes.IsConfigured) return toolkitAlterEchoes.IsOpen;
            if (window == Window.Cauldron && toolkitCauldron != null && toolkitCauldron.IsConfigured) return toolkitCauldron.IsOpen;
            if (window == Window.Inventory && toolkitResources != null && toolkitResources.IsConfigured) return toolkitResources.IsOpen;
            if (window == Window.Quests && toolkitQuests != null && toolkitQuests.IsConfigured) return toolkitQuests.IsOpen;
            if (window == Window.Skills && toolkitSkills != null && toolkitSkills.IsConfigured) return toolkitSkills.IsOpen;
            if (window == Window.Buffs && toolkitBuffs != null && toolkitBuffs.IsConfigured) return toolkitBuffs.IsOpen;
            if (window == Window.Options && toolkitOptions != null && toolkitOptions.IsConfigured) return toolkitOptions.IsOpen;
            if (window == Window.Library && toolkitLibrary != null && toolkitLibrary.IsConfigured) return toolkitLibrary.IsOpen;
            if (window == Window.Credits && toolkitCredits != null && toolkitCredits.IsConfigured) return toolkitCredits.IsOpen;
            var reference = window switch
            {
                Window.Upgrades => upgrades, Window.Buffs => buffs, Window.Quests => quests,
                Window.Credits => credits, Window.AlterEchoes => alterEchoes, Window.Stats => stats,
                Window.Skills => skills, Window.Library => wiki,
                Window.Forge => forge, Window.Inventory => inventory, Window.Options => options, _ => null
            };
            return reference?.window != null && reference.window.activeSelf;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // Expose simple open-state checks for other systems
        public static bool IsForgeOpen => Instance != null && Instance.IsWindowOpen(Window.Forge);
        public static bool IsCauldronOpen => Instance != null && Instance.IsWindowOpen(Window.Cauldron);

        [Title("Attention Indicators")]
        [SerializeField] private GameObject cauldronAttentionObject;
        [SerializeField] private GameObject forgeAttentionObject;

        public static void ShowForgeAttention()
        {
            var inst = Instance;
            if (inst == null) return;
            if (IsForgeOpen) return;
            inst.ForgeNeedsAttention = true;
            if (inst.forgeAttentionObject != null)
                inst.forgeAttentionObject.SetActive(true);
        }

        public static void ShowCauldronAttention()
        {
            var inst = Instance;
            if (inst == null) return;
            if (IsCauldronOpen) return;
            inst.CauldronNeedsAttention = true;
            if (inst.cauldronAttentionObject != null)
                inst.cauldronAttentionObject.SetActive(true);
        }

        public static void ClearForgeAttention()
        {
            var inst = Instance;
            if (inst != null) inst.ForgeNeedsAttention = false;
            if (inst?.forgeAttentionObject != null)
                inst.forgeAttentionObject.SetActive(false);
        }

        public static void ClearCauldronAttention()
        {
            var inst = Instance;
            if (inst != null) inst.CauldronNeedsAttention = false;
            if (inst?.cauldronAttentionObject != null)
                inst.cauldronAttentionObject.SetActive(false);
        }

        private bool _cauldronStopHooked;
        private bool _cauldronTastingStartedThisSession;
        private Coroutine _pollCloseWindowsCoroutine;
        private void HookCauldronStop()
        {
            if (_cauldronStopHooked) return;
            var mgr = CauldronManager.Instance;
            if (mgr != null)
            {
                _cauldronTastingStartedThisSession = mgr.IsTasting;
                mgr.OnTasteSessionStarted += OnCauldronStartedGlobal;
                mgr.OnTasteSessionStopped += OnCauldronStoppedGlobal;
                _cauldronStopHooked = true;
            }
        }

        private void UnhookCauldronStop()
        {
            if (!_cauldronStopHooked) return;
            var mgr = CauldronManager.Instance;
            if (mgr != null)
            {
                mgr.OnTasteSessionStarted -= OnCauldronStartedGlobal;
                mgr.OnTasteSessionStopped -= OnCauldronStoppedGlobal;
            }
            _cauldronStopHooked = false;
        }

        private void OnCauldronStartedGlobal()
        {
            _cauldronTastingStartedThisSession = true;
            ClearCauldronAttention();
        }

        private void OnCauldronStoppedGlobal()
        {
            if (!_cauldronTastingStartedThisSession) return;

            _cauldronTastingStartedThisSession = false;

            if (!IsCauldronOpen)
            {
                ShowCauldronAttention();
                FindAnyObjectByType<TaskbarFlasher>()?.FlashNow();
            }
            else
            {
                ClearCauldronAttention();
            }
        }

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
        [SerializeField] private Toolkit.ToolkitBookScreen toolkitLibrary;
        [SerializeField] private Toolkit.ToolkitBookScreen toolkitCredits;
        [SerializeField] private Toolkit.ToolkitOptionsScreen toolkitOptions;
        [SerializeField] private Toolkit.ToolkitBuffsScreen toolkitBuffs;
        [SerializeField] private Toolkit.ToolkitSkillsScreen toolkitSkills;
        [SerializeField] private Toolkit.ToolkitQuestsScreen toolkitQuests;
        [SerializeField] private Toolkit.ToolkitCauldronScreen toolkitCauldron;
        [SerializeField] private Toolkit.ToolkitAlterEchoesScreen toolkitAlterEchoes;
        [SerializeField] private Toolkit.ToolkitFarmScreen toolkitFarm;
        public void ConfigureFarm(Toolkit.ToolkitFarmScreen screen) => toolkitFarm = screen;
        private void OpenFarm()
        {
            if (toolkitFarm == null || !toolkitFarm.IsConfigured) return;
            var wasOpen = toolkitFarm.IsOpen;
            CloseAllWindows();
            if (!wasOpen) toolkitFarm.Show();
            UpdateTownButtonsVisibility();
        }
        [SerializeField] private Toolkit.ToolkitStatisticsScreen toolkitStatistics;
        [SerializeField] private Toolkit.ToolkitForgeScreen toolkitForge;
        [SerializeField] private GameObject toolkitQuestLayoutSpace;
        [SerializeField] private Toolkit.ToolkitResourceInventoryScreen toolkitResources;
        [SerializeField] private RectTransform toolkitResourceLayoutSpace;
        private readonly Vector3[] resourceLayoutCorners = new Vector3[4];
        [SerializeField] [Space] private WindowReference forge = new();
        [SerializeField] [Space] private WindowReference inventory = new();
        [SerializeField] [Space] private GameObject forgeInfo;
        [SerializeField] [Space] private Button forgeInfoButton;
        [SerializeField] [Space] private TMP_Text forgeInfoButtonText;
        [SerializeField] [Space] private WindowReference options = new();


        [SerializeField] [Space] private GameObject discord;
        [SerializeField] [Space] private GameObject autoPin;
        [SerializeField] [Space] private GameObject stopOnVastium;
        [SerializeField] [Space] private GameObject lockStats;
        [SerializeField] [Space] private GameObject townButtons;
        [SerializeField] [Space] private GameObject windowsOpenIndicator;
        [SerializeField] [Space] private Button closeButton;
        [SerializeField] [Space] private Button townsfolkButton;
        [SerializeField] [Space] private GameObject townsfolkDropdown;
        [FormerlySerializedAs("calebButton")]
        [SerializeField] [Space] private Button hubButton;
        [FormerlySerializedAs("calebDropdown")]
        [SerializeField] [Space] private GameObject hubDropdown;
        [SerializeField] [Space] private Button beginAdventureButton;
        [SerializeField] [Space] private GameObject beginAdventureDropdown;

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
            if (hubButton != null)
                hubButton.onClick.AddListener(ToggleHubDropdown);
            if (beginAdventureButton != null)
                beginAdventureButton.onClick.AddListener(ToggleBeginAdventureDropdown);

        }

        private void OnEnable()
        {
            _cauldronTastingStartedThisSession = false;
            EventHandler.OnLoadData += HandleLoadData;
            _pollCloseWindowsCoroutine = StartCoroutine(PollCloseAllWindowsCoroutine());
            HookCauldronStop();
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= HandleLoadData;
            if (_pollCloseWindowsCoroutine != null)
            {
                StopCoroutine(_pollCloseWindowsCoroutine);
                _pollCloseWindowsCoroutine = null;
            }
            UnhookCauldronStop();
            _cauldronTastingStartedThisSession = false;
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
            if (hubButton != null)
                hubButton.onClick.RemoveListener(ToggleHubDropdown);
            if (beginAdventureButton != null)
                beginAdventureButton.onClick.RemoveListener(ToggleBeginAdventureDropdown);


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

        private IEnumerator PollCloseAllWindowsCoroutine()
        {
            var wait = new WaitForSecondsRealtime(0.05f);
            while (true)
            {
                PollCloseAllWindows();
                yield return wait;
            }
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
                    if (toolkitQuests != null && toolkitQuests.IsConfigured) ShowNativeQuests(true);
                    else Debug.LogError("Native Quests view is not configured.", this);
                    if (autoPin != null)
                        autoPin.SetActive(true);
                    if (quests.button != null)
                        quests.button.interactable = false;
                    tutorialLockedRoutes.Add(Window.Quests);
                }

                if (inventory.window != null)
                {
                    SetResourceInventoryVisible(true);
                    if (inventory.button != null)
                        inventory.button.interactable = false;
                    tutorialLockedRoutes.Add(Window.Inventory);
                }

                UpdateTownButtonsVisibility();
                Oracle.oracle.saveData.SavedPreferences.Tutorial = true;
                EventHandler.SaveData();
            }
        }

        private void OpenUpgrades() => OpenForge();

        private void OpenBuffs()
        {
            if (toolkitBuffs != null && toolkitBuffs.IsConfigured)
            {
                var wasOpen = toolkitBuffs.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitBuffs.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Buffs view is not configured.", this);
        }

        private void OpenQuests()
        {
            if (toolkitQuests != null && toolkitQuests.IsConfigured)
            {
                var wasOpen = toolkitQuests.IsOpen;
                CloseAllWindows();
                if (!wasOpen) ShowNativeQuests(quests.openInventory);
                if (autoPin != null) autoPin.SetActive(!wasOpen);
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Quests view is not configured.", this);
        }

        private void ShowNativeQuests(bool withInventory)
        {
            
            SetResourceInventoryVisible(withInventory);
            toolkitQuests.CompanionWidth = withInventory ? 178 : 0;
            toolkitQuests.Show();
        }

        private void OpenCredits()
        {
            if (toolkitCredits != null && toolkitCredits.IsConfigured)
            {
                var wasOpen = toolkitCredits.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitCredits.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Credits view is not configured.", this);
        }

        private void OpenAlterEchoes()
        {
            if (toolkitAlterEchoes != null && toolkitAlterEchoes.IsConfigured)
            {
                var wasOpen = toolkitAlterEchoes.IsOpen;
                CloseAllWindows();
                if (!wasOpen)
                {
                    
                    SetResourceInventoryVisible(alterEchoes.openInventory);
                    toolkitAlterEchoes.CompanionWidth = alterEchoes.openInventory ? 178 : 0;
                    toolkitAlterEchoes.Show();
                }
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native AlterEchoes view is not configured.", this);
        }

        private void OpenStats()
        {
            if (toolkitStatistics != null && toolkitStatistics.IsConfigured)
            {
                var wasOpen = toolkitStatistics.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitStatistics.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Stats view is not configured.", this);
        }

        private void OpenSkills()
        {
            if (toolkitSkills != null && toolkitSkills.IsConfigured)
            {
                var wasOpen = toolkitSkills.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitSkills.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Skills view is not configured.", this);
        }

        private void OpenWiki()
        {
            if (toolkitLibrary != null && toolkitLibrary.IsConfigured)
            {
                var wasOpen = toolkitLibrary.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitLibrary.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Wiki view is not configured.", this);
        }

        private void OpenCauldron()
        {
            if (toolkitCauldron != null && toolkitCauldron.IsConfigured)
            {
                var wasOpen = toolkitCauldron.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitCauldron.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Cauldron view is not configured.", this);
        }

        private void OpenOptions()
        {
            if (toolkitOptions != null && toolkitOptions.IsConfigured)
            {
                var wasOpen = toolkitOptions.IsOpen;
                CloseAllWindows();
                if (!wasOpen) toolkitOptions.Show();
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Options view is not configured.", this);
        }

        private void OpenForge()
        {
            if (toolkitForge != null && toolkitForge.IsConfigured)
            {
                var wasOpen = toolkitForge.IsOpen;
                CloseAllWindows();
                if (!wasOpen) { toolkitForge.Show(); ClearForgeAttention(); }
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Forge view is not configured.", this);
        }

        private void OpenInventory()
        {
            if (toolkitResources != null && toolkitResources.IsConfigured)
            {
                var wasOpen = toolkitResources.IsOpen;
                CloseAllWindows();
                SetResourceInventoryVisible(!wasOpen);
                UpdateTownButtonsVisibility();
                return;
            }
            Debug.LogError("Native Inventory view is not configured.", this);
        }

        private void SetResourceInventoryVisible(bool visible)
        {
            if (toolkitResources != null && toolkitResources.IsConfigured)
            {
                if (inventory.window != null) inventory.window.SetActive(false);
                if (toolkitResourceLayoutSpace != null) toolkitResourceLayoutSpace.gameObject.SetActive(false);
                if (visible) toolkitResources.Show(); else toolkitResources.Hide();
            }
            else if (visible) Debug.LogError("Native inventory is not configured.", this);
        }

        public bool TryHighlightNativeResource(Upgrades.Resource resource, bool scrollToSlot)
        {
            if (toolkitResources == null || !toolkitResources.IsConfigured) return false;
            SetResourceInventoryVisible(true);
            toolkitResources.HighlightResource(resource, scrollToSlot);
            UpdateTownButtonsVisibility();
            return true;
        }

        private void LateUpdate()
        {
            if (toolkitResources == null || !toolkitResources.IsOpen || toolkitResources.ManualLayout) return;
            var area = Toolkit.ToolkitWindowLayout.SafeArea;
            toolkitResources.Bounds = new Rect(area.xMax - 190, area.y + 44, 178, Mathf.Max(0,area.height - 56));
        }

        private void ToggleForgeInfo()
        {
            if (forgeInfo == null || inventory.window == null)
                return;

            var inventoryActive = IsWindowOpen(Window.Inventory);
            var showInventory = !inventoryActive;

            SetResourceInventoryVisible(showInventory);

            forgeInfo.SetActive(!showInventory);

            if (forgeInfoButtonText != null)
                forgeInfoButtonText.text = showInventory ? "Info" : "Inventory";
        }

        private void ToggleTownsfolkDropdown()
        {
            if (townsfolkDropdown == null)
                return;

            // Ensure mutual exclusivity with other dropdowns
            CloseHubDropdown();
            CloseBeginAdventureDropdown();

            var newActive = !townsfolkDropdown.activeSelf;
            townsfolkDropdown.SetActive(newActive);
        }

        private void CloseTownsfolkDropdown()
        {
            if (townsfolkDropdown != null && townsfolkDropdown.activeSelf)
                townsfolkDropdown.SetActive(false);
        }

        private void ToggleHubDropdown()
        {
            if (hubDropdown == null)
                return;

            // Ensure mutual exclusivity with other dropdowns
            CloseTownsfolkDropdown();
            CloseBeginAdventureDropdown();

            var newActive = !hubDropdown.activeSelf;
            hubDropdown.SetActive(newActive);
        }

        private void CloseHubDropdown()
        {
            if (hubDropdown != null && hubDropdown.activeSelf)
                hubDropdown.SetActive(false);
        }

        private void ToggleBeginAdventureDropdown()
        {
            if (beginAdventureDropdown == null)
                return;

            // Ensure mutual exclusivity with other dropdowns
            CloseTownsfolkDropdown();
            CloseHubDropdown();

            var newActive = !beginAdventureDropdown.activeSelf;
            beginAdventureDropdown.SetActive(newActive);
        }

        private void CloseBeginAdventureDropdown()
        {
            if (beginAdventureDropdown != null && beginAdventureDropdown.activeSelf)
                beginAdventureDropdown.SetActive(false);
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

            // Close all dropdowns when any window button is pressed
            CloseTownsfolkDropdown();
            CloseHubDropdown();
            CloseBeginAdventureDropdown();

            CloseAllWindows();

            if (!windowWasActive)
            {
                reference.window.SetActive(true);

                if (reference.openInventory)
                    if (inventory.window != null)
                        SetResourceInventoryVisible(true);
            }

            UpdateTownButtonsVisibility();
        }

        public void CloseAllWindows()
        {
            CloseRequested?.Invoke();
            if (toolkitLibrary != null) toolkitLibrary.Hide();
            if (toolkitCredits != null) toolkitCredits.Hide();
            if (toolkitOptions != null) toolkitOptions.Hide();
            if (toolkitBuffs != null) toolkitBuffs.Hide();
            if (toolkitSkills != null) toolkitSkills.Hide();
            if (toolkitQuests != null) toolkitQuests.Hide();
            if (toolkitCauldron != null) toolkitCauldron.Hide();
            if (toolkitFarm != null) toolkitFarm.Hide();
            if (toolkitAlterEchoes != null) toolkitAlterEchoes.Hide();
            if (toolkitStatistics != null) toolkitStatistics.Hide();
            if (toolkitForge != null) toolkitForge.Hide();
            if (toolkitQuestLayoutSpace != null) toolkitQuestLayoutSpace.SetActive(false);
            SetResourceInventoryVisible(false);
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
            if (lockStats != null)
                lockStats.SetActive(false);
            CloseTownsfolkDropdown();
            CloseHubDropdown();
            CloseBeginAdventureDropdown();

            EnableAllWindowButtons();
            UpdateTownButtonsVisibility();
        }

        private void EnableAllWindowButtons()
        {
            tutorialLockedRoutes.Clear();
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
            if (forge.button != null)
                forge.button.interactable = true;
            if (options.button != null)
                options.button.interactable = true;
            if (inventory.button != null)
                inventory.button.interactable = true;
        }

        private bool AnyWindowOpen()
        {
            return (toolkitFarm != null && toolkitFarm.IsOpen)
                   || (toolkitForge != null && toolkitForge.IsOpen)
                   || (toolkitStatistics != null && toolkitStatistics.IsOpen)
                   || (toolkitAlterEchoes != null && toolkitAlterEchoes.IsOpen)
                   || (toolkitCauldron != null && toolkitCauldron.IsOpen)
                   || (toolkitResources != null && toolkitResources.IsOpen)
                   || (toolkitQuests != null && toolkitQuests.IsOpen)
                   || (toolkitSkills != null && toolkitSkills.IsOpen)
                   || (toolkitBuffs != null && toolkitBuffs.IsOpen)
                   || (toolkitOptions != null && toolkitOptions.IsOpen)
                   || (toolkitLibrary != null && toolkitLibrary.IsOpen)
                   || (toolkitCredits != null && toolkitCredits.IsOpen)
                   || (upgrades.window != null && upgrades.window.activeSelf)
                   || (buffs.window != null && buffs.window.activeSelf)
                   || (quests.window != null && quests.window.activeSelf)
                   || (credits.window != null && credits.window.activeSelf)
                   || (alterEchoes.window != null && alterEchoes.window.activeSelf)
                   || (stats.window != null && stats.window.activeSelf)
                   || (skills.window != null && skills.window.activeSelf)
                   || (wiki.window != null && wiki.window.activeSelf)
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
            WindowsChanged?.Invoke();
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

