using System.Collections;
using System.Collections.Generic;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays the player's resource amounts and allows selecting a resource slot.
    /// </summary>
    public class ResourceInventoryUI : MonoBehaviour
    {
        public static ResourceInventoryUI Instance { get; private set; }
        private ResourceManager resourceManager;
        [SerializeField] private List<Resource> resources = new();
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject inventoryWindow;
        [SerializeField] private ScrollRect scrollRect;
        public Sprite UnknownSprite;
        [SerializeField] private float highlightDuration = 3f;
        [SerializeField] private TMP_Text selectedResourceNameText;

        private readonly List<ResourceUIReferences> slots = new();

        private int selectedIndex = -1;

        private void Awake()
        {
            Instance = this;
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);

            if (slotParent == null)
                slotParent = transform;

            slots.Clear();
            foreach (var res in resources)
            {
                if (res == null || slotPrefab == null) continue;
                var slot = Instantiate(slotPrefab, slotParent);
                slots.Add(slot);
                var index = slots.Count - 1;
                slot.PointerClick += (_, button) =>
                {
                    if (button == PointerEventData.InputButton.Left)
                        SelectSlot(index);
                };
                if (slot.highlightButton != null)
                {
                    var r = res;
                    slot.highlightButton.onClick.RemoveAllListeners();
                    slot.highlightButton.onClick.AddListener(() => HighlightResource(r));
                }
                if (slot.countText != null)
                    slot.countText.gameObject.SetActive(true);
            }

            if (selectedResourceNameText != null)
                selectedResourceNameText.text = string.Empty;

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
            DeselectSlot();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void DeselectSlot()
        {
            selectedIndex = -1;
            foreach (var slot in slots)
                if (slot != null && slot.selectionImage != null)
                    slot.selectionImage.enabled = false;
            if (selectedResourceNameText != null)
                selectedResourceNameText.text = string.Empty;
        }


        /// <summary>
        ///     Updates all resource slots using the current ResourceManager values.
        /// </summary>
        public void UpdateSlots()
        {
            for (var i = 0; i < slots.Count && i < resources.Count; i++)
                UpdateSlot(i);
        }

        private void UpdateSlot(int index)
        {
            var slot = slots[index];
            var resource = resources[index];
            if (slot == null) return;

            var amount = resourceManager ? resourceManager.GetAmount(resource) : 0;
            var unlocked = resourceManager && resourceManager.IsUnlocked(resource);

            var unknownColor = new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
            if (slot.iconImage)
            {
                slot.iconImage.sprite = unlocked ? resource?.icon : UnknownSprite;
                slot.iconImage.color = unlocked ? Color.white : unknownColor;
                slot.iconImage.enabled = true;
                slot.gameObject.name = resource.name;
            }

            if (slot.countText)
            {
                slot.countText.text = FormatNumber(amount, true);
                slot.countText.gameObject.SetActive(true);
            }
        }

        public void SelectSlot(int index)
        {
            selectedIndex = index;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].selectionImage != null)
                    slots[i].selectionImage.enabled = i == selectedIndex;

            if (selectedResourceNameText != null && index >= 0 && index < resources.Count)
            {
                var res = resources[index];
                selectedResourceNameText.text = res ? res.name : string.Empty;
            }

            ScrollToSlot(selectedIndex);
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
                if (highlightDuration > 0f)
                    StartCoroutine(DelayedDeselect(index, highlightDuration));
            }
        }

        private IEnumerator DelayedDeselect(int index, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (index >= 0 && index < slots.Count && slots[index] != null && slots[index].selectionImage != null)
                slots[index].selectionImage.enabled = false;
            selectedIndex = -1;
            if (selectedResourceNameText != null)
                selectedResourceNameText.text = string.Empty;
        }

        private void ScrollToSlot(int index)
        {
            if (scrollRect == null || scrollRect.content == null) return;
            if (index < 0 || index >= slots.Count) return;

            var slotRT = slots[index].GetComponent<RectTransform>();
            var content = scrollRect.content;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
            if (slotRT == null || content == null || viewport == null) return;

            Canvas.ForceUpdateCanvases();
            var contentHeight = content.rect.height - viewport.rect.height;
            if (contentHeight <= 0f) return;

            var itemPos = Mathf.Abs(slotRT.anchoredPosition.y);
            var normalized = Mathf.Clamp01(itemPos / contentHeight);
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }
    }
}