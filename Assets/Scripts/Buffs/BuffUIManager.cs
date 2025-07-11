using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Buffs
{
    public class BuffUIManager : MonoBehaviour
    {
        private ResourceManager resourceManager;
        private ResourceInventoryUI resourceInventoryUI;
        private BuffManager buffManager;
        [SerializeField] private BuffRecipeUIReferences recipePrefab;
        [SerializeField] private Transform recipeParent;
        [SerializeField] private Button openPurchaseButton;
        [SerializeField] private GameObject buffPurchaseWindow;

        [Header("Slot UI References")] [SerializeField]
        private GameObject slotAssignWindow;

        [SerializeField] private BuffSlotUIReferences[] assignSlotButtons = new BuffSlotUIReferences[5];
        [SerializeField] private BuffSlotUIReferences[] runSlotButtons = new BuffSlotUIReferences[5];

        [Header("Tooltip References")] [SerializeField]
        private RunBuffTooltipUIReferences runSlotTooltip;

        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;

        private BuffRecipe selectedRecipe;
        private int hoveredRunSlot = -1;

        private readonly Dictionary<BuffRecipe, BuffRecipeUIReferences> recipeEntries = new();

        private void RefreshSlots()
        {
            if (buffManager == null) return;

            var transparent = new Color(1f, 1f, 1f, 0f);
            var grey = new Color(1f, 1f, 1f, 0.4f);

            for (var i = 0; i < assignSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = assignSlotButtons[i];
                if (ui != null && ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    ui.iconImage.color = recipe ? Color.white : transparent;
                }
            }

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = runSlotButtons[i];
                if (ui == null) continue;
                if (ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (recipe == null)
                    {
                        ui.iconImage.color = transparent;
                    }
                    else
                    {
                        var canBuy = buffManager != null && buffManager.CanPurchase(recipe);
                        ui.iconImage.color = canBuy ? Color.white : grey;
                    }
                }

                if (ui.durationText != null)
                {
                    var remain = recipe ? buffManager.GetRemaining(recipe) : 0f;
                    ui.durationText.text = remain > 0f ? Mathf.Ceil(remain).ToString() : string.Empty;
                }
            }
        }

        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            resourceInventoryUI = ResourceInventoryUI.Instance;
            if (resourceInventoryUI == null)
                Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            buffManager = BuffManager.Instance;
            if (buffManager == null)
                Log("BuffManager missing", TELogCategory.Buff, this);

            if (runSlotTooltip == null)
                runSlotTooltip = FindFirstObjectByType<RunBuffTooltipUIReferences>();

            BuildRecipeEntries();

            OnLoadData += OnLoadDataHandler;

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChanged;

            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(false);
            if (slotAssignWindow != null)
                slotAssignWindow.SetActive(false);

            if (openPurchaseButton != null)
                openPurchaseButton.onClick.AddListener(OpenPurchaseWindow);

            for (var i = 0; i < assignSlotButtons.Length; i++)
            {
                var index = i;
                if (assignSlotButtons[i] != null && assignSlotButtons[i].activateButton != null)
                    assignSlotButtons[i].activateButton.onClick.AddListener(() => OnAssignSlot(index));
            }

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var index = i;
                if (runSlotButtons[i] != null && runSlotButtons[i].activateButton != null)
                    runSlotButtons[i].activateButton.onClick.AddListener(() => OnRunSlot(index));

                if (runSlotButtons[i] != null)
                {
                    runSlotButtons[i].PointerEnter += _ =>
                    {
                        hoveredRunSlot = index;
                        ShowRunSlotTooltip(index);
                    };
                    runSlotButtons[i].PointerExit += _ =>
                    {
                        hoveredRunSlot = -1;
                        if (runSlotTooltip != null && runSlotTooltip.tooltipPanel != null)
                            runSlotTooltip.tooltipPanel.SetActive(false);
                    };
                }
            }
        }

        private void OnEnable()
        {
            RefreshSlots();
            OnInventoryChanged();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChanged;
            OnLoadData -= OnLoadDataHandler;

            for (var i = 0; i < assignSlotButtons.Length; i++)
                if (assignSlotButtons[i] != null && assignSlotButtons[i].activateButton != null)
                    assignSlotButtons[i].activateButton.onClick.RemoveAllListeners();

            for (var i = 0; i < runSlotButtons.Length; i++)
                if (runSlotButtons[i] != null && runSlotButtons[i].activateButton != null)
                    runSlotButtons[i].activateButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            RefreshSlots();

            foreach (var pair in recipeEntries)
            {
                var panel = pair.Value;
                if (panel.durationText == null) continue;
                var recipe = pair.Key;
                var remaining = buffManager != null ? buffManager.GetRemaining(recipe) : 0f;
                var extra = recipe.baseDuration;
                if (buffManager != null && buffManager.DiminishingCurve != null)
                    extra *= buffManager.DiminishingCurve.Evaluate(remaining);
                panel.durationText.text =
                    $"Time remaining: {Mathf.Ceil(remaining)} -> {Mathf.Ceil(remaining + extra)}";
            }
        }

        private void BuildRecipeEntries()
        {
            if (recipePrefab == null || recipeParent == null)
                return;

            var manager = buffManager;
            if (manager == null) return;

            foreach (var recipe in manager.Recipes)
            {
                var panel = Instantiate(recipePrefab, recipeParent);
                if (panel.iconImage != null)
                    panel.iconImage.sprite = recipe.buffIcon;
                if (panel.nameText != null)
                    panel.nameText.text = string.IsNullOrEmpty(recipe.title) ? recipe.name : recipe.title;
                if (panel.descriptionText != null)
                    panel.descriptionText.text = recipe.description;
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

                var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);

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
            {
                BuildCostSlots(pair.Value, pair.Key);
                if (pair.Value.purchaseButton != null)
                    pair.Value.purchaseButton.interactable =
                        buffManager != null && buffManager.CanPurchase(pair.Key);
            }

            RefreshSlots();

            if (hoveredRunSlot >= 0 && runSlotTooltip != null && runSlotTooltip.tooltipPanel != null &&
                runSlotTooltip.tooltipPanel.activeSelf)
                ShowRunSlotTooltip(hoveredRunSlot);
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DeferredRefresh());
        }

        private IEnumerator DeferredRefresh()
        {
            yield return null;
            RefreshSlots();
            OnInventoryChanged();
        }

        private void PurchaseBuff(BuffRecipe recipe)
        {
            selectedRecipe = recipe;
            if (slotAssignWindow != null)
                slotAssignWindow.SetActive(true);
        }

        private void OnAssignSlot(int slot)
        {
            if (selectedRecipe != null)
                buffManager?.AssignBuff(slot, selectedRecipe);
            if (slotAssignWindow != null)
                slotAssignWindow.SetActive(false);
            RefreshSlots();
        }

        private void OnRunSlot(int slot)
        {
            buffManager?.ActivateSlot(slot);
            RefreshSlots();
        }


        private void OpenPurchaseWindow()
        {
            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(true);
            if (slotAssignWindow != null)
                slotAssignWindow.SetActive(false);
        }

        private void ShowRunSlotTooltip(int slot)
        {
            if (runSlotTooltip == null || runSlotTooltip.tooltipPanel == null || slot < 0 ||
                slot >= runSlotButtons.Length)
                return;

            var recipe = buffManager != null ? buffManager.GetAssigned(slot) : null;
            if (recipe == null)
            {
                runSlotTooltip.tooltipPanel.SetActive(false);
                return;
            }

            foreach (Transform child in runSlotTooltip.tooltipCostParent)
                Destroy(child.gameObject);

            var grey = new Color(1f, 1f, 1f, 0.4f);

            foreach (var req in recipe.requirements)
            {
                var slotRef = Instantiate(runSlotTooltip.tooltipCostPrefab, runSlotTooltip.tooltipCostParent);
                if (slotRef.resourceIcon != null)
                    slotRef.resourceIcon.sprite = req.resource ? req.resource.icon : null;
                if (slotRef.resourceCostText != null)
                    slotRef.resourceCostText.text = $"Cost: {FormatNumber(req.amount, true)}";

                var held = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                if (slotRef.resourceHeldText != null)
                    slotRef.resourceHeldText.text = FormatNumber(held, true);

                var enough = held >= req.amount;
                if (slotRef.resourceIcon != null)
                    slotRef.resourceIcon.color = enough ? Color.white : grey;
            }

            var ui = runSlotButtons[slot];
            if (ui != null)
                runSlotTooltip.tooltipPanel.transform.position = ui.transform.position + (Vector3)tooltipOffset;

            runSlotTooltip.tooltipPanel.SetActive(true);
        }
    }
}