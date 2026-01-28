using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Manages the result preview display in the forge UI.
    /// </summary>
    public class ForgeResultPreview : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text resultTierText;
        [SerializeField] private Image resultImage;
        [SerializeField] private List<Sprite> unknownGearSprites = new();
        [SerializeField] private Sprite migratedHelmetSprite;

        private static readonly string[] SlotOrder = { "Weapon", "Helmet", "Chest", "Boots" };

        /// <summary>
        /// Shows the result of a crafted item with comparison to equipped.
        /// </summary>
        public void ShowResult(GearItem item, GearItem equipped, CraftingService crafting)
        {
            if (item == null)
            {
                Clear();
                return;
            }

            // Build result text
            if (resultText != null)
                resultText.text = GearStatTextBuilder.BuildCraftResultSummary(item, equipped);

            // Set rarity text color
            ApplyTierText(item.rarity);

            // Set result image
            UpdateResultImage(item);
        }

        /// <summary>
        /// Shows an unknown result preview for a slot.
        /// </summary>
        public void ShowUnknown(string slot)
        {
            if (resultText != null)
                resultText.text = string.Empty;

            if (resultTierText != null)
                resultTierText.text = string.Empty;

            if (resultImage != null)
            {
                var idx = GetSlotIndex(slot);
                if (idx >= 0 && idx < unknownGearSprites.Count && unknownGearSprites[idx] != null)
                {
                    resultImage.sprite = unknownGearSprites[idx];
                    resultImage.enabled = true;
                }
                else
                {
                    resultImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// Clears the result preview.
        /// </summary>
        public void Clear()
        {
            if (resultText != null)
                resultText.text = string.Empty;

            if (resultTierText != null)
                resultTierText.text = string.Empty;

            if (resultImage != null)
                resultImage.enabled = false;
        }

        /// <summary>
        /// Applies the tier/rarity text styling.
        /// </summary>
        public void ApplyTierText(RaritySO rarity)
        {
            if (resultTierText == null) return;

            if (rarity == null)
            {
                resultTierText.text = string.Empty;
                return;
            }

            resultTierText.text = rarity.name;
            resultTierText.color = rarity.color;
        }

        private void UpdateResultImage(GearItem item)
        {
            if (resultImage == null) return;

            // Try to get sprite from rarity + slot
            var sprite = GetSpriteForItem(item);
            if (sprite != null)
            {
                resultImage.sprite = sprite;
                resultImage.enabled = true;
            }
            else
            {
                // Fallback to unknown sprite
                var idx = GetSlotIndex(item?.slot);
                if (idx >= 0 && idx < unknownGearSprites.Count && unknownGearSprites[idx] != null)
                {
                    resultImage.sprite = unknownGearSprites[idx];
                    resultImage.enabled = true;
                }
                else
                {
                    resultImage.enabled = false;
                }
            }
        }

        private Sprite GetSpriteForItem(GearItem item)
        {
            if (item == null) return null;

            var slot = item.slot ?? string.Empty;

            // Check for migrated helmet case
            if (string.Equals(slot, "Helmet", StringComparison.OrdinalIgnoreCase) && migratedHelmetSprite != null)
                return migratedHelmetSprite;

            // No rarity-based sprites - use slot-based unknown sprites via fallback
            return null;
        }

        private static int GetSlotIndex(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return -1;

            for (var i = 0; i < SlotOrder.Length; i++)
            {
                if (string.Equals(SlotOrder[i], slot, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }
    }
}
