using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
        // User-selected amounts for conversions (persisted in PlayerPrefs)
        private int ingotCraftAmount = 1;
        private int crystalCraftAmount = 1;
        private int chunkCraftAmount = 1;

        private int GetCraftAmountForIngots(ResourceManager rm, CoreSO core)
        {
            var max = int.MaxValue;
            if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                max = Mathf.Min(max, (int)(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot));
            if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                max = Mathf.Min(max, (int)(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot));
            if (max <= 0) return 0;
            var desired = Mathf.Max(1, ingotCraftAmount);
            return Mathf.Min(desired, max);
        }

        private int GetCraftAmountForCrystals(ResourceManager rm, CoreSO core)
        {
            if (core.crystalResource == null || core.chunkResource == null || slimeResource == null) return 0;
            var max = Mathf.Min((int)(rm.GetAmount(core.chunkResource) / 2f),
                (int)(rm.GetAmount(slimeResource) / 1f));
            if (max <= 0) return 0;
            var desired = Mathf.Max(1, crystalCraftAmount);
            return Mathf.Min(desired, max);
        }

        private int GetCraftAmountForChunks(ResourceManager rm, CoreSO core)
        {
            if (core.crystalResource == null || core.chunkResource == null || stoneResource == null) return 0;
            var max = Mathf.Min((int)(rm.GetAmount(core.crystalResource) / 1f),
                (int)(rm.GetAmount(stoneResource) / 2f));
            if (max <= 0) return 0;
            var desired = Mathf.Max(1, chunkCraftAmount);
            return Mathf.Min(desired, max);
        }

        private void OnAmountInputChanged(CraftSection2x1UIReferences section, ref int backingField)
        {
            if (section == null || section.amountInput == null) return;
            var text = section.amountInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                backingField = 1;
                return;
            }
            if (int.TryParse(text, out var value))
            {
                backingField = Mathf.Max(1, value);
            }
            else
            {
                backingField = 1;
                section.amountInput.text = "1";
            }
        }

        private void OnCraftClicked()
        {
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
            equipment ??= EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();
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
                RefreshOdds();
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
            RefreshOdds();

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
            if (replaceButton != null) replaceButton.interactable = false;
            // Clear result preview when result is equipped
            ClearResultPreview();
            UpdateAllGearSlots();
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
                return;
            }

            // Handle migrated helmet with no rarity: show explicit migrated sprite
            if (item.rarity == null && string.Equals(item.slot, "Helmet"))
            {
                if (migratedHelmetSprite != null)
                {
                    img.sprite = migratedHelmetSprite;
                    img.enabled = true;
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
                    return;
                }
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
            rm.Add(core.requiredIngot, amount, trackStats: false);
            // Stats: conversion action and resource deltas
            var o = Blindsided.Oracle.oracle;
            if (o != null && o.saveData != null && o.saveData.Forge != null)
            {
                var forge = o.saveData.Forge;
                forge.IngotConversions++;
                if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                {
                    var k = core.chunkResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += core.chunkCostPerIngot * amount;
                }
                if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                {
                    var k = core.crystalResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += core.crystalCostPerIngot * amount;
                }
            }
            // Persist desired amount
            ingotCraftAmount = Mathf.Max(1, ingotCraftAmount);
            PlayerPrefs.SetInt("IngotCraftAmount", ingotCraftAmount);
            PlayerPrefs.Save();
            OnResourcesChanged();
            // Refresh stats UI
            var statsUi1 = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi1 != null) statsUi1.MarkDirty();

            // Persist conversion to in-memory save (defer disk write) and update stats
            try
            {
                var o1 = Blindsided.Oracle.oracle;
                if (o1 != null && o1.saveData != null && o1.saveData.Forge != null)
                {
                    var forge = o1.saveData.Forge;
                    var ingotName = core.requiredIngot != null ? core.requiredIngot.name : "Ingot";
                    if (!forge.IngotsCraftedByResource.ContainsKey(ingotName)) forge.IngotsCraftedByResource[ingotName] = 0;
                    forge.IngotsCraftedByResource[ingotName] += amount;
                }
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after ingot craft failed: {ex}");
            }
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
                rm.Add(core.crystalResource, amount, trackStats: false);
            // Refresh stats UI
            var statsUi2 = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi2 != null) statsUi2.MarkDirty();
            // Stats: crystal conversion
            {
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                {
                    var forge = o.saveData.Forge;
                    forge.CrystalCrafted += amount;
                    if (core.chunkResource != null) { var k = core.chunkResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += 2 * amount; }
                    if (slimeResource != null) { var k = slimeResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += 1 * amount; }
                }
            }
            crystalCraftAmount = Mathf.Max(1, crystalCraftAmount);
            PlayerPrefs.SetInt("CrystalCraftAmount", crystalCraftAmount);
            PlayerPrefs.Save();
            OnResourcesChanged();

            // Persist conversion to in-memory save (defer disk write) and update stats
            try
            {
                var o2 = Blindsided.Oracle.oracle;
                if (o2 != null && o2.saveData != null && o2.saveData.Forge != null)
                {
                    var forge = o2.saveData.Forge;
                    var crystalName = core.crystalResource != null ? core.crystalResource.name : "Crystal";
                    if (!forge.CrystalsCraftedByResource.ContainsKey(crystalName)) forge.CrystalsCraftedByResource[crystalName] = 0;
                    forge.CrystalsCraftedByResource[crystalName] += amount;
                }
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after crystal craft failed: {ex}");
            }
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
                rm.Add(core.chunkResource, amount, trackStats: false);
            // Refresh stats UI
            var statsUi3 = FindFirstObjectByType<ForgeStatsUIController>();
            if (statsUi3 != null) statsUi3.MarkDirty();
            // Stats: chunk conversion
            {
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                {
                    var forge = o.saveData.Forge;
                    forge.ChunksCrafted += amount;
                    if (core.crystalResource != null) { var k = core.crystalResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += 1 * amount; }
                    if (stoneResource != null) { var k = stoneResource.name; if (!forge.ConversionSpentByResource.ContainsKey(k)) forge.ConversionSpentByResource[k] = 0; forge.ConversionSpentByResource[k] += 2 * amount; }
                }
            }
            chunkCraftAmount = Mathf.Max(1, chunkCraftAmount);
            PlayerPrefs.SetInt("ChunkCraftAmount", chunkCraftAmount);
            PlayerPrefs.Save();
            OnResourcesChanged();

            // Persist conversion to in-memory save (defer disk write) and update stats
            try
            {
                var o3 = Blindsided.Oracle.oracle;
                if (o3 != null && o3.saveData != null && o3.saveData.Forge != null)
                {
                    var forge = o3.saveData.Forge;
                    var chunkName = core.chunkResource != null ? core.chunkResource.name : "Chunk";
                    if (!forge.ChunksCraftedByResource.ContainsKey(chunkName)) forge.ChunksCraftedByResource[chunkName] = 0;
                    forge.ChunksCraftedByResource[chunkName] += amount;
                }
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after chunk craft failed: {ex}");
            }
        }
    }
}