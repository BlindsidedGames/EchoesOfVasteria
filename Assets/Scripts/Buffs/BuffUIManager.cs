using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Hero;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Slot UI References")] [SerializeField]
        private BuffSlotUIReferences[] assignSlotButtons = new BuffSlotUIReferences[5];

        [SerializeField] private BuffSlotUIReferences[] runSlotButtons = new BuffSlotUIReferences[5];
        [SerializeField] private GameObject buffPurchaseWindow;
        private BuffRecipe selectedRecipe;
        private bool isAssigning;

        private readonly Dictionary<BuffRecipe, BuffRecipeUIReferences> recipeEntries = new();

        private float nextUiRefresh;
        [SerializeField] [Min(0.05f)] private float refreshInterval = 0.1f;

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
                if (ui.radialFillImage != null)
                    ui.radialFillImage.fillAmount = 0f;
                if (ui.cooldownRadialFillImage != null)
                    ui.cooldownRadialFillImage.fillAmount = 0f;
                var cooldown = recipe != null && buffManager != null
                    ? buffManager.GetCooldownRemaining(recipe)
                    : 0f;
                var remain = recipe != null && buffManager != null
                    ? buffManager.GetRemaining(recipe)
                    : 0f;
                var canActivate = recipe != null && buffManager != null && buffManager.CanActivate(recipe) && heroAlive;
                var distanceOk = true;
                var tracker = GameplayStatTracker.Instance;
                var expireDist = 0f;
                if (recipe != null && tracker != null)
                {
                    if (recipe.durationType == BuffDurationType.DistancePercent)
                    {
                        var longest = Mathf.Max(1f, tracker.LongestRun);
                        expireDist = longest * recipe.GetDuration();
                        distanceOk = tracker.CurrentRunDistance < expireDist;
                    }
                }

                if (ui.iconImage != null)
                {
                    ui.iconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                        ui.iconImage.color = recipe ? grey : transparent;
                    else if (recipe == null)
                        ui.iconImage.color = transparent;

                    else if (!distanceOk)
                        ui.iconImage.color = grey;
                    else if (remain <= 0f && cooldown > 0f)
                        ui.iconImage.color = grey;
                    else
                        ui.iconImage.color = Color.white;
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
                        if (!heroAlive)
                        {
                            ui.durationText.text = "Dead";
                        }
                        else if (recipe != null && tracker != null &&
                                 recipe.durationType == BuffDurationType.DistancePercent)
                        {
                            if (!distanceOk)
                            {
                                ui.durationText.text = "Too Far";
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = 0f;
                            }
                            else if (remain > 0f)
                            {
                                var percent = expireDist > 0f ? tracker.CurrentRunDistance / expireDist * 100f : 0f;
                                ui.durationText.text = $"{Mathf.FloorToInt(percent)}%";
                                if (ui.radialFillImage != null)
                                {
                                    var remainDist = expireDist - tracker.CurrentRunDistance;
                                    ui.radialFillImage.fillAmount = expireDist > 0f
                                        ? Mathf.Clamp01(remainDist / expireDist)
                                        : 0f;
                                }
                            }
                            else if (cooldown > 0f)
                            {
                                ui.durationText.text = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                                if (ui.cooldownRadialFillImage != null)
                                    ui.cooldownRadialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(1f - cooldown / recipe.GetCooldown())
                                        : 0f;
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = 0f;
                            }
                            else
                            {
                                ui.durationText.text = string.Empty;
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = 0f;
                            }
                        }
                        // Removed ExtraDistancePercent UI handling
                        else if (!distanceOk)
                        {
                            ui.durationText.text = "Too Far";
                            if (ui.radialFillImage != null)
                                ui.radialFillImage.fillAmount = 0f;
                        }
                        else
                        {
                            if (remain > 0f)
                            {
                                ui.durationText.text = FormatTime(remain, remain < 10f, shortForm: true);
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(remain / recipe.GetDuration())
                                        : 0f;
                            }
                            else if (cooldown > 0f)
                            {
                                ui.durationText.text = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                                if (ui.cooldownRadialFillImage != null)
                                    ui.cooldownRadialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(1f - cooldown / recipe.GetCooldown())
                                        : 0f;
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = 0f;
                            }
                            else
                            {
                                ui.durationText.text = string.Empty;
                                if (ui.radialFillImage != null)
                                    ui.radialFillImage.fillAmount = 0f;
                            }
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            buffManager = BuffManager.Instance;
            if (buffManager == null)
                Log("BuffManager missing", TELogCategory.Buff, this);

            OnLoadData += OnLoadDataHandler;
            OnQuestHandin += OnQuestHandinHandler;

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
            var windowActive = gameObject.activeInHierarchy ||
                               (buffPurchaseWindow != null && buffPurchaseWindow.activeInHierarchy);
            if (!windowActive) return;

            // Throttle UI refresh to reduce UGUI rebuilds
            if (Time.unscaledTime >= nextUiRefresh)
            {
                nextUiRefresh = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
                RefreshSlots();
            }

            if (!Application.isMobilePlatform && buffManager != null && Keyboard.current != null)
                for (var i = 0; i < 5; i++)
                {
                    var digitKey = Keyboard.current[(Key)((int)Key.Digit1 + i)];
                    var numpadKey = Keyboard.current[(Key)((int)Key.Numpad1 + i)];
                    var pressed = (digitKey != null && digitKey.wasPressedThisFrame) ||
                                  (numpadKey != null && numpadKey.wasPressedThisFrame);
                    if (pressed)
                    {
                        var recipe = buffManager.GetAssigned(i);
                        if (recipe != null && buffManager.CanActivate(recipe))
                            buffManager.ActivateSlot(i);
                    }
                }

            // Descriptions are static; avoid updating every frame
        }

        private void BuildRecipeEntries()
        {
            if (recipePrefab == null || recipeParent == null)
                return;

            var manager = buffManager;
            if (manager == null) return;

            foreach (var panel in recipeEntries.Values)
                if (panel != null)
                    Destroy(panel.gameObject);
            recipeEntries.Clear();

            var qm = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();

            foreach (var recipe in manager.Recipes)
            {
                if (recipe == null) continue;
                if (recipe.requiredQuest != null && (qm == null || !qm.IsQuestCompleted(recipe.requiredQuest)))
                    continue;
                var panel = Instantiate(recipePrefab, recipeParent);
                if (panel.iconImage != null)
                    panel.iconImage.sprite = recipe.buffIcon;
                if (panel.nameText != null)
                    panel.nameText.text = recipe.GetDisplayName();
                if (panel.descriptionText != null)
                    panel.descriptionText.text = string.Join("\n", recipe.GetDescriptionLines());
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
            CoroutineUtils.RunNextFrame(this, () =>
            {
                BuildRecipeEntries();
                RefreshSlots();
            });
        }

        private void OnQuestHandinHandler(string questId)
        {
            CoroutineUtils.RunNextFrame(this, () =>
            {
                BuildRecipeEntries();
                RefreshSlots();
            });
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
    }
}