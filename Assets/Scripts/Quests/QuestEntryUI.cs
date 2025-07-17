using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using References.UI;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Displays a single quest entry and handles its progress bar and turn in button.
    /// </summary>
    public class QuestEntryUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text rewardText;
        public Button turnInButton;
        public SlicedFilledImage progressImage;
        public Image questImage;
        public GameObject progressBar;
        public CostResourceUIReferences costSlotPrefab;
        public Transform costParent;

        private Action onTurnIn;
        private readonly List<(CostResourceUIReferences slot, QuestData.Requirement req)> costSlots = new();

        public void Setup(QuestData data, Action turnIn, bool showRequirements = true, bool completed = false)
        {
            onTurnIn = turnIn;
            if (nameText != null)
                nameText.text = data != null
                    ? data.questName + (completed ? " | Completed" : string.Empty)
                    : string.Empty;
            if (descriptionText != null)
                descriptionText.text = data != null ? data.description : string.Empty;
            if (rewardText != null)
                rewardText.text = data != null ? $"Reward: {data.rewardDescription}" : string.Empty;

            if (turnInButton != null)
            {
                turnInButton.onClick.RemoveAllListeners();
                if (onTurnIn != null && !completed) turnInButton.onClick.AddListener(() => onTurnIn());
                turnInButton.gameObject.SetActive(onTurnIn != null && !completed);
            }

            if (questImage != null)
            {
                var color = questImage.color;
                color.a = completed ? 0.7f : 1f;
                questImage.color = color;
            }

            if (progressBar != null)
                progressBar.SetActive(!completed);

            costSlots.Clear();
            if (costParent != null)
            {
                foreach (Transform child in costParent)
                    Destroy(child.gameObject);
                if (data != null && costSlotPrefab != null && showRequirements)
                {
                    var inventoryUI = ResourceInventoryUI.Instance;
                    var resourceManager = ResourceManager.Instance;
                    foreach (var req in data.requirements)
                    {
                        var slot = Instantiate(costSlotPrefab, costParent);
                        slot.resource = req.type == QuestData.RequirementType.Resource ||
                                        req.type == QuestData.RequirementType.Donation
                            ? req.resource
                            : null;
                        costSlots.Add((slot, req));

                        if (slot.iconImage != null)
                        {
                            if (req.type == QuestData.RequirementType.Resource ||
                                req.type == QuestData.RequirementType.Donation)
                            {
                                var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                                var unknownSprite = inventoryUI ? inventoryUI.UnknownSprite : null;
                                slot.iconImage.sprite =
                                    unlocked ? req.resource ? req.resource.icon : null : unknownSprite;
                                slot.iconImage.color =
                                    unlocked ? Color.white : new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
                            }
                            else if (req.type == QuestData.RequirementType.Kill)
                            {
                                slot.iconImage.sprite = req.killIcon;
                                slot.iconImage.color = Color.white;
                            }
                        }

                        if (slot.countText != null)
                            slot.countText.text = req.amount.ToString();

                        if (inventoryUI != null &&
                            (req.type == QuestData.RequirementType.Resource ||
                             req.type == QuestData.RequirementType.Donation))
                            slot.PointerClick += (_, button) =>
                            {
                                if (button == PointerEventData.InputButton.Left)
                                    inventoryUI.HighlightResource(req.resource);
                            };
                    }
                }
            }
        }

        public void SetProgress(float pct)
        {
            pct = Mathf.Clamp01(pct);
            if (progressImage != null)
                progressImage.fillAmount = pct;
            if (turnInButton != null)
                turnInButton.interactable = pct >= 1f;
        }

        /// <summary>
        ///     Refresh requirement icons based on current discovery state.
        /// </summary>
        public void UpdateRequirementIcons()
        {
            var inventoryUI = ResourceInventoryUI.Instance;
            var resourceManager = ResourceManager.Instance;
            foreach (var (slot, req) in costSlots)
            {
                if (slot == null || slot.iconImage == null || req == null)
                    continue;

                if (req.type == QuestData.RequirementType.Resource ||
                    req.type == QuestData.RequirementType.Donation)
                {
                    var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                    var unknownSprite = inventoryUI ? inventoryUI.UnknownSprite : null;
                    slot.iconImage.sprite = unlocked ? req.resource ? req.resource.icon : null : unknownSprite;
                    slot.iconImage.color = unlocked ? Color.white : new Color(0x74 / 255f, 0x3E / 255f, 0x38 / 255f);
                }
                else if (req.type == QuestData.RequirementType.Kill)
                {
                    slot.iconImage.sprite = req.killIcon;
                    slot.iconImage.color = Color.white;
                }
            }
        }
    }
}