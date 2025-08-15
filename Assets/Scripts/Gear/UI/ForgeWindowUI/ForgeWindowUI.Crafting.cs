using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
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
            var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
            ShowResult(summary);
            UpdateResultPreview(lastCrafted);
            // Ensure selected core/ingot previews reflect updated resource counts after craft
            OnResourcesChanged();
            ForceRefreshAllCoreSlots();
            RefreshActionButtons();
            // Odds may change due to pity counter updates; refresh the pie/text
            RefreshOdds();
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
    }
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
    }
}
