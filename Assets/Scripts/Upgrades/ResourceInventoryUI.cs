using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Displays the player's resource amounts and allows selecting a resource slot.
    /// </summary>
    public class ResourceInventoryUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private List<Resource> resources = new();
        [SerializeField] private List<ResourceUIReferences> slots = new();
        [SerializeField] private TooltipUIReferences tooltip;

        private int selectedIndex = -1;

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();

            if (tooltip == null)
                tooltip = FindFirstObjectByType<TooltipUIReferences>();

            if (slots.Count == 0)
                slots.AddRange(GetComponentsInChildren<ResourceUIReferences>(true));

            for (int i = 0; i < slots.Count; i++)
            {
                var index = i;
                if (slots[i] != null && slots[i].selectButton != null)
                    slots[i].selectButton.onClick.AddListener(() => SelectSlot(index));
            }

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
            for (int i = 0; i < slots.Count && i < resources.Count; i++)
                UpdateSlot(i);
        }

        private void UpdateSlot(int index)
        {
            var slot = slots[index];
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
                slot.countText.text = amount.ToString();
        }

        public void SelectSlot(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].selectionImage != null)
                    slots[i].selectionImage.enabled = i == selectedIndex;
            }

            ShowTooltip(selectedIndex);
        }

        public void HighlightResource(Resource resource)
        {
            int index = resources.IndexOf(resource);
            if (index >= 0)
                SelectSlot(index);
        }

        private void ShowTooltip(int index)
        {
            if (tooltip == null)
                return;

            if (index < 0 || index >= slots.Count || index >= resources.Count)
            {
                tooltip.gameObject.SetActive(false);
                return;
            }

            var slot = slots[index];
            var resource = resources[index];

            tooltip.transform.position = slot.transform.position;

            bool unlocked = resourceManager && resourceManager.IsUnlocked(resource);
            if (tooltip.resourceNameText)
                tooltip.resourceNameText.text = unlocked && resource ? resource.name : "Undiscovered";

            double amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            if (tooltip.resourceCountText)
                tooltip.resourceCountText.text = amount.ToString();

            tooltip.gameObject.SetActive(true);
        }
    }
}
