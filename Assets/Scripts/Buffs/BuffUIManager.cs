using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Buffs
{
    public class BuffUIManager : MonoBehaviour
    {
        [SerializeField] private BuffManager controller;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceInventoryUI resourceInventoryUI;
        [SerializeField] private List<BuffRecipe> buffs = new();
        [SerializeField] private BuffRecipeUIReferences recipePrefab;
        [SerializeField] private Transform recipeParent;
        [SerializeField] private BuffIconUIReferences activeBuffPrefab;
        [SerializeField] private Transform activeBuffParent;
        [SerializeField] private Button openPurchaseButton;
        [SerializeField] private GameObject buffPurchaseWindow;

        private readonly Dictionary<BuffRecipe, BuffRecipeUIReferences> recipeEntries = new();
        private readonly List<ActiveIconEntry> iconEntries = new();

        private class ActiveIconEntry
        {
            public BuffManager.ActiveBuff buff;
            public BuffIconUIReferences refs;
        }

        private void Awake()
        {
            if (controller == null)
                controller = BuffManager.Instance ?? FindFirstObjectByType<BuffManager>();
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceInventoryUI == null)
                resourceInventoryUI = FindFirstObjectByType<ResourceInventoryUI>();

            BuildRecipeEntries();

            OnLoadData += OnLoadDataHandler;

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChanged;

            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(false);

            if (openPurchaseButton != null)
                openPurchaseButton.onClick.AddListener(OpenPurchaseWindow);
        }

        private void OnEnable()
        {
            UpdateActiveIcons();
            OnInventoryChanged();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChanged;
            OnLoadData -= OnLoadDataHandler;
        }

        private void Update()
        {
            for (int i = iconEntries.Count - 1; i >= 0; i--)
            {
                var entry = iconEntries[i];
                if (entry.buff.remaining <= 0f)
                {
                    Destroy(entry.refs.gameObject);
                    iconEntries.RemoveAt(i);
                    continue;
                }

                if (entry.refs.durationText != null)
                    entry.refs.durationText.text = Mathf.Ceil(entry.buff.remaining).ToString();
            }

            foreach (var pair in recipeEntries)
            {
                var panel = pair.Value;
                if (panel.durationText == null) continue;
                var recipe = pair.Key;
                float remaining = controller ? controller.GetRemaining(recipe) : 0f;
                float extra = recipe.baseDuration;
                if (controller != null && controller.DiminishingCurve != null)
                    extra *= controller.DiminishingCurve.Evaluate(remaining);
                panel.durationText.text =
                    $"{Mathf.Ceil(remaining)} -> {Mathf.Ceil(remaining + extra)}";
            }
        }

        private void BuildRecipeEntries()
        {
            if (recipePrefab == null || recipeParent == null)
                return;

            foreach (var recipe in buffs)
            {
                var panel = Instantiate(recipePrefab, recipeParent);
                if (panel.iconImage != null)
                    panel.iconImage.sprite = recipe.buffIcon;
                if (panel.durationText != null)
                    panel.durationText.text = recipe.baseDuration.ToString();
                if (panel.purchaseButton != null)
                {
                    var r = recipe;
                    panel.purchaseButton.onClick.AddListener(() => PurchaseBuff(r));
                }

                BuildCostSlots(panel, recipe);
                recipeEntries[recipe] = panel;
            }
        }

        private void BuildCostSlots(BuffRecipeUIReferences panel, BuffRecipe recipe)
        {
            if (panel.costGridLayoutParent == null || panel.costSlotPrefab == null)
                return;

            foreach (Transform child in panel.costGridLayoutParent.transform)
                Destroy(child.gameObject);

            foreach (var req in recipe.requirements)
            {
                var slot = Instantiate(panel.costSlotPrefab, panel.costGridLayoutParent.transform);
                slot.resource = req.resource;

                if (slot.selectButton != null)
                {
                    var res = req.resource;
                    slot.selectButton.onClick.AddListener(() => resourceInventoryUI?.HighlightResource(res));
                    slot.selectButton.interactable = true;
                }

                bool unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);

                if (slot.iconImage != null)
                {
                    slot.iconImage.sprite = req.resource ? req.resource.icon : null;
                    slot.iconImage.enabled = unlocked;
                }

                if (slot.questionMarkImage != null)
                    slot.questionMarkImage.enabled = !unlocked;

                if (slot.countText != null)
                    slot.countText.text = req.amount.ToString();

                if (slot.selectionImage != null)
                    slot.selectionImage.enabled = false;
            }
        }

        private void OnInventoryChanged()
        {
            foreach (var pair in recipeEntries)
                BuildCostSlots(pair.Value, pair.Key);
        }

        private void OnLoadDataHandler()
        {
            UpdateActiveIcons();
            OnInventoryChanged();
        }

        private void PurchaseBuff(BuffRecipe recipe)
        {
            if (controller != null && controller.PurchaseBuff(recipe))
                UpdateActiveIcons();
        }

        private void UpdateActiveIcons()
        {
            foreach (var entry in iconEntries)
                if (entry.refs != null)
                    Destroy(entry.refs.gameObject);
            iconEntries.Clear();

            if (activeBuffParent == null || activeBuffPrefab == null || controller == null)
                return;

            foreach (var buff in controller.ActiveBuffs)
            {
                var obj = Instantiate(activeBuffPrefab, activeBuffParent);
                if (obj.iconImage != null)
                    obj.iconImage.sprite = buff.recipe.buffIcon;
                if (obj.durationText != null)
                    obj.durationText.text = Mathf.Ceil(buff.remaining).ToString();
                iconEntries.Add(new ActiveIconEntry { buff = buff, refs = obj });
            }
        }

        private void OpenPurchaseWindow()
        {
            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(true);
        }
    }
}
