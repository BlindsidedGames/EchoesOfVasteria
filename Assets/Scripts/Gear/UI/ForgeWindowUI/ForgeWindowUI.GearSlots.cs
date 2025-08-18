using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
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

        // Called by UI slot buttons (e.g., Weapon/Helmet/Chest/Boots)
        public void SelectSlot(string slot)
        {
            if (lastCrafted != null && !string.Equals(lastCrafted.slot, slot))
            {
                SalvageService.Instance?.Salvage(lastCrafted);
                lastCrafted = null;
                if (resultText != null) resultText.text = string.Empty;
                ClearResultPreview();
            }

            // Stop auto-crafting if the player changes the selected gear slot
            if (isAutoCrafting && !string.Equals(selectedSlot, slot))
                StopAutoCrafting();
            selectedSlot = slot;
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
                {
                    // If migrated helmet has no rarity, show migrated sprite
                    if (string.Equals(name, "Helmet") && (item.rarity == null) && slotRef.GearImage != null)
                    {
                        if (migratedHelmetSprite != null)
                        {
                            slotRef.GearImage.sprite = migratedHelmetSprite;
                            slotRef.GearImage.enabled = true;
                        }
                        else
                        {
                            slotRef.ApplyGearSprite(item);
                        }
                    }
                    else
                    {
                        slotRef.ApplyGearSprite(item);
                    }
                }
                else
                {
                    // If unknown state, show the unknown sprite for this slot and set native size
                    var order = new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
                    var idx = order.IndexOf(name);
                    if (idx >= 0 && idx < unknownGearSprites.Count && slotRef.GearImage != null)
                    {
                        slotRef.GearImage.sprite = unknownGearSprites[idx];
                        slotRef.GearImage.enabled = true;
                    }
                    else
                    {
                        slotRef.ClearGearSprite();
                    }
                }
            }
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
            selectedSlotStatsText.text = GearStatTextBuilder.BuildEquippedStatsText(equipped, selectedSlot);
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
