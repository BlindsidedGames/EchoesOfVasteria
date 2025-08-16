using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI : MonoBehaviour
    {
        private CraftingService crafting;
        private EquipmentController equipment;

        private List<CoreSO> cores = new();
        private CoreSO selectedCore;
        private string selectedSlot;
        private GearItem lastCrafted;
        private bool isAutoCrafting;
        private Coroutine autoCraftCoroutine;

        // Runtime maps for robust selection/highlight handling
        private readonly Dictionary<GearSlotUIReferences, string> gearSlotNameByRef = new();
        private readonly Dictionary<CoreSlotUIReferences, CoreSO> coreSlotCoreByRef = new();

        private void Awake()
        {
            crafting = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
            equipment = EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();
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
            if (salvageButton != null)
                salvageButton.onClick.AddListener(OnSalvageClicked);
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
                if (ingotConversionSection.modeButton != null)
                {
                    ingotConversionSection.modeButton.onClick.AddListener(OnIngotModeClicked);
                    // Restore saved mode
                    ingotCraftMode = IntToCraftMode(IngotCraftMode);
                    UpdateModeButtonText(ingotConversionSection, ingotCraftMode);
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
                if (crystalConversionSection.modeButton != null)
                {
                    crystalConversionSection.modeButton.onClick.AddListener(OnCrystalModeClicked);
                    crystalCraftMode = IntToCraftMode(CrystalCraftMode);
                    UpdateModeButtonText(crystalConversionSection, crystalCraftMode);
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
                if (chunkConversionSection.modeButton != null)
                {
                    chunkConversionSection.modeButton.onClick.AddListener(OnChunkModeClicked);
                    chunkCraftMode = IntToCraftMode(ChunkCraftMode);
                    UpdateModeButtonText(chunkConversionSection, chunkCraftMode);
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
            if (replaceButton != null) replaceButton.interactable = false;
            if (salvageButton != null) salvageButton.interactable = false;

            // Ensure TMP texts that use <sprite> tags render with the StatIcons sprite asset
            var statSpriteAsset = StatIconLookup.GetSpriteAsset();
            if (statSpriteAsset != null)
            {
                if (selectedSlotStatsText != null) selectedSlotStatsText.spriteAsset = statSpriteAsset;
                if (resultText != null) resultText.spriteAsset = statSpriteAsset;
            }

            // Initialize previews
            ClearResultPreview();
            UpdateAllGearSlots();

            // Default selection on start: first core and Weapon slot
            if (coreSlots != null && coreSlots.Count > 0)
            {
                var firstCore = coreSlotCoreByRef.TryGetValue(coreSlots[0], out var c0) ? c0 : coreSlots[0].Core;
                if (firstCore != null)
                    SelectCore(firstCore);
            }

            // Select Weapon by default if present
            var defaultSlotName = equipment != null && equipment.Slots.Count > 0 ? equipment.Slots[0] : "Weapon";
            OnGearSlotClicked(defaultSlotName);

            // Initialize button states based on current selections/resources
            RefreshActionButtons();

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
            ClearResultPreview();
            RefreshActionButtons();
            UpdateAllGearSlots();
            UpdateSelectedSlotStats();
            // Restore saved craft modes and refresh previews
            ingotCraftMode = IntToCraftMode(IngotCraftMode);
            UpdateModeButtonText(ingotConversionSection, ingotCraftMode);
            crystalCraftMode = IntToCraftMode(CrystalCraftMode);
            UpdateModeButtonText(crystalConversionSection, crystalCraftMode);
            chunkCraftMode = IntToCraftMode(ChunkCraftMode);
            UpdateModeButtonText(chunkConversionSection, chunkCraftMode);
            OnResourcesChanged();
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged += UpdateAllGearSlots;
                equipment.OnEquipmentChanged += UpdateSelectedSlotStats;
            }

            // Refresh Ivan XP display on open
            UpdateIvanXpUI();
            // Refresh selected previews when inventory changes (e.g., crafting spends ingots)
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.OnInventoryChanged += OnResourcesChanged;
            // Subscribe to Ivan XP events if available
            var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
            if (svc != null)
            {
                svc.OnIvanXpChanged += OnIvanXpChanged;
                svc.OnIvanLevelUp += OnIvanLevelUp;
            }

            // Clear UI on load (do not clear on save to avoid autosave side-effects)
            EventHandler.OnLoadData += OnPostLoad;
            OnResourcesChanged();
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= UpdateAllGearSlots;
                equipment.OnEquipmentChanged -= UpdateSelectedSlotStats;
            }

            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.OnInventoryChanged -= OnResourcesChanged;
            var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
            if (svc != null)
            {
                svc.OnIvanXpChanged -= OnIvanXpChanged;
                svc.OnIvanLevelUp -= OnIvanLevelUp;
            }

            EventHandler.OnLoadData -= OnPostLoad;
            StopAutoCrafting();
        }
    }
}