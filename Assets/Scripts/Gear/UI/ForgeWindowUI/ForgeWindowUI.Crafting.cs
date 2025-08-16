using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
        private enum CraftMode
        {
            Single,
            Half,
            All
        }

        private CraftMode ingotCraftMode = CraftMode.Single;
        private CraftMode crystalCraftMode = CraftMode.Single;
        private CraftMode chunkCraftMode = CraftMode.Single;

        private static CraftMode NextMode(CraftMode mode)
        {
            return (CraftMode)(((int)mode + 1) % 3);
        }

        private static string ModeToText(CraftMode mode)
        {
            return mode switch
            {
                CraftMode.Half => "50%",
                CraftMode.All => "All",
                _ => "1"
            };
        }

        private void UpdateModeButtonText(CraftSection2x1UIReferences section, CraftMode mode)
        {
            if (section?.modeButtonText != null)
                section.modeButtonText.text = ModeToText(mode);
        }

        private int GetCraftAmountForIngots(ResourceManager rm, CoreSO core)
        {
            var max = int.MaxValue;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                max = Mathf.Min(max, (int)(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                max = Mathf.Min(max, (int)(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
            if (max <= 0) return 0;
            return ingotCraftMode switch
            {
                CraftMode.All => max,
                CraftMode.Half => Mathf.Max(1, max / 2),
                _ => 1
            };
        }

        private int GetCraftAmountForCrystals(ResourceManager rm, CoreSO core)
        {
            if (core.crystalResource == null || core.chunkResource == null || slimeResource == null) return 0;
            var max = Mathf.Min((int)(rm.GetAmount(core.chunkResource) / 2f),
                                (int)(rm.GetAmount(slimeResource) / 1f));
            if (max <= 0) return 0;
            return crystalCraftMode switch
            {
                CraftMode.All => max,
                CraftMode.Half => Mathf.Max(1, max / 2),
                _ => 1
            };
        }

        private int GetCraftAmountForChunks(ResourceManager rm, CoreSO core)
        {
            if (core.crystalResource == null || core.chunkResource == null || stoneResource == null) return 0;
            var max = Mathf.Min((int)(rm.GetAmount(core.crystalResource) / 1f),
                                (int)(rm.GetAmount(stoneResource) / 2f));
            if (max <= 0) return 0;
            return chunkCraftMode switch
            {
                CraftMode.All => max,
                CraftMode.Half => Mathf.Max(1, max / 2),
                _ => 1
            };
        }

        private void OnIngotModeClicked()
        {
            ingotCraftMode = NextMode(ingotCraftMode);
            UpdateModeButtonText(ingotConversionSection, ingotCraftMode);
        }

        private void OnCrystalModeClicked()
        {
            crystalCraftMode = NextMode(crystalCraftMode);
            UpdateModeButtonText(crystalConversionSection, crystalCraftMode);
        }

        private void OnChunkModeClicked()
        {
            chunkCraftMode = NextMode(chunkCraftMode);
            UpdateModeButtonText(chunkConversionSection, chunkCraftMode);
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
            var amount = GetCraftAmountForIngots(rm, core);
            if (amount <= 0) return;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                rm.Spend(core.chunkResource, core.chunkCostPerIngot * amount);
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                rm.Spend(core.crystalResource, core.crystalCostPerIngot * amount);
            rm.Add(core.requiredIngot, amount);
            OnResourcesChanged();
        }

        private void OnCraftCrystalClicked()
        {
            if (!CanCraftCrystal()) return;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null) return;
            var amount = GetCraftAmountForCrystals(rm, core);
            if (amount <= 0) return;
            if (core.chunkResource != null)
                rm.Spend(core.chunkResource, 2 * amount);
            if (slimeResource != null)
                rm.Spend(slimeResource, 1 * amount);
            if (core.crystalResource != null)
                rm.Add(core.crystalResource, amount);
            OnResourcesChanged();
        }

        private void OnCraftChunkClicked()
        {
            if (!CanCraftChunk()) return;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var core = selectedCore;
            if (rm == null || core == null) return;
            var amount = GetCraftAmountForChunks(rm, core);
            if (amount <= 0) return;
            if (core.crystalResource != null)
                rm.Spend(core.crystalResource, 1 * amount);
            if (stoneResource != null)
                rm.Spend(stoneResource, 2 * amount);
            if (core.chunkResource != null)
                rm.Add(core.chunkResource, amount);
            OnResourcesChanged();
        }
    }
}
