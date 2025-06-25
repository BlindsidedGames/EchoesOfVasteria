using System.Collections.Generic;
using References.UI;
using UnityEngine;

using Blindsided.Utilities;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Displays the player's resource amounts and allows selecting a resource slot.
    /// </summary>
    public class ResourceInventoryUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private List<Resource> resources = new();
        [SerializeField] private SelectableSlotManager slotManager;

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (slotManager == null)
                slotManager = GetComponent<SelectableSlotManager>();
            if (slotManager != null)
                slotManager.TooltipRequested += ShowTooltip;
            UpdateSlots();
        }

        private void OnEnable()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += UpdateSlots;
            UpdateSlots();
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= UpdateSlots;
        }

        /// <summary>
        /// Updates all resource slots using the current ResourceManager values.
        /// </summary>
        public void UpdateSlots()
        {
            if (slotManager == null) return;

            for (int i = 0; i < slotManager.Slots.Count && i < resources.Count; i++)
                UpdateSlot(i);

            if (slotManager.SelectedIndex >= 0 && slotManager.Tooltip != null && slotManager.Tooltip.gameObject.activeSelf)
                ShowTooltip(slotManager.SelectedIndex);
        }

        private void UpdateSlot(int index)
        {
            if (slotManager == null) return;
            var slot = slotManager.Slots[index] as ResourceUIReferences;
            var resource = resources[index];
            if (slot == null) return;

            double amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            bool unlocked = resourceManager && resourceManager.IsUnlocked(resource);

            if (slot.iconImage)
            {
                slot.iconImage.sprite = resource ? resource.icon : null;
                slot.iconImage.enabled = unlocked;
            }

            if (slot.questionMarkImage)
                slot.questionMarkImage.enabled = !unlocked;
            if (slot.countText)
                slot.countText.gameObject.SetActive(false);
        }

        public void HighlightResource(Resource resource)
        {
            int index = resources.IndexOf(resource);
            if (index >= 0 && slotManager != null)
                slotManager.Select(index);
        }

        private void ShowTooltip(int index)
        {
            if (slotManager == null || slotManager.Tooltip == null)
                return;

            if (index < 0 || index >= slotManager.Slots.Count || index >= resources.Count)
            {
                slotManager.HideTooltip();
                return;
            }

            var resource = resources[index];

            bool unlocked = resourceManager && resourceManager.IsUnlocked(resource);
            if (slotManager.Tooltip.resourceNameText)
                slotManager.Tooltip.resourceNameText.text = unlocked && resource ? resource.name : "Undiscovered";

            double amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            if (slotManager.Tooltip.resourceCountText)
                slotManager.Tooltip.resourceCountText.text = CalcUtils.FormatNumber(amount, true, 400f, false);
        }
    }
}
