using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using MPUIKIT;
using TimelessEchoes.Gear;
using TimelessEchoes.Skills;
using TimelessEchoes.UI;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;
using EventHandler = Blindsided.EventHandler;

namespace TimelessEchoes.Gear.UI
{
    public class ForgeWindowUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")] [SerializeField]
        private Button craftButton;

        [SerializeField] private TMP_Text craftButtonText;

        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text resultTierText;
        [SerializeField] private Button replaceButton;
        [SerializeField] private Button craftUntilUpgradeButton;
        [SerializeField] private TMP_Text craftUntilUpgradeButtonText;

        [Header("Gear Slot UI")]
        [Tooltip("References to each visible gear slot in this window. Their Button will be wired to SelectSlot.")]
        [SerializeField] private List<GearSlotUIReferences> gearSlots = new();

        [Header("Core Slot UI")]
        [Tooltip("Pre-placed core slot references in the scene. No prefab route is used.")]
        [SerializeField] private List<CoreSlotUIReferences> coreSlots = new();

        [Header("Odds UI")] [SerializeField] private List<MPImageBasic> oddsPieSlices = new();

        [Header("Core Weight Tooltip")] [SerializeField] private Image coreWeightHoverImage;
        [SerializeField] private TMP_Text coreWeightHoverText;
        [SerializeField] private GameObject coreWeightHoverObject;

        [Header("Ivan XP UI")] [SerializeField]
        private SlicedFilledImage ivanXpBar;

        [SerializeField] private TMP_Text ivanXpText;
        [SerializeField] private TMP_Text ivanLevelText;

        [Header("Craft UI")] [SerializeField] private CraftSection2x1UIReferences craftSection;

        [Header("Ingot Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences ingotConversionSection;

        [Header("Crystal Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences crystalConversionSection;

        [Header("Chunk Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences chunkConversionSection;

        [Header("Core Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences coreConversionSection;

        [Header("Additional Resource References")] [SerializeField]
        private Resource slimeResource;

        [SerializeField] private Resource stoneResource;

        [Header("Attention Indicator")]
        [Tooltip("Optional: object to enable when forge autocrafting naturally stops while the window is closed.")]
        [SerializeField] private GameObject forgeAttentionObject;

        [Header("Core Conversion Logic Resources")]
        [Tooltip("Reference to all Core resources in tier order for conversion calculations (optional; otherwise discovered via selected core slots).")]
        [SerializeField] private List<Resource> coreResourcesInTierOrder = new();

        [Header("Selected Slot UI")]
        [Tooltip("Text to display the stats of the currently equipped gear in the selected slot.")]
        [SerializeField] private TMP_Text selectedSlotStatsText;
        [Header("Aggregate Stats UI")]
        [Tooltip("Text to display overall totals.")]
        [SerializeField] private TMP_Text aggregateTotalsChestText;

        [Tooltip("Text to display weapon and chest breakdown.")]
        [SerializeField] private TMP_Text aggregateWeaponBootsText;

        [Tooltip("Text to display helmet and boots breakdown.")]
        [SerializeField] private TMP_Text aggregateHelmetText;

        [Header("Unknown Gear Sprites (by slot order)")]
        [Tooltip("Fallback unknown sprites for each gear slot: Weapon, Helmet, Chest, Boots")]
        [SerializeField] private List<Sprite> unknownGearSprites = new();

        [Header("Migrated Gear Sprites")] [SerializeField]
        [Tooltip("Sprite to use for migrated Helmet gear that has no rarity assigned.")]
        private Sprite migratedHelmetSprite;
        #endregion

        #region Cached Services & State
        private CraftingService crafting;
        private EquipmentController equipment;
        private ResourceManager rm;
        private float nextOddsRefreshTime;
        private ResourceManager RM => rm ?? (rm = ResourceManager.Instance);

        private List<CoreSO> cores = new();
        private CoreSO selectedCore;
        private string selectedSlot;
        private GearItem lastCrafted;
        private bool isAutoCrafting;
        private Coroutine autoCraftCoroutine;

        // Runtime maps for robust selection/highlight handling
        private readonly Dictionary<GearSlotUIReferences, string> gearSlotNameByRef = new();
        private readonly Dictionary<CoreSlotUIReferences, CoreSO> coreSlotCoreByRef = new();

        // User-selected amounts for conversions (persisted per-save)
        private double ingotCraftAmount = 1;
        private double crystalCraftAmount = 1;
        private double chunkCraftAmount = 1;
        private double coreCraftAmount = 1;

        // Conversion pipelines for unified conversion handling
        private ConversionPipeline ingotPipeline;
        private ConversionPipeline crystalPipeline;
        private ConversionPipeline chunkPipeline;
        private ConversionPipeline corePipeline;

        // Dirty flags for deferred UI updates (Phase 1.5 optimization)
        private bool _gearSlotsNeedRefresh;
        private bool _statsNeedRefresh;
        #endregion

        #region Unity Lifecycle
    private void Awake()
        {
            crafting = CraftingService.Instance;
            equipment = EquipmentController.Instance;
            rm = ResourceManager.Instance;
            cores = AssetCache.GetAll<CoreSO>().Where(b => b != null).OrderBy(b => b.tierIndex).ToList();

            // Build Core selection UI using only pre-placed slots (no prefab route)
            coreSlotCoreByRef.Clear();
            for (var i = 0; i < coreSlots.Count; i++)
            {
                var slot = coreSlots[i];
                if (slot == null) continue;
                slot.SetSelected(false);
                var mappedCore = slot.Core != null ? slot.Core : i < cores.Count ? cores[i] : null;
                coreSlotCoreByRef[slot] = mappedCore;
                if (slot.SelectSlotButton != null)
                {
                    slot.SelectSlotButton.onClick.RemoveAllListeners();
                    var capturedSlot = slot;
                    var capturedCore = mappedCore;
                    slot.SelectSlotButton.onClick.AddListener(() => OnCoreSlotClicked(capturedSlot, capturedCore));
                }

                if (mappedCore == null)
                    Debug.LogWarning(
                        $"ForgeWindowUI: Core slot at index {i} has no Core mapped; assign Core on the slot or ensure cores are discoverable.");
            }

            if (craftButton != null)
                craftButton.onClick.AddListener(OnCraftClicked);
            if (replaceButton != null)
                replaceButton.onClick.AddListener(OnReplaceClicked);
            if (craftUntilUpgradeButton != null)
                craftUntilUpgradeButton.onClick.AddListener(OnCraftUntilUpgradeClicked);
            if (ingotConversionSection != null)
            {
                if (ingotConversionSection.craftButton != null)
                {
                    ingotConversionSection.craftButton.onClick.AddListener(OnCraftIngotClicked);
                    var repeat = ingotConversionSection.craftButton.GetComponent<RepeatButtonClick>() ??
                                 ingotConversionSection.craftButton.gameObject.AddComponent<RepeatButtonClick>();
                    repeat.button = ingotConversionSection.craftButton;
                }
                // Wire input
                if (ingotConversionSection.amountInput != null)
                {
                    ingotConversionSection.amountInput.onValueChanged.RemoveAllListeners();
                    ingotConversionSection.amountInput.onValueChanged.AddListener(_ =>
                        OnAmountInputChanged(ingotConversionSection, ref ingotCraftAmount));
                }
            }

            if (crystalConversionSection != null)
            {
                if (crystalConversionSection.craftButton != null)
                {
                    crystalConversionSection.craftButton.onClick.AddListener(OnCraftCrystalClicked);
                    var repeat = crystalConversionSection.craftButton.GetComponent<RepeatButtonClick>() ??
                                 crystalConversionSection.craftButton.gameObject.AddComponent<RepeatButtonClick>();
                    repeat.button = crystalConversionSection.craftButton;
                }
                if (crystalConversionSection.amountInput != null)
                {
                    crystalConversionSection.amountInput.onValueChanged.RemoveAllListeners();
                    crystalConversionSection.amountInput.onValueChanged.AddListener(_ =>
                        OnAmountInputChanged(crystalConversionSection, ref crystalCraftAmount));
                }
            }

            if (chunkConversionSection != null)
            {
                if (chunkConversionSection.craftButton != null)
                {
                    chunkConversionSection.craftButton.onClick.AddListener(OnCraftChunkClicked);
                    var repeat = chunkConversionSection.craftButton.GetComponent<RepeatButtonClick>() ??
                                 chunkConversionSection.craftButton.gameObject.AddComponent<RepeatButtonClick>();
                    repeat.button = chunkConversionSection.craftButton;
                }
                if (chunkConversionSection.amountInput != null)
                {
                    chunkConversionSection.amountInput.onValueChanged.RemoveAllListeners();
                    chunkConversionSection.amountInput.onValueChanged.AddListener(_ =>
                        OnAmountInputChanged(chunkConversionSection, ref chunkCraftAmount));
                }
            }

            if (coreConversionSection != null)
            {
                if (coreConversionSection.craftButton != null)
                {
                    coreConversionSection.craftButton.onClick.AddListener(OnCraftCoreConversionClicked);
                    var repeat = coreConversionSection.craftButton.GetComponent<RepeatButtonClick>() ??
                                 coreConversionSection.craftButton.gameObject.AddComponent<RepeatButtonClick>();
                    repeat.button = coreConversionSection.craftButton;
                }
                if (coreConversionSection.amountInput != null)
                {
                    coreConversionSection.amountInput.onValueChanged.RemoveAllListeners();
                    coreConversionSection.amountInput.onValueChanged.AddListener(_ =>
                        OnAmountInputChanged(coreConversionSection, ref coreCraftAmount));
                }
            }

            // Wire gear slot buttons with fallback to EquipmentController order
            gearSlotNameByRef.Clear();
            var slotNames = equipment != null
                ? equipment.Slots
                : new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
            for (var i = 0; i < gearSlots.Count; i++)
            {
                var slotRef = gearSlots[i];
                if (slotRef == null) continue;
                var resolvedName = !string.IsNullOrWhiteSpace(slotRef.SlotName) ? slotRef.SlotName :
                    i < slotNames.Count ? slotNames[i] : null;
                if (string.IsNullOrWhiteSpace(resolvedName))
                {
                    Debug.LogWarning(
                        $"ForgeWindowUI: Could not resolve slot name for gear slot at index {i}. Assign SlotName or extend EquipmentController.Slots.");
                    continue;
                }

                gearSlotNameByRef[slotRef] = resolvedName;
                if (slotRef.SelectSlotButton != null)
                {
                    slotRef.SelectSlotButton.onClick.RemoveAllListeners();
                    var capturedName = resolvedName;
                    slotRef.SelectSlotButton.onClick.AddListener(() => OnGearSlotClicked(capturedName));
                }

                slotRef.SetSelected(false);
            }

            // Assume result panel is always active; clear text and disable action buttons
            if (resultText != null)
                resultText.text = string.Empty;
            if (resultTierText != null) resultTierText.text = string.Empty;
            if (replaceButton != null) replaceButton.interactable = false;

            // Ensure TMP texts that use <sprite> tags render with the StatIcons sprite asset
            var statSpriteAsset = StatIconLookup.GetSpriteAsset();
            if (statSpriteAsset != null)
            {
                if (selectedSlotStatsText != null) selectedSlotStatsText.spriteAsset = statSpriteAsset;
                if (aggregateTotalsChestText != null) aggregateTotalsChestText.spriteAsset = statSpriteAsset;
                if (aggregateWeaponBootsText != null) aggregateWeaponBootsText.spriteAsset = statSpriteAsset;
                if (aggregateHelmetText != null) aggregateHelmetText.spriteAsset = statSpriteAsset;
                if (resultText != null) resultText.spriteAsset = statSpriteAsset;
                if (resultTierText != null) resultTierText.spriteAsset = statSpriteAsset;
            }

            // Initialize previews
            ClearResultPreview();
            UpdateAllGearSlots();
            UpdateAggregateStatsText();

            // Do not force default core/slot here - restored later
            // Initialize button states based on current selections/resources
            RefreshActionButtons();

            // Initialize conversion pipelines
            InitializeConversionPipelines();

            if (coreWeightHoverObject != null)
                coreWeightHoverObject.SetActive(false);
            if (coreWeightHoverImage != null)
            {
                var trigger = coreWeightHoverImage.GetComponent<EventTrigger>() ??
                              coreWeightHoverImage.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ShowCoreWeightTooltip());
                trigger.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => HideCoreWeightTooltip());
                trigger.triggers.Add(exit);
            }
        }

        private void OnPostLoad()
        {
            // Clear any stale UI state after loading another save
            lastCrafted = null;
            if (resultText != null) resultText.text = string.Empty;
            if (resultTierText != null) resultTierText.text = string.Empty;
            ClearResultPreview();
            RefreshActionButtons();
            UpdateAllGearSlots();
            UpdateSelectedSlotStats();
            UpdateAggregateStatsText();
            // Restore saved craft amounts and refresh previews
            ingotCraftAmount = ForgeIngotCraftAmount;
            crystalCraftAmount = ForgeCrystalCraftAmount;
            chunkCraftAmount = ForgeChunkCraftAmount;
            coreCraftAmount = ForgeCoreCraftAmount;
            if (ingotConversionSection?.amountInput != null)
                ingotConversionSection.amountInput.text = ingotCraftAmount.ToString("0");
            if (crystalConversionSection?.amountInput != null)
                crystalConversionSection.amountInput.text = crystalCraftAmount.ToString("0");
            if (chunkConversionSection?.amountInput != null)
                chunkConversionSection.amountInput.text = chunkCraftAmount.ToString("0");
            if (coreConversionSection?.amountInput != null)
                coreConversionSection.amountInput.text = coreCraftAmount.ToString("0");
            // Restore last selected core/slot from save if available
            TryRestoreSavedSelections();
            UpdateAggregateStatsText();
            OnResourcesChanged();
        }

        private void LateUpdate()
        {
            // Process deferred UI updates to coalesce multiple events
            if (_gearSlotsNeedRefresh)
            {
                UpdateAllGearSlots();
                _gearSlotsNeedRefresh = false;
            }
            if (_statsNeedRefresh)
            {
                UpdateSelectedSlotStats();
                UpdateAggregateStatsText();
                _statsNeedRefresh = false;
            }
        }

        /// <summary>
        /// Combined handler for equipment changes to reduce event cascade.
        /// </summary>
        private void OnEquipmentChangedCombined()
        {
            _gearSlotsNeedRefresh = true;
            _statsNeedRefresh = true;
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged += OnEquipmentChangedCombined;
            }

            // Refresh Ivan XP display on open
            UpdateIvanXpUI();
            // Attempt to restore saved selections when window opens
            TryRestoreSavedSelections();
            UpdateAggregateStatsText();
            // Refresh selected previews when inventory changes (e.g., crafting spends ingots)
            if (RM != null) RM.OnInventoryChanged += OnResourcesChanged;
            // Subscribe to Ivan XP events if available
            var svc = CraftingService.Instance;
            if (svc != null)
            {
                svc.OnIvanXpChanged += OnIvanXpChanged;
                svc.OnIvanLevelUp += OnIvanLevelUp;
            }

            // Clear UI on load (do not clear on save to avoid autosave side-effects)
            // Also clear any forge attention indicator when the window is reopened
            if (forgeAttentionObject != null)
                forgeAttentionObject.SetActive(false);
            EventHandler.OnLoadData += OnPostLoad;
            OnResourcesChanged();
        }

        private void TryRestoreSavedSelections()
        {
            var o = Oracle.oracle;
            if (o == null || o.saveData == null || o.saveData.SavedPreferences == null)
                return;

            // Restore Core selection
            var coreName = o.saveData.SavedPreferences.LastSelectedForgeCore;
            if (!string.IsNullOrWhiteSpace(coreName))
            {
                var core = cores != null ? cores.FirstOrDefault(c => c != null && c.name == coreName) : null;
                if (core != null)
                    SelectCore(core);
            }

            // Restore Gear slot selection
            var slotName = o.saveData.SavedPreferences.LastSelectedForgeSlot;
            if (!string.IsNullOrWhiteSpace(slotName))
            {
                OnGearSlotClicked(slotName);
            }

            // Fallbacks only if nothing was restored
            if (selectedCore == null)
            {
                if (coreSlots != null && coreSlots.Count > 0)
                {
                    var firstCore = coreSlotCoreByRef.TryGetValue(coreSlots[0], out var c0) ? c0 : coreSlots[0].Core;
                    if (firstCore != null)
                        SelectCore(firstCore);
                }
            }

            if (string.IsNullOrWhiteSpace(selectedSlot))
            {
                var defaultSlotName = equipment != null && equipment.Slots.Count > 0 ? equipment.Slots[0] : "Weapon";
                OnGearSlotClicked(defaultSlotName);
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= OnEquipmentChangedCombined;
            }

            if (RM != null) RM.OnInventoryChanged -= OnResourcesChanged;
            var svc = CraftingService.Instance;
            if (svc != null)
            {
                svc.OnIvanXpChanged -= OnIvanXpChanged;
                svc.OnIvanLevelUp -= OnIvanLevelUp;
            }

            EventHandler.OnLoadData -= OnPostLoad;
            StopAutoCrafting();
            TownWindowManager.Instance?.CloseForgeInfo();
        }
        #endregion

        #region Crafting
        private double GetCraftAmountForIngots(ResourceManager rm, CoreSO core)
        {
            if (rm == null || core == null) return 0d;
            var max = double.PositiveInfinity;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
            {
                var chunkMax = Math.Floor(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot);
                max = Math.Min(max, chunkMax);
            }
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
            {
                var crystalMax = Math.Floor(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot);
                max = Math.Min(max, crystalMax);
            }
            if (max <= 0d) return 0d;
            var desired = ClampCraftAmount(ingotCraftAmount);
            return Math.Min(desired, max);
        }

        private double GetCraftAmountForCrystals(ResourceManager rm, CoreSO core)
        {
            if (rm == null || core?.crystalResource == null || core.chunkResource == null || slimeResource == null)
                return 0d;
            var chunkMax = Math.Floor(rm.GetAmount(core.chunkResource) / 2d);
            var slimeMax = Math.Floor(rm.GetAmount(slimeResource) / 1d);
            var max = Math.Min(chunkMax, slimeMax);
            if (max <= 0d) return 0d;
            var desired = ClampCraftAmount(crystalCraftAmount);
            return Math.Min(desired, max);
        }

        private double GetCraftAmountForChunks(ResourceManager rm, CoreSO core)
        {
            if (rm == null || core?.crystalResource == null || core.chunkResource == null || stoneResource == null)
                return 0d;
            var crystalMax = Math.Floor(rm.GetAmount(core.crystalResource) / 1d);
            var stoneMax = Math.Floor(rm.GetAmount(stoneResource) / 2d);
            var max = Math.Min(crystalMax, stoneMax);
            if (max <= 0d) return 0d;
            var desired = ClampCraftAmount(chunkCraftAmount);
            return Math.Min(desired, max);
        }

        private void OnAmountInputChanged(CraftSection2x1UIReferences section, ref double backingField)
        {
            if (section == null || section.amountInput == null) return;
            var text = section.amountInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                backingField = 1d;
                section.amountInput.text = "1";
                if (ReferenceEquals(section, ingotConversionSection)) UpdateIngotCraftPreview(selectedCore);
                else if (ReferenceEquals(section, crystalConversionSection)) UpdateCrystalCraftPreview(selectedCore);
                else if (ReferenceEquals(section, chunkConversionSection)) UpdateChunkCraftPreview(selectedCore);
                else if (ReferenceEquals(section, coreConversionSection)) UpdateCoreCraftPreview(selectedCore);
                return;
            }

            if (double.TryParse(text, out var value))
            {
                var clamped = ClampCraftAmount(value);
                backingField = clamped;
                var sanitized = clamped.ToString("0");
                if (!string.Equals(text, sanitized, StringComparison.Ordinal))
                    section.amountInput.text = sanitized;
                if (ReferenceEquals(section, ingotConversionSection)) UpdateIngotCraftPreview(selectedCore);
                else if (ReferenceEquals(section, crystalConversionSection)) UpdateCrystalCraftPreview(selectedCore);
                else if (ReferenceEquals(section, chunkConversionSection)) UpdateChunkCraftPreview(selectedCore);
                else if (ReferenceEquals(section, coreConversionSection)) UpdateCoreCraftPreview(selectedCore);
            }
            else
            {
                backingField = 1d;
                section.amountInput.text = "1";
                if (ReferenceEquals(section, ingotConversionSection)) UpdateIngotCraftPreview(selectedCore);
                else if (ReferenceEquals(section, crystalConversionSection)) UpdateCrystalCraftPreview(selectedCore);
                else if (ReferenceEquals(section, chunkConversionSection)) UpdateChunkCraftPreview(selectedCore);
                else if (ReferenceEquals(section, coreConversionSection)) UpdateCoreCraftPreview(selectedCore);
            }
        }

        private double GetCraftAmountForCores(ResourceManager rm, CoreSO core)
        {
            var (curRes, nextRes, isFinalTier) = ResolveCurrentAndNextCoreResources(core);
            if (rm == null || isFinalTier || curRes == null || nextRes == null) return 0d;
            var maxByCurrent = Math.Floor(rm.GetAmount(curRes) / 5d);
            var maxByNext = Math.Floor(rm.GetAmount(nextRes) / 1d);
            var max = Math.Min(maxByCurrent, maxByNext);
            if (max <= 0d) return 0d;
            var desired = ClampCraftAmount(coreCraftAmount);
            return Math.Min(desired, max);
        }

        private void OnCraftClicked()
        {
            if (isAutoCrafting)
            {
                StopAutoCrafting();
                return;
            }

            if (!CanCraft())
            {
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                    o.saveData.Forge.TotalFailedCraftAttempts++;
                RefreshActionButtons();
                return;
            }

            if (selectedCore == null || crafting == null)
            {
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                    o.saveData.Forge.TotalFailedCraftAttempts++;
                RefreshActionButtons();
                return;
            }

            // Auto-salvage previous craft if one exists
            if (lastCrafted != null)
            {
                SalvageService.Instance?.Salvage(lastCrafted, isAuto: false);
                lastCrafted = null;
            }

            // Consume one core item along with ingots
            var coreSlot = GetSlotForCore(selectedCore);
            var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
            lastCrafted = crafting.Craft(selectedCore, selectedSlot, null, coreRes);
            if (lastCrafted == null)
            {
                // Do not show error text; just ensure buttons reflect current state
                RefreshActionButtons();
                return;
            }

            // Ensure we have an EquipmentController reference
            equipment ??= EquipmentController.Instance;
            var eq = equipment != null ? equipment.GetEquipped(lastCrafted.slot) : null;
            if (eq == null && equipment != null)
            {
                equipment.Equip(lastCrafted);
                // Track immediate equip from craft
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                {
                    var forge = o.saveData.Forge;
                    forge.TotalEquippedFromCraft++;
                    if (!string.IsNullOrWhiteSpace(selectedSlot))
                    {
                        if (!forge.EquipsBySlot.ContainsKey(selectedSlot)) forge.EquipsBySlot[selectedSlot] = 0;
                        forge.EquipsBySlot[selectedSlot]++;
                    }
                }
                lastCrafted = null;
                if (resultText != null) resultText.text = string.Empty;
                UpdateAllGearSlots();
                UpdateSelectedSlotStats();
                ClearResultPreview();
                RefreshActionButtons();
                // Ensure selected core/ingot previews reflect updated resource counts after craft
                OnResourcesChanged();
                ForceRefreshAllCoreSlots();
                // Refresh the odds display after crafting
                ThrottledRefreshOdds();
                try
                {
                    Blindsided.EventHandler.SaveData();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"SaveData after crafting failed: {ex}");
                }
                return;
            }

            var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
            ShowResult(summary);
            UpdateResultPreview(lastCrafted);
            // Ensure selected core/ingot previews reflect updated resource counts after craft
            OnResourcesChanged();
            ForceRefreshAllCoreSlots();
            RefreshActionButtons();
            // Refresh the odds display after crafting
            ThrottledRefreshOdds();

            // Persist resource spends and craft result to in-memory save (defer disk write)
            try
            {
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after crafting failed: {ex}");
            }
        }

        private void ShowResult(string text)
        {
            if (resultText != null) resultText.text = text;
            else Debug.LogWarning("ForgeWindowUI: resultText is not assigned; cannot display result text.");
            // Button interactivity is controlled by RefreshActionButtons
            RefreshActionButtons();
        }

        private void OnReplaceClicked()
        {
            if (lastCrafted == null || equipment == null) return;
            equipment.Equip(lastCrafted);
            // Track replace equip from craft
            var o = Blindsided.Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.Forge != null)
                o.saveData.Forge.TotalEquippedFromCraft++;
            lastCrafted = null;
            // Clear result text and disable action buttons when no active craft
            if (resultText != null) resultText.text = string.Empty;
            ClearResultTierText();
            if (replaceButton != null) replaceButton.interactable = false;
            // Clear result preview when result is equipped
            ClearResultPreview();
            UpdateAllGearSlots();
            UpdateAggregateStatsText();
            RefreshActionButtons();
        }

        private void UpdateResultPreview(GearItem item)
        {
            var img = craftSection != null ? craftSection.resultImage : null;
            if (img == null) return;
            if (item == null)
            {
                img.enabled = false;
                img.sprite = null;
                ClearResultTierText();
                return;
            }

            // Handle migrated helmet with no rarity: show explicit migrated sprite
            if (item.rarity == null && string.Equals(item.slot, "Helmet"))
            {
                if (migratedHelmetSprite != null)
                {
                    img.sprite = migratedHelmetSprite;
                    img.enabled = true;
                    ClearResultTierText();
                    return;
                }
            }

            // Use the mapped sprite from the appropriate gear slot UI rather than a separate rarity list
            Sprite sprite = null;
            // Find the gear slot UI that corresponds to the crafted item's slot
            for (var i = 0; i < gearSlots.Count; i++)
            {
                var gs = gearSlots[i];
                if (gs == null) continue;
                var resolved = gearSlotNameByRef.TryGetValue(gs, out var name) ? name : gs.SlotName;
                if (!string.IsNullOrWhiteSpace(resolved) && string.Equals(resolved, item.slot))
                {
                    sprite = gs.GetSpriteForItem(item);
                    break;
                }
            }

            // Fallback to unknown sprite per slot order if needed
            if (sprite == null)
            {
                var order = new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
                var idx = order.IndexOf(item.slot);
                if (idx >= 0 && idx < unknownGearSprites.Count)
                    sprite = unknownGearSprites[idx];
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.enabled = true;
                    ClearResultTierText();
                    return;
                }
            }

            img.sprite = sprite;
            img.enabled = sprite != null;
            ApplyResultTierText(item);
        }

        private void ClearResultPreview()
        {
            // Show unknown gear sprite for the currently selected slot
            SetResultUnknownForSlot(selectedSlot);
            // Ensure tier text clears as well
            ClearResultTierText();
        }

        private void SetResultUnknownForSlot(string slot)
        {
            var img = craftSection != null ? craftSection.resultImage : null;
            if (img == null) return;
            if (string.IsNullOrWhiteSpace(slot))
            {
                img.enabled = false;
                img.sprite = null;
                ClearResultTierText();
                return;
            }

            Sprite sprite = null;
            // Prefer finding the gear slot index by matching resolved names
            var idx = -1;
            for (var i = 0; i < gearSlots.Count; i++)
            {
                var gs = gearSlots[i];
                if (gs == null) continue;
                var name = gearSlotNameByRef.TryGetValue(gs, out var resolved) ? resolved : gs.SlotName;
                if (!string.IsNullOrWhiteSpace(name) && string.Equals(name, slot))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                var order = new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
                idx = order.IndexOf(slot);
            }

            if (idx >= 0 && idx < unknownGearSprites.Count)
                sprite = unknownGearSprites[idx];

            img.sprite = sprite;
            img.enabled = sprite != null;
            ClearResultTierText();
        }

        private void ApplyResultTierText(GearItem item)
        {
            if (resultTierText == null)
                return;
            if (item == null || item.rarity == null)
            {
                resultTierText.text = string.Empty;
                resultTierText.enabled = false;
                return;
            }
            var tierDisplay = Mathf.Clamp(item.rarity.tierIndex + 1, 1, 8);
            resultTierText.text = $"Tier {tierDisplay}";
            resultTierText.enabled = true;
        }

        private void ClearResultTierText()
        {
            if (resultTierText == null)
                return;
            resultTierText.text = string.Empty;
            resultTierText.enabled = false;
        }

        private bool CanCraft()
        {
            // Validate core and required resources
            if (crafting == null || selectedCore == null) return false;
            var rm = RM;
            if (rm == null) return false;
            var coreSlot = GetSlotForCore(selectedCore);
            var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
            if (coreRes == null) return false;
            var ingot = coreSlot != null && coreSlot.IngotResource != null
                ? coreSlot.IngotResource
                : selectedCore.requiredIngot;
            if (ingot == null) return false;
            var haveIngots = rm.GetAmount(ingot) >= Math.Max(0, selectedCore.ingotCost);
            var haveCores = rm.GetAmount(coreRes) >= 1;
            return haveIngots && haveCores;
        }

        #region Conversion Pipeline

        private void InitializeConversionPipelines()
        {
            // Create pipelines for each conversion type
            ingotPipeline = ConversionPipelineFactory.CreateIngotPipeline(ingotConversionSection);
            crystalPipeline = ConversionPipelineFactory.CreateCrystalPipeline(crystalConversionSection, slimeResource);
            chunkPipeline = ConversionPipelineFactory.CreateChunkPipeline(chunkConversionSection, stoneResource);
            corePipeline = ConversionPipelineFactory.CreateCorePipeline(coreConversionSection, ResolveCurrentAndNextCoreResources);
        }

        private void OnPipelineConversionCompleted(ConversionType type, ref double craftAmount, double persistedAmount)
        {
            // Refresh stats UI
            var statsUi = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi != null) statsUi.MarkDirty();

            // Clamp and persist the amount
            craftAmount = ClampCraftAmount(craftAmount);
            persistedAmount = craftAmount;

            // Refresh resources display
            OnResourcesChanged();

            // Save to disk
            try
            {
                EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after {type} conversion failed: {ex}");
            }
        }

        #endregion

        private bool CanCraftCoreConversion()
        {
            if (selectedCore == null) return false;
            var rm = RM; if (rm == null) return false;
            var (curRes, nextRes, isFinalTier) = ResolveCurrentAndNextCoreResources(selectedCore);
            if (isFinalTier || curRes == null || nextRes == null) return false;
            if (rm.GetAmount(curRes) < 5) return false;
            if (rm.GetAmount(nextRes) < 1) return false;
            return true;
        }

        private bool CanCraftIngot()
        {
            if (selectedCore == null || selectedCore.requiredIngot == null)
                return false;
            var rm = RM;
            if (rm == null) return false;
            if (selectedCore.chunkResource != null &&
                rm.GetAmount(selectedCore.chunkResource) < selectedCore.chunkCostPerIngot)
                return false;
            if (selectedCore.crystalResource != null &&
                rm.GetAmount(selectedCore.crystalResource) < selectedCore.crystalCostPerIngot)
                return false;
            return true;
        }

        private bool CanCraftCrystal()
        {
            if (selectedCore == null)
                return false;
            var rm = RM;
            if (rm == null) return false;
            if (selectedCore.chunkResource == null || slimeResource == null || selectedCore.crystalResource == null)
                return false;
            if (rm.GetAmount(selectedCore.chunkResource) < 2)
                return false;
            if (rm.GetAmount(slimeResource) < 1)
                return false;
            return true;
        }

        private bool CanCraftChunk()
        {
            if (selectedCore == null)
                return false;
            var rm = RM;
            if (rm == null) return false;
            if (selectedCore.crystalResource == null || stoneResource == null || selectedCore.chunkResource == null)
                return false;
            if (rm.GetAmount(selectedCore.crystalResource) < 1)
                return false;
            if (rm.GetAmount(stoneResource) < 2)
                return false;
            return true;
        }

        private void OnCraftIngotClicked()
        {
            if (ingotPipeline == null || !CanCraftIngot()) return;
            var rm = RM;
            var core = selectedCore;
            if (rm == null || core == null) return;

            // Set desired amount and perform conversion
            ingotPipeline.DesiredAmount = ingotCraftAmount;
            if (!ingotPipeline.PerformConversion(rm, core)) return;

            // Persist desired amount
            ingotCraftAmount = ClampCraftAmount(ingotCraftAmount);
            ForgeIngotCraftAmount = ingotCraftAmount;

            // Refresh UI
            OnResourcesChanged();
            var statsUi = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi != null) statsUi.MarkDirty();

            // Save to disk
            try { EventHandler.SaveData(); }
            catch (System.Exception ex) { Debug.LogError($"SaveData after ingot craft failed: {ex}"); }
        }

        private void OnCraftCrystalClicked()
        {
            if (crystalPipeline == null || !CanCraftCrystal()) return;
            var rm = RM;
            var core = selectedCore;
            if (rm == null || core == null) return;

            // Set desired amount and perform conversion
            crystalPipeline.DesiredAmount = crystalCraftAmount;
            if (!crystalPipeline.PerformConversion(rm, core)) return;

            // Persist desired amount
            crystalCraftAmount = ClampCraftAmount(crystalCraftAmount);
            ForgeCrystalCraftAmount = crystalCraftAmount;

            // Refresh UI
            OnResourcesChanged();
            var statsUi = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi != null) statsUi.MarkDirty();

            // Save to disk
            try { EventHandler.SaveData(); }
            catch (System.Exception ex) { Debug.LogError($"SaveData after crystal craft failed: {ex}"); }
        }

        private void OnCraftChunkClicked()
        {
            if (chunkPipeline == null || !CanCraftChunk()) return;
            var rm = RM;
            var core = selectedCore;
            if (rm == null || core == null) return;

            // Set desired amount and perform conversion
            chunkPipeline.DesiredAmount = chunkCraftAmount;
            if (!chunkPipeline.PerformConversion(rm, core)) return;

            // Persist desired amount
            chunkCraftAmount = ClampCraftAmount(chunkCraftAmount);
            ForgeChunkCraftAmount = chunkCraftAmount;

            // Refresh UI
            OnResourcesChanged();
            var statsUi = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi != null) statsUi.MarkDirty();

            // Save to disk
            try { EventHandler.SaveData(); }
            catch (System.Exception ex) { Debug.LogError($"SaveData after chunk craft failed: {ex}"); }
        }

        private void OnCraftCoreConversionClicked()
        {
            if (corePipeline == null || !CanCraftCoreConversion()) return;
            var rm = RM;
            var core = selectedCore;
            if (rm == null || core == null) return;

            // Set desired amount and perform conversion
            corePipeline.DesiredAmount = coreCraftAmount;
            if (!corePipeline.PerformConversion(rm, core)) return;

            // Persist desired amount
            coreCraftAmount = ClampCraftAmount(coreCraftAmount);
            ForgeCoreCraftAmount = coreCraftAmount;

            // Refresh UI
            OnResourcesChanged();
            var statsUi = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi != null) statsUi.MarkDirty();

            // Save to disk
            try { EventHandler.SaveData(); }
            catch (System.Exception ex) { Debug.LogError($"SaveData after core conversion failed: {ex}"); }
        }

        private (Resource currentTierCore, Resource nextTierCore, bool isFinalTier) ResolveCurrentAndNextCoreResources(CoreSO core)
        {
            // Try to resolve by selected core slot index in coreSlots list
            if (core == null) return (null, null, true);
            // Determine index of selected core among ordered cores/coreSlots
            int idx = -1;
            for (var i = 0; i < coreSlots.Count; i++)
            {
                var mapped = coreSlotCoreByRef.TryGetValue(coreSlots[i], out var mc) ? mc : coreSlots[i].Core;
                if (mapped == core) { idx = i; break; }
            }
            if (idx < 0) idx = core.tierIndex; // fallback to tierIndex

            // Prefer resources from the CoreSlotUIReferences if present; else fallback to serialized list
            Resource cur = null, next = null;
            if (idx >= 0 && idx < coreSlots.Count)
            {
                var slot = coreSlots[idx];
                cur = slot != null ? slot.CoreResource : null;
                var nextSlot = (idx + 1) < coreSlots.Count ? coreSlots[idx + 1] : null;
                next = nextSlot != null ? nextSlot.CoreResource : null;
            }
            if (cur == null && coreResourcesInTierOrder != null && idx >= 0 && idx < coreResourcesInTierOrder.Count)
                cur = coreResourcesInTierOrder[idx];
            if (next == null && coreResourcesInTierOrder != null && (idx + 1) >= 0 && (idx + 1) < coreResourcesInTierOrder.Count)
                next = coreResourcesInTierOrder[idx + 1];

            bool finalTier = next == null; // if we cannot resolve next, consider this final tier
            return (cur, next, finalTier);
        }
        #endregion

        #region Auto Crafting
        private void OnCraftUntilUpgradeClicked()
        {
            if (isAutoCrafting)
            {
                StopAutoCrafting();
                return;
            }

            if (!CanCraft())
            {
                RefreshActionButtons();
                return;
            }

            isAutoCrafting = true;
            // Track autocraft session
            var o = Blindsided.Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.Forge != null)
            {
                o.saveData.Forge.TotalAutocraftSessions++;
                o.saveData.Forge.TotalCraftUntilUpgradeSessions++;
            }
            autoCraftCoroutine = StartCoroutine(CraftUntilUpgradeCoroutine());
            RefreshActionButtons();
        }

        private void StopAutoCrafting()
        {
            if (!isAutoCrafting) return;
            isAutoCrafting = false;
            // Track stop reason: Cancelled
            var o = Blindsided.Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.Forge != null)
            {
                var forge = o.saveData.Forge;
                if (!forge.AutocraftStopReasons.ContainsKey("Cancelled")) forge.AutocraftStopReasons["Cancelled"] = 0;
                forge.AutocraftStopReasons["Cancelled"]++;
            }
            if (autoCraftCoroutine != null)
            {
                StopCoroutine(autoCraftCoroutine);
                autoCraftCoroutine = null;
            }

            // Notify: forge autocrafting stopped
            FindFirstObjectByType<TaskbarFlasher>()?.FlashNow();

            RefreshActionButtons();
        }

        /// <summary>
        /// Gets the current crafts per second based on config and milestone bonuses.
        /// </summary>
        private int GetCurrentCraftsPerSecond()
        {
            var config = crafting?.Config;
            if (config == null) return 10;

            int baseSpeed = Mathf.Max(1, config.baseCraftsPerSecond);
            int maxSpeed = config.maxCraftsPerSecond;

            // Get milestone bonus from skill controller
            float milestoneBonus = 0f;
            var controller = SkillController.Instance;
            if (controller != null)
                milestoneBonus = controller.Aggregator.GetForgeCraftSpeedBonus();

            int total = baseSpeed + Mathf.RoundToInt(milestoneBonus);

            // Apply cap if configured
            if (maxSpeed > 0)
                total = Mathf.Min(total, maxSpeed);

            return Mathf.Max(1, total);
        }

        private IEnumerator CraftUntilUpgradeCoroutine()
        {
            // Calculate dynamic speed from config + milestone bonuses
            var config = crafting?.Config;
            float batchInterval = config != null ? config.batchInterval : 0.1f;
            int craftsPerSec = GetCurrentCraftsPerSecond();

            // For low speeds (≤ batchInterval rate), use longer wait times
            // For high speeds (> batchInterval rate), batch multiple crafts per frame
            int craftsPerBatch = Mathf.Max(1, Mathf.RoundToInt(craftsPerSec * batchInterval));
            float waitTime = craftsPerSec > 0 ? Mathf.Max(batchInterval, 1.0f / craftsPerSec) : batchInterval;
            var wait = new WaitForSecondsRealtime(waitTime);

            // Turbo mode: batch salvage yields when processing multiple crafts (>1) for maximum speed
            // Uses expected value calculation instead of rolling each item individually
            bool turboMode = craftsPerBatch > 1;
            int turboSalvageCount = 0; // Accumulated items to batch salvage

            // Hot-reload config once per second
            float lastConfigCheck = Time.unscaledTime;
            const float configCheckInterval = 1.0f;

            // Throttle timers for UI updates during autocraft
            float lastVisualUpdate = Time.unscaledTime;
            float lastStatsUpdate = Time.unscaledTime;
            float lastResourceUpdate = Time.unscaledTime - 0.5f; // Stagger by 0.5s from stats
            const float visualUpdateInterval = 0.1f;   // 10 Hz
            const float statsUpdateInterval = 1.0f;    // 1 Hz
            const float resourceUpdateInterval = 1.0f; // 1 Hz

            // Capture baseline affix stat set at the start of the session if Lock Stats is enabled
            HashSet<StatDefSO> baselineSet = null;
            if (StaticReferences.LockAutocraftStatSet)
            {
                var baseline = equipment?.GetEquipped(selectedSlot);
                if (baseline != null)
                    baselineSet = BuildAffixStatSet(baseline);
            }

            var pendingAutoSalvage = lastCrafted != null;
            bool shouldBreak = false;

            while (isAutoCrafting && !shouldBreak)
            {
                // Hot-reload config check (once per second)
                if (Time.unscaledTime - lastConfigCheck >= configCheckInterval)
                {
                    lastConfigCheck = Time.unscaledTime;
                    config = crafting?.Config;
                    batchInterval = config != null ? config.batchInterval : 0.1f;
                    int newCraftsPerSec = GetCurrentCraftsPerSecond();
                    if (newCraftsPerSec != craftsPerSec)
                    {
                        craftsPerSec = newCraftsPerSec;
                        craftsPerBatch = Mathf.Max(1, Mathf.RoundToInt(craftsPerSec * batchInterval));
                        waitTime = craftsPerSec > 0 ? Mathf.Max(batchInterval, 1.0f / craftsPerSec) : batchInterval;
                        wait = new WaitForSecondsRealtime(waitTime);
                        turboMode = craftsPerBatch > 1;
                    }
                }

                // Batch multiple crafts per frame for high speeds
                // Wrap in ResourceManager batch to defer OnInventoryChanged until batch ends
                var rm = ResourceManager.Instance;
                rm?.BeginBatch();
                try
                {
                for (int batch = 0; batch < craftsPerBatch && isAutoCrafting; batch++)
                {
                    bool isLastInBatch = (batch == craftsPerBatch - 1);

                    // Discard previous craft - turbo mode accumulates for batch salvage
                    if (pendingAutoSalvage && lastCrafted != null)
                    {
                        if (turboMode)
                        {
                            // Fast path: release to pool and count for batch salvage
                            GearObjectPool.ReleaseItem(lastCrafted);
                            turboSalvageCount++;
                        }
                        else
                        {
                            // Normal path: salvage for resources
                            SalvageService.Instance?.Salvage(lastCrafted, isAuto: true);
                        }
                        lastCrafted = null;
                        pendingAutoSalvage = false;
                    }

                    if (!CanCraft())
                    {
                        // Out of resources stop reason
                        var o = Blindsided.Oracle.oracle;
                        if (o != null && o.saveData != null && o.saveData.Forge != null)
                        {
                            var forge = o.saveData.Forge;
                            if (!forge.AutocraftStopReasons.ContainsKey("OutOfResources")) forge.AutocraftStopReasons["OutOfResources"] = 0;
                            forge.AutocraftStopReasons["OutOfResources"]++;
                        }
                        shouldBreak = true;
                        break;
                    }

                    if (selectedCore == null || crafting == null)
                    {
                        shouldBreak = true;
                        break;
                    }

                    var coreSlot = GetSlotForCore(selectedCore);
                    var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
                    var craftedItem = crafting.Craft(selectedCore, selectedSlot, null, coreRes);
                    if (craftedItem == null)
                    {
                        RefreshActionButtons();
                        shouldBreak = true;
                        break;
                    }

                    lastCrafted = craftedItem;

                    // Count autocraft craft
                    {
                        var o2 = Blindsided.Oracle.oracle;
                        if (o2 != null && o2.saveData != null && o2.saveData.Forge != null)
                            o2.saveData.Forge.AutocraftCrafts++;
                    }

                    var eq = equipment?.GetEquipped(lastCrafted.slot);

                    // Check for stop conditions (always check, may stop early)
                    bool isUpgrade = UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted, eq);
                    bool isVastium = StaticReferences.StopAutocraftOnVastium &&
                                     lastCrafted?.rarity?.GetName() == "Vastium";
                    bool isStopping = isUpgrade || isVastium;

                    // Throttled UI updates during autocraft
                    float now = Time.unscaledTime;

                    // Visual preview: 10 Hz or immediate on stop
                    bool doVisualUpdate = isStopping || (isLastInBatch && (now - lastVisualUpdate) >= visualUpdateInterval);
                    if (doVisualUpdate)
                    {
                        lastVisualUpdate = now;
                        var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
                        ShowResult(summary);
                        UpdateResultPreview(lastCrafted);
                    }

                    // Stats rebuild: 1 Hz or immediate on stop
                    bool doStatsUpdate = isStopping || (isLastInBatch && (now - lastStatsUpdate) >= statsUpdateInterval);
                    if (doStatsUpdate)
                    {
                        lastStatsUpdate = now;
                        ForceRefreshAllCoreSlots();
                        ThrottledRefreshOdds();
                    }

                    // Resource display: 1 Hz staggered or immediate on stop
                    bool doResourceUpdate = isStopping || (isLastInBatch && (now - lastResourceUpdate) >= resourceUpdateInterval);
                    if (doResourceUpdate)
                    {
                        lastResourceUpdate = now;
                        OnResourcesChanged();
                    }

                    if (isUpgrade)
                    {
                        bool passesLock = true;
                        if (StaticReferences.LockAutocraftStatSet)
                        {
                            if (baselineSet != null)
                            {
                                var rolledSet = BuildAffixStatSet(lastCrafted);
                                passesLock = AffixSetsEqual(baselineSet, rolledSet);
                            }
                            else
                            {
                                // No baseline equipped; allow any upgrade to stop
                                passesLock = true;
                            }
                        }

                        if (passesLock)
                        {
                            // Stop reason: Upgraded
                            var o = Blindsided.Oracle.oracle;
                            if (o != null && o.saveData != null && o.saveData.Forge != null)
                            {
                                var forge = o.saveData.Forge;
                                if (!forge.AutocraftStopReasons.ContainsKey("Upgraded")) forge.AutocraftStopReasons["Upgraded"] = 0;
                                forge.AutocraftStopReasons["Upgraded"]++;
                                // Track best rarity reached by slot
                                var slot = lastCrafted != null ? lastCrafted.slot : null;
                                if (!string.IsNullOrWhiteSpace(slot) && lastCrafted != null && lastCrafted.rarity != null)
                                {
                                    var tier = lastCrafted.rarity.tierIndex;
                                    if (!forge.AutocraftBestRarityTierBySlot.ContainsKey(slot) || forge.AutocraftBestRarityTierBySlot[slot] < tier)
                                        forge.AutocraftBestRarityTierBySlot[slot] = tier;
                                }
                            }
                            shouldBreak = true;
                            break; // leave lastCrafted for player to review/replace/salvage
                        }
                    }

                    if (isVastium)
                    {
                        var o3 = Blindsided.Oracle.oracle;
                        if (o3 != null && o3.saveData != null && o3.saveData.Forge != null)
                        {
                            var forge = o3.saveData.Forge;
                            if (!forge.AutocraftStopReasons.ContainsKey("Vastium")) forge.AutocraftStopReasons["Vastium"] = 0;
                            forge.AutocraftStopReasons["Vastium"]++;
                        }
                        shouldBreak = true;
                        break;
                    }

                    pendingAutoSalvage = true;
                }
                }
                finally
                {
                    rm?.EndBatch();
                }

                // Batch salvage turbo mode items at end of each batch
                if (turboSalvageCount > 0 && selectedCore != null)
                {
                    SalvageService.Instance?.BatchSalvage(selectedCore, turboSalvageCount);
                    turboSalvageCount = 0;
                }

                if (!shouldBreak)
                {
                    RefreshActionButtons();
                    yield return wait;
                }
            }

            // Final batch salvage for any remaining turbo items
            if (turboSalvageCount > 0 && selectedCore != null)
            {
                SalvageService.Instance?.BatchSalvage(selectedCore, turboSalvageCount);
                turboSalvageCount = 0;
            }

            isAutoCrafting = false;
            autoCraftCoroutine = null;
            RefreshActionButtons();

            // Notify: forge autocrafting stopped (due to upgrade/out-of-resources/vastium)
            FindFirstObjectByType<TaskbarFlasher>()?.FlashNow();
            // If the forge window is not open, enable the attention indicator(s)
            if (!TimelessEchoes.UI.TownWindowManager.IsForgeOpen)
            {
                if (forgeAttentionObject != null) forgeAttentionObject.SetActive(true);
                TimelessEchoes.UI.TownWindowManager.ShowForgeAttention();
            }
        }

        private static HashSet<StatDefSO> BuildAffixStatSet(GearItem item)
        {
            var set = new HashSet<StatDefSO>();
            if (item?.affixes != null)
            {
                for (int i = 0; i < item.affixes.Count; i++)
                {
                    var a = item.affixes[i];
                    if (a?.stat != null)
                        set.Add(a.stat);
                }
            }
            return set;
        }

        private static bool AffixSetsEqual(HashSet<StatDefSO> a, HashSet<StatDefSO> b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            return a.SetEquals(b);
        }
        #endregion

        #region Core Slots
        private void SelectCore(CoreSO core)
        {
            var changed = core != selectedCore;
            // Stop auto-crafting if the player changes the selected core
            if (isAutoCrafting && changed)
                StopAutoCrafting();
            selectedCore = core;
            // Persist last selected core per save
            var o = Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.SavedPreferences != null)
                o.saveData.SavedPreferences.LastSelectedForgeCore = selectedCore != null ? selectedCore.name : null;
            // update visual selections using mapped cores
            foreach (var slot in coreSlots)
            {
                if (slot == null) continue;
                var mappedCore = coreSlotCoreByRef.TryGetValue(slot, out var mc) ? mc : slot.Core;
                slot.SetSelected(mappedCore == selectedCore);
            }

            // update selected core/ingot previews
            var previewSlot = GetSlotForCore(selectedCore);
            UpdateSelectedCorePreview(previewSlot);
            UpdateIngotPreview(selectedCore);
            UpdateIngotCraftPreview(selectedCore);
            UpdateCrystalCraftPreview(selectedCore);
            UpdateChunkCraftPreview(selectedCore);
            UpdateCoreCraftPreview(selectedCore);
            UpdateMaxCraftsText();
            ThrottledRefreshOdds();
            // Clear any pending result text when the player changes core
            if (changed)
            {
                if (resultText != null) resultText.text = string.Empty;
                ClearResultTierText();
            }
            RefreshActionButtons();
        }

        private void OnCoreSlotClicked(CoreSlotUIReferences slot, CoreSO core)
        {
            Debug.Log(core != null
                ? $"ForgeWindowUI: Core clicked -> {core.name}"
                : "ForgeWindowUI: Core clicked -> (null)");
            SelectCore(core);
        }

        private void RefreshOdds()
        {
            var core = selectedCore ?? (cores != null && cores.Count > 0 ? cores[0] : null);
            if (core == null)
            {
                RefreshOddsPieChart(null);
                return;
            }

            var info = RarityOddsCalculator.BuildRarityWeightInfo(core);
            RefreshOddsPieChart(info.weights);
        }

        private void ThrottledRefreshOdds()
        {
            // refresh at most 5 times per second
            if (Time.unscaledTime < nextOddsRefreshTime)
                return;
            nextOddsRefreshTime = Time.unscaledTime + 0.2f;
            RefreshOdds();
        }

        private void RefreshOddsPieChart(List<(RaritySO r, float w)> weights)
        {
            if (oddsPieSlices == null || oddsPieSlices.Count == 0)
                return;

            if (weights == null || weights.Count == 0)
            {
                for (var i = 0; i < oddsPieSlices.Count; i++)
                    if (oddsPieSlices[i] != null)
                        oddsPieSlices[i].enabled = false;
                return;
            }

            var total = 0f;
            for (var i = 0; i < weights.Count; i++) total += Mathf.Max(0f, weights[i].w);
            if (total <= 0f)
            {
                for (var i = 0; i < oddsPieSlices.Count; i++)
                    if (oddsPieSlices[i] != null)
                        oddsPieSlices[i].enabled = false;
                return;
            }

            // Layered approach with background at index 0, reversed order
            var overlayCapacity = Mathf.Max(0, oddsPieSlices.Count - 1);
            var sliceCount = Mathf.Min(overlayCapacity, weights.Count);

            // Keep background (index 0) where it is; ensure overlays are in indices [1..sliceCount]
            for (var i = 0; i < sliceCount; i++)
            {
                var img = oddsPieSlices[i + 1];
                if (img != null)
                    img.transform.SetSiblingIndex(i + 1);
            }

            // Precompute normalized fractions of weights we will use
            var fractions = new float[sliceCount];
            for (var i = 0; i < sliceCount; i++)
                fractions[i] = Mathf.Max(0f, weights[i].w) / total;

            var used = 0f;
            for (var layer = 0; layer < sliceCount; layer++)
            {
                // Reverse the mapping so visual order is reversed
                var weightIndex = sliceCount - 1 - layer;
                var img = oddsPieSlices[layer + 1];
                if (img == null) { used += fractions[weightIndex]; continue; }

                var fill = layer == 0 ? 1f : Mathf.Clamp01(1f - used);
                used += fractions[weightIndex];

                img.enabled = fill > 0f;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = 2;
                img.fillClockwise = true;
                img.fillAmount = fill;
                img.color = weights[weightIndex].r != null ? weights[weightIndex].r.color : Color.white;
                var rt = img.rectTransform;
                if (rt != null)
                    rt.localEulerAngles = Vector3.zero;
            }

            // Disable any extra overlay slices beyond available weights (leave background alone)
            for (var i = sliceCount + 1; i < oddsPieSlices.Count; i++)
                if (oddsPieSlices[i] != null)
                    oddsPieSlices[i].enabled = false;
        }

        private void UpdateCoreWeightTooltipText()
        {
            if (coreWeightHoverText == null)
                return;

            var core = selectedCore ?? (cores != null && cores.Count > 0 ? cores[0] : null);
            if (core == null)
            {
                coreWeightHoverText.text = string.Empty;
                return;
            }

            var info = RarityOddsCalculator.BuildRarityWeightInfo(core);

            // Ensure TMP can render color tags
            coreWeightHoverText.richText = true;

            // Build colored lines: color the rarity name and trailing ':' using the rarity color
            float total = 0f;
            for (int i = 0; i < info.weights.Count; i++)
                total += Mathf.Max(0f, info.weights[i].w);

            System.Text.StringBuilder sb = new System.Text.StringBuilder(info.weights.Count * 16);
            for (int i = 0; i < info.weights.Count; i++)
            {
                var (r, wRaw) = info.weights[i];
                float w = Mathf.Max(0f, wRaw);
                float p = total > 0f ? w / total : 0f;
                string name = r != null ? r.GetName() : "(null)";

                if (r != null)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(r.color);
                    sb.Append("<color=#").Append(hex).Append(">").Append(name).Append(":</color> ");
                }
                else
                {
                    sb.Append(name).Append(": ");
                }

                sb.Append((p * 100f).ToString("0.000")).Append('%');

                if (i < info.weights.Count - 1)
                    sb.Append('\n');
            }

            coreWeightHoverText.text = sb.ToString();
        }

        private void ShowCoreWeightTooltip()
        {
            UpdateCoreWeightTooltipText();
            if (coreWeightHoverObject != null)
                coreWeightHoverObject.SetActive(true);
        }

        private void HideCoreWeightTooltip()
        {
            if (coreWeightHoverObject != null)
                coreWeightHoverObject.SetActive(false);
        }

        private void ForceRefreshAllCoreSlots()
        {
            // Force refresh all core slots to ensure UI consistency
            foreach (var slot in coreSlots)
                if (slot != null)
                    slot.Refresh();
        }

        private CoreSlotUIReferences GetSlotForCore(CoreSO core)
        {
            if (core == null) return null;
            foreach (var s in coreSlots)
            {
                if (s == null) continue;
                var mapped = coreSlotCoreByRef.TryGetValue(s, out var mc) ? mc : s.Core;
                if (mapped == core) return s;
            }

            return null;
        }
        #endregion

        #region Gear Slots
        private void OnGearSlotClicked(string slot)
        {
            Debug.Log($"ForgeWindowUI: Gear slot clicked -> {slot}");
            SelectSlot(slot);
            // update visual selections for gear slot highlights
            foreach (var gs in gearSlots)
            {
                if (gs == null) continue;
                var resolved = gearSlotNameByRef.TryGetValue(gs, out var name) ? name : gs.SlotName;
                gs.SetSelected(string.Equals(resolved, slot));
            }

            // When choosing a slot (but not crafting), show the unknown gear sprite for that slot in result
            SetResultUnknownForSlot(slot);
            ClearResultTierText();
            // Update equipped stats display for the selected slot
            UpdateSelectedSlotStats();
            RefreshActionButtons();
        }

        // Called by UI slot buttons (e.g., Weapon/Helmet/Chest/Boots)
        public void SelectSlot(string slot)
        {
            var changed = !string.Equals(selectedSlot, slot);
            if (lastCrafted != null && !string.Equals(lastCrafted.slot, slot))
            {
                SalvageService.Instance?.Salvage(lastCrafted, isAuto: true);
                lastCrafted = null;
                if (resultText != null) resultText.text = string.Empty;
                ClearResultPreview();
            }

            // Stop auto-crafting if the player changes the selected gear slot
            if (isAutoCrafting && !string.Equals(selectedSlot, slot))
                StopAutoCrafting();
            selectedSlot = slot;
            // Clear any pending result text when the player changes slot
            if (changed)
            {
                if (resultText != null) resultText.text = string.Empty;
                ClearResultTierText();
            }
            // Persist last selected gear slot per save
            var o = Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.SavedPreferences != null)
                o.saveData.SavedPreferences.LastSelectedForgeSlot = selectedSlot;
        }

        private void UpdateAllGearSlots()
        {
            var slotNames = equipment != null
                ? equipment.Slots
                : new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
            for (var i = 0; i < gearSlots.Count; i++)
            {
                var slotRef = gearSlots[i];
                if (slotRef == null) continue;
                var name = gearSlotNameByRef.TryGetValue(slotRef, out var n)
                    ? n
                    : !string.IsNullOrWhiteSpace(slotRef.SlotName)
                        ? slotRef.SlotName
                        : i < slotNames.Count
                            ? slotNames[i]
                            : null;
                if (string.IsNullOrWhiteSpace(name))
                {
                    slotRef.ClearGearSprite();
                    continue;
                }

                var item = equipment != null ? equipment.GetEquipped(name) : null;
                if (item != null)
                {
                    // If migrated helmet has no rarity, show migrated sprite
                    if (string.Equals(name, "Helmet") && (item.rarity == null) && slotRef.GearImage != null)
                    {
                        if (migratedHelmetSprite != null)
                        {
                            slotRef.GearImage.sprite = migratedHelmetSprite;
                            slotRef.GearImage.enabled = true;
                            slotRef.ClearGearTierText();
                        }
                        else
                        {
                            slotRef.ApplyGearSprite(item);
                        }
                    }
                    else
                    {
                        slotRef.ApplyGearSprite(item);
                    }
                }
                else
                {
                    // If unknown state, show the unknown sprite for this slot and set native size
                    var order = new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
                    var idx = order.IndexOf(name);
                    if (idx >= 0 && idx < unknownGearSprites.Count && slotRef.GearImage != null)
                    {
                        slotRef.GearImage.sprite = unknownGearSprites[idx];
                        slotRef.GearImage.enabled = true;
                        slotRef.ClearGearTierText();
                    }
                    else
                    {
                        slotRef.ClearGearSprite();
                    }
                }
            }
        }

        private void UpdateSelectedSlotStats()
        {
            if (selectedSlotStatsText == null)
                return;

            if (string.IsNullOrWhiteSpace(selectedSlot))
            {
                selectedSlotStatsText.text = string.Empty;
                return;
            }

            var equipped = equipment != null ? equipment.GetEquipped(selectedSlot) : null;
            selectedSlotStatsText.text = GearStatTextBuilder.BuildEquippedStatsText(equipped, selectedSlot);
        }

        private void UpdateAggregateStatsText()
        {
            if (aggregateTotalsChestText == null && aggregateWeaponBootsText == null && aggregateHelmetText == null)
                return;

            equipment ??= EquipmentController.Instance;
            var minusIcon = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);
            var minusFallback = string.IsNullOrEmpty(minusIcon) ? string.Empty : minusIcon;

            void ApplyText(TMP_Text target, string value)
            {
                if (target == null) return;
                target.text = string.IsNullOrWhiteSpace(value) ? minusFallback : value;
            }

            void ApplyFallback()
            {
                ApplyText(aggregateTotalsChestText, string.Empty);
                ApplyText(aggregateWeaponBootsText, string.Empty);
                ApplyText(aggregateHelmetText, string.Empty);
            }

            if (equipment == null)
            {
                ApplyFallback();
                return;
            }

            var slots = equipment.Slots;
            if (slots == null || slots.Count == 0)
            {
                ApplyFallback();
                return;
            }

            var entries = new List<(string slot, GearItem item)>(slots.Count);
            for (var i = 0; i < slots.Count; i++)
            {
                var slotName = slots[i];
                if (string.IsNullOrWhiteSpace(slotName))
                    continue;

                var item = equipment.GetEquipped(slotName);
                entries.Add((slotName, item));
            }

            if (entries.Count == 0)
            {
                ApplyFallback();
                return;
            }

            var sections = GearStatTextBuilder.BuildAggregateStatsTextSections(entries);
            ApplyText(aggregateTotalsChestText, sections.Totals);
            ApplyText(aggregateWeaponBootsText, sections.WeaponAndChest);
            ApplyText(aggregateHelmetText, sections.HelmetAndBoots);
        }

        private string BuildEquippedStatsText(GearItem item, string slotName)
        {
            if (item == null)
                return StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);

            var lines = new List<string>();

            // Display equipped stats (no +/- prefix) in standardized order
            var sortedAffixes = new List<GearAffix>(item.affixes);
            sortedAffixes.Sort((x, y) => StatSortOrder.Compare(x?.stat != null ? x.stat.heroMapping : default, y?.stat != null ? y.stat.heroMapping : default));
            foreach (var a in sortedAffixes)
            {
                if (a == null || a.stat == null) continue;
                var iconTag = StatIconLookup.GetIconTag(a.stat.heroMapping);
                var valueText = $"{CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
                var nameText = a.stat.GetName();
                if (!string.IsNullOrEmpty(iconTag))
                    lines.Add($"{iconTag} {valueText}");
                else
                    lines.Add($"{nameText} {valueText}");
            }

            return string.Join("\n", lines);
        }
        #endregion

        #region UI Updates
        // Determine if there is any visible pending text (result summary)
        private bool HasPendingText()
        {
            return resultText != null && !string.IsNullOrWhiteSpace(resultText.text);
        }

        // Helpers for conversion sections
        private double GetSelectedAmountForSection(CraftSection2x1UIReferences section, ResourceManager rm, CoreSO core)
        {
            if (ReferenceEquals(section, ingotConversionSection)) return GetCraftAmountForIngots(rm, core);
            if (ReferenceEquals(section, crystalConversionSection)) return GetCraftAmountForCrystals(rm, core);
            if (ReferenceEquals(section, chunkConversionSection)) return GetCraftAmountForChunks(rm, core);
            if (ReferenceEquals(section, coreConversionSection)) return GetCraftAmountForCores(rm, core);
            return 0d;
        }

        private double GetDesiredAmountForSection(CraftSection2x1UIReferences section)
        {
            if (ReferenceEquals(section, ingotConversionSection)) return ClampCraftAmount(ingotCraftAmount);
            if (ReferenceEquals(section, crystalConversionSection)) return ClampCraftAmount(crystalCraftAmount);
            if (ReferenceEquals(section, chunkConversionSection)) return ClampCraftAmount(chunkCraftAmount);
            if (ReferenceEquals(section, coreConversionSection)) return ClampCraftAmount(coreCraftAmount);
            return 1d;
        }

        private static double ClampCraftAmount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 1d;
            var clamped = Math.Floor(value);
            return clamped < 1d ? 1d : clamped;
        }

        private static string FormatScaledCost(double perUnitCost, double selectedAmount)
        {
            var total = Math.Max(0d, perUnitCost) * Math.Max(0d, Math.Floor(selectedAmount));
            return Blindsided.Utilities.CalcUtils.FormatNumber(total, hideDecimal: true);
        }
        private void OnIvanXpChanged(int level, float current, float needed)
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
            SetIvanLevelLabel(level);
            if (ivanXpText != null)
                ivanXpText.text = $"{current:0}/{needed:0}";
            if (ivanXpBar != null)
                ivanXpBar.fillAmount = needed > 0f ? Mathf.Clamp01(current / needed) : 0f;
        }

        private void OnIvanLevelUp(int newLevel)
        {
            // Could play an effect or flash; for now just update text immediately
            OnIvanXpChanged(newLevel,
                Oracle.oracle != null ? Oracle.oracle.saveData.CraftingMasteryXP : 0f,
                CraftingService.Instance != null
                    ? CraftingService.Instance.Config.xpForFirstLevel * Mathf.Pow(Mathf.Max(1, newLevel),
                        CraftingService.Instance.Config.xpLevelMultiplier)
                    : 1f);
            // Odds depend on level scaling; refresh them when level changes
            ThrottledRefreshOdds();
        }

        private void OnResourcesChanged()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
            var previewSlot = GetSlotForCore(selectedCore);
            UpdateSelectedCorePreview(previewSlot);
            UpdateIngotPreview(selectedCore);
            UpdateIngotCraftPreview(selectedCore);
            UpdateCrystalCraftPreview(selectedCore);
            UpdateChunkCraftPreview(selectedCore);
            UpdateCoreCraftPreview(selectedCore);
            UpdateMaxCraftsText();
            UpdateIvanXpUI();
            RefreshActionButtons();
            ThrottledRefreshOdds();
        }

        private void UpdateIvanXpUI()
        {
            var o = Oracle.oracle;
            if (o == null || o.saveData == null) return;
            SetIvanLevelLabel(o.saveData.CraftingMasteryLevel);
            if (ivanXpText != null)
            {
                var svc = CraftingService.Instance;
                var conf = svc != null ? svc.Config : null;
                float currentLevel = Mathf.Max(1, o.saveData.CraftingMasteryLevel);
                var need = conf != null ? conf.xpForFirstLevel * Mathf.Pow(currentLevel, conf.xpLevelMultiplier) : 1f;
                ivanXpText.text = $"{o.saveData.CraftingMasteryXP:N0}/{need:N0}";
            }

            if (ivanXpBar != null)
            {
                var svc = CraftingService.Instance;
                var conf = svc != null ? svc.Config : null;
                float currentLevel = Mathf.Max(1, o.saveData.CraftingMasteryLevel);
                var need = conf != null ? conf.xpForFirstLevel * Mathf.Pow(currentLevel, conf.xpLevelMultiplier) : 1f;
                var ratio = need > 0f ? Mathf.Clamp01(o.saveData.CraftingMasteryXP / need) : 0f;
                ivanXpBar.fillAmount = ratio;
            }
        }

        private void SetIvanLevelLabel(int level)
        {
            if (ivanLevelText != null)
                ivanLevelText.text = $"Ivan | Level {Mathf.Max(0, level)}";
        }

        private void UpdateSelectedCorePreview(CoreSlotUIReferences slot)
        {
            // Update selected core image and count based on the clicked slot
            var rm = RM;
            var section = craftSection;
            if (section != null && section.cost1Image != null)
            {
                var res = slot != null ? slot.CoreResource : null;
                Sprite sprite = null;
                if (res != null)
                {
                    const int coreCost = 1;
                    var discovered = rm != null && rm.IsUnlocked(res);
                    var have = rm != null && rm.GetAmount(res) >= coreCost;
                    var baseSprite = slot != null && slot.CoreImage != null && slot.CoreImage.sprite != null
                        ? slot.CoreImage.sprite
                        : res.icon;
                    sprite = discovered && have ? baseSprite : res.UnknownIcon;
                }

                section.cost1Image.sprite = sprite;
                section.cost1Image.enabled = sprite != null;
            }

            if (section != null && section.cost1Text != null)
            {
                const int coreCost = 1;
                section.cost1Text.text = selectedCore != null ? coreCost.ToString("0") : "0";
            }
        }

        private void UpdateIngotPreview(CoreSO core)
        {
            // Resolve from the selected core slot's ingot resource reference first
            var slot = GetSlotForCore(core);
            var ingot = slot != null && slot.IngotResource != null ? slot.IngotResource :
                core != null ? core.requiredIngot : null;

            var section = craftSection;
            if (section != null && section.cost2Image != null)
            {
                Sprite sprite = null;
                if (ingot != null)
                {
                    var rm = RM;
                    var discovered = rm != null && rm.IsUnlocked(ingot);
                    var have = rm != null && rm.GetAmount(ingot) >= (core != null ? core.ingotCost : 0);
                    sprite = discovered && have ? ingot.icon : ingot.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section != null && section.cost2Text != null)
                section.cost2Text.text = core != null ? Math.Max(0, core.ingotCost).ToString("0") : "0";
        }

        private void UpdateIngotCraftPreview(CoreSO core)
        {
            var rm = RM;
            var section = ingotConversionSection;
            if (section == null) return;

            if (section.resultImage != null)
            {
                Sprite sprite = null;
                var ingotRes = core != null ? core.requiredIngot : null;
                if (ingotRes != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(ingotRes);
                    sprite = discovered ? ingotRes.icon : ingotRes.UnknownIcon;
                }

                section.resultImage.sprite = sprite;
                section.resultImage.enabled = sprite != null;
            }

            if (section.resultText != null)
            {
                // Show selected craft amount (clamped by resources), not owned amount
                var amount = GetCraftAmountForIngots(rm, core);
                section.resultText.text = Blindsided.Utilities.CalcUtils.FormatNumber(amount, hideDecimal: true);
            }

            if (section.maxCraftsText != null)
            {
                // Show how many batches of the chosen size we can craft
                double maxSingle = 0d;
                if (core != null && rm != null)
                {
                    maxSingle = double.MaxValue;
                    if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                        maxSingle = Math.Min(maxSingle, Math.Floor(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
                    if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                        maxSingle = Math.Min(maxSingle, Math.Floor(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
                    if (maxSingle < 0d) maxSingle = 0d;
                }
                var desired = GetDesiredAmountForSection(section);
                double maxBatches;
                if (maxSingle <= 0d) maxBatches = 0d; // cannot craft at all (missing costs)
                else if (desired > maxSingle) maxBatches = 1d; // input above max -> defaults to maxSingle, show 1
                else maxBatches = desired > 0d ? Math.Floor(maxSingle / desired) : 0d;
                section.maxCraftsText.text = Blindsided.Utilities.CalcUtils.FormatNumber(maxBatches, hideDecimal: true);
            }

            if (section.cost1Image != null)
            {
                Sprite sprite = null;
                if (core != null && core.chunkResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(core.chunkResource);
                    var have = rm != null && rm.GetAmount(core.chunkResource) >= core.chunkCostPerIngot;
                    sprite = discovered && have ? core.chunkResource.icon : core.chunkResource.UnknownIcon;
                }

                section.cost1Image.sprite = sprite;
                section.cost1Image.enabled = sprite != null;
            }

            if (section.cost1Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost1Text.text = core != null
                    ? FormatScaledCost(Math.Max(0, core.chunkCostPerIngot), selected)
                    : string.Empty;
            }
            if (section.cost2Image != null)
            {
                Sprite sprite = null;
                if (core != null && core.crystalResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(core.crystalResource);
                    var have = rm != null && rm.GetAmount(core.crystalResource) >= core.crystalCostPerIngot;
                    sprite = discovered && have ? core.crystalResource.icon : core.crystalResource.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section.cost2Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost2Text.text = core != null
                    ? FormatScaledCost(Math.Max(0, core.crystalCostPerIngot), selected)
                    : string.Empty;
            }

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftIngot() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateCrystalCraftPreview(CoreSO core)
        {
            var rm = RM;
            var section = crystalConversionSection;
            if (section == null) return;

            if (section.resultImage != null)
            {
                Sprite sprite = null;
                var res = core != null ? core.crystalResource : null;
                if (res != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(res);
                    sprite = discovered ? res.icon : res.UnknownIcon;
                }

                section.resultImage.sprite = sprite;
                section.resultImage.enabled = sprite != null;
            }

            if (section.resultText != null)
            {
                // Show selected craft amount (clamped by resources), not owned amount
                var amount = GetCraftAmountForCrystals(rm, core);
                section.resultText.text = Blindsided.Utilities.CalcUtils.FormatNumber(amount, hideDecimal: true);
            }

            if (section.maxCraftsText != null)
            {
                double maxSingle = 0d;
                if (core != null && rm != null)
                {
                    var chunkMax = Math.Floor(rm.GetAmount(core.chunkResource) / 2d);
                    var slimeMax = Math.Floor(rm.GetAmount(slimeResource) / 1d);
                    maxSingle = Math.Min(chunkMax, slimeMax);
                }
                if (maxSingle < 0d) maxSingle = 0d;
                var desired = GetDesiredAmountForSection(section);
                double maxBatches;
                if (maxSingle <= 0d) maxBatches = 0d;
                else if (desired > maxSingle) maxBatches = 1d;
                else maxBatches = desired > 0d ? Math.Floor(maxSingle / desired) : 0d;
                section.maxCraftsText.text = Blindsided.Utilities.CalcUtils.FormatNumber(maxBatches, hideDecimal: true);
            }

            if (section.cost1Image != null)
            {
                Sprite sprite = null;
                if (core != null && core.chunkResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(core.chunkResource);
                    var have = rm != null && rm.GetAmount(core.chunkResource) >= 2;
                    sprite = discovered && have ? core.chunkResource.icon : core.chunkResource.UnknownIcon;
                }

                section.cost1Image.sprite = sprite;
                section.cost1Image.enabled = sprite != null;
            }

            if (section.cost1Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost1Text.text = core != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(2d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.cost2Image != null)
            {
                Sprite sprite = null;
                if (slimeResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(slimeResource);
                    var have = rm != null && rm.GetAmount(slimeResource) >= 1;
                    sprite = discovered && have ? slimeResource.icon : slimeResource.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section.cost2Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost2Text.text = slimeResource != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftCrystal() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateChunkCraftPreview(CoreSO core)
        {
            var rm = RM;
            var section = chunkConversionSection;
            if (section == null) return;

            if (section.resultImage != null)
            {
                Sprite sprite = null;
                var res = core != null ? core.chunkResource : null;
                if (res != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(res);
                    sprite = discovered ? res.icon : res.UnknownIcon;
                }

                section.resultImage.sprite = sprite;
                section.resultImage.enabled = sprite != null;
            }

            if (section.resultText != null)
            {
                // Show selected craft amount (clamped by resources), not owned amount
                var amount = GetCraftAmountForChunks(rm, core);
                section.resultText.text = Blindsided.Utilities.CalcUtils.FormatNumber(amount, hideDecimal: true);
            }

            if (section.maxCraftsText != null)
            {
                double maxSingle = 0d;
                if (core != null && rm != null)
                {
                    var crystalMax = Math.Floor(rm.GetAmount(core.crystalResource) / 1d);
                    var stoneMax = Math.Floor(rm.GetAmount(stoneResource) / 2d);
                    maxSingle = Math.Min(crystalMax, stoneMax);
                }
                if (maxSingle < 0d) maxSingle = 0d;
                var desired = GetDesiredAmountForSection(section);
                double maxBatches;
                if (maxSingle <= 0d) maxBatches = 0d;
                else if (desired > maxSingle) maxBatches = 1d;
                else maxBatches = desired > 0d ? Math.Floor(maxSingle / desired) : 0d;
                section.maxCraftsText.text = Blindsided.Utilities.CalcUtils.FormatNumber(maxBatches, hideDecimal: true);
            }

            if (section.cost1Image != null)
            {
                Sprite sprite = null;
                if (core != null && core.crystalResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(core.crystalResource);
                    var have = rm != null && rm.GetAmount(core.crystalResource) >= 1;
                    sprite = discovered && have ? core.crystalResource.icon : core.crystalResource.UnknownIcon;
                }

                section.cost1Image.sprite = sprite;
                section.cost1Image.enabled = sprite != null;
            }

            if (section.cost1Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost1Text.text = core != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.cost2Image != null)
            {
                Sprite sprite = null;
                if (stoneResource != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(stoneResource);
                    var have = rm != null && rm.GetAmount(stoneResource) >= 2;
                    sprite = discovered && have ? stoneResource.icon : stoneResource.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section.cost2Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost2Text.text = stoneResource != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(2d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftChunk() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateCoreCraftPreview(CoreSO core)
        {
            var rm = RM;
            var section = coreConversionSection;
            if (section == null) return;

            var (curRes, nextRes, isFinalTier) = ResolveCurrentAndNextCoreResources(core);
            // If final tier, disable the entire section GameObject
            if (section.gameObject != null) section.gameObject.SetActive(!isFinalTier);
            if (isFinalTier)
                return;

            if (section.resultImage != null)
            {
                Sprite sprite = null;
                var res = nextRes;
                if (res != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(res);
                    sprite = discovered ? res.icon : res.UnknownIcon;
                }

                section.resultImage.sprite = sprite;
                section.resultImage.enabled = sprite != null;
            }

            if (section.resultText != null)
            {
                // Show selected craft amount (clamped by resources), not owned amount
                var amount = GetCraftAmountForCores(rm, core);
                section.resultText.text = Blindsided.Utilities.CalcUtils.FormatNumber(2d * amount, hideDecimal: true);
            }

            if (section.maxCraftsText != null)
            {
                double maxSingle = 0d;
                if (rm != null && curRes != null && nextRes != null)
                    maxSingle = Math.Min(Math.Floor(rm.GetAmount(curRes) / 5d), Math.Floor(rm.GetAmount(nextRes) / 1d));
                if (maxSingle < 0d) maxSingle = 0d;
                var desired = GetDesiredAmountForSection(section);
                double maxBatches;
                if (maxSingle <= 0d) maxBatches = 0d;
                else if (desired > maxSingle) maxBatches = 1d;
                else maxBatches = desired > 0d ? Math.Floor(maxSingle / desired) : 0d;
                section.maxCraftsText.text = Blindsided.Utilities.CalcUtils.FormatNumber(maxBatches, hideDecimal: true);
            }

            if (section.cost1Image != null)
            {
                Sprite sprite = null;
                if (curRes != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(curRes);
                    var have = rm != null && rm.GetAmount(curRes) >= 5;
                    sprite = discovered && have ? curRes.icon : curRes.UnknownIcon;
                }

                section.cost1Image.sprite = sprite;
                section.cost1Image.enabled = sprite != null;
            }

            if (section.cost1Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost1Text.text = curRes != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(5d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.cost2Image != null)
            {
                Sprite sprite = null;
                if (nextRes != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(nextRes);
                    var have = rm != null && rm.GetAmount(nextRes) >= 1;
                    sprite = discovered && have ? nextRes.icon : nextRes.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section.cost2Text != null)
            {
                var selected = GetSelectedAmountForSection(section, rm, core);
                section.cost2Text.text = nextRes != null
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1d * Math.Max(0d, Math.Floor(selected)), hideDecimal: true)
                    : string.Empty;
            }

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftCoreConversion() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateMaxCraftsText()
        {
            var rm = RM;
            var text = craftSection != null ? craftSection.maxCraftsText : null;
            if (text == null)
                return;

            if (selectedCore == null)
            {
                text.text = "Max: 0";
                return;
            }

            var coreSlot = GetSlotForCore(selectedCore);
            var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
            var ingotRes = coreSlot != null && coreSlot.IngotResource != null
                ? coreSlot.IngotResource
                : selectedCore.requiredIngot;

            if (rm == null || coreRes == null || ingotRes == null)
            {
                text.text = "Max: 0";
                return;
            }

            var coreAmount = rm.GetAmount(coreRes);
            var ingotAmount = rm.GetAmount(ingotRes);
            var ingotCost = Math.Max(1, selectedCore.ingotCost);
            var maxByIngots = Math.Floor(ingotAmount / ingotCost);
            var maxByCores = Math.Floor(coreAmount);
            var max = Math.Min(maxByIngots, maxByCores);
            text.text = $"Max: {Math.Max(0d, max):N0}";
        }

        private void RefreshActionButtons()
        {
            var canCraft = CanCraft();
            if (craftButton != null) craftButton.interactable = isAutoCrafting || canCraft;
            if (craftButtonText != null) craftButtonText.text = isAutoCrafting ? "Stop" : "Craft";
            if (craftSection != null && craftSection.craftArrow != null)
            {
                var arrowSprite = canCraft ? craftSection.validArrow : craftSection.invalidArrow;
                craftSection.craftArrow.sprite = arrowSprite;
            }

            var canCraftIngot = CanCraftIngot();
            if (ingotConversionSection != null)
            {
                if (ingotConversionSection.craftButton != null)
                    ingotConversionSection.craftButton.interactable = canCraftIngot && !isAutoCrafting;
            }

            var canCraftCrystal = CanCraftCrystal();
            if (crystalConversionSection != null)
            {
                if (crystalConversionSection.craftButton != null)
                    crystalConversionSection.craftButton.interactable = canCraftCrystal && !isAutoCrafting;
                if (crystalConversionSection.craftArrow != null)
                {
                    var arrowSprite = canCraftCrystal
                        ? crystalConversionSection.validArrow
                        : crystalConversionSection.invalidArrow;
                    crystalConversionSection.craftArrow.sprite = arrowSprite;
                }
            }

            var canCraftChunk = CanCraftChunk();
            if (chunkConversionSection != null)
            {
                if (chunkConversionSection.craftButton != null)
                    chunkConversionSection.craftButton.interactable = canCraftChunk && !isAutoCrafting;
                if (chunkConversionSection.craftArrow != null)
                {
                    var arrowSprite = canCraftChunk
                        ? chunkConversionSection.validArrow
                        : chunkConversionSection.invalidArrow;
                    chunkConversionSection.craftArrow.sprite = arrowSprite;
                }
            }

            var canCraftCore = CanCraftCoreConversion();
            if (coreConversionSection != null)
            {
                if (coreConversionSection.craftButton != null)
                    coreConversionSection.craftButton.interactable = canCraftCore && !isAutoCrafting;
                if (coreConversionSection.craftArrow != null)
                {
                    var arrowSprite = canCraftCore
                        ? coreConversionSection.validArrow
                        : coreConversionSection.invalidArrow;
                    coreConversionSection.craftArrow.sprite = arrowSprite;
                }
            }

            // Replace depends only on having pending text; do not gate on craftability
            var hasPending = HasPendingText();
            if (replaceButton != null) replaceButton.interactable = hasPending && !isAutoCrafting;
            // Auto-craft button toggles; interactable if we can craft or we are currently auto-crafting (to allow stopping)
            if (craftUntilUpgradeButton != null) craftUntilUpgradeButton.interactable = !isAutoCrafting && canCraft;
            if (craftUntilUpgradeButtonText != null)
                craftUntilUpgradeButtonText.text = "Craft Until Upgrade";
        }
        #endregion
    }
}
