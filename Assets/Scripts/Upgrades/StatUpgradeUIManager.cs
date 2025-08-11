using System.Collections;
using System.Collections.Generic;
using References.UI;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using UnityEngine;
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
                TELogger.Log("StatUpgradeController missing", TELogCategory.Upgrade, this);
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            resourceInventoryUI = ResourceInventoryUI.Instance;
            if (resourceInventoryUI == null)
                TELogger.Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            skillController = SkillController.Instance;
            if (skillController == null)
                TELogger.Log("SkillController missing", TELogCategory.Upgrade, this);
            if (statReferences.Count == 0)
                statReferences.AddRange(GetComponentsInChildren<StatUIReferences>(true));

            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var upgrade = upgrades[i];
                if (refs.nameText != null)
                    refs.nameText.text = upgrade ? upgrade.name : string.Empty;
                if (refs.descriptionText != null)
                    refs.descriptionText.text = upgrade ? upgrade.description : string.Empty;
            }

            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                int index = i;
                var refs = statReferences[i];
                if (refs != null && refs.upgradeButton != null)
                {
                    refs.upgradeButton.onClick.AddListener(() => ApplyUpgrade(index));
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
            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var list = new List<CostResourceUIReferences>();
                var parent = refs.costGridLayoutParent;
                var prefab = costSlotPrefab;

                if (parent != null && prefab != null)
                {
                    foreach (Transform child in parent.transform)
                        Destroy(child.gameObject);

                    var threshold = GetThreshold(upgrades[i]);
                    if (threshold != null)
                    {
                        foreach (var req in threshold.requirements)
                        {
                            var slot = Instantiate(prefab, parent.transform);
                            slot.resource = req.resource;
                            slot.PointerClick += (_, button) =>
                            {
                                if (button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                                    resourceInventoryUI?.HighlightResource(req.resource);
                            };
                            list.Add(slot);
                        }
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
            for (int i = 0; i < costSlots.Count && i < upgrades.Count; i++)
                UpdateCostSlotValues(i);
        }

        private void UpdateCostSlotValues(int index)
        {
            if (index < 0 || index >= costSlots.Count || index >= upgrades.Count)
                return;

            var threshold = GetThreshold(upgrades[index]);
            var slots = costSlots[index];
            if (threshold == null) return;

            for (int j = 0; j < slots.Count && j < threshold.requirements.Count; j++)
            {
                var slot = slots[j];
                var req = threshold.requirements[j];
                int lvl = controller ? controller.GetLevel(upgrades[index]) : 0;
                int cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;


                bool unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                bool hasEnough = resourceManager == null || resourceManager.GetAmount(req.resource) >= cost;

                if (slot.iconImage)
                {
                    slot.iconImage.sprite = unlocked ? req.resource?.icon : req.resource?.UnknownIcon;
                    var grey = new Color(1f, 1f, 1f, 0.4f);
                    slot.iconImage.color = unlocked ? (hasEnough ? Color.white : grey) : Color.white;
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
            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                if (refs != null && refs.upgradeButton != null)
                {
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
            int lvl = controller ? controller.GetLevel(upgrade) : 0;
            foreach (var t in upgrade.thresholds)
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            return null;
        }

        private void UpdateStatLevels()
        {
            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var selector = statReferences[i];
                var upgrade = upgrades[i];
                if (selector == null || selector.countText == null) continue;

                if (ShowLevelText)
                {
                    int lvl = controller ? controller.GetLevel(upgrade) : 0;
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
            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var refs = statReferences[i];
                var upgrade = upgrades[i];
                if (refs == null || refs.statDisplayText == null || upgrade == null) continue;

                int lvl = controller ? controller.GetLevel(upgrade) : 0;
                float flat = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                float percent = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;

                float baseCurrent = upgrade.baseValue + lvl * upgrade.statIncreasePerLevel + flat;
                float current = baseCurrent * (1f + percent);

                float baseNext = upgrade.baseValue + (lvl + 1) * upgrade.statIncreasePerLevel + flat;
                float next = baseNext * (1f + percent);

                if (GetThreshold(upgrade) == null)
                    refs.statDisplayText.text = $"{current:0.###}";
                else
                    refs.statDisplayText.text = $"{current:0.###} -> {next:0.###}";
            }
        }

        /// <summary>
        /// Updates the Defense upgrade's title to include current reduction percent and
        /// sets a concise description with the formula and key breakpoints.
        /// </summary>
        private void UpdateDefenseInfoAndTitle()
        {
            // Find the Defense upgrade entry and its UI refs
            for (int i = 0; i < statReferences.Count && i < upgrades.Count; i++)
            {
                var upgrade = upgrades[i];
                var refs = statReferences[i];
                if (upgrade == null || refs == null) continue;
                if (!string.Equals(upgrade.name, "Defense")) continue;

                // Compute current Defense value shown in this panel (same logic as display text)
                int lvl = controller ? controller.GetLevel(upgrade) : 0;
                float flat = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                float percent = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;
                float baseCurrent = upgrade.baseValue + lvl * upgrade.statIncreasePerLevel + flat;
                float currentDefense = baseCurrent * (1f + percent);

                // Use Combat's default defense tuning to avoid duplicating the scalar.
                float damageFraction = TimelessEchoes.Combat.ApplyDefense(1f, currentDefense);
                float reduction = 1f - Mathf.Clamp01(damageFraction);

                if (refs.nameText != null)
                    refs.nameText.text = $"{upgrade.name} | {(reduction * 100f):0.#}%";

                if (refs.descriptionText != null)
                {
                    refs.descriptionText.text =
                        "Reduces incoming damage with diminishing returns.\n" +
                        "Key breakpoints: 25 Def = 50% less, 50 = 66.7% less, 125 = 83.3% less.";
                }

                // Only one Defense entry expected
                break;
            }
        }
    }
}
