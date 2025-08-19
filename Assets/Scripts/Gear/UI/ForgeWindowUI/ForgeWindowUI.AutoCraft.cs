using System.Collections;
using UnityEngine;
using Blindsided.SaveData;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
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

            RefreshActionButtons();
        }

        private IEnumerator CraftUntilUpgradeCoroutine()
        {
            var wait = new WaitForSecondsRealtime(0.1f); // ~10 crafts per second
            while (isAutoCrafting)
            {
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
                    break;
                }

                // Auto-salvage previous craft before rolling a new one
                if (lastCrafted != null)
                {
                    SalvageService.Instance?.Salvage(lastCrafted, isAuto: true);
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

                // Count autocraft craft
                {
                    var o2 = Blindsided.Oracle.oracle;
                    if (o2 != null && o2.saveData != null && o2.saveData.Forge != null)
                        o2.saveData.Forge.AutocraftCrafts++;
                }

                var eq = equipment?.GetEquipped(lastCrafted.slot);
                var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
                ShowResult(summary);
                UpdateResultPreview(lastCrafted);
                OnResourcesChanged();
                ForceRefreshAllCoreSlots();
                RefreshOdds();

                if (UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted,
                        eq))
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
                    break; // leave lastCrafted for player to review/replace/salvage
                }

                if (StaticReferences.StopAutocraftOnVastium &&
                    lastCrafted?.rarity?.GetName() == "Vastium")
                {
                    var o3 = Blindsided.Oracle.oracle;
                    if (o3 != null && o3.saveData != null && o3.saveData.Forge != null)
                    {
                        var forge = o3.saveData.Forge;
                        if (!forge.AutocraftStopReasons.ContainsKey("Vastium")) forge.AutocraftStopReasons["Vastium"] = 0;
                        forge.AutocraftStopReasons["Vastium"]++;
                    }
                    break;
                }

                // Not an upgrade, salvage and continue
                SalvageService.Instance?.Salvage(lastCrafted, isAuto: true);
                lastCrafted = null;
                RefreshActionButtons();
                yield return wait;
            }

            isAutoCrafting = false;
            autoCraftCoroutine = null;
            RefreshActionButtons();
        }
    }
}