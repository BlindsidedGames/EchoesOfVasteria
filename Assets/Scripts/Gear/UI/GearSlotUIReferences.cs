using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Reference holder for a single Gear slot in the equipment UI.
    /// Shows the crafted item sprite when available, based on item rarity and slot type.
    /// </summary>
    public class GearSlotUIReferences : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Logical slot name this UI represents (e.g., Weapon, Helmet, Chest, Boots). Must match EquipmentController.Slots entry.")]
        [SerializeField] private string slotName;

        [Header("Wiring")]
        [SerializeField] private Button selectSlotButton;
        [SerializeField] private Image selectionImage;
        [SerializeField] private Image gearImage;

        [Header("Sprites by Rarity (index = rarity.tierIndex 0..7)")]
        [Tooltip("Sprites to use for this slot, indexed by RaritySO.tierIndex.")]
        [SerializeField] private List<Sprite> spritesByRarity = new List<Sprite>(8);

        public Button SelectSlotButton => selectSlotButton;
        public Image SelectionImage => selectionImage;
        public Image GearImage => gearImage;
        public string SlotName => slotName;

        /// <summary>
        /// Clears the gear icon (e.g., before a craft has occurred).
        /// </summary>
        public void ClearGearSprite()
        {
            if (gearImage != null)
                gearImage.enabled = false;
        }

        /// <summary>
        /// Applies the appropriate sprite based on the provided item. The image is disabled if sprite missing.
        /// </summary>
        public void ApplyGearSprite(GearItem item)
        {
            if (gearImage == null)
                return;

            if (item == null || item.rarity == null)
            {
                gearImage.enabled = false;
                return;
            }

            Sprite sprite = null;
            var idx = Mathf.Clamp(item.rarity.tierIndex, 0, spritesByRarity.Count > 0 ? spritesByRarity.Count - 1 : 0);
            if (spritesByRarity != null && idx >= 0 && idx < spritesByRarity.Count)
                sprite = spritesByRarity[idx];

            gearImage.sprite = sprite;
            gearImage.enabled = sprite != null;
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionImage != null)
                selectionImage.enabled = isSelected;
        }

        /// <summary>
        /// Returns the sprite this slot would use for the provided item based on its rarity.
        /// </summary>
        public Sprite GetSpriteForItem(GearItem item)
        {
            if (item == null || item.rarity == null)
                return null;
            if (spritesByRarity == null || spritesByRarity.Count == 0)
                return null;
            var idx = Mathf.Clamp(item.rarity.tierIndex, 0, spritesByRarity.Count - 1);
            return idx >= 0 && idx < spritesByRarity.Count ? spritesByRarity[idx] : null;
        }
    }
}


