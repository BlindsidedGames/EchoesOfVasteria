using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.Utilities;
using MPUIKIT;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TimelessEchoes.Gear.UI
{
    public class ForgeWindowUI : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Button craftButton;

        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button replaceButton;
        [SerializeField] private Button salvageButton;
        [SerializeField] private Button craftUntilUpgradeButton;
        [SerializeField] private TMP_Text craftUntilUpgradeButtonText;

        [Header("Gear Slot UI")]
        [Tooltip("References to each visible gear slot in this window. Their Button will be wired to SelectSlot.")]
        [SerializeField]
        private List<GearSlotUIReferences> gearSlots = new();

        [Header("Core Slot UI")]
        [Tooltip("Pre-placed core slot references in the scene. No prefab route is used.")]
        [SerializeField]
        private List<CoreSlotUIReferences> coreSlots = new();

        [Header("Odds UI")] [SerializeField] private TMP_Text rarityOddsLeftText;
        [SerializeField] private TMP_Text rarityOddsRightText;
        [SerializeField] private List<MPImageBasic> oddsPieSlices = new();

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

        [Header("Additional Resource References")] [SerializeField]
        private Resource slimeResource;

        [SerializeField] private Resource stoneResource;

        [Header("Selected Slot UI")]
        [Tooltip("Text to display the stats of the currently equipped gear in the selected slot.")]
        [SerializeField]
        private TMP_Text selectedSlotStatsText;

        [Header("Unknown Gear Sprites (by slot order)")]
        [Tooltip("Fallback unknown sprites for each gear slot: Weapon, Helmet, Chest, Boots")]
        [SerializeField]
        private List<Sprite> unknownGearSprites = new();

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

                if (ingotConversionSection.craftAllButton != null)
                    ingotConversionSection.craftAllButton.onClick.AddListener(OnCraftAllIngotsClicked);
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

                if (crystalConversionSection.craftAllButton != null)
                    crystalConversionSection.craftAllButton.onClick.AddListener(OnCraftAllCrystalsClicked);
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

                if (chunkConversionSection.craftAllButton != null)
                    chunkConversionSection.craftAllButton.onClick.AddListener(OnCraftAllChunksClicked);
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
                var trigger = coreWeightHoverImage.GetComponent<EventTrigger>() ?? coreWeightHoverImage.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ShowCoreWeightTooltip());
                trigger.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => HideCoreWeightTooltip());
                trigger.triggers.Add(exit);
            }
        }

        private void SelectCore(CoreSO core)
        {
            // Stop auto-crafting if the player changes the selected core
            if (isAutoCrafting && core != selectedCore)
                StopAutoCrafting();
            selectedCore = core;
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
            UpdateMaxCraftsText();
            RefreshOdds();
            RefreshActionButtons();
        }

        private void OnCoreSlotClicked(CoreSlotUIReferences slot, CoreSO core)
        {
            Debug.Log(core != null
                ? $"ForgeWindowUI: Core clicked -> {core.name}"
                : "ForgeWindowUI: Core clicked -> (null)");
            SelectCore(core);
        }

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
            // Update equipped stats display for the selected slot
            UpdateSelectedSlotStats();
            RefreshActionButtons();
        }

        private (List<string> lines, List<(RaritySO r, float w)> weights) BuildRarityWeightInfo(CoreSO core)
        {
            var rarities = AssetCache.GetAll<RaritySO>().OrderBy(r => r.tierIndex).ToList();
            var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
            var conf = svc != null ? svc.Config : null;
            var o = Oracle.oracle;
            var level = o != null && o.saveData != null ? Mathf.Max(0, o.saveData.CraftingMasteryLevel) : 0;
            var craftsSince = o != null && o.saveData != null ? Mathf.Max(0, o.saveData.PityCraftsSinceLast) : 0;
            var pityMinTier = 0;
            if (conf != null && !UpgradeFeatureToggle.DisableCraftingPity)
            {
                if (craftsSince >= conf.pityMythicWithin) pityMinTier = 5;
                else if (craftsSince >= conf.pityLegendaryWithin) pityMinTier = 4;
                else if (craftsSince >= conf.pityEpicWithin) pityMinTier = 3;
                else if (craftsSince >= conf.pityRareWithin) pityMinTier = 2;
            }

            var weights = new List<(RaritySO r, float w)>();
            foreach (var r in rarities)
            {
                var baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
                var bonus = r != null && conf != null && conf.enableLevelScaling
                    ? core.GetRarityWeightPerLevel(r) * level
                    : 0f;
                var w = Mathf.Max(0f, baseW + bonus);
                if (r != null && r.tierIndex < pityMinTier)
                    w = 0f;
                weights.Add((r, w));
            }

            var total = weights.Sum(t => t.w);
            var lines = new List<string>(rarities.Count);
            foreach (var (r, w) in weights)
            {
                var p = total > 0f ? w / total : 0f;
                var name = r != null ? r.GetName() : "(null)";
                lines.Add($"{name}: {p * 100f:0.000}%");
            }

            return (lines, weights);
        }

        private void RefreshOdds()
        {
            var hasLeft = rarityOddsLeftText != null;
            var hasRight = rarityOddsRightText != null;
            if (!hasLeft && !hasRight)
            {
                Debug.LogWarning(
                    "ForgeWindowUI: rarityOddsLeftText/rarityOddsRightText are not assigned; odds UI will not be displayed.");
                return;
            }

            var core = selectedCore;
            if (core == null && cores != null && cores.Count > 0)
                core = cores[0];
            if (core == null)
            {
                if (hasLeft) rarityOddsLeftText.text = string.Empty;
                if (hasRight) rarityOddsRightText.text = string.Empty;
                RefreshOddsPieChart(null);
                return;
            }

            var info = BuildRarityWeightInfo(core);
            var lines = info.lines;
            if (hasLeft && hasRight)
            {
                var mid = (lines.Count + 1) / 2; // put the longer half on the left
                var leftLines = lines.Take(mid);
                var rightLines = lines.Skip(mid);
                rarityOddsLeftText.text = string.Join("\n", leftLines);
                rarityOddsRightText.text = string.Join("\n", rightLines);
            }
            else if (hasLeft)
            {
                rarityOddsLeftText.text = string.Join("\n", lines);
            }
            else if (hasRight)
            {
                rarityOddsRightText.text = string.Join("\n", lines);
            }

            RefreshOddsPieChart(info.weights);
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

            var sliceCount = Mathf.Min(oddsPieSlices.Count, weights.Count);
            var used = 0f;
            var startAngle = 0f;
            for (var i = 0; i < sliceCount; i++)
            {
                var img = oddsPieSlices[i];
                if (img == null) continue;

                var fraction = Mathf.Max(0f, weights[i].w) / total;
                if (i == sliceCount - 1) fraction = Mathf.Clamp01(1f - used); // ensure we cover full 360
                else used += fraction;

                img.enabled = fraction > 0f;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = 2; // Top origin; we rotate transform to position
                img.fillClockwise = true;
                img.fillAmount = Mathf.Clamp01(fraction);
                img.color = weights[i].r != null ? weights[i].r.color : Color.white;
                var rt = img.rectTransform;
                if (rt != null)
                {
                    var e = rt.localEulerAngles;
                    e.z = -startAngle;
                    rt.localEulerAngles = e;
                }

                startAngle += fraction * 360f;
            }

            // Disable any extra slices beyond available weights
            for (var i = sliceCount; i < oddsPieSlices.Count; i++)
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

            var info = BuildRarityWeightInfo(core);
            coreWeightHoverText.text = string.Join("\n", info.lines);
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

        private void OnCraftClicked()
        {
            if (!CanCraft())
            {
                RefreshActionButtons();
                return;
            }

            if (selectedCore == null || crafting == null)
            {
                RefreshActionButtons();
                return;
            }

            // Auto-salvage previous craft if one exists
            if (lastCrafted != null)
            {
                SalvageService.Instance?.Salvage(lastCrafted);
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

            crafting.RegisterCraftOutcome(lastCrafted.rarity);
            var eq = equipment?.GetEquipped(lastCrafted.slot);
            var summary = BuildItemSummary(lastCrafted, eq);
            ShowResult(summary);
            UpdateResultPreview(lastCrafted);
            // Ensure selected core/ingot previews reflect updated resource counts after craft
            OnResourcesChanged();
            ForceRefreshAllCoreSlots();
            RefreshActionButtons();
            // Odds may change due to pity counter updates; refresh the pie/text
            RefreshOdds();
        }

        // Called by UI slot buttons (e.g., Weapon/Helmet/Chest/Boots)
        public void SelectSlot(string slot)
        {
            // Stop auto-crafting if the player changes the selected gear slot
            if (isAutoCrafting && !string.Equals(selectedSlot, slot))
                StopAutoCrafting();
            selectedSlot = slot;
        }

        private string BuildItemSummary(GearItem item, GearItem current)
        {
            var lines = new List<string>();
            // Build a quick lookup of current affix values by hero stat mapping for comparison
            var currentByMapping = new Dictionary<HeroStatMapping, (float value, bool isPercent, string name)>();
            if (current != null)
                foreach (var ca in current.affixes)
                {
                    if (ca == null || ca.stat == null) continue;
                    currentByMapping[ca.stat.heroMapping] = (ca.value, ca.stat.isPercent, ca.stat.GetName());
                }

            var craftedMappings = new HashSet<HeroStatMapping>();
            var currentMappings = new HashSet<HeroStatMapping>(currentByMapping.Keys);
            foreach (var a in item.affixes)
            {
                if (a == null || a.stat == null) continue;
                var iconTag = StatIconLookup.GetIconTag(a.stat.heroMapping);
                var valueText = $"{CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
                var nameText = a.stat.GetName();

                // Compare against current equipped's same stat (if present)
                var cv = currentByMapping.TryGetValue(a.stat.heroMapping, out var cur) ? cur.value : 0f;
                var diff = a.value - cv;
                var arrow = diff > 0.0001f
                    ? StatIconLookup.GetIconTag(StatIconLookup.StatKey.UpArrow)
                    : diff < -0.0001f
                        ? StatIconLookup.GetIconTag(StatIconLookup.StatKey.DownArrow)
                        : StatIconLookup.GetIconTag(StatIconLookup.StatKey.RightArrow);
                var arrowPrefix = string.IsNullOrEmpty(arrow) ? string.Empty : arrow + " ";

                // Entirely new stat (not present on current): use plus glyph instead of up arrow
                if (!currentMappings.Contains(a.stat.heroMapping))
                    arrowPrefix = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Plus) + " ";

                if (!string.IsNullOrEmpty(iconTag))
                    lines.Add($"{arrowPrefix}{iconTag} {valueText}");
                else
                    lines.Add($"{arrowPrefix}{nameText} {valueText}");

                craftedMappings.Add(a.stat.heroMapping);
            }

            // Include stats that were present on current but missing on the crafted item as 0 with minus icon
            foreach (var kv in currentByMapping)
            {
                var mapping = kv.Key;
                if (craftedMappings.Contains(mapping)) continue;

                var minus = StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);
                var prefix = string.IsNullOrEmpty(minus) ? string.Empty : minus + " ";
                var iconTag = StatIconLookup.GetIconTag(mapping);
                var isPercent = kv.Value.isPercent;
                var name = kv.Value.name;
                var valueText = $"{CalcUtils.FormatNumber(0)}{(isPercent ? "%" : "")}";
                if (!string.IsNullOrEmpty(iconTag))
                    lines.Add($"{prefix}{iconTag} {valueText}");
                else
                    lines.Add($"{prefix}{name} {valueText}");
            }

            return string.Join("\n", lines);
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
            lastCrafted = null;
            // Clear result text and disable action buttons when no active craft
            if (resultText != null) resultText.text = string.Empty;
            if (replaceButton != null) replaceButton.interactable = false;
            if (salvageButton != null) salvageButton.interactable = false;
            // Clear result preview when result is equipped
            ClearResultPreview();
            UpdateAllGearSlots();
            RefreshActionButtons();
        }

        private void OnSalvageClicked()
        {
            if (lastCrafted == null) return;
            SalvageService.Instance?.Salvage(lastCrafted);
            lastCrafted = null;
            // Clear result text and disable action buttons when no active craft
            if (resultText != null) resultText.text = string.Empty;
            if (replaceButton != null) replaceButton.interactable = false;
            if (salvageButton != null) salvageButton.interactable = false;
            // Clear result preview when salvaged
            ClearResultPreview();
            RefreshActionButtons();
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

        private void OnIvanXpChanged(int level, float current, float needed)
        {
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
            RefreshOdds();
        }

        private void OnResourcesChanged()
        {
            var previewSlot = GetSlotForCore(selectedCore);
            UpdateSelectedCorePreview(previewSlot);
            UpdateIngotPreview(selectedCore);
            UpdateIngotCraftPreview(selectedCore);
            UpdateCrystalCraftPreview(selectedCore);
            UpdateChunkCraftPreview(selectedCore);
            UpdateMaxCraftsText();
            UpdateIvanXpUI();
            RefreshActionButtons();
        }

        private void ForceRefreshAllCoreSlots()
        {
            // Force refresh all core slots to ensure UI consistency
            foreach (var slot in coreSlots)
                if (slot != null)
                    slot.Refresh();
        }

        private void UpdateIvanXpUI()
        {
            var o = Oracle.oracle;
            if (o == null || o.saveData == null) return;
            SetIvanLevelLabel(o.saveData.CraftingMasteryLevel);
            if (ivanXpText != null)
            {
                var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
                var conf = svc != null ? svc.Config : null;
                float currentLevel = Mathf.Max(1, o.saveData.CraftingMasteryLevel);
                var need = conf != null ? conf.xpForFirstLevel * Mathf.Pow(currentLevel, conf.xpLevelMultiplier) : 1f;
                ivanXpText.text = $"{o.saveData.CraftingMasteryXP:0}/{need:0}";
            }

            if (ivanXpBar != null)
            {
                var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
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
                    slotRef.ApplyGearSprite(item);
                else
                    slotRef.ClearGearSprite();
            }
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

        private void UpdateSelectedCorePreview(CoreSlotUIReferences slot)
        {
            // Update selected core image and count based on the clicked slot
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
                    var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
                    var discovered = rm != null && rm.IsUnlocked(ingot);
                    var have = rm != null && rm.GetAmount(ingot) >= (core != null ? core.ingotCost : 0);
                    sprite = discovered && have ? ingot.icon : ingot.UnknownIcon;
                }

                section.cost2Image.sprite = sprite;
                section.cost2Image.enabled = sprite != null;
            }

            if (section != null && section.cost2Text != null)
                section.cost2Text.text = core != null ? Mathf.Max(0, core.ingotCost).ToString("0") : "0";
        }

        private void UpdateIngotCraftPreview(CoreSO core)
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
                var ingotRes = core != null ? core.requiredIngot : null;
                var amount = rm != null && ingotRes != null ? rm.GetAmount(ingotRes) : 0;
                section.resultText.text = amount.ToString("0");
            }

            if (section.maxCraftsText != null)
            {
                var max = 0;
                if (core != null && rm != null)
                {
                    var chunkMax = int.MaxValue;
                    if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                        chunkMax = Mathf.FloorToInt((float)(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
                    var crystalMax = int.MaxValue;
                    if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                        crystalMax =
                            Mathf.FloorToInt((float)(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
                    max = Mathf.Min(chunkMax, crystalMax);
                    if (max == int.MaxValue) max = 0;
                }

                section.maxCraftsText.text = $"Max: {Mathf.Max(0, max)}";
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
                section.cost1Text.text = core != null ? core.chunkCostPerIngot.ToString("0") : string.Empty;
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
                section.cost2Text.text = core != null ? core.crystalCostPerIngot.ToString("0") : string.Empty;

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftIngot() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateCrystalCraftPreview(CoreSO core)
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
                var res = core != null ? core.crystalResource : null;
                var amount = rm != null && res != null ? rm.GetAmount(res) : 0;
                section.resultText.text = amount.ToString("0");
            }

            if (section.maxCraftsText != null)
            {
                var max = 0;
                if (core != null && rm != null && core.chunkResource != null && slimeResource != null)
                {
                    var chunkMax = Mathf.FloorToInt((float)(rm.GetAmount(core.chunkResource) / 2f));
                    var slimeMax = Mathf.FloorToInt((float)(rm.GetAmount(slimeResource) / 1f));
                    max = Mathf.Min(chunkMax, slimeMax);
                }

                section.maxCraftsText.text = $"Max: {Mathf.Max(0, max)}";
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
                section.cost1Text.text = core != null ? "2" : string.Empty;

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
                section.cost2Text.text = slimeResource != null ? "1" : string.Empty;

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftCrystal() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateChunkCraftPreview(CoreSO core)
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
                var res = core != null ? core.chunkResource : null;
                var amount = rm != null && res != null ? rm.GetAmount(res) : 0;
                section.resultText.text = amount.ToString("0");
            }

            if (section.maxCraftsText != null)
            {
                var max = 0;
                if (core != null && rm != null && core.crystalResource != null && stoneResource != null)
                {
                    var crystalMax = Mathf.FloorToInt((float)(rm.GetAmount(core.crystalResource) / 1f));
                    var stoneMax = Mathf.FloorToInt((float)(rm.GetAmount(stoneResource) / 2f));
                    max = Mathf.Min(crystalMax, stoneMax);
                }

                section.maxCraftsText.text = $"Max: {Mathf.Max(0, max)}";
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
                section.cost1Text.text = core != null ? "1" : string.Empty;

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
                section.cost2Text.text = stoneResource != null ? "2" : string.Empty;

            if (section.craftArrow != null)
            {
                var arrowSprite = CanCraftChunk() ? section.validArrow : section.invalidArrow;
                section.craftArrow.sprite = arrowSprite;
            }
        }

        private void UpdateMaxCraftsText()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
            var ingotCost = Mathf.Max(1, selectedCore.ingotCost);
            var maxByIngots = Mathf.FloorToInt((float)(ingotAmount / ingotCost));
            var maxByCores = Mathf.FloorToInt((float)coreAmount);
            var max = Mathf.Min(maxByIngots, maxByCores);
            text.text = $"Max: {Mathf.Max(0, max)}";
        }

        private void UpdateResultPreview(GearItem item)
        {
            var img = craftSection != null ? craftSection.resultImage : null;
            if (img == null) return;
            if (item == null || item.rarity == null)
            {
                img.enabled = false;
                img.sprite = null;
                return;
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
            }

            img.sprite = sprite;
            img.enabled = sprite != null;
        }

        private bool CanCraft()
        {
            // Validate core and required resources
            if (crafting == null || selectedCore == null) return false;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm == null) return false;
            var coreSlot = GetSlotForCore(selectedCore);
            var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
            if (coreRes == null) return false;
            var ingot = coreSlot != null && coreSlot.IngotResource != null
                ? coreSlot.IngotResource
                : selectedCore.requiredIngot;
            if (ingot == null) return false;
            var haveIngots = rm.GetAmount(ingot) >= Mathf.Max(0, selectedCore.ingotCost);
            var haveCores = rm.GetAmount(coreRes) >= 1;
            return haveIngots && haveCores;
        }

        private bool CanCraftIngot()
        {
            if (selectedCore == null || selectedCore.requiredIngot == null)
                return false;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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
            if (!CanCraftIngot()) return;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null) return;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                rm.Spend(core.chunkResource, core.chunkCostPerIngot);
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                rm.Spend(core.crystalResource, core.crystalCostPerIngot);
            rm.Add(core.requiredIngot, 1);
            OnResourcesChanged();
        }

        private void OnCraftAllIngotsClicked()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null || core.requiredIngot == null) return;
            var craftable = int.MaxValue;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                craftable = Mathf.Min(craftable, (int)(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                craftable = Mathf.Min(craftable, (int)(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
            if (craftable <= 0) return;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                rm.Spend(core.chunkResource, core.chunkCostPerIngot * craftable);
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                rm.Spend(core.crystalResource, core.crystalCostPerIngot * craftable);
            rm.Add(core.requiredIngot, craftable);
            OnResourcesChanged();
        }

        private void OnCraftCrystalClicked()
        {
            if (!CanCraftCrystal()) return;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null) return;
            if (core.chunkResource != null)
                rm.Spend(core.chunkResource, 2);
            if (slimeResource != null)
                rm.Spend(slimeResource, 1);
            if (core.crystalResource != null)
                rm.Add(core.crystalResource, 1);
            OnResourcesChanged();
        }

        private void OnCraftAllCrystalsClicked()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null || core.crystalResource == null || core.chunkResource == null ||
                slimeResource == null)
                return;
            var craftable = Mathf.Min((int)(rm.GetAmount(core.chunkResource) / 2f),
                (int)(rm.GetAmount(slimeResource) / 1f));
            if (craftable <= 0) return;
            rm.Spend(core.chunkResource, 2 * craftable);
            rm.Spend(slimeResource, 1 * craftable);
            rm.Add(core.crystalResource, craftable);
            OnResourcesChanged();
        }

        private void OnCraftChunkClicked()
        {
            if (!CanCraftChunk()) return;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null) return;
            if (core.crystalResource != null)
                rm.Spend(core.crystalResource, 1);
            if (stoneResource != null)
                rm.Spend(stoneResource, 2);
            if (core.chunkResource != null)
                rm.Add(core.chunkResource, 1);
            OnResourcesChanged();
        }

        private void OnCraftAllChunksClicked()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null || core.crystalResource == null || core.chunkResource == null ||
                stoneResource == null)
                return;
            var craftable = Mathf.Min((int)(rm.GetAmount(core.crystalResource) / 1f),
                (int)(rm.GetAmount(stoneResource) / 2f));
            if (craftable <= 0) return;
            rm.Spend(core.crystalResource, 1 * craftable);
            rm.Spend(stoneResource, 2 * craftable);
            rm.Add(core.chunkResource, craftable);
            OnResourcesChanged();
        }

        private void RefreshActionButtons()
        {
            var canCraft = CanCraft();
            if (craftButton != null) craftButton.interactable = canCraft && !isAutoCrafting;
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
                if (ingotConversionSection.craftAllButton != null)
                    ingotConversionSection.craftAllButton.interactable = canCraftIngot && !isAutoCrafting;
            }

            var canCraftCrystal = CanCraftCrystal();
            if (crystalConversionSection != null)
            {
                if (crystalConversionSection.craftButton != null)
                    crystalConversionSection.craftButton.interactable = canCraftCrystal && !isAutoCrafting;
                if (crystalConversionSection.craftAllButton != null)
                    crystalConversionSection.craftAllButton.interactable = canCraftCrystal && !isAutoCrafting;
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
                if (chunkConversionSection.craftAllButton != null)
                    chunkConversionSection.craftAllButton.interactable = canCraftChunk && !isAutoCrafting;
                if (chunkConversionSection.craftArrow != null)
                {
                    var arrowSprite = canCraftChunk
                        ? chunkConversionSection.validArrow
                        : chunkConversionSection.invalidArrow;
                    chunkConversionSection.craftArrow.sprite = arrowSprite;
                }
            }

            // Replace/Salvage depend only on having a pending result; do not gate on craftability
            var hasResult = lastCrafted != null;
            if (replaceButton != null) replaceButton.interactable = hasResult && !isAutoCrafting;
            if (salvageButton != null) salvageButton.interactable = hasResult && !isAutoCrafting;
            // Auto-craft button toggles; interactable if we can craft or we are currently auto-crafting (to allow stopping)
            if (craftUntilUpgradeButton != null) craftUntilUpgradeButton.interactable = isAutoCrafting || canCraft;
            if (craftUntilUpgradeButtonText != null)
                craftUntilUpgradeButtonText.text = isAutoCrafting ? "Stop" : "Craft Until Upgrade";
        }

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
            autoCraftCoroutine = StartCoroutine(CraftUntilUpgradeCoroutine());
            RefreshActionButtons();
        }

        private void StopAutoCrafting()
        {
            if (!isAutoCrafting) return;
            isAutoCrafting = false;
            if (autoCraftCoroutine != null)
            {
                StopCoroutine(autoCraftCoroutine);
                autoCraftCoroutine = null;
            }

            RefreshActionButtons();
        }

        private IEnumerator CraftUntilUpgradeCoroutine()
        {
            var wait = new WaitForSecondsRealtime(0.1f); // ~10 crafts per second
            while (isAutoCrafting)
            {
                if (!CanCraft())
                    break;

                // Auto-salvage previous craft before rolling a new one
                if (lastCrafted != null)
                {
                    SalvageService.Instance?.Salvage(lastCrafted);
                    lastCrafted = null;
                }

                if (selectedCore == null || crafting == null)
                    break;

                var coreSlot = GetSlotForCore(selectedCore);
                var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
                lastCrafted = crafting.Craft(selectedCore, selectedSlot, null, coreRes);
                if (lastCrafted == null)
                {
                    RefreshActionButtons();
                    break;
                }

                crafting.RegisterCraftOutcome(lastCrafted.rarity);
                var eq = equipment?.GetEquipped(lastCrafted.slot);
                var summary = BuildItemSummary(lastCrafted, eq);
                ShowResult(summary);
                UpdateResultPreview(lastCrafted);
                OnResourcesChanged();
                ForceRefreshAllCoreSlots();
                RefreshOdds();

                if (IsPotentialUpgrade(lastCrafted,
                        eq)) break; // leave lastCrafted for player to review/replace/salvage

                // Not an upgrade, salvage and continue
                SalvageService.Instance?.Salvage(lastCrafted);
                lastCrafted = null;
                RefreshActionButtons();
                yield return wait;
            }

            isAutoCrafting = false;
            autoCraftCoroutine = null;
            RefreshActionButtons();
        }

        private bool IsPotentialUpgrade(GearItem candidate, GearItem current)
        {
            if (candidate == null) return false;
            var score = ComputeUpgradeScore(candidate, current);
            return score > 0.0001f;
        }

        private float ComputeUpgradeScore(GearItem candidate, GearItem current)
        {
            // Aggregate by hero mapping to compare like-for-like
            var deltaByMapping = new Dictionary<HeroStatMapping, float>();
            if (candidate != null)
                for (var i = 0; i < candidate.affixes.Count; i++)
                {
                    var a = candidate.affixes[i];
                    if (a == null || a.stat == null) continue;
                    var map = a.stat.heroMapping;
                    if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
                    deltaByMapping[map] += a.value;
                }

            if (current != null)
                for (var i = 0; i < current.affixes.Count; i++)
                {
                    var a = current.affixes[i];
                    if (a == null || a.stat == null) continue;
                    var map = a.stat.heroMapping;
                    if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
                    deltaByMapping[map] -= a.value;
                }

            var score = 0f;
            foreach (var kv in deltaByMapping)
            {
                var def = crafting != null ? crafting.GetStatByMapping(kv.Key) : null;
                var scale = def != null ? Mathf.Max(0f, def.comparisonScale) : 1f;
                score += kv.Value * scale;
            }

            return score;
        }

        private void ClearResultPreview()
        {
            // Show unknown gear sprite for the currently selected slot
            SetResultUnknownForSlot(selectedSlot);
        }

        private void SetResultUnknownForSlot(string slot)
        {
            var img = craftSection != null ? craftSection.resultImage : null;
            if (img == null) return;
            if (string.IsNullOrWhiteSpace(slot))
            {
                img.enabled = false;
                img.sprite = null;
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
            selectedSlotStatsText.text = BuildEquippedStatsText(equipped, selectedSlot);
        }

        private string BuildEquippedStatsText(GearItem item, string slotName)
        {
            if (item == null)
                return StatIconLookup.GetIconTag(StatIconLookup.StatKey.Minus);

            var lines = new List<string>();

            // Display equipped stats (no +/- prefix);
            foreach (var a in item.affixes)
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
    }
}