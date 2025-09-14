using Blindsided;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
        // Determine if there is any visible pending text (result summary)
        private bool HasPendingText()
        {
            return resultText != null && !string.IsNullOrWhiteSpace(resultText.text);
        }

        // Helpers for conversion sections
        private int GetSelectedAmountForSection(CraftSection2x1UIReferences section, ResourceManager rm, CoreSO core)
        {
            if (ReferenceEquals(section, ingotConversionSection)) return GetCraftAmountForIngots(rm, core);
            if (ReferenceEquals(section, crystalConversionSection)) return GetCraftAmountForCrystals(rm, core);
            if (ReferenceEquals(section, chunkConversionSection)) return GetCraftAmountForChunks(rm, core);
            if (ReferenceEquals(section, coreConversionSection)) return GetCraftAmountForCores(rm, core);
            return 0;
        }

        private int GetDesiredAmountForSection(CraftSection2x1UIReferences section)
        {
            if (ReferenceEquals(section, ingotConversionSection)) return Mathf.Max(1, ingotCraftAmount);
            if (ReferenceEquals(section, crystalConversionSection)) return Mathf.Max(1, crystalCraftAmount);
            if (ReferenceEquals(section, chunkConversionSection)) return Mathf.Max(1, chunkCraftAmount);
            if (ReferenceEquals(section, coreConversionSection)) return Mathf.Max(1, coreCraftAmount);
            return 1;
        }

        private static string FormatScaledCost(int perUnitCost, int selectedAmount)
        {
            var total = Mathf.Max(0, perUnitCost) * Mathf.Max(0, selectedAmount);
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
                section.cost2Text.text = core != null ? Mathf.Max(0, core.ingotCost).ToString("0") : "0";
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
                var maxSingle = 0;
                if (core != null && rm != null)
                {
                    maxSingle = int.MaxValue;
                    if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                        maxSingle = Mathf.Min(maxSingle, (int)(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
                    if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                        maxSingle = Mathf.Min(maxSingle, (int)(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
                    if (maxSingle < 0) maxSingle = 0;
                }
                var desired = GetDesiredAmountForSection(section);
                int maxBatches;
                if (maxSingle <= 0) maxBatches = 0; // cannot craft at all (missing costs)
                else if (desired > maxSingle) maxBatches = 1; // input above max -> defaults to maxSingle, show 1
                else maxBatches = desired > 0 ? maxSingle / desired : 0;
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
                    ? FormatScaledCost(Mathf.Max(0, core.chunkCostPerIngot), selected)
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
                    ? FormatScaledCost(Mathf.Max(0, core.crystalCostPerIngot), selected)
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
                var maxSingle = 0;
                if (core != null && rm != null)
                    maxSingle = Mathf.Min((int)(rm.GetAmount(core.chunkResource) / 2f),
                        (int)(rm.GetAmount(slimeResource) / 1f));
                if (maxSingle < 0) maxSingle = 0;
                var desired = GetDesiredAmountForSection(section);
                int maxBatches;
                if (maxSingle <= 0) maxBatches = 0;
                else if (desired > maxSingle) maxBatches = 1;
                else maxBatches = desired > 0 ? maxSingle / desired : 0;
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(2 * Mathf.Max(0, selected), hideDecimal: true)
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1 * Mathf.Max(0, selected), hideDecimal: true)
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
                var maxSingle = 0;
                if (core != null && rm != null)
                    maxSingle = Mathf.Min((int)(rm.GetAmount(core.crystalResource) / 1f),
                        (int)(rm.GetAmount(stoneResource) / 2f));
                if (maxSingle < 0) maxSingle = 0;
                var desired = GetDesiredAmountForSection(section);
                int maxBatches;
                if (maxSingle <= 0) maxBatches = 0;
                else if (desired > maxSingle) maxBatches = 1;
                else maxBatches = desired > 0 ? maxSingle / desired : 0;
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1 * Mathf.Max(0, selected), hideDecimal: true)
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(2 * Mathf.Max(0, selected), hideDecimal: true)
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
                section.resultText.text = Blindsided.Utilities.CalcUtils.FormatNumber(amount, hideDecimal: true);
            }

            if (section.maxCraftsText != null)
            {
                var maxSingle = 0;
                if (rm != null && curRes != null && nextRes != null)
                    maxSingle = Mathf.Min((int)(rm.GetAmount(curRes) / 5f), (int)(rm.GetAmount(nextRes) / 1f));
                if (maxSingle < 0) maxSingle = 0;
                var desired = GetDesiredAmountForSection(section);
                int maxBatches;
                if (maxSingle <= 0) maxBatches = 0;
                else if (desired > maxSingle) maxBatches = 1;
                else maxBatches = desired > 0 ? maxSingle / desired : 0;
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(5 * Mathf.Max(0, selected), hideDecimal: true)
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
                    ? Blindsided.Utilities.CalcUtils.FormatNumber(1 * Mathf.Max(0, selected), hideDecimal: true)
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
            var ingotCost = Mathf.Max(1, selectedCore.ingotCost);
            var maxByIngots = Mathf.FloorToInt((float)(ingotAmount / ingotCost));
            var maxByCores = Mathf.FloorToInt((float)coreAmount);
            var max = Mathf.Min(maxByIngots, maxByCores);
            var val = Mathf.Max(0, max);
            text.text = $"Max: {Mathf.Max(0, max):N0}";
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
            if (craftUntilUpgradeButton != null) craftUntilUpgradeButton.interactable = isAutoCrafting || canCraft;
            if (craftUntilUpgradeButtonText != null)
                craftUntilUpgradeButtonText.text = isAutoCrafting ? "Stop" : "Craft Until Upgrade";
        }
    }
}
