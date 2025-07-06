using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Skills;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays stat upgrades with their costs and handles upgrading.
    /// </summary>
    public class StatUpgradeUIManager : MonoBehaviour
    {
        [SerializeField] private StatUpgradeController controller;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceInventoryUI resourceInventoryUI;
        [SerializeField] private List<StatUIReferences> statReferences = new();
        [SerializeField] private List<StatUpgrade> upgrades = new();
        [SerializeField] private CostResourceUIReferences costSlotPrefab;

        private SkillController skillController;
        private readonly List<List<CostResourceUIReferences>> costSlots = new();

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<StatUpgradeController>();
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceInventoryUI == null)
                resourceInventoryUI = FindFirstObjectByType<ResourceInventoryUI>();
            if (skillController == null)
                skillController = FindFirstObjectByType<SkillController>();
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
                    refs.upgradeButton.onClick.AddListener(() => ApplyUpgrade(index));
            }

            BuildAllCostSlots();
            UpdateStatLevels();
            UpdateStatDisplayValues();
        }

        private void OnEnable()
        {
            ShowLevelTextChanged += OnShowLevelTextChanged;
            OnLoadData += OnLoadDataHandler;
            OnShowLevelTextChanged();
        }

        private void OnDisable()
        {
            ShowLevelTextChanged -= OnShowLevelTextChanged;
            OnLoadData -= OnLoadDataHandler;
        }

        private void OnShowLevelTextChanged()
        {
            UpdateStatLevels();
            UpdateAllCostSlotValues();
            UpdateStatDisplayValues();
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
                            if (slot.selectButton != null)
                            {
                                var res = req.resource;
                                slot.selectButton.onClick.RemoveAllListeners();
                                slot.selectButton.onClick.AddListener(() => resourceInventoryUI?.HighlightResource(res));
                            }
                            list.Add(slot);
                        }
                    }
                }
                costSlots.Add(list);
            }
            UpdateAllCostSlotValues();
            UpdateStatDisplayValues();
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
                if (slot.questionMarkImage)
                    slot.questionMarkImage.enabled = !unlocked;

                if (slot.iconImage)
                {
                    slot.iconImage.sprite = req.resource ? req.resource.icon : null;
                    slot.iconImage.enabled = unlocked;
                }

                if (slot.countText)
                    slot.countText.text = cost.ToString();

                bool hasEnough = resourceManager == null || resourceManager.GetAmount(req.resource) >= cost;
                var grey = new Color(1f, 1f, 1f, 0.4f);
                if (slot.iconImage)
                    slot.iconImage.color = hasEnough ? Color.white : grey;

                if (slot.selectionImage)
                    slot.selectionImage.enabled = false;
                if (slot.selectButton)
                    slot.selectButton.interactable = true;
            }
        }

        private void ApplyUpgrade(int index)
        {
            if (index < 0 || index >= upgrades.Count) return;
            if (controller != null && controller.ApplyUpgrade(upgrades[index]))
            {
                BuildAllCostSlots();
                UpdateStatLevels();
                UpdateStatDisplayValues();
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

                refs.statDisplayText.text = $"{current:0.###} -> {next:0.###}";
            }
        }
    }
}
