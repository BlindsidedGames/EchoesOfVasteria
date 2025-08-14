using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using UnityEngine.Serialization;
using TMPro;
using Blindsided.Utilities;

namespace TimelessEchoes.Gear.UI
{
    public class ForgeWindowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button replaceButton;
		[SerializeField] private Button salvageButton;
		[SerializeField] private Button craftUntilUpgradeButton;
		[SerializeField] private TMP_Text craftUntilUpgradeButtonText;

        [Header("Gear Slot UI")]
        [Tooltip("References to each visible gear slot in this window. Their Button will be wired to SelectSlot.")]
        [SerializeField] private List<GearSlotUIReferences> gearSlots = new();

        [Header("Core Slot UI")]
        [Tooltip("Pre-placed core slot references in the scene. No prefab route is used.")]
        [SerializeField] private List<CoreSlotUIReferences> coreSlots = new();

		[Header("Odds UI")]
		[SerializeField] private TMP_Text rarityOddsLeftText;
		[SerializeField] private TMP_Text rarityOddsRightText;
		[SerializeField] private List<MPImageBasic> oddsPieSlices = new();

		[Header("Ivan XP UI")]
		[SerializeField] private Blindsided.Utilities.SlicedFilledImage ivanXpBar;
		[SerializeField] private TMP_Text ivanXpText;
		[SerializeField] private TMP_Text ivanLevelText;

        [Header("Preview UI")]
        [Tooltip("Image to display the currently selected Core.")]
        [SerializeField] private Image selectedCoreImage;
        [Tooltip("Image to display the required Ingot for the selected Core.")]
        [SerializeField] private Image selectedIngotImage;
        [Tooltip("Image to display the most recently crafted item preview.")]
        [SerializeField] private Image resultItemImage;
        [Tooltip("Text to display remaining Core count for the selected Core.")]
        [SerializeField] private TMP_Text selectedCoreCountText;
        [Tooltip("Text to display remaining Ingot count for the selected Core.")]
        [SerializeField] private TMP_Text selectedIngotCountText;

        [Header("Selected Slot UI")]
        [Tooltip("Text to display the stats of the currently equipped gear in the selected slot.")]
        [SerializeField] private TMP_Text selectedSlotStatsText;

        [Header("Unknown Gear Sprites (by slot order)")]
        [Tooltip("Fallback unknown sprites for each gear slot: Weapon, Helmet, Chest, Boots")]
        [SerializeField] private List<Sprite> unknownGearSprites = new();

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
            cores = AssetCache.GetAll<CoreSO>("").Where(b => b != null).OrderBy(b => b.tierIndex).ToList();

            // Build Core selection UI using only pre-placed slots (no prefab route)
            coreSlotCoreByRef.Clear();
            for (int i = 0; i < coreSlots.Count; i++)
            {
                var slot = coreSlots[i];
                if (slot == null) continue;
                slot.SetSelected(false);
                var mappedCore = slot.Core != null ? slot.Core : (i < cores.Count ? cores[i] : null);
                coreSlotCoreByRef[slot] = mappedCore;
                if (slot.SelectSlotButton != null)
                {
                    slot.SelectSlotButton.onClick.RemoveAllListeners();
                    var capturedSlot = slot;
                    var capturedCore = mappedCore;
                    slot.SelectSlotButton.onClick.AddListener(() => OnCoreSlotClicked(capturedSlot, capturedCore));
                }
                if (mappedCore == null)
                    Debug.LogWarning($"ForgeWindowUI: Core slot at index {i} has no Core mapped; assign Core on the slot or ensure cores are discoverable.");
            }

			if (craftButton != null)
                craftButton.onClick.AddListener(OnCraftClicked);
            if (replaceButton != null)
                replaceButton.onClick.AddListener(OnReplaceClicked);
            if (salvageButton != null)
                salvageButton.onClick.AddListener(OnSalvageClicked);
			if (craftUntilUpgradeButton != null)
				craftUntilUpgradeButton.onClick.AddListener(OnCraftUntilUpgradeClicked);

            // Wire gear slot buttons with fallback to EquipmentController order
            gearSlotNameByRef.Clear();
            var slotNames = equipment != null ? equipment.Slots : new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
            for (int i = 0; i < gearSlots.Count; i++)
            {
                var slotRef = gearSlots[i];
                if (slotRef == null) continue;
                var resolvedName = !string.IsNullOrWhiteSpace(slotRef.SlotName) ? slotRef.SlotName : (i < slotNames.Count ? slotNames[i] : null);
                if (string.IsNullOrWhiteSpace(resolvedName))
                {
                    Debug.LogWarning($"ForgeWindowUI: Could not resolve slot name for gear slot at index {i}. Assign SlotName or extend EquipmentController.Slots.");
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
			var statSpriteAsset = TimelessEchoes.Upgrades.StatIconLookup.GetSpriteAsset();
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
            RefreshOdds();
			RefreshActionButtons();
        }

        private void OnCoreSlotClicked(CoreSlotUIReferences slot, CoreSO core)
        {
            Debug.Log(core != null ? $"ForgeWindowUI: Core clicked -> {core.name}" : "ForgeWindowUI: Core clicked -> (null)");
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

		private void RefreshOdds()
		{
			bool hasLeft = rarityOddsLeftText != null;
			bool hasRight = rarityOddsRightText != null;
			if (!hasLeft && !hasRight)
			{
				Debug.LogWarning("ForgeWindowUI: rarityOddsLeftText/rarityOddsRightText are not assigned; odds UI will not be displayed.");
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
			var rarities = AssetCache.GetAll<RaritySO>("").OrderBy(r => r.tierIndex).ToList();
			var svc = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
			var conf = svc != null ? svc.Config : null;
			var o = Blindsided.Oracle.oracle;
			int level = (o != null && o.saveData != null) ? Mathf.Max(0, o.saveData.CraftingMasteryLevel) : 0;
            int craftsSince = (o != null && o.saveData != null) ? Mathf.Max(0, o.saveData.PityCraftsSinceLast) : 0;
            int pityMinTier = 0;
            if (conf != null && !TimelessEchoes.Upgrades.UpgradeFeatureToggle.DisableCraftingPity)
            {
                if (craftsSince >= conf.pityMythicWithin) pityMinTier = 5;
                else if (craftsSince >= conf.pityLegendaryWithin) pityMinTier = 4;
                else if (craftsSince >= conf.pityEpicWithin) pityMinTier = 3;
                else if (craftsSince >= conf.pityRareWithin) pityMinTier = 2;
            }

			// Compute weights consistent with CraftingService.RollRarity (including pity clamp)
			var weights = new List<(RaritySO r, float w)>();
			foreach (var r in rarities)
			{
				float baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
				float bonus = (r != null && conf != null && conf.enableLevelScaling) ? core.GetRarityWeightPerLevel(r) * level : 0f;
				float w = Mathf.Max(0f, baseW + bonus);
				if (r != null && r.tierIndex < pityMinTier)
					w = 0f;
				weights.Add((r, w));
			}

			float total = weights.Sum(t => t.w);
			var lines = new List<string>(rarities.Count);
			foreach (var (r, w) in weights)
			{
				float p = total > 0f ? (w / total) : 0f;
				var name = r != null ? r.GetName() : "(null)";
				lines.Add($"{name}: {(p * 100f):0.###}%");
			}

			// Split lines between left/right columns. If only one is assigned, show all in that one.
			if (hasLeft && hasRight)
			{
				int mid = (lines.Count + 1) / 2; // put the longer half on the left
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

			// Update pie chart slices
			RefreshOddsPieChart(weights);
		}

		private void RefreshOddsPieChart(List<(RaritySO r, float w)> weights)
		{
			if (oddsPieSlices == null || oddsPieSlices.Count == 0)
				return;

			if (weights == null || weights.Count == 0)
			{
				for (int i = 0; i < oddsPieSlices.Count; i++)
					if (oddsPieSlices[i] != null) oddsPieSlices[i].enabled = false;
				return;
			}

			float total = 0f;
			for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0f, weights[i].w);
			if (total <= 0f)
			{
				for (int i = 0; i < oddsPieSlices.Count; i++)
					if (oddsPieSlices[i] != null) oddsPieSlices[i].enabled = false;
				return;
			}

			int sliceCount = Mathf.Min(oddsPieSlices.Count, weights.Count);
			float used = 0f;
			float startAngle = 0f;
			for (int i = 0; i < sliceCount; i++)
			{
				var img = oddsPieSlices[i];
				if (img == null) continue;

				float fraction = Mathf.Max(0f, weights[i].w) / total;
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
			for (int i = sliceCount; i < oddsPieSlices.Count; i++)
				if (oddsPieSlices[i] != null) oddsPieSlices[i].enabled = false;
		}

        private void OnCraftClicked()
        {
            if (!CanCraft())
            {
                RefreshActionButtons();
                return;
            }
            if (selectedCore == null || crafting == null) { RefreshActionButtons(); return; }
            // Auto-salvage previous craft if one exists
            if (lastCrafted != null)
            {
                SalvageService.Instance?.Salvage(lastCrafted);
                lastCrafted = null;
            }
            // Consume one core item along with ingots
            var coreSlot = GetSlotForCore(selectedCore);
            var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
            lastCrafted = crafting.Craft(selectedCore, selectedSlot, null, coreRes, 1);
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
            var currentByMapping = new Dictionary<TimelessEchoes.Gear.HeroStatMapping, (float value, bool isPercent, string name)>();
            if (current != null)
            {
                foreach (var ca in current.affixes)
                {
                    if (ca == null || ca.stat == null) continue;
                    currentByMapping[ca.stat.heroMapping] = (ca.value, ca.stat.isPercent, ca.stat.GetName());
                }
            }

            var craftedMappings = new HashSet<TimelessEchoes.Gear.HeroStatMapping>();
            var currentMappings = new HashSet<TimelessEchoes.Gear.HeroStatMapping>(currentByMapping.Keys);
            foreach (var a in item.affixes)
            {
                if (a == null || a.stat == null) continue;
                string iconTag = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(a.stat.heroMapping);
                var valueText = $"{Blindsided.Utilities.CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
                var nameText = a.stat.GetName();

                // Compare against current equipped's same stat (if present)
                var cv = currentByMapping.TryGetValue(a.stat.heroMapping, out var cur) ? cur.value : 0f;
                float diff = a.value - cv;
                string arrow = diff > 0.0001f
                    ? TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.UpArrow)
                    : (diff < -0.0001f
                        ? TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.DownArrow)
                        : TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.RightArrow));
                string arrowPrefix = string.IsNullOrEmpty(arrow) ? string.Empty : (arrow + " ");

                // Entirely new stat (not present on current): use plus glyph instead of up arrow
                if (!currentMappings.Contains(a.stat.heroMapping))
                {
                    arrowPrefix = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.Plus) + " ";
                }

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

                string minus = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.Minus);
                string prefix = string.IsNullOrEmpty(minus) ? string.Empty : (minus + " ");
                string iconTag = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(mapping);
                var isPercent = kv.Value.isPercent;
                var name = kv.Value.name;
                var valueText = $"{Blindsided.Utilities.CalcUtils.FormatNumber(0)}{(isPercent ? "%" : "")}";
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
            var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
            if (rm != null) rm.OnInventoryChanged += OnResourcesChanged;
			// Subscribe to Ivan XP events if available
			var svc = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
			if (svc != null)
			{
				svc.OnIvanXpChanged += OnIvanXpChanged;
				svc.OnIvanLevelUp += OnIvanLevelUp;
			}
			// Clear UI on load (do not clear on save to avoid autosave side-effects)
			Blindsided.EventHandler.OnLoadData += OnPostLoad;
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= UpdateAllGearSlots;
                equipment.OnEquipmentChanged -= UpdateSelectedSlotStats;
            }
			var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
            if (rm != null) rm.OnInventoryChanged -= OnResourcesChanged;
			var svc = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
			if (svc != null)
			{
				svc.OnIvanXpChanged -= OnIvanXpChanged;
				svc.OnIvanLevelUp -= OnIvanLevelUp;
			}
			Blindsided.EventHandler.OnLoadData -= OnPostLoad;
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
				Blindsided.Oracle.oracle != null ? Blindsided.Oracle.oracle.saveData.CraftingMasteryXP : 0f,
				TimelessEchoes.Gear.CraftingService.Instance != null ?
					TimelessEchoes.Gear.CraftingService.Instance.Config.xpForFirstLevel * Mathf.Pow(Mathf.Max(1, newLevel), TimelessEchoes.Gear.CraftingService.Instance.Config.xpLevelMultiplier)
					: 1f);
			// Odds depend on level scaling; refresh them when level changes
			RefreshOdds();
		}

        		private void OnResourcesChanged()
		{
			var previewSlot = GetSlotForCore(selectedCore);
			UpdateSelectedCorePreview(previewSlot);
			UpdateIngotPreview(selectedCore);
			UpdateIvanXpUI();
			RefreshActionButtons();
		}

		private void ForceRefreshAllCoreSlots()
		{
			// Force refresh all core slots to ensure UI consistency
			foreach (var slot in coreSlots)
			{
				if (slot != null) slot.Refresh();
			}
		}

		private void UpdateIvanXpUI()
		{
			var o = Blindsided.Oracle.oracle;
			if (o == null || o.saveData == null) return;
			SetIvanLevelLabel(o.saveData.CraftingMasteryLevel);
			if (ivanXpText != null)
			{
				var svc = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
				var conf = svc != null ? svc.Config : null;
				float currentLevel = Mathf.Max(1, o.saveData.CraftingMasteryLevel);
				float need = conf != null ? conf.xpForFirstLevel * Mathf.Pow(currentLevel, conf.xpLevelMultiplier) : 1f;
				ivanXpText.text = $"{o.saveData.CraftingMasteryXP:0}/{need:0}";
			}
			if (ivanXpBar != null)
			{
				var svc = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
				var conf = svc != null ? svc.Config : null;
				float currentLevel = Mathf.Max(1, o.saveData.CraftingMasteryLevel);
				float need = conf != null ? conf.xpForFirstLevel * Mathf.Pow(currentLevel, conf.xpLevelMultiplier) : 1f;
				float ratio = need > 0f ? Mathf.Clamp01(o.saveData.CraftingMasteryXP / need) : 0f;
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
            var slotNames = equipment != null ? equipment.Slots : new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
            for (int i = 0; i < gearSlots.Count; i++)
            {
                var slotRef = gearSlots[i];
                if (slotRef == null) continue;
                var name = gearSlotNameByRef.TryGetValue(slotRef, out var n)
                    ? n
                    : (!string.IsNullOrWhiteSpace(slotRef.SlotName) ? slotRef.SlotName : (i < slotNames.Count ? slotNames[i] : null));
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
            var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
            if (selectedCoreImage != null)
            {
                var res = slot != null ? slot.CoreResource : null;
                Sprite sprite = null;
                if (res != null)
                {
                    var discovered = rm != null && rm.IsUnlocked(res);
                    sprite = discovered ? (slot != null && slot.CoreImage != null && slot.CoreImage.sprite != null ? slot.CoreImage.sprite : res.icon) : res.UnknownIcon;
                }
                selectedCoreImage.sprite = sprite;
                selectedCoreImage.enabled = sprite != null;
            }
            if (selectedCoreCountText != null)
            {
                var res = slot != null ? slot.CoreResource : null;
                if (rm != null && res != null)
                {
                    var amt = rm.GetAmount(res);
                    selectedCoreCountText.text = amt > 0 ? amt.ToString("0") : "0";
                }
                else
                {
                    selectedCoreCountText.text = string.Empty;
                }
            }
        }

        private void UpdateIngotPreview(CoreSO core)
        {
            // Resolve from the selected core slot's ingot resource reference first
            var slot = GetSlotForCore(core);
            var ingot = slot != null && slot.IngotResource != null ? slot.IngotResource : (core != null ? core.requiredIngot : null);

            if (selectedIngotImage != null)
            {
                Sprite sprite = null;
                if (ingot != null)
                {
                    var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
                    var discovered = rm != null && rm.IsUnlocked(ingot);
                    sprite = discovered ? ingot.icon : ingot.UnknownIcon;
                }
                selectedIngotImage.sprite = sprite;
                selectedIngotImage.enabled = sprite != null;
            }
            if (selectedIngotCountText != null)
            {
                var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
                if (rm != null && ingot != null)
                {
                    var amt = rm.GetAmount(ingot);
                    selectedIngotCountText.text = amt > 0 ? amt.ToString("0") : "0";
                }
                else
                {
                    selectedIngotCountText.text = string.Empty;
                }
            }
        }

        private void UpdateResultPreview(GearItem item)
        {
            if (resultItemImage == null) return;
            if (item == null || item.rarity == null)
            {
                resultItemImage.enabled = false;
                resultItemImage.sprite = null;
                return;
            }
            // Use the mapped sprite from the appropriate gear slot UI rather than a separate rarity list
            Sprite sprite = null;
            // Find the gear slot UI that corresponds to the crafted item's slot
            for (int i = 0; i < gearSlots.Count; i++)
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
            resultItemImage.sprite = sprite;
            resultItemImage.enabled = sprite != null;
        }

		private bool CanCraft()
		{
			// Validate core and required resources
			if (crafting == null || selectedCore == null) return false;
			var rm = TimelessEchoes.Upgrades.ResourceManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Upgrades.ResourceManager>();
			if (rm == null) return false;
			var coreSlot = GetSlotForCore(selectedCore);
			var coreRes = coreSlot != null ? coreSlot.CoreResource : null;
			if (coreRes == null) return false;
			var ingot = (coreSlot != null && coreSlot.IngotResource != null) ? coreSlot.IngotResource : selectedCore.requiredIngot;
			if (ingot == null) return false;
			bool haveIngots = rm.GetAmount(ingot) >= Mathf.Max(0, selectedCore.ingotCost);
			bool haveCores = rm.GetAmount(coreRes) >= 1;
			return haveIngots && haveCores;
		}

		private void RefreshActionButtons()
		{
			bool canCraft = CanCraft();
			if (craftButton != null) craftButton.interactable = canCraft && !isAutoCrafting;
			// Replace/Salvage depend only on having a pending result; do not gate on craftability
			bool hasResult = lastCrafted != null;
			if (replaceButton != null) replaceButton.interactable = hasResult && !isAutoCrafting;
			if (salvageButton != null) salvageButton.interactable = hasResult && !isAutoCrafting;
			// Auto-craft button toggles; interactable if we can craft or we are currently auto-crafting (to allow stopping)
			if (craftUntilUpgradeButton != null) craftUntilUpgradeButton.interactable = isAutoCrafting || canCraft;
			if (craftUntilUpgradeButtonText != null) craftUntilUpgradeButtonText.text = isAutoCrafting ? "Stop" : "Craft Until Upgrade";
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
				lastCrafted = crafting.Craft(selectedCore, selectedSlot, null, coreRes, 1);
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

				if (IsPotentialUpgrade(lastCrafted, eq))
				{
					break; // leave lastCrafted for player to review/replace/salvage
				}

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
			float score = ComputeUpgradeScore(candidate, current);
			return score > 0.0001f;
		}

		private float ComputeUpgradeScore(GearItem candidate, GearItem current)
		{
			// Aggregate by hero mapping to compare like-for-like
			var deltaByMapping = new Dictionary<TimelessEchoes.Gear.HeroStatMapping, float>();
			if (candidate != null)
			{
				for (int i = 0; i < candidate.affixes.Count; i++)
				{
					var a = candidate.affixes[i];
					if (a == null || a.stat == null) continue;
					var map = a.stat.heroMapping;
					if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
					deltaByMapping[map] += a.value;
				}
			}
			if (current != null)
			{
				for (int i = 0; i < current.affixes.Count; i++)
				{
					var a = current.affixes[i];
					if (a == null || a.stat == null) continue;
					var map = a.stat.heroMapping;
					if (!deltaByMapping.ContainsKey(map)) deltaByMapping[map] = 0f;
					deltaByMapping[map] -= a.value;
				}
			}

			float score = 0f;
			foreach (var kv in deltaByMapping)
			{
				var def = crafting != null ? crafting.GetStatByMapping(kv.Key) : null;
				float scale = def != null ? Mathf.Max(0f, def.comparisonScale) : 1f;
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
            if (resultItemImage == null) return;
            if (string.IsNullOrWhiteSpace(slot))
            {
                resultItemImage.enabled = false;
                resultItemImage.sprite = null;
                return;
            }

            Sprite sprite = null;
            // Prefer finding the gear slot index by matching resolved names
            int idx = -1;
            for (int i = 0; i < gearSlots.Count; i++)
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

            resultItemImage.sprite = sprite;
            resultItemImage.enabled = sprite != null;
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
                return TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.Minus);

            var lines = new List<string>();

            // Display equipped stats (no +/- prefix);
            foreach (var a in item.affixes)
            {
                if (a == null || a.stat == null) continue;
                string iconTag = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(a.stat.heroMapping);
                var valueText = $"{Blindsided.Utilities.CalcUtils.FormatNumber(a.value)}{(a.stat.isPercent ? "%" : "")}";
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


