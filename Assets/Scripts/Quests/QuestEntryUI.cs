using System;
using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using References.UI;
using TimelessEchoes.UI;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using TimelessEchoes.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
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
        [SerializeField] private LocalizeStringEvent nameStringEvent;
        [SerializeField] private LocalizeStringEvent descriptionStringEvent;
        [SerializeField] private LocalizeStringEvent rewardStringEvent;
        public Button turnInButton;
        public TMP_Text turnInText;
        // Aggregated type/progress removed in favor of per-requirement rows
        public QuestRequirementUIReferences requirementSlotPrefab;
        public Transform requirementParent;
        public Image questImage;
        public Button pinButton;
        // Legacy cost UI removed

        private Action onTurnIn;
        private bool entryCompleted;
        private bool pinnedState;
        private bool rewardHasQuestData;
        private string baseNameText = string.Empty;
        private readonly List<(QuestRequirementUIReferences ui, QuestData.Requirement req)> requirementRows = new();

        public void Setup(QuestData data, Action turnIn, bool showRequirements = true, bool completed = false)
        {
            onTurnIn = turnIn;
            entryCompleted = completed;
            pinnedState = false;

            var localizedName = data != null ? data.questName.GetLocalizedString() : string.Empty;
            baseNameText = BuildBaseName(localizedName);

            ConfigureLocalizationEvents(data);

            if (nameText != null)
                nameText.text = baseNameText;

            if (descriptionStringEvent == null && descriptionText != null)
                descriptionText.text = data != null ? data.description.GetLocalizedString() : string.Empty;
            else if (data == null && descriptionText != null)
                descriptionText.text = string.Empty;

            if (rewardStringEvent == null && rewardText != null)
                rewardText.text = data != null
                    ? $"Reward: {data.rewardDescription.GetLocalizedString()}"
                    : string.Empty;
            else if (data == null && rewardText != null)
                rewardText.text = string.Empty;
            // Build per-requirement rows (skip for completed entries or if disabled)
            if (requirementParent != null)
            {
                UIUtils.ClearChildren(requirementParent);
                requirementRows.Clear();

                if (data != null && !completed && showRequirements)
                {
                    if (requirementSlotPrefab != null)
                    {
                        foreach (var req in data.requirements)
                        {
                            if (req == null) continue;
                            // Skip instant requirements entirely
                            if (req.type == QuestData.RequirementType.Instant)
                                continue;

                            var row = UnityEngine.Object.Instantiate(requirementSlotPrefab, requirementParent);
                            requirementRows.Add((row, req));
                        }
                    }
                }
            }

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
                    var qm = QuestManager.Instance;
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

            // Initial update of requirement rows
            UpdateGoalText(data);
        }

        public void SetProgress(float pct)
        {
            pct = Mathf.Clamp01(pct);
            if (turnInButton != null)
                turnInButton.interactable = pct >= 1f;
        }

        // Legacy signature retained; rows update handled in UpdateGoalText
        public void UpdateRequirementIcons() { }

        public void UpdateGoalText(QuestData data)
        {
            if (data == null) return;
            GameData.QuestRecord rec = null;
            var quests = Oracle.oracle?.saveData?.Quests;
            if (quests != null)
                quests.TryGetValue(data.questId, out rec);

            // If no rows were created (completed entries), nothing to update
            if (requirementRows.Count == 0)
                return;

            foreach (var (ui, req) in requirementRows)
            {
                if (ui == null || req == null) continue;
                double current;
                double target;
                string label;
                bool showCounts;
                bool isMeet;
                bool appendSpace;
                ComputeRequirementProgress(data, req, rec, out current, out target, out label, out showCounts, out isMeet, out appendSpace);

                if (ui.requirementText != null)
                {
                    if (isMeet)
                    {
                        if (current >= target && target > 0)
                            ui.requirementText.text = "<size=80%>Complete 1/1</size>";
                        else
                            ui.requirementText.text = "<size=80%>Meet an NPC</size>";
                    }
                    else
                    {
                        var space = appendSpace ? " " : string.Empty;
                        if (showCounts && target > 0)
                        {
                            var clamped = Math.Min(current, target);
                            ui.requirementText.text = $"{label}{space}{FormatValue(data, clamped)} / {FormatValue(data, target)}</size>";
                        }
                        else if (showCounts)
                        {
                            ui.requirementText.text = $"{label}{space}{FormatValue(data, current)}</size>";
                        }
                        else
                        {
                            ui.requirementText.text = label + "</size>";
                        }
                    }
                }

                if (ui.progressBar != null)
                {
                    float pct = 0f;
                    if (target > 0)
                        pct = (float)(current / target);
                    if (isMeet)
                        pct = current >= target && target > 0 ? 1f : 0f;
                    pct = Mathf.Clamp01(pct);
                    ui.progressBar.fillAmount = pct;
                }
            }
        }

        private void ConfigureLocalizationEvents(QuestData data)
        {
            rewardHasQuestData = data != null;
            ConfigureNameLocalization(data);
            ConfigureDescriptionLocalization(data);
            ConfigureRewardLocalization(data);
        }

        private void ConfigureNameLocalization(QuestData data)
        {
            if (nameStringEvent == null)
                return;

            nameStringEvent.OnUpdateString.RemoveListener(OnNameLocalized);
            nameStringEvent.StringReference = data != null ? data.questName : default;
            if (data != null)
            {
                nameStringEvent.OnUpdateString.AddListener(OnNameLocalized);
                nameStringEvent.RefreshString();
            }
            else
            {
                nameStringEvent.RefreshString();
                if (nameText != null)
                {
                    baseNameText = string.Empty;
                    nameText.text = string.Empty;
                }
            }
        }

        private void ConfigureDescriptionLocalization(QuestData data)
        {
            if (descriptionStringEvent == null)
                return;

            descriptionStringEvent.StringReference = data != null ? data.description : default;
            descriptionStringEvent.RefreshString();
            if (data == null && descriptionText != null)
                descriptionText.text = string.Empty;
        }

        private void ConfigureRewardLocalization(QuestData data)
        {
            if (rewardStringEvent == null)
                return;

            rewardStringEvent.OnUpdateString.RemoveListener(OnRewardLocalized);
            rewardStringEvent.StringReference = data != null ? data.rewardDescription : default;
            rewardStringEvent.OnUpdateString.AddListener(OnRewardLocalized);
            rewardStringEvent.RefreshString();
            if (!rewardHasQuestData && rewardText != null)
                rewardText.text = string.Empty;
        }

        private string BuildBaseName(string localizedName)
        {
            localizedName ??= string.Empty;
            return entryCompleted ? localizedName + " | Completed" : localizedName;
        }

        private void OnNameLocalized(string localizedValue)
        {
            baseNameText = BuildBaseName(localizedValue);
            UpdatePinVisual(pinnedState);
        }

        private void OnRewardLocalized(string localizedValue)
        {
            if (rewardText == null)
                return;

            if (!rewardHasQuestData)
            {
                rewardText.text = string.Empty;
                return;
            }

            rewardText.text = localizedValue != null
                ? $"Reward: {localizedValue}"
                : "Reward: ";

        }

        private void UpdatePinVisual(bool pinned)
        {
            pinnedState = pinned;

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
                case QuestData.RequirementType.CriticalStrike:
                    return "Critical Hits";
                case QuestData.RequirementType.ResourcesGathered:
                    return "Gather Resources";
                case QuestData.RequirementType.TasksCompleted:
                    return "Tasks Completed";
                case QuestData.RequirementType.CauldronMix:
                    return "Mix Resources";
                default:
                    return type.ToString();
            }
        }

        private static void ComputeRequirementProgress(QuestData data, QuestData.Requirement req, GameData.QuestRecord rec,
            out double current, out double target, out string label, out bool showCounts, out bool isMeet, out bool appendSpace)
        {
            QuestRequirementPresentation.ComputeRequirementProgress(data, req, rec, out current, out target, out label, out showCounts, out isMeet, out appendSpace);
        }

        private void OnDestroy()
        {
            if (nameStringEvent != null)
                nameStringEvent.OnUpdateString.RemoveListener(OnNameLocalized);

            if (rewardStringEvent != null)
                rewardStringEvent.OnUpdateString.RemoveListener(OnRewardLocalized);
        }

        private static string FormatValue(QuestData data, double value)
        {
            return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0") : CalcUtils.FormatNumber(value, true);
        }
    }
}
