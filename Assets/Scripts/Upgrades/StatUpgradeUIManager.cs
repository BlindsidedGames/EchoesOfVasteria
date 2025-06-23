using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Manages the UI for a single stat upgrade entry.
    /// </summary>
    public class StatUpgradeUIManager : MonoBehaviour
    {
        [SerializeField] private StatUpgradeController controller;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private StatUpgrade upgrade;
        [SerializeField] private StatUpgradeUIReferences references;

        private readonly List<ResourceUIReferences> costSlots = new();

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<StatUpgradeController>();
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (references == null)
                references = GetComponent<StatUpgradeUIReferences>();

            BuildCostSlots();
            UpdateUI();

            if (references.upgradeButton != null)
                references.upgradeButton.onClick.AddListener(ApplyUpgrade);
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        private void BuildCostSlots()
        {
            if (references == null || references.resourceSlotPrefab == null || references.costGridLayoutParent == null)
                return;

            foreach (Transform child in references.costGridLayoutParent.transform)
                Destroy(child.gameObject);
            costSlots.Clear();

            var threshold = GetThreshold();
            if (threshold == null) return;

            foreach (var req in threshold.requirements)
            {
                var obj = Instantiate(references.resourceSlotPrefab, references.costGridLayoutParent.transform);
                if (obj.TryGetComponent(out ResourceUIReferences slot))
                    costSlots.Add(slot);
            }
        }

        private void UpdateUI()
        {
            UpdateCostSlotValues();
            UpdateInfoText();
            if (references.upgradeButton)
                references.upgradeButton.interactable = controller && controller.CanUpgrade(upgrade);
        }

        private void UpdateCostSlotValues()
        {
            var threshold = GetThreshold();
            if (threshold == null) return;

            for (int i = 0; i < costSlots.Count && i < threshold.requirements.Count; i++)
            {
                var slot = costSlots[i];
                var req = threshold.requirements[i];
                int lvl = controller ? controller.GetLevel(upgrade) : 0;
                int cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;
                if (slot.questionMarkImage) slot.questionMarkImage.enabled = false;
                if (slot.iconImage)
                {
                    slot.iconImage.sprite = req.resource ? req.resource.icon : null;
                    slot.iconImage.enabled = true;
                }
                if (slot.countText) slot.countText.text = cost.ToString();
                if (slot.selectionImage) slot.selectionImage.enabled = false;
                if (slot.selectButton) slot.selectButton.interactable = false;
            }
        }

        private void UpdateInfoText()
        {
            if (references == null || references.statUpgradeInfoText == null) return;
            int lvl = controller ? controller.GetLevel(upgrade) : 0;
            float current = 1f + lvl * upgrade.statIncreasePerLevel;
            float next = current + upgrade.statIncreasePerLevel;
            references.statUpgradeInfoText.text = $"{upgrade.name} {current:0.###} -> {next:0.###}";
        }

        private void ApplyUpgrade()
        {
            if (controller != null && controller.ApplyUpgrade(upgrade))
            {
                BuildCostSlots();
                UpdateUI();
            }
        }

        private StatUpgrade.Threshold GetThreshold()
        {
            if (upgrade == null) return null;
            int lvl = controller ? controller.GetLevel(upgrade) : 0;
            foreach (var t in upgrade.thresholds)
            {
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            }
            return null;
        }
    }
}
