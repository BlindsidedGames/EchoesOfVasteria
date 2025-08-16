using Blindsided;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
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
                if (ingotConversionSection.modeButton != null)
                    ingotConversionSection.modeButton.interactable = canCraftIngot && !isAutoCrafting;
            }

            var canCraftCrystal = CanCraftCrystal();
            if (crystalConversionSection != null)
            {
                if (crystalConversionSection.craftButton != null)
                    crystalConversionSection.craftButton.interactable = canCraftCrystal && !isAutoCrafting;
                if (crystalConversionSection.modeButton != null)
                    crystalConversionSection.modeButton.interactable = canCraftCrystal && !isAutoCrafting;
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
                if (chunkConversionSection.modeButton != null)
                    chunkConversionSection.modeButton.interactable = canCraftChunk && !isAutoCrafting;
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
    }
}