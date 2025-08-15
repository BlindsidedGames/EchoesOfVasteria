using System.Collections;
using System.Collections.Generic;
using Blindsided.Utilities;
using References.UI;
using TimelessEchoes.Skills;
using TimelessEchoes.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.EventHandler;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays stat upgrades with their costs and handles upgrading.
    /// </summary>
    public class StatUpgradeUIManager : MonoBehaviour
    {
        private StatUpgradeController controller;
        private ResourceManager resourceManager;
        private ResourceInventoryUI resourceInventoryUI;
        [SerializeField] private List<StatUIReferences> statReferences = new();
        [SerializeField] private List<StatUpgrade> upgrades = new();
        [SerializeField] private CostResourceUIReferences costSlotPrefab;

        private SkillController skillController;
        private readonly List<List<CostResourceUIReferences>> costSlots = new();

        private void Awake()
        {
            controller = StatUpgradeController.Instance;
            if (controller == null)
                Log("StatUpgradeController missing", TELogCategory.Upgrade, this);
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            resourceInventoryUI = ResourceInventoryUI.Instance;
            if (resourceInventoryUI == null)
                Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            skillController = SkillController.Instance;
            if (skillController == null)
                Log("SkillController missing", TELogCategory.Upgrade, this);
            if (statReferences.Count == 0)
                statReferences.AddRange(GetComponentsInChildren<StatUIReferences>(true));

            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var upgrade = upgrades[i];
                if (refs.nameText != null)
                    refs.nameText.text = upgrade ? upgrade.name : string.Empty;
                if (refs.descriptionText != null)
                    refs.descriptionText.text = upgrade ? upgrade.description : string.Empty;

                // Set stat icon from the StatIcons TMP sprite asset using the upgrade's name mapping
                if (refs.iconImage != null)
                {
                    if (upgrade != null && StatIconLookup.TryGetIcon(upgrade.name, out var sprite))
                    {
                        refs.iconImage.sprite = sprite;
                        refs.iconImage.enabled = sprite != null;
                        if (sprite != null)
                            refs.iconImage.SetNativeSize();
                    }
                    else
                    {
                        refs.iconImage.sprite = null;
                        refs.iconImage.enabled = false;
                    }
                }
            }

            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var index = i;
                var refs = statReferences[i];
                if (refs != null && refs.upgradeButton != null)
                {
                    refs.upgradeButton.onClick.AddListener(() => ApplyUpgrade(index));
                    // Disable interactivity entirely when feature is off
                    if (UpgradeFeatureToggle.DisableStatUpgrades) refs.upgradeButton.interactable = false;
                    var repeat = refs.upgradeButton.gameObject.AddComponent<RepeatButtonClick>();
                    repeat.button = refs.upgradeButton;
                }
            }

            BuildAllCostSlots();
            UpdateStatLevels();
            UpdateStatDisplayValues();
            UpdateDefenseInfoAndTitle();
        }

        private void OnEnable()
        {
            ShowLevelTextChanged += OnShowLevelTextChanged;
            OnLoadData += OnLoadDataHandler;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChangedHandler;
            OnShowLevelTextChanged();
        }

        private void OnDisable()
        {
            ShowLevelTextChanged -= OnShowLevelTextChanged;
            OnLoadData -= OnLoadDataHandler;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChangedHandler;
        }

        private void OnShowLevelTextChanged()
        {
            BuildAllCostSlots();
            UpdateStatLevels();
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DelayedUpdate());
        }

        private IEnumerator DelayedUpdate()
        {
            yield return null;
            OnShowLevelTextChanged();
        }

        private void BuildAllCostSlots()
        {
            costSlots.Clear();
            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var list = new List<CostResourceUIReferences>();
                var parent = refs.costGridLayoutParent;
                var prefab = costSlotPrefab;

                if (parent != null && prefab != null)
                {
                    UIUtils.ClearChildren(parent.transform);

                    var threshold = GetThreshold(upgrades[i]);
                    if (threshold != null)
                        foreach (var req in threshold.requirements)
                        {
                            var slot = Instantiate(prefab, parent.transform);
                            slot.resource = req.resource;
                            slot.PointerClick += (_, button) =>
                            {
                                if (button == PointerEventData.InputButton.Left)
                                    resourceInventoryUI?.HighlightResource(req.resource);
                            };
                            list.Add(slot);
                        }
                }

                costSlots.Add(list);
            }

            UpdateAllCostSlotValues();
            UpdateStatDisplayValues();
            UpdateUpgradeButtons();
            UpdateDefenseInfoAndTitle();
        }

        private void UpdateAllCostSlotValues()
        {
            for (var i = 0; i < costSlots.Count && i < upgrades.Count; i++)
                UpdateCostSlotValues(i);
        }

        private void UpdateCostSlotValues(int index)
        {
            if (index < 0 || index >= costSlots.Count || index >= upgrades.Count)
                return;

            var threshold = GetThreshold(upgrades[index]);
            var slots = costSlots[index];
            if (threshold == null) return;

            for (var j = 0; j < slots.Count && j < threshold.requirements.Count; j++)
            {
                var slot = slots[j];
                var req = threshold.requirements[j];
                var lvl = controller ? controller.GetLevel(upgrades[index]) : 0;
                var cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;


                var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                var hasEnough = resourceManager == null || resourceManager.GetAmount(req.resource) >= cost;

                if (slot.iconImage)
                {
                    slot.iconImage.sprite = unlocked ? req.resource?.icon : req.resource?.UnknownIcon;
                    var grey = new Color(1f, 1f, 1f, 0.4f);
                    slot.iconImage.color = unlocked ? hasEnough ? Color.white : grey : Color.white;
                    slot.iconImage.enabled = true;
                }

                if (slot.countText)
                    slot.countText.text = cost.ToString();

                if (slot.selectionImage)
                    slot.selectionImage.enabled = false;
            }
        }

        private void UpdateUpgradeButtons()
        {
            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                if (refs != null && refs.upgradeButton != null)
                {
                    if (UpgradeFeatureToggle.DisableStatUpgrades)
                    {
                        refs.upgradeButton.interactable = false;
                        if (refs.upgradeButtonText != null)
                            refs.upgradeButtonText.text = "Disabled";
                        continue;
                    }

                    var threshold = GetThreshold(upgrades[i]);
                    if (threshold == null)
                    {
                        refs.upgradeButton.interactable = false;
                        if (refs.upgradeButtonText != null)
                            refs.upgradeButtonText.text = "Maxed";
                    }
                    else
                    {
                        refs.upgradeButton.interactable = controller != null && controller.CanUpgrade(upgrades[i]);
                        if (refs.upgradeButtonText != null)
                            refs.upgradeButtonText.text = "Upgrade";
                    }
                }
            }
        }

        private void OnInventoryChangedHandler()
        {
            UpdateAllCostSlotValues();
            UpdateUpgradeButtons();
        }

        private void ApplyUpgrade(int index)
        {
            if (index < 0 || index >= upgrades.Count) return;
            if (UpgradeFeatureToggle.DisableStatUpgrades) return;
            if (controller != null && controller.ApplyUpgrade(upgrades[index]))
            {
                BuildAllCostSlots();
                UpdateStatLevels();
                UpdateStatDisplayValues();
                UpdateDefenseInfoAndTitle();
            }
        }

        private StatUpgrade.Threshold GetThreshold(StatUpgrade upgrade)
        {
            if (upgrade == null) return null;
            var lvl = controller ? controller.GetLevel(upgrade) : 0;
            foreach (var t in upgrade.thresholds)
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            return null;
        }

        private void UpdateStatLevels()
        {
            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var selector = statReferences[i];
                var upgrade = upgrades[i];
                if (selector == null || selector.countText == null) continue;

                if (ShowLevelText)
                {
                    var lvl = controller ? controller.GetLevel(upgrade) : 0;
                    selector.countText.text = $"Lvl {lvl}";
                }
                else
                {
                    selector.countText.text = string.Empty;
                }
            }
        }

        private void UpdateStatDisplayValues()
        {
            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var upgrade = upgrades[i];
                if (refs == null || refs.statDisplayText == null || upgrade == null) continue;

                var lvl = controller ? controller.GetLevel(upgrade) : 0;
                var flat = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                var percent = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;

                var baseCurrent = upgrade.baseValue + lvl * upgrade.statIncreasePerLevel + flat;
                var current = baseCurrent * (1f + percent);

                var baseNext = upgrade.baseValue + (lvl + 1) * upgrade.statIncreasePerLevel + flat;
                var next = baseNext * (1f + percent);

                if (GetThreshold(upgrade) == null)
                    refs.statDisplayText.text = $"{current:0.###}";
                else
                    refs.statDisplayText.text = $"{current:0.###} -> {next:0.###}";
            }
        }

        /// <summary>
        ///     Updates the Defense upgrade's title to include current reduction percent and
        ///     sets a concise description with the formula and key breakpoints.
        /// </summary>
        private void UpdateDefenseInfoAndTitle()
        {
            // Find the Defense upgrade entry and its UI refs
            for (var i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var upgrade = upgrades[i];
                var refs = statReferences[i];
                if (upgrade == null || refs == null) continue;
                if (!string.Equals(upgrade.name, "Defense")) continue;

                // Compute current Defense value shown in this panel (same logic as display text)
                var lvl = controller ? controller.GetLevel(upgrade) : 0;
                var flat = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                var percent = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;
                var baseCurrent = upgrade.baseValue + lvl * upgrade.statIncreasePerLevel + flat;
                var currentDefense = baseCurrent * (1f + percent);

                // Use Combat's default defense tuning to avoid duplicating the scalar.
                var damageFraction = Combat.ApplyDefense(1f, currentDefense);
                var reduction = 1f - Mathf.Clamp01(damageFraction);

                if (refs.nameText != null)
                    refs.nameText.text = $"{upgrade.name} | {reduction * 100f:0.#}%";

                if (refs.descriptionText != null)
                    refs.descriptionText.text =
                        "Reduces incoming damage with diminishing returns.\n" +
                        "Key breakpoints: 25 Def = 50% less, 50 = 66.7% less, 125 = 83.3% less.";

                // Only one Defense entry expected
                break;
            }
        }
    }
}