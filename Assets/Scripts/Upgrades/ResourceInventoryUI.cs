using System.Collections.Generic;
using Blindsided.Utilities;
using References.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays the player's resource amounts and allows selecting a resource slot.
    /// </summary>
    public class ResourceInventoryUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private List<Resource> resources = new();
        [SerializeField] private List<ResourceUIReferences> slots = new();
        [SerializeField] private GameObject inventoryWindow;
        [SerializeField] private TooltipUIReferences tooltip;
        [SerializeField] private bool showTooltipOnHover;
        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;
        [SerializeField] [HideInInspector] private bool iconsVisible = true;

        private int selectedIndex = -1;

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();

            if (tooltip == null)
                tooltip = FindFirstObjectByType<TooltipUIReferences>();

            if (slots.Count == 0)
                slots.AddRange(GetComponentsInChildren<ResourceUIReferences>(true));

            for (var i = 0; i < slots.Count; i++)
            {
                var index = i;
                if (slots[i] != null && slots[i].selectButton != null)
                    slots[i].selectButton.onClick.AddListener(() => SelectSlot(index));

                if (slots[i] != null)
                {
                    if (slots[i].countText != null)
                        slots[i].countText.gameObject.SetActive(false);

                    slots[i].PointerClick += (_, button) =>
                    {
                        if (button == PointerEventData.InputButton.Right && tooltip != null)
                            tooltip.gameObject.SetActive(false);
                    };

                    slots[i].PointerEnter += _ =>
                    {
                        if (showTooltipOnHover)
                            ShowTooltip(index);
                    };

                    slots[i].PointerExit += _ =>
                    {
                        if (showTooltipOnHover && tooltip != null)
                            tooltip.gameObject.SetActive(false);
                    };
                }
            }

            UpdateSlots();
        }

        private void OnEnable()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += UpdateSlots;
            UpdateSlots();

            if (selectedIndex >= 0)
                ShowTooltip(selectedIndex);
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= UpdateSlots;

            if (tooltip != null)
                tooltip.gameObject.SetActive(false);

            DeselectSlot();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (tooltip != null && tooltip.gameObject.activeSelf)
                    tooltip.gameObject.SetActive(false);
                DeselectSlot();
            }
        }

        private void DeselectSlot()
        {
            selectedIndex = -1;
            foreach (var slot in slots)
                if (slot != null && slot.selectionImage != null)
                    slot.selectionImage.enabled = false;
        }

        [Button(ButtonSizes.Medium)]
        private void ToggleIcons()
        {
            iconsVisible = !iconsVisible;
            UpdateSlots();
        }

        /// <summary>
        ///     Updates all resource slots using the current ResourceManager values.
        /// </summary>
        public void UpdateSlots()
        {
            for (var i = 0; i < slots.Count && i < resources.Count; i++)
                UpdateSlot(i);

            if (selectedIndex >= 0 && tooltip != null && tooltip.gameObject.activeSelf)
                ShowTooltip(selectedIndex);
        }

        private void UpdateSlot(int index)
        {
            var slot = slots[index];
            var resource = resources[index];
            if (slot == null) return;

            var amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            var unlocked = resourceManager && resourceManager.IsUnlocked(resource);

            if (slot.iconImage)
            {
                slot.iconImage.sprite = resource ? resource.icon : null;
                slot.iconImage.enabled = unlocked;
            }

            if (slot.questionMarkImage)
                slot.questionMarkImage.enabled = !unlocked;
            if (slot.countText)
                slot.countText.gameObject.SetActive(false);
            if (iconsVisible)
            {
                slot.iconImage.enabled = true;
                slot.questionMarkImage.enabled = false;
                slot.gameObject.name = resource.name;
            }
        }

        public void SelectSlot(int index)
        {
            selectedIndex = index;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].selectionImage != null)
                    slots[i].selectionImage.enabled = i == selectedIndex;

            ShowTooltip(selectedIndex);
        }

        public void HighlightResource(Resource resource)
        {
            var index = resources.IndexOf(resource);
            if (index < 0)
                index = resources.FindIndex(r => r != null && resource != null && r.name == resource.name);
            if (index >= 0)
            {
                if (inventoryWindow != null && !inventoryWindow.activeSelf)
                    inventoryWindow.SetActive(true);
                SelectSlot(index);
            }
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

            tooltip.transform.position = slot.transform.position + (Vector3)tooltipOffset;

            var unlocked = resourceManager && resourceManager.IsUnlocked(resource);
            if (tooltip.resourceNameText)
                tooltip.resourceNameText.text = unlocked && resource ? resource.name : "Undiscovered";

            var amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            if (tooltip.resourceCountText)
                tooltip.resourceCountText.text = CalcUtils.FormatNumber(amount, true);

            tooltip.gameObject.SetActive(true);
        }
    }
}