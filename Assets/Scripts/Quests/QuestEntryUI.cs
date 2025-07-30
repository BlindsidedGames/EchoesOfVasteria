using System;
using System.Collections.Generic;
using Blindsided;
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
        public TMP_Text typeText;
        public Button turnInButton;
        public TMP_Text turnInText;
        public SlicedFilledImage progressImage;
        public Image questImage;
        public GameObject progressBar;
        public Button pinButton;
        public CostResourceUIReferences costSlotPrefab;
        public Transform costParent;

        private Action onTurnIn;
        private string baseNameText = string.Empty;
        private readonly List<(CostResourceUIReferences slot, QuestData.Requirement req)> costSlots = new();

        public void Setup(QuestData data, Action turnIn, bool showRequirements = true, bool completed = false)
        {
            onTurnIn = turnIn;
            if (nameText != null)
            {
                baseNameText = data != null
                    ? data.questName + (completed ? " | Completed" : string.Empty)
                    : string.Empty;
                nameText.text = baseNameText;
            }
            if (descriptionText != null)
                descriptionText.text = data != null ? data.description : string.Empty;
            if (rewardText != null)
                rewardText.text = data != null ? $"Reward: {data.rewardDescription}" : string.Empty;
            if (typeText != null)
                typeText.text = data != null ? $"Type | {GetQuestType(data)}" : string.Empty;

            if (turnInButton != null)
            {
                turnInButton.onClick.RemoveAllListeners();
                if (onTurnIn != null && !completed) turnInButton.onClick.AddListener(() => onTurnIn());
                turnInButton.gameObject.SetActive(onTurnIn != null && !completed);
            }
            if (turnInText != null)
            {
                var label = "Turn In";
                if (data != null && data.requirements != null && data.requirements.Count > 0)
                {
                    var type = data.requirements[0].type;
                    if (type == QuestData.RequirementType.Meet)
                        label = "Done";
                    else if (type == QuestData.RequirementType.Instant)
                        label = "Okay";
                }
                turnInText.text = label;
            }

            if (pinButton != null && data != null)
            {
                pinButton.onClick.RemoveAllListeners();
                var pinned = Oracle.oracle != null &&
                             Oracle.oracle.saveData != null &&
                             Oracle.oracle.saveData.PinnedQuests != null &&
                             Oracle.oracle.saveData.PinnedQuests.Contains(data.questId);
                UpdatePinVisual(pinned);
                pinButton.onClick.AddListener(() =>
                {
                    var qm = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
                    qm?.TogglePinned(data.questId);
                    var nowPinned = Oracle.oracle != null &&
                                    Oracle.oracle.saveData != null &&
                                    Oracle.oracle.saveData.PinnedQuests != null &&
                                    Oracle.oracle.saveData.PinnedQuests.Contains(data.questId);
                    UpdatePinVisual(nowPinned);
                });
                var instant = false;
                if (data.requirements != null)
                    foreach (var req in data.requirements)
                        if (req != null && req.type == QuestData.RequirementType.Instant)
                        {
                            instant = true;
                            break;
                        }
                pinButton.gameObject.SetActive(!completed && !instant);
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
                        if (req.type == QuestData.RequirementType.Instant ||
                            req.type == QuestData.RequirementType.DistanceRun ||
                            req.type == QuestData.RequirementType.DistanceTravel ||
                            req.type == QuestData.RequirementType.Meet ||
                            req.type == QuestData.RequirementType.BuffCast)
                            continue;

                        var slot = Instantiate(costSlotPrefab, costParent);
                        slot.resource = req.type == QuestData.RequirementType.Resource
                            ? req.resource
                            : null;
                        costSlots.Add((slot, req));

                        if (slot.iconImage != null)
                        {
                            if (req.type == QuestData.RequirementType.Resource)
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
                            else if (req.type == QuestData.RequirementType.DistanceRun ||
                                     req.type == QuestData.RequirementType.DistanceTravel ||
                                     req.type == QuestData.RequirementType.Meet ||
                                     req.type == QuestData.RequirementType.BuffCast)
                            {
                                slot.iconImage.sprite = null;
                                slot.iconImage.color = Color.white;
                            }
                            else if (req.type == QuestData.RequirementType.Instant)
                            {
                                slot.iconImage.sprite = null;
                                slot.iconImage.color = Color.white;
                            }
                        }

                        if (slot.countText != null)
                            slot.countText.text = req.amount.ToString();

                        if (inventoryUI != null &&
                            req.type == QuestData.RequirementType.Resource)
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

                if (req.type == QuestData.RequirementType.Resource)
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
                else if (req.type == QuestData.RequirementType.DistanceRun ||
                         req.type == QuestData.RequirementType.DistanceTravel ||
                         req.type == QuestData.RequirementType.Meet ||
                         req.type == QuestData.RequirementType.BuffCast)
                {
                    slot.iconImage.sprite = null;
                    slot.iconImage.color = Color.white;
                }
                else if (req.type == QuestData.RequirementType.Instant)
                {
                    slot.iconImage.sprite = null;
                    slot.iconImage.color = Color.white;
                }
            }
        }

        private void UpdatePinVisual(bool pinned)
        {
            if (pinButton != null)
            {
                var txt = pinButton.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                    txt.text = pinned ? "Unpin" : "Pin";
            }

            if (nameText != null)
                nameText.text = baseNameText + (pinned ? " | Pinned" : string.Empty);
        }

        private static string GetQuestType(QuestData data)
        {
            if (data == null || data.requirements == null || data.requirements.Count == 0)
                return string.Empty;
            var type = data.requirements[0].type;
            switch (type)
            {
                case QuestData.RequirementType.Resource:
                    return "Gathering";
                case QuestData.RequirementType.Kill:
                    return "Kill";
                case QuestData.RequirementType.DistanceRun:
                    return "Run Distance";
                case QuestData.RequirementType.DistanceTravel:
                    return "Travel";
                case QuestData.RequirementType.BuffCast:
                    return "Buffs";
                case QuestData.RequirementType.Instant:
                    return "Information";
                case QuestData.RequirementType.Meet:
                    return "Meet";
                default:
                    return type.ToString();
            }
        }
    }
}