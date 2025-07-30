using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Hero;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;
using TimelessEchoes.UI;

namespace TimelessEchoes.Buffs
{
    public class BuffUIManager : MonoBehaviour
    {
        private ResourceManager resourceManager;
        private ResourceInventoryUI resourceInventoryUI;
        private BuffManager buffManager;
        private Hero.HeroHealth heroHealth;
        [SerializeField] private BuffRecipeUIReferences recipePrefab;
        [SerializeField] private Transform recipeParent;
        [SerializeField] private Button openPurchaseButton;
        [SerializeField] private GameObject buffPurchaseWindow;

        [Header("Slot UI References")] [SerializeField]
        private BuffSlotUIReferences[] assignSlotButtons = new BuffSlotUIReferences[5];
        [SerializeField] private BuffSlotUIReferences[] runSlotButtons = new BuffSlotUIReferences[5];

        [Header("Tooltip References")] [SerializeField]
        private RunBuffTooltipUIReferences runSlotTooltip;

        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;

        private BuffRecipe selectedRecipe;
        private bool isAssigning = false;
        private int hoveredRunSlot = -1;

        private readonly Dictionary<BuffRecipe, BuffRecipeUIReferences> recipeEntries = new();

        private void RefreshSlots()
        {
            if (buffManager == null) return;

            if (heroHealth == null || !heroHealth.gameObject.activeInHierarchy)
                heroHealth = Hero.HeroHealth.Instance ??
                             FindFirstObjectByType<Hero.HeroHealth>();

            var transparent = new Color(1f, 1f, 1f, 0f);
            var grey = new Color(1f, 1f, 1f, 0.4f);
            var unlocked = buffManager.UnlockedSlots;

            for (var i = 0; i < assignSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = assignSlotButtons[i];
                if (ui != null && ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                        ui.iconImage.color = recipe ? grey : transparent;
                    else
                        ui.iconImage.color = recipe ? Color.white : transparent;
                }
                if (ui != null && ui.activateButton != null)
                {
                    if (isAssigning)
                        ui.activateButton.interactable = i < unlocked;
                    else
                        ui.activateButton.interactable = buffManager != null && buffManager.IsAutoSlotUnlocked(i);
                }
                if (ui != null && ui.autoCastImage != null)
                    ui.autoCastImage.enabled = buffManager != null && buffManager.IsSlotAutoCasting(i);
            }

            bool heroAlive = heroHealth != null && heroHealth.gameObject.activeInHierarchy && heroHealth.CurrentHealth > 0f;

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = runSlotButtons[i];
                if (ui == null) continue;
                bool canBuy = recipe != null && buffManager != null && buffManager.CanPurchase(recipe) && heroAlive;
                bool distanceOk = true;
                var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
                if (recipe != null && tracker != null && recipe.distancePercent > 0f)
                {
                    float expireDist = tracker.LongestRun * recipe.distancePercent;
                    distanceOk = tracker.CurrentRunDistance < expireDist;
                }

                if (ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                    {
                        ui.iconImage.color = recipe ? grey : transparent;
                    }
                    else if (recipe == null)
                    {
                        ui.iconImage.color = transparent;
                    }
                    else
                    {
                        ui.iconImage.color = canBuy && distanceOk ? Color.white : grey;
                    }
                }

                if (ui.activateButton != null)
                    ui.activateButton.interactable = i < unlocked && recipe != null && canBuy && distanceOk && heroAlive;
                if (ui.autoCastImage != null)
                    ui.autoCastImage.enabled = buffManager != null && buffManager.IsSlotAutoCasting(i);

                if (ui.durationText != null)
                {
                    if (i >= unlocked)
                    {
                        ui.durationText.text = "Locked";
                    }
                    else
                    {
                        var remain = recipe ? buffManager.GetRemaining(recipe) : 0f;
                        if (!heroAlive)
                        {
                            ui.durationText.text = "Dead";
                        }
                        else if (!distanceOk)
                        {
                            ui.durationText.text = "Too Far";
                        }
                        else
                        {
                            ui.durationText.text = remain > 0f
                                ? FormatTime(remain, shortForm: true)
                                : string.Empty;
                        }
                    }
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
            OnQuestHandin += OnQuestHandinHandler;

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChanged;

            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(false);

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
            heroHealth = Hero.HeroHealth.Instance ?? FindFirstObjectByType<Hero.HeroHealth>();
            RefreshSlots();
            OnInventoryChanged();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChanged;
            OnLoadData -= OnLoadDataHandler;
            OnQuestHandin -= OnQuestHandinHandler;

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
                panel.durationText.text = $"Duration: {Mathf.Ceil(recipe.baseDuration)}";
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
                    panel.durationText.text = $"Duration: {Mathf.Ceil(recipe.baseDuration)}";
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

                slot.PointerClick += (_, button) =>
                {
                    if (button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                        resourceInventoryUI?.HighlightResource(req.resource);
                };

                var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);

                if (slot.iconImage != null)
                {
                    var unknownSprite = resourceInventoryUI ? resourceInventoryUI.UnknownSprite : null;
                    slot.iconImage.sprite = unlocked ? req.resource?.icon : unknownSprite;
                    slot.iconImage.color = unlocked ? Color.white : new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
                    slot.iconImage.enabled = true;
                }

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
                    pair.Value.purchaseButton.interactable = true;
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

        private void OnQuestHandinHandler(string questId)
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
            isAssigning = true;
            RefreshSlots();
        }

        private void OnAssignSlot(int slot)
        {
            if (isAssigning && selectedRecipe != null && buffManager != null && buffManager.IsSlotUnlocked(slot))
            {
                buffManager.AssignBuff(slot, selectedRecipe);
            }
            else
            {
                buffManager?.ToggleSlotAutoCast(slot);
            }
            selectedRecipe = null;
            isAssigning = false;
            RefreshSlots();
        }

        private void OnRunSlot(int slot)
        {
            if (buffManager != null)
                buffManager.ActivateSlot(slot);
            RefreshSlots();
        }


        private void OpenPurchaseWindow()
        {
            if (buffPurchaseWindow != null)
                buffPurchaseWindow.SetActive(true);
        }

        private void ShowRunSlotTooltip(int slot)
        {
            if (runSlotTooltip == null || runSlotTooltip.tooltipPanel == null || slot < 0 ||
                slot >= runSlotButtons.Length)
                return;

            if (RunCalebUIManager.Instance != null && RunCalebUIManager.Instance.IsSkillsWindowOpen)
            {
                runSlotTooltip.tooltipPanel.SetActive(false);
                return;
            }

            if (buffManager != null && !buffManager.IsSlotUnlocked(slot))
            {
                runSlotTooltip.tooltipPanel.SetActive(false);
                return;
            }

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
                bool unlockedRes = resourceManager && resourceManager.IsUnlocked(req.resource);
                if (slotRef.resourceIcon != null)
                {
                    var unknownSprite = resourceInventoryUI ? resourceInventoryUI.UnknownSprite : null;
                    slotRef.resourceIcon.sprite = unlockedRes && req.resource ? req.resource.icon : unknownSprite;
                    slotRef.resourceIcon.color = unlockedRes ? Color.white : new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
                }
                if (slotRef.resourceCostText != null)
                    slotRef.resourceCostText.text = $"Cost: {FormatNumber(req.amount, true)}";

                var held = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                if (slotRef.resourceHeldText != null)
                    slotRef.resourceHeldText.text = FormatNumber(held, true);

                var enough = held >= req.amount;
                if (slotRef.resourceIcon != null)
                {
                    if (!unlockedRes)
                        slotRef.resourceIcon.color = new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
                    else
                        slotRef.resourceIcon.color = enough ? Color.white : grey;
                }
            }

            var ui = runSlotButtons[slot];
            if (ui != null)
                runSlotTooltip.tooltipPanel.transform.position = ui.transform.position + (Vector3)tooltipOffset;

            runSlotTooltip.tooltipPanel.SetActive(true);
        }
    }
}