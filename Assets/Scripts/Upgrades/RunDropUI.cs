using System.Collections.Generic;
using References.UI;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Displays resources dropped during the current run.
    /// </summary>
    public class RunDropUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject displayObject;
        [SerializeField] private SelectableSlotManager slotManager;

        private readonly List<Resource> resources = new();
        private readonly List<ResourceUIReferences> slots = new();
        private readonly Dictionary<Resource, double> amounts = new();

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (slotParent == null)
                slotParent = transform;
            if (displayObject == null)
                displayObject = gameObject;
            if (slotManager == null)
                slotManager = GetComponent<SelectableSlotManager>();
            if (slotManager != null)
                slotManager.TooltipRequested += ShowTooltip;
            ClearDrops();
        }

        private void OnEnable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded += OnResourceAdded;
            ClearDrops();
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded -= OnResourceAdded;
        }

        private void ClearDrops()
        {
            foreach (var slot in slots)
                if (slot != null)
                    Destroy(slot.gameObject);
            resources.Clear();
            slots.Clear();
            amounts.Clear();
            slotManager?.ClearSlots();
            if (displayObject != null)
                displayObject.SetActive(false);
        }

        private void OnResourceAdded(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return;
            if (!amounts.ContainsKey(resource))
            {
                amounts[resource] = amount;
                resources.Add(resource);
                var slot = Instantiate(slotPrefab, slotParent);
                slots.Add(slot);
                slotManager?.AddSlot(slot);
            }
            else
            {
                amounts[resource] += amount;
            }
            if (displayObject != null)
                displayObject.SetActive(true);
            UpdateSlot(resources.IndexOf(resource));
        }

        private void UpdateSlot(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            var slot = slots[index];
            var resource = resources[index];
            if (slot == null) return;

            if (slot.iconImage)
            {
                slot.iconImage.sprite = resource ? resource.icon : null;
                slot.iconImage.enabled = true;
            }
            if (slot.questionMarkImage)
                slot.questionMarkImage.enabled = false;
            if (slot.countText)
                slot.countText.gameObject.SetActive(false);
            if (slot.selectionImage)
                slot.selectionImage.enabled = index == (slotManager ? slotManager.SelectedIndex : -1);

            if (slotManager != null && slotManager.SelectedIndex == index && slotManager.Tooltip != null && slotManager.Tooltip.gameObject.activeSelf)
                ShowTooltip(index);
        }


        private void ShowTooltip(int index)
        {
            if (slotManager == null || slotManager.Tooltip == null)
                return;
            if (index < 0 || index >= slots.Count || index >= resources.Count)
            {
                slotManager.HideTooltip();
                return;
            }
            var slot = slots[index];
            var resource = resources[index];
            slotManager.Tooltip.transform.position = slot.transform.position;
            if (slotManager.Tooltip.resourceNameText)
                slotManager.Tooltip.resourceNameText.text = resource ? resource.name : string.Empty;
            if (slotManager.Tooltip.resourceCountText)
            {
                double count = amounts.TryGetValue(resource, out var val) ? val : 0;
                slotManager.Tooltip.resourceCountText.text = count.ToString();
            }
        }
    }
}
