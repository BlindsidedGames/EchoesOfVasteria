using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Hero;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.Utilities;
using TMPro;
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
        private readonly System.Action<BuffSlotUIReferences>[] runSlotAutoCastHandlers = new System.Action<BuffSlotUIReferences>[5];
        [SerializeField] private GameObject buffPurchaseWindow;
        private BuffRecipe selectedRecipe;
        private bool isAssigning;
        [Header("Run State UI")]
        [SerializeField] private TMP_Text runStateInfoText;
        [SerializeField] private string inRunAssignDisabledMessage = "Cannot assign buffs during a run.";
        private bool inRun;

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
                if (ui != null && ui.IconImage != null)
                {
                    ui.IconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                        ui.IconImage.color = recipe ? grey : transparent;
                    else
                        ui.IconImage.color = recipe ? Color.white : transparent;
                }

                if (ui != null && ui.ActivateButton != null)
                {
                    if (isAssigning)
                        ui.ActivateButton.interactable = i < unlocked;
                    else
                        ui.ActivateButton.interactable = buffManager != null && buffManager.IsAutoSlotUnlocked(i);
                }

                if (ui != null && ui.AutoCastImage != null)
                    ui.AutoCastImage.enabled = buffManager != null && buffManager.IsSlotAutoCasting(i);
                if (ui != null && ui.DurationText != null)
                    ui.DurationText.text = i >= unlocked ? "Locked" : string.Empty;
            }

            var heroAlive = heroHealth != null && heroHealth.gameObject.activeInHierarchy &&
                            heroHealth.CurrentHealth > 0f;

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = runSlotButtons[i];
                if (ui == null) continue;
                if (ui.RadialFillImage != null)
                    ui.RadialFillImage.fillAmount = 0f;
                if (ui.CooldownRadialFillImage != null)
                    ui.CooldownRadialFillImage.fillAmount = 0f;
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

                if (ui.IconImage != null)
                {
                    ui.IconImage.sprite = recipe ? recipe.buffIcon : null;
                    if (i >= unlocked)
                        ui.IconImage.color = recipe ? grey : transparent;
                    else if (recipe == null)
                        ui.IconImage.color = transparent;

                    else if (!distanceOk)
                        ui.IconImage.color = grey;
                    else if (remain <= 0f && cooldown > 0f)
                        ui.IconImage.color = grey;
                    else
                        ui.IconImage.color = Color.white;
                }

                if (ui.ActivateButton != null)
                    ui.ActivateButton.interactable = i < unlocked && recipe != null && canActivate;
                if (ui.AutoCastImage != null)
                    ui.AutoCastImage.enabled = buffManager != null && buffManager.IsSlotAutoCasting(i);

                if (ui.DurationText != null)
                {
                    if (i >= unlocked)
                    {
                        ui.DurationText.text = "Locked";
                    }
                    else
                    {
                        if (!heroAlive)
                        {
                            ui.DurationText.text = "Dead";
                        }
                        else if (recipe != null && tracker != null &&
                                 recipe.durationType == BuffDurationType.DistancePercent)
                        {
                            if (!distanceOk)
                            {
                                ui.DurationText.text = "Jumped";
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = 0f;
                            }
                            else if (remain > 0f)
                            {
                                var percent = expireDist > 0f ? tracker.CurrentRunDistance / expireDist * 100f : 0f;
                                ui.DurationText.text = $"{Mathf.FloorToInt(percent)}%";
                                if (ui.RadialFillImage != null)
                                {
                                    var remainDist = expireDist - tracker.CurrentRunDistance;
                                    ui.RadialFillImage.fillAmount = expireDist > 0f
                                        ? Mathf.Clamp01(remainDist / expireDist)
                                        : 0f;
                                }
                            }
                            else if (cooldown > 0f)
                            {
                                ui.DurationText.text = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                                if (ui.CooldownRadialFillImage != null)
                                    ui.CooldownRadialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(1f - cooldown / recipe.GetCooldown())
                                        : 0f;
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = 0f;
                            }
                            else
                            {
                                ui.DurationText.text = string.Empty;
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = 0f;
                            }
                        }
                        // Removed ExtraDistancePercent UI handling
                        else if (!distanceOk)
                        {
                            ui.DurationText.text = "Jumped";
                            if (ui.RadialFillImage != null)
                                ui.RadialFillImage.fillAmount = 0f;
                        }
                        else
                        {
                            if (remain > 0f)
                            {
                                ui.DurationText.text = FormatTime(remain, remain < 10f, shortForm: true);
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(remain / recipe.GetDuration())
                                        : 0f;
                            }
                            else if (cooldown > 0f)
                            {
                                ui.DurationText.text = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                                if (ui.CooldownRadialFillImage != null)
                                    ui.CooldownRadialFillImage.fillAmount = recipe != null
                                        ? Mathf.Clamp01(1f - cooldown / recipe.GetCooldown())
                                        : 0f;
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = 0f;
                            }
                            else
                            {
                                ui.DurationText.text = string.Empty;
                                if (ui.RadialFillImage != null)
                                    ui.RadialFillImage.fillAmount = 0f;
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
            OnRunStarted += HandleRunStarted;
            OnRunEnded += HandleRunEnded;

            for (var i = 0; i < assignSlotButtons.Length; i++)
            {
                var index = i;
                var slot = assignSlotButtons[i];
                if (slot?.ActivateButton != null)
                    slot.ActivateButton.onClick.AddListener(() => OnAssignSlot(index));
            }

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var index = i;
                var slot = runSlotButtons[i];
                if (slot == null)
                    continue;

                if (slot.ActivateButton != null)
                    slot.ActivateButton.onClick.AddListener(() => OnRunSlot(index));

                runSlotAutoCastHandlers[i] = _ => OnRunSlotAutoCast(index);
                slot.AutoCastToggleRequested += runSlotAutoCastHandlers[i];
            }
        }
        private void OnEnable()
        {
            heroHealth = HeroHealth.Instance;
            var tracker = GameplayStatTracker.Instance;
            inRun = tracker != null && tracker.RunInProgress;
            ApplyRunStateToUI();
            RefreshSlots();
        }

        private void OnDestroy()
        {
            OnLoadData -= OnLoadDataHandler;
            OnQuestHandin -= OnQuestHandinHandler;
            OnRunStarted -= HandleRunStarted;
            OnRunEnded -= HandleRunEnded;

            for (var i = 0; i < assignSlotButtons.Length; i++)
            {
                var slot = assignSlotButtons[i];
                if (slot?.ActivateButton != null)
                    slot.ActivateButton.onClick.RemoveAllListeners();
            }

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var slot = runSlotButtons[i];
                if (slot?.ActivateButton != null)
                    slot.ActivateButton.onClick.RemoveAllListeners();

                if (slot != null && runSlotAutoCastHandlers[i] != null)
                    slot.AutoCastToggleRequested -= runSlotAutoCastHandlers[i];
                runSlotAutoCastHandlers[i] = null;
            }
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

            var qm = QuestManager.Instance;

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
                    panel.purchaseButton.interactable = !inRun;
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
            if (inRun)
            {
                if (runStateInfoText != null)
                {
                    runStateInfoText.text = inRunAssignDisabledMessage;
                    runStateInfoText.gameObject.SetActive(true);
                }
                ApplyRunStateToUI();
                return;
            }

            selectedRecipe = recipe;
            isAssigning = true;
            RefreshSlots();
        }

        private void OnAssignSlot(int slot)
        {
            if (!inRun && isAssigning && selectedRecipe != null && buffManager != null && buffManager.IsSlotUnlocked(slot))
                buffManager.AssignBuff(slot, selectedRecipe);
            else
                buffManager?.ToggleSlotAutoCast(slot);
            selectedRecipe = null;
            isAssigning = false;
            RefreshSlots();
        }

        private void HandleRunStarted()
        {
            inRun = true;
            // Clear any pending assign state when a run starts
            selectedRecipe = null;
            isAssigning = false;
            ApplyRunStateToUI();
            RefreshSlots();
        }

        private void HandleRunEnded()
        {
            inRun = false;
            ApplyRunStateToUI();
            RefreshSlots();
        }

        private void ApplyRunStateToUI()
        {
            // Update recipe purchase buttons
            foreach (var kv in recipeEntries)
            {
                var ui = kv.Value;
                if (ui != null && ui.purchaseButton != null)
                    ui.purchaseButton.interactable = !inRun;
            }

            // Show/hide info text
            if (runStateInfoText != null)
            {
                if (inRun)
                {
                    runStateInfoText.text = inRunAssignDisabledMessage;
                    runStateInfoText.gameObject.SetActive(true);
                }
                else
                {
                    runStateInfoText.gameObject.SetActive(false);
                }
            }
        }

        private void OnRunSlot(int slot)
        {
            var ui = slot >= 0 && slot < runSlotButtons.Length ? runSlotButtons[slot] : null;
            if (ui != null && ui.ConsumePendingSuppression())
            {
                RefreshSlots();
                return;
            }

            if (buffManager != null)
                buffManager.ActivateSlot(slot);
            RefreshSlots();
        }

        private void OnRunSlotAutoCast(int slot)
        {
            if (buffManager == null)
                return;

            buffManager.ToggleSlotAutoCast(slot);
            RefreshSlots();
        }
    }
}

