using System.Collections;
using System.Collections.Generic;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;
using TimelessEchoes.References.StatPanel;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays the player's resource amounts and allows selecting a resource slot.
    /// </summary>
    public class ResourceInventoryUI : MonoBehaviour
    {
        public static ResourceInventoryUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private ResourceManager resourceManager;
        [SerializeField] private List<Resource> resources = new();
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject inventoryWindow;
        [SerializeField] private ScrollRect scrollRect;
        public Sprite UnknownSprite;
        [SerializeField] private float highlightDuration = 3f;
        [SerializeField] private TMP_Text selectedResourceNameText;
        [SerializeField] private StatPanelReferences statPanelReferences;
        [SerializeField] private bool showTierBorder = true;
        [SerializeField] private bool showTierBackground = false;

        private readonly List<ResourceUIReferences> slots = new();

        private int selectedIndex = -1;
        private Coroutine highlightRoutine;

        private void Awake()
        {
            Instance = this;
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);

            // Cache StatPanelReferences if not assigned in Inspector
            if (statPanelReferences == null)
                statPanelReferences = FindAnyObjectByType<StatPanelReferences>();

            if (slotParent == null)
                slotParent = transform;

            slots.Clear();
            foreach (var res in resources)
            {
                if (res == null || slotPrefab == null) continue;
                var slot = Instantiate(slotPrefab, slotParent);
                slots.Add(slot);
                var index = slots.Count - 1;
                if (slot.highlightButton != null)
                {
                    var r = res;
                    slot.highlightButton.onClick.RemoveAllListeners();
                    slot.highlightButton.onClick.AddListener(() => HighlightResource(r, false));
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
            if (highlightRoutine != null)
            {
                StopCoroutine(highlightRoutine);
                highlightRoutine = null;
            }

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

            if (slot.iconImage)
            {
                slot.iconImage.sprite = unlocked ? resource?.icon : resource?.UnknownIcon;
                slot.iconImage.color = Color.white;
                slot.iconImage.enabled = true;
                slot.gameObject.name = resource.name;
            }

            if (slot.countText)
            {
                slot.countText.text = FormatNumber(amount, true);
                slot.countText.gameObject.SetActive(true);
            }

            // Compute tier once for both visuals (treat Tier 0 as Tier 1)
            int resolvedTier = 1;
            if (unlocked)
            {
                if (resource != null && resource.DisableAlterEcho && statPanelReferences != null && statPanelReferences.tierBackgroundSprites != null)
                {
                    // Crafted-only: display max tier visuals
                    resolvedTier = Mathf.Max(1, statPanelReferences.tierBackgroundSprites.Count);
                }
                else
                {
                    resolvedTier = resourceManager != null ? resourceManager.GetTier(resource) : 1;
                }
            }
            // Ensure Tier 0 is treated as Tier 1 for visuals
            if (resolvedTier < 1) resolvedTier = 1;

            // Tier border image
            if (slot.tierBorderImage != null)
            {
                var borderSprites = statPanelReferences != null ? statPanelReferences.tierBorderSprites : null;
                if (borderSprites != null && borderSprites.Count > 0)
                {
                    // If unchecked, default to Tier 1 (index 0)
                    var bIdx = showTierBorder ? Mathf.Clamp(resolvedTier - 1, 0, borderSprites.Count - 1) : 0;
                    slot.tierBorderImage.sprite = (bIdx >= 0 && bIdx < borderSprites.Count) ? borderSprites[bIdx] : null;
                    slot.tierBorderImage.enabled = slot.tierBorderImage.sprite != null;
                }
                else
                {
                    slot.tierBorderImage.enabled = false;
                }
            }

            // Tier background image (new field on slot)
            if (slot.newTierBackgroundImage != null)
            {
                var bgSprites = statPanelReferences != null ? statPanelReferences.tierBackgroundSprites : null;
                if (bgSprites != null && bgSprites.Count > 0)
                {
                    // If unchecked, default to Tier 1 (index 0)
                    var gIdx = showTierBackground ? Mathf.Clamp(resolvedTier - 1, 0, bgSprites.Count - 1) : 0;
                    slot.newTierBackgroundImage.sprite = (gIdx >= 0 && gIdx < bgSprites.Count) ? bgSprites[gIdx] : null;
                    slot.newTierBackgroundImage.enabled = slot.newTierBackgroundImage.sprite != null;
                }
                else
                {
                    slot.newTierBackgroundImage.enabled = false;
                }
            }
        }

        public void SelectSlot(int index, bool scrollToSlot = true)
        {
            selectedIndex = index;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].selectionImage != null)
                    slots[i].selectionImage.enabled = i == selectedIndex;

            if (selectedResourceNameText != null && index >= 0 && index < resources.Count)
            {
                var res = resources[index];
                if (res)
                {
                    var unlocked = resourceManager && resourceManager.IsUnlocked(res);
                    if (unlocked)
                    {
                        // Mirror visual tier logic used in UpdateSlot
                        int resolvedTier = 1;
                        if (res.DisableAlterEcho && statPanelReferences != null && statPanelReferences.tierBackgroundSprites != null)
                        {
                            // Crafted-only: display max configured visual tier
                            resolvedTier = Mathf.Max(1, statPanelReferences.tierBackgroundSprites.Count);
                        }
                        else
                        {
                            resolvedTier = resourceManager != null ? resourceManager.GetTier(res) : 1;
                        }
                        if (resolvedTier < 1) resolvedTier = 1; // force to 1 if zero or less

                        selectedResourceNameText.text = $"{res.name} - Tier {resolvedTier}";
                    }
                    else
                    {
                        selectedResourceNameText.text = "???";
                    }
                }
                else
                {
                    selectedResourceNameText.text = string.Empty;
                }
            }

            if (scrollToSlot)
                ScrollToSlot(selectedIndex);
        }

        public void HighlightResource(Resource resource, bool scrollToSlot = true)
        {
            var index = resources.IndexOf(resource);
            if (index < 0)
                index = resources.FindIndex(r => r != null && resource != null && r.name == resource.name);
            if (index >= 0)
            {
                if (inventoryWindow != null && !inventoryWindow.activeSelf)
                    inventoryWindow.SetActive(true);
                SelectSlot(index, scrollToSlot);
                if (highlightDuration > 0f)
                {
                    if (highlightRoutine != null)
                        StopCoroutine(highlightRoutine);
                    highlightRoutine = StartCoroutine(DelayedDeselect(index, highlightDuration));
                }
            }
        }

        private IEnumerator DelayedDeselect(int index, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (index >= 0 && index < slots.Count && slots[index] != null && slots[index].selectionImage != null)
                slots[index].selectionImage.enabled = false;
            selectedIndex = -1;
            highlightRoutine = null;
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
