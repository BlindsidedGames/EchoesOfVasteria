using System.Collections;
using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using TimelessEchoes.Upgrades;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Displays resources dropped during the current run.
    /// </summary>
    public class RunDropUI : MonoBehaviour
    {
        private ResourceManager resourceManager;
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private TooltipUIReferences tooltip;
        [SerializeField] private GameObject displayObject;
        [SerializeField] private bool showTooltipOnHover = false;
        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;
        [SerializeField] [Min(1)] private int maxVisibleDrops = 5;

        private readonly List<Resource> resources = new();
        private readonly List<ResourceUIReferences> slots = new();
        private readonly Dictionary<Resource, double> amounts = new();
        /// <summary>
        /// Current amounts collected during this run.
        /// </summary>
        public IReadOnlyDictionary<Resource, double> Amounts => amounts;
        private int selectedIndex = -1;

        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
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

        /// <summary>
        ///     Clears all collected resource counts.
        /// </summary>
        public void ResetDrops()
        {
            ClearDrops();
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

            ResourceUIReferences slot = null;
            bool newSlot = false;
            bool removedSelected = false;
            int index = resources.IndexOf(resource);
            bool moved = index > 0;

            if (index >= 0)
            {
                amounts[resource] += amount;
                slot = slots[index];
                if (index > 0)
                {
                    resources.RemoveAt(index);
                    slots.RemoveAt(index);
                    resources.Insert(0, resource);
                    slots.Insert(0, slot);
                    slot.transform.SetSiblingIndex(0);

                    if (selectedIndex >= 0)
                    {
                        if (selectedIndex < index)
                            selectedIndex++;
                        else if (selectedIndex == index)
                            selectedIndex = 0;

                        if (tooltip != null && tooltip.gameObject.activeSelf)
                            ShowTooltip(selectedIndex);
                        else
                            SelectSlot(selectedIndex);
                    }
                }
            }
            else
            {
                double current = 0;
                if (amounts.TryGetValue(resource, out var val))
                    current = val;
                amounts[resource] = current + amount;

                if (resources.Count >= maxVisibleDrops)
                {
                    int removeIndex = resources.Count - 1;
                    resources.RemoveAt(removeIndex);
                    var removedSlot = slots[removeIndex];
                    if (removedSlot != null)
                        Destroy(removedSlot.gameObject);
                    slots.RemoveAt(removeIndex);

                    if (selectedIndex == removeIndex)
                    {
                        removedSelected = true;
                        selectedIndex = removeIndex - 1;
                    }
                    else if (selectedIndex > removeIndex)
                    {
                        selectedIndex--;
                    }
                }

                slot = Instantiate(slotPrefab, slotParent);
                resources.Insert(0, resource);
                slots.Insert(0, slot);
                slot.transform.SetSiblingIndex(0);
                newSlot = true;

                if (slot != null && slot.selectButton != null)
                    slot.selectButton.onClick.AddListener(() => SelectSlot(slots.IndexOf(slot)));

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
                            ShowTooltip(slots.IndexOf(slot));
                        if (slot.highlightImage != null)
                            slot.highlightImage.enabled = false;
                    };
                    slot.PointerExit += _ =>
                    {
                        if (showTooltipOnHover && tooltip != null)
                            tooltip.gameObject.SetActive(false);
                    };
                }

                if (selectedIndex >= 0 || removedSelected)
                    selectedIndex++;

                if (removedSelected)
                {
                    if (tooltip != null && tooltip.gameObject.activeSelf)
                        ShowTooltip(selectedIndex);
                    else
                        SelectSlot(selectedIndex);
                }
            }

            if (slot != null && slot.highlightImage != null)
                slot.highlightImage.enabled = true;

            if (displayObject != null)
                displayObject.SetActive(true);

            UpdateSlot(resources.IndexOf(resource));

            if (slot != null)
            {
                if (newSlot || moved)
                    StartCoroutine(SpawnFloatingTextNextFrame(slot, amount));
                else
                    FloatingText.Spawn(
                        $"+{Mathf.FloorToInt((float)amount)}",
                        slot.transform.position + Vector3.up,
                        Color.white, 8f, transform);
            }

            if (tooltip != null && tooltip.gameObject.activeSelf)
                StartCoroutine(DelayedTooltipUpdate());
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

        private IEnumerator DelayedTooltipUpdate()
        {
            yield return null;
            if (selectedIndex >= 0 && tooltip != null && tooltip.gameObject.activeSelf)
                ShowTooltip(selectedIndex);
        }
    }
}
