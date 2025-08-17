using System.Collections;
using UnityEngine;

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

                var eq = equipment?.GetEquipped(lastCrafted.slot);
                var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
                ShowResult(summary);
                UpdateResultPreview(lastCrafted);
                OnResourcesChanged();
                ForceRefreshAllCoreSlots();
                RefreshOdds();

                if (UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted,
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
    }
}