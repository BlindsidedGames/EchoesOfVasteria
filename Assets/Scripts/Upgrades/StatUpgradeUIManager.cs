using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Skills;
using TMPro;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Manages the stat upgrade UI allowing selection between multiple stats.
    /// </summary>
    public class StatUpgradeUIManager : MonoBehaviour
    {
        [SerializeField] private StatUpgradeController controller;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceInventoryUI resourceInventoryUI;
        [SerializeField] private List<StatUIReferences> statSelectors = new();
        [SerializeField] private List<StatUpgrade> upgrades = new();
        [SerializeField] private StatUpgradeUIReferences references;
        [SerializeField] private TMP_Text descriptionText;
        private SkillController skillController;

        private readonly List<CostResourceUIReferences> costSlots = new();

        private int selectedIndex = -1;

        private StatUpgrade CurrentUpgrade =>
            selectedIndex >= 0 && selectedIndex < upgrades.Count ? upgrades[selectedIndex] : null;

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
            if (references == null)
                references = GetComponent<StatUpgradeUIReferences>();
            if (statSelectors.Count == 0)
                statSelectors.AddRange(GetComponentsInChildren<StatUIReferences>(true));

            for (var i = 0; i < statSelectors.Count; i++)
            {
                var index = i;
                if (statSelectors[i] != null && statSelectors[i].selectButton != null)
                    statSelectors[i].selectButton.onClick.AddListener(() => SelectStat(index));
            }

            if (references.upgradeButton != null)
                references.upgradeButton.onClick.AddListener(ApplyUpgrade);
            if (references.descriptionButton != null)
                references.descriptionButton.onClick.AddListener(ToggleDescription);
            if (references.descriptionPanel != null)
                references.descriptionPanel.SetActive(false);

            DeselectStat();
            if (references != null)
                references.gameObject.SetActive(false);
            UpdateStatSelectorLevels();
        }

        private void OnEnable()
        {
            if (selectedIndex < 0)
            {
                DeselectStat();
                if (references != null)
                    references.gameObject.SetActive(false);
            }
            else
            {
                UpdateUI();
            }

            UpdateStatSelectorLevels();

            ShowLevelTextChanged += OnShowLevelTextChanged;
            OnLoadData += OnLoadDataHandler;
            OnShowLevelTextChanged();
        }

        private void OnDisable()
        {
            ShowLevelTextChanged -= OnShowLevelTextChanged;
            OnLoadData -= OnLoadDataHandler;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (references != null && references.gameObject.activeSelf)
                    references.gameObject.SetActive(false);
                DeselectStat();
            }
        }

        private void OnShowLevelTextChanged()
        {
            UpdateStatSelectorLevels();
            if (selectedIndex >= 0)
                UpdateUI();
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

        private void DeselectStat()
        {
            selectedIndex = -1;
            foreach (var selector in statSelectors)
                if (selector != null && selector.selectionImage != null)
                    selector.selectionImage.enabled = false;
            if (references?.descriptionPanel != null)
                references.descriptionPanel.SetActive(false);
        }

        private void SelectStat(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, statSelectors.Count - 1);

            for (var i = 0; i < statSelectors.Count; i++)
            {
                var selector = statSelectors[i];
                if (selector != null && selector.selectionImage != null)
                    selector.selectionImage.enabled = i == selectedIndex;
            }

            if (references != null && selectedIndex >= 0 && selectedIndex < statSelectors.Count)
            {
                var pos = references.transform.position;
                var target = statSelectors[selectedIndex].transform.position;
                references.transform.position = new Vector3(pos.x, target.y, pos.z);
                references.gameObject.SetActive(true);
                if (references.descriptionPanel)
                    references.descriptionPanel.SetActive(false);
            }

            BuildCostSlots();
            UpdateUI();
        }

        private void BuildCostSlots()
        {
            if (references == null || references.costSlotPrefab == null || references.costGridLayoutParent == null)
                return;

            foreach (Transform child in references.costGridLayoutParent.transform)
                Destroy(child.gameObject);
            costSlots.Clear();

            var threshold = GetThreshold();
            if (threshold == null) return;

            foreach (var req in threshold.requirements)
            {
                var slot = Instantiate(references.costSlotPrefab, references.costGridLayoutParent.transform);
                slot.resource = req.resource;
                if (slot.selectButton != null)
                {
                    var res = req.resource;
                    slot.selectButton.onClick.AddListener(() => resourceInventoryUI?.HighlightResource(res));
                }

                costSlots.Add(slot);
            }
        }

        private void UpdateUI()
        {
            UpdateCostSlotValues();
            UpdateInfoText();
            UpdateDescription();
            if (references.upgradeButton)
                references.upgradeButton.interactable = controller && controller.CanUpgrade(CurrentUpgrade);
            UpdateStatSelectorLevels();
        }

        private void UpdateCostSlotValues()
        {
            var threshold = GetThreshold();
            if (threshold == null) return;

            for (var i = 0; i < costSlots.Count && i < threshold.requirements.Count; i++)
            {
                var slot = costSlots[i];
                var req = threshold.requirements[i];
                var lvl = controller ? controller.GetLevel(CurrentUpgrade) : 0;
                var cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;

                var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                if (slot.questionMarkImage)
                    slot.questionMarkImage.enabled = !unlocked;

                if (slot.iconImage)
                {
                    slot.iconImage.sprite = req.resource ? req.resource.icon : null;
                    slot.iconImage.enabled = unlocked;
                }

                if (slot.countText)
                    slot.countText.text = cost.ToString();

                // Grey out the icon and text when the player lacks resources
                var hasEnough = resourceManager == null || resourceManager.GetAmount(req.resource) >= cost;
                var grey = new Color(1f, 1f, 1f, 0.4f);
                if (slot.iconImage)
                    slot.iconImage.color = hasEnough ? Color.white : grey;

                if (slot.selectionImage)
                    slot.selectionImage.enabled = false;
                if (slot.selectButton)
                    slot.selectButton.interactable = true;
            }
        }

        private void UpdateInfoText()
        {
            if (references == null || references.statUpgradeInfoText == null) return;
            var upgrade = CurrentUpgrade;
            if (upgrade == null) return;
            var lvl = controller ? controller.GetLevel(upgrade) : 0;
            var flat = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
            var percent = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;

            var baseCurrent = upgrade.baseValue + lvl * upgrade.statIncreasePerLevel + flat;
            var current = baseCurrent * (1f + percent);

            var baseNext = upgrade.baseValue + (lvl + 1) * upgrade.statIncreasePerLevel + flat;
            var next = baseNext * (1f + percent);
            references.statUpgradeInfoText.text = $"{current:0.###} -> {next:0.###}";
        }

        private void UpdateDescription()
        {
            if (descriptionText == null || references == null) return;
            var upgrade = CurrentUpgrade;
            if (upgrade == null)
            {
                descriptionText.text = string.Empty;
                if (references.descriptionButton)
                    references.descriptionButton.gameObject.SetActive(false);
                if (references.descriptionPanel)
                    references.descriptionPanel.SetActive(false);
                return;
            }

            var hasDescription = !string.IsNullOrWhiteSpace(upgrade.description);
            if (references.descriptionButton)
                references.descriptionButton.gameObject.SetActive(hasDescription);
            if (!hasDescription)
            {
                descriptionText.text = string.Empty;
                if (references.descriptionPanel)
                    references.descriptionPanel.SetActive(false);
                return;
            }

            descriptionText.text = upgrade.description;
        }

        private void ApplyUpgrade()
        {
            if (controller != null && controller.ApplyUpgrade(CurrentUpgrade))
            {
                BuildCostSlots();
                UpdateUI();
                UpdateStatSelectorLevels();
            }
        }

        private void ToggleDescription()
        {
            if (references?.descriptionPanel == null) return;
            var active = references.descriptionPanel.activeSelf;
            references.descriptionPanel.SetActive(!active);
        }

        private StatUpgrade.Threshold GetThreshold()
        {
            var upgrade = CurrentUpgrade;
            if (upgrade == null) return null;
            var lvl = controller ? controller.GetLevel(upgrade) : 0;
            foreach (var t in upgrade.thresholds)
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            return null;
        }

        private void UpdateStatSelectorLevels()
        {
            for (var i = 0; i < statSelectors.Count && i < upgrades.Count; i++)
            {
                var selector = statSelectors[i];
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
    }
}