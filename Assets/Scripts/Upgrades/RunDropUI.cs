using System.Collections;
using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using TimelessEchoes.Upgrades;

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
        [SerializeField] private TooltipUIReferences tooltip;
        [SerializeField] private GameObject displayObject;
        [SerializeField] private bool showTooltipOnHover = false;
        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;

        private readonly List<Resource> resources = new();
        private readonly List<ResourceUIReferences> slots = new();
        private readonly Dictionary<Resource, double> amounts = new();
        private int selectedIndex = -1;

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (tooltip == null)
                tooltip = FindFirstObjectByType<TooltipUIReferences>();
            if (slotParent == null)
                slotParent = transform;
            if (displayObject == null)
                displayObject = gameObject;
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

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (tooltip != null && tooltip.gameObject.activeSelf)
                    tooltip.gameObject.SetActive(false);
                DeselectSlot();
            }
        }

        private void ClearDrops()
        {
            foreach (var slot in slots)
                if (slot != null)
                    Destroy(slot.gameObject);
            resources.Clear();
            slots.Clear();
            amounts.Clear();
            selectedIndex = -1;
            if (displayObject != null)
                displayObject.SetActive(false);
        }

        private void DeselectSlot()
        {
            selectedIndex = -1;
            foreach (var slot in slots)
                if (slot != null && slot.selectionImage != null)
                    slot.selectionImage.enabled = false;
        }

        private void OnResourceAdded(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return;

            ResourceUIReferences slot;
            int index;
            bool newSlot = false;

            if (!amounts.ContainsKey(resource))
            {
                amounts[resource] = amount;
                resources.Add(resource);
                slot = Instantiate(slotPrefab, slotParent);
                index = slots.Count;
                slots.Add(slot);
                newSlot = true;

                if (slot != null && slot.selectButton != null)
                    slot.selectButton.onClick.AddListener(() => SelectSlot(index));

                if (slot != null)
                {
                    slot.PointerClick += (_, button) =>
                    {
                        if (button == PointerEventData.InputButton.Right && tooltip != null)
                            tooltip.gameObject.SetActive(false);
                        if (slot.highlightImage != null)
                            slot.highlightImage.enabled = false;
                    };
                    slot.PointerEnter += _ =>
                    {
                        if (showTooltipOnHover)
                            ShowTooltip(index);
                        if (slot.highlightImage != null)
                            slot.highlightImage.enabled = false;
                    };
                    slot.PointerExit += _ =>
                    {
                        if (showTooltipOnHover && tooltip != null)
                            tooltip.gameObject.SetActive(false);
                    };
                }
            }
            else
            {
                amounts[resource] += amount;
                index = resources.IndexOf(resource);
                slot = index >= 0 && index < slots.Count ? slots[index] : null;
            }

            if (slot != null && slot.highlightImage != null)
                slot.highlightImage.enabled = true;

            if (displayObject != null)
                displayObject.SetActive(true);

            UpdateSlot(resources.IndexOf(resource));

            if (slot != null)
            {
                if (newSlot)
                    StartCoroutine(SpawnFloatingTextNextFrame(slot, amount));
                else
                    FloatingText.Spawn($"+{Mathf.FloorToInt((float)amount)}", slot.transform.position + Vector3.up, Color.white, 8f, transform);
            }
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
                slot.selectionImage.enabled = index == selectedIndex;

            if (selectedIndex == index && tooltip != null && tooltip.gameObject.activeSelf)
                ShowTooltip(index);
        }

        private void SelectSlot(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].selectionImage != null)
                    slots[i].selectionImage.enabled = i == selectedIndex;
            ShowTooltip(selectedIndex);
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
            if (tooltip.resourceNameText)
                tooltip.resourceNameText.text = resource ? resource.name : string.Empty;
            if (tooltip.resourceCountText)
            {
                double count = amounts.TryGetValue(resource, out var val) ? val : 0;
                tooltip.resourceCountText.text = count.ToString();
            }
            tooltip.gameObject.SetActive(true);
        }

        private IEnumerator SpawnFloatingTextNextFrame(ResourceUIReferences slot, double amount)
        {
            yield return null; // wait one frame for layout groups to update
            if (slot != null)
                FloatingText.Spawn($"+{Mathf.FloorToInt((float)amount)}", slot.transform.position + Vector3.up, Color.white, 8f, transform);
        }
    }
}
