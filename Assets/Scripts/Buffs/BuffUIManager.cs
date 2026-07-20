using System.Collections.Generic;
using System.Text;
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
    /// <summary>
    /// Represents the visual state of a run slot for dirty checking.
    /// </summary>
    internal enum SlotDisplayState : byte
    {
        Empty,
        Locked,
        Dead,
        TooFar,
        DistanceActive,
        TimeActive,
        Cooldown,
        Ready
    }

    /// <summary>
    /// Cached state for a run slot to avoid redundant UI updates.
    /// </summary>
    internal struct RunSlotCache
    {
        public SlotDisplayState state;
        public Sprite icon;
        public float fillAmount;
        public float cooldownFillAmount;
        public bool autoCast;
        public bool interactable;
        public string durationText;
        public Color iconColor;
    }

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

        // Slot state caching to avoid redundant UI updates
        private readonly RunSlotCache[] _runSlotCache = new RunSlotCache[5];
        private readonly StringBuilder _sb = new StringBuilder(32);

        private void RefreshSlots()
        {
            if (buffManager == null) return;

            if (heroHealth == null || !heroHealth.gameObject.activeInHierarchy)
                heroHealth = HeroHealth.Instance ??
                             FindAnyObjectByType<HeroHealth>();

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
            var tracker = GameplayStatTracker.Instance;

            for (var i = 0; i < runSlotButtons.Length; i++)
            {
                var recipe = buffManager.GetAssigned(i);
                var ui = runSlotButtons[i];
                if (ui == null) continue;

                // Compute new slot state
                var newState = ComputeRunSlotState(i, recipe, unlocked, heroAlive, tracker, transparent, grey,
                    out var newIcon, out var newIconColor, out var newFill, out var newCooldownFill,
                    out var newAutoCast, out var newInteractable, out var newDurationText);

                ref var cache = ref _runSlotCache[i];

                // Only update UI elements that have changed
                if (cache.icon != newIcon && ui.IconImage != null)
                {
                    ui.IconImage.sprite = newIcon;
                    cache.icon = newIcon;
                }

                if (cache.iconColor != newIconColor && ui.IconImage != null)
                {
                    ui.IconImage.color = newIconColor;
                    cache.iconColor = newIconColor;
                }

                if (cache.interactable != newInteractable && ui.ActivateButton != null)
                {
                    ui.ActivateButton.interactable = newInteractable;
                    cache.interactable = newInteractable;
                }

                if (cache.autoCast != newAutoCast && ui.AutoCastImage != null)
                {
                    ui.AutoCastImage.enabled = newAutoCast;
                    cache.autoCast = newAutoCast;
                }

                // Fill amounts use epsilon comparison for floats
                const float epsilon = 0.001f;
                if (Mathf.Abs(cache.fillAmount - newFill) > epsilon && ui.RadialFillImage != null)
                {
                    ui.RadialFillImage.fillAmount = newFill;
                    cache.fillAmount = newFill;
                }

                if (Mathf.Abs(cache.cooldownFillAmount - newCooldownFill) > epsilon && ui.CooldownRadialFillImage != null)
                {
                    ui.CooldownRadialFillImage.fillAmount = newCooldownFill;
                    cache.cooldownFillAmount = newCooldownFill;
                }

                if (cache.durationText != newDurationText && ui.DurationText != null)
                {
                    ui.DurationText.text = newDurationText;
                    cache.durationText = newDurationText;
                }

                cache.state = newState;
            }
        }

        private SlotDisplayState ComputeRunSlotState(int slotIndex, BuffRecipe recipe, int unlocked, bool heroAlive,
            GameplayStatTracker tracker, Color transparent, Color grey,
            out Sprite icon, out Color iconColor, out float fillAmount, out float cooldownFillAmount,
            out bool autoCast, out bool interactable, out string durationText)
        {
            // Initialize outputs
            icon = recipe != null ? recipe.buffIcon : null;
            iconColor = transparent;
            fillAmount = 0f;
            cooldownFillAmount = 0f;
            autoCast = buffManager != null && buffManager.IsSlotAutoCasting(slotIndex);
            interactable = false;
            durationText = string.Empty;

            // Locked slot
            if (slotIndex >= unlocked)
            {
                iconColor = recipe != null ? grey : transparent;
                durationText = "Locked";
                return SlotDisplayState.Locked;
            }

            // Empty slot
            if (recipe == null)
            {
                return SlotDisplayState.Empty;
            }

            var cooldown = buffManager.GetCooldownRemaining(recipe);
            var remain = buffManager.GetRemaining(recipe);
            var canActivate = buffManager.CanActivate(recipe) && heroAlive;
            interactable = canActivate;

            // Check distance-based conditions
            var distanceOk = true;
            var expireDist = 0f;
            if (tracker != null && recipe.durationType == BuffDurationType.DistancePercent)
            {
                var longest = Mathf.Max(1f, tracker.LongestRun);
                expireDist = longest * recipe.GetDuration();
                distanceOk = tracker.CurrentRunDistance < expireDist;
            }

            // Dead state
            if (!heroAlive)
            {
                iconColor = grey;
                durationText = "Dead";
                return SlotDisplayState.Dead;
            }

            // Too far state
            if (!distanceOk)
            {
                iconColor = grey;
                durationText = "Too Far";
                return SlotDisplayState.TooFar;
            }

            // Distance-based buff active
            if (recipe.durationType == BuffDurationType.DistancePercent && tracker != null)
            {
                if (remain > 0f)
                {
                    iconColor = Color.white;
                    var percent = expireDist > 0f ? tracker.CurrentRunDistance / expireDist * 100f : 0f;
                    _sb.Clear();
                    _sb.Append(Mathf.FloorToInt(percent));
                    _sb.Append('%');
                    durationText = _sb.ToString();
                    var remainDist = expireDist - tracker.CurrentRunDistance;
                    fillAmount = expireDist > 0f ? Mathf.Clamp01(remainDist / expireDist) : 0f;
                    return SlotDisplayState.DistanceActive;
                }

                if (cooldown > 0f)
                {
                    iconColor = grey;
                    durationText = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                    cooldownFillAmount = Mathf.Clamp01(1f - cooldown / recipe.GetCooldown());
                    return SlotDisplayState.Cooldown;
                }

                iconColor = Color.white;
                return SlotDisplayState.Ready;
            }

            // Time-based buff active
            if (remain > 0f)
            {
                iconColor = Color.white;
                durationText = FormatTime(remain, remain < 10f, shortForm: true);
                fillAmount = Mathf.Clamp01(remain / recipe.GetDuration());
                return SlotDisplayState.TimeActive;
            }

            // Cooldown
            if (cooldown > 0f)
            {
                iconColor = grey;
                durationText = FormatTime(cooldown, cooldown < 10f, shortForm: true);
                cooldownFillAmount = Mathf.Clamp01(1f - cooldown / recipe.GetCooldown());
                return SlotDisplayState.Cooldown;
            }

            // Ready
            iconColor = Color.white;
            return SlotDisplayState.Ready;
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

