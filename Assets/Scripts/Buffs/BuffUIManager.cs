using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Buffs
{
    public class BuffUIManager : MonoBehaviour
    {
        private BuffManager buffManager;
        private HeroHealth heroHealth;
        [SerializeField] private BuffRecipeUIReferences recipePrefab;
        [SerializeField] private Transform recipeParent;
        [SerializeField] private Button openPurchaseButton;
        [SerializeField] private GameObject buffPurchaseWindow;

        [Header("Slot UI References")] [SerializeField]
        private BuffSlotUIReferences[] assignSlotButtons = new BuffSlotUIReferences[5];

        [SerializeField] private BuffSlotUIReferences[] runSlotButtons = new BuffSlotUIReferences[5];

        private BuffRecipe selectedRecipe;
        private bool isAssigning;

        private readonly Dictionary<BuffRecipe, BuffRecipeUIReferences> recipeEntries = new();

        private void RefreshSlots()
        {
            if (buffManager == null) return;

            if (heroHealth == null || !heroHealth.gameObject.activeInHierarchy)
                heroHealth = HeroHealth.Instance ??
                             FindFirstObjectByType<HeroHealth>();

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
                if (ui != null && ui.durationText != null)
                    ui.durationText.text = i >= unlocked ? "Locked" : string.Empty;
            }

            var heroAlive = heroHealth != null && heroHealth.gameObject.activeInHierarchy &&
                            heroHealth.CurrentHealth > 0f;

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = runSlotButtons[i];
                if (ui == null) continue;
                var canActivate = recipe != null && buffManager != null && buffManager.CanActivate(recipe) && heroAlive;
                var distanceOk = true;
                var tracker = GameplayStatTracker.Instance;
                var expireDist = 0f;
                if (recipe != null && tracker != null && recipe.distancePercent > 0f)
                {
                    expireDist = tracker.LongestRun * recipe.distancePercent;
                    distanceOk = tracker.CurrentRunDistance < expireDist;
                }

                if (ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                        ui.iconImage.color = recipe ? grey : transparent;
                    else if (recipe == null)
                        ui.iconImage.color = transparent;
                    else
                        ui.iconImage.color = canActivate ? Color.white : grey;
                }

                if (ui.activateButton != null)
                    ui.activateButton.interactable = i < unlocked && recipe != null && canActivate;
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
                            ui.durationText.text = "Dead";
                        else if (recipe != null && tracker != null && recipe.distancePercent > 0f)
                        {
                            if (!distanceOk)
                                ui.durationText.text = "Too Far";
                            else if (remain > 0f)
                            {
                                var percent = expireDist > 0f ? tracker.CurrentRunDistance / expireDist * 100f : 0f;
                                ui.durationText.text = $"{Mathf.FloorToInt(percent)}%";
                            }
                            else
                                ui.durationText.text = string.Empty;
                        }
                        else if (!distanceOk)
                            ui.durationText.text = "Too Far";
                        else
                            ui.durationText.text = remain > 0f
                                ? FormatTime(remain, shortForm: true)
                                : string.Empty;
                    }
                }
            }
        }

        private void Awake()
        {
            buffManager = BuffManager.Instance;
            if (buffManager == null)
                Log("BuffManager missing", TELogCategory.Buff, this);

            BuildRecipeEntries();

            OnLoadData += OnLoadDataHandler;
            OnQuestHandin += OnQuestHandinHandler;

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
            }
        }

        private void OnEnable()
        {
            heroHealth = HeroHealth.Instance ?? FindFirstObjectByType<HeroHealth>();
            RefreshSlots();
        }

        private void OnDestroy()
        {
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
                var recipe = pair.Key;
                if (panel.nameText != null)
                    panel.nameText.text = recipe.GetDisplayName();
                if (panel.descriptionText != null)
                    panel.descriptionText.text = string.Join("\n", recipe.GetDescriptionLines());
                if (panel.durationText != null)
                    panel.durationText.text = recipe.durationType == BuffDurationType.Distance
                        ? $"Distance: {Mathf.CeilToInt(recipe.durationMagnitude * 100f)}%"
                        : $"Duration: {Mathf.CeilToInt(recipe.durationMagnitude)}";
            }
        }

        public void BuildRecipeEntries()
        {
            if (recipePrefab == null || recipeParent == null)
                return;

            foreach (Transform child in recipeParent)
                Destroy(child.gameObject);
            recipeEntries.Clear();

            var manager = buffManager;
            if (manager == null) return;

            foreach (var recipe in manager.Recipes)
            {
                if (recipe != null && !recipe.IsUnlocked())
                    continue;

                var panel = Instantiate(recipePrefab, recipeParent);
                if (panel.iconImage != null)
                    panel.iconImage.sprite = recipe.buffIcon;
                if (panel.nameText != null)
                    panel.nameText.text = recipe.GetDisplayName();
                if (panel.descriptionText != null)
                    panel.descriptionText.text = string.Join("\n", recipe.GetDescriptionLines());
                if (panel.durationText != null)
                    panel.durationText.text = recipe.durationType == BuffDurationType.Distance
                        ? $"Distance: {Mathf.CeilToInt(recipe.durationMagnitude * 100f)}%"
                        : $"Duration: {Mathf.CeilToInt(recipe.durationMagnitude)}";
                if (panel.purchaseButton != null)
                {
                    var r = recipe;
                    panel.purchaseButton.onClick.AddListener(() => PurchaseBuff(r));
                }

                recipeEntries[recipe] = panel;
            }
        }

        private void OnLoadDataHandler()
        {
            BuildRecipeEntries();
            StartCoroutine(DeferredRefresh());
        }

        private void OnQuestHandinHandler(string questId)
        {
            BuildRecipeEntries();
            StartCoroutine(DeferredRefresh());
        }

        private IEnumerator DeferredRefresh()
        {
            yield return null;
            RefreshSlots();
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
                buffManager.AssignBuff(slot, selectedRecipe);
            else
                buffManager?.ToggleSlotAutoCast(slot);
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
    }
}