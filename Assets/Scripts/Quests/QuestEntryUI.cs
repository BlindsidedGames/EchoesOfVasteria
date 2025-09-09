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
        public TMP_Text turnInText;
        // Aggregated type/progress removed in favor of per-requirement rows
        public QuestRequirementUIReferences requirementSlotPrefab;
        public Transform requirementParent;
        public Image questImage;
        public Button pinButton;
        // Legacy cost UI removed

        private Action onTurnIn;
        private string baseNameText = string.Empty;
        private readonly List<(QuestRequirementUIReferences ui, QuestData.Requirement req)> requirementRows = new();

        public void Setup(QuestData data, Action turnIn, bool showRequirements = true, bool completed = false)
        {
            onTurnIn = turnIn;
            if (nameText != null)
            {
                baseNameText = data != null
                    ? data.questName.GetLocalizedString() + (completed ? " | Completed" : string.Empty)
                    : string.Empty;
                nameText.text = baseNameText;
            }

            if (descriptionText != null)
                descriptionText.text = data != null ? data.description.GetLocalizedString() : string.Empty;
            if (rewardText != null)
                rewardText.text =
                    data != null ? $"Reward: {data.rewardDescription.GetLocalizedString()}" : string.Empty;
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

        private static void ComputeRequirementProgress(
            QuestData data,
            QuestData.Requirement req,
            GameData.QuestRecord rec,
            out double current,
            out double target,
            out string label,
            out bool showCounts,
            out bool isMeet,
            out bool appendSpace)
        {
            current = 0;
            target = req != null ? req.amount : 0;
            label = string.Empty;
            showCounts = true;
            isMeet = false;
            appendSpace = true;

            var resourceManager = ResourceManager.Instance;
            var tracker = GameplayStatTracker.Instance;

            if (req == null)
                return;

            switch (req.type)
            {
                case QuestData.RequirementType.Resource:
                    current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                    {
                        var iconTag = string.Empty;
                        if (req.resource)
                        {
                            var unlocked = resourceManager && resourceManager.IsUnlocked(req.resource);
                            iconTag = unlocked ? ResourceIconLookup.GetIconTag(req.resource.resourceID)
                                               : ResourceIconLookup.GetUnknownIconTag(req.resource.resourceID);
                        }
                        var fallbackName = req.resource ? req.resource.name : string.Empty;
                        var labelText = string.IsNullOrEmpty(iconTag) ? fallbackName : iconTag;
                        // If using a sprite tag, do not add an extra space after it
                        var separator = string.IsNullOrEmpty(iconTag) ? ": " : string.Empty;
                        label = $"<size=90%>{labelText}{separator}";
                        // Resource rows include their own separator; do not auto-append an extra space
                        appendSpace = false;
                    }
                    break;
                case QuestData.RequirementType.Kill:
                    if (rec != null)
                    {
                        if (req.enemies != null && req.enemies.Count > 0)
                        {
                            foreach (var enemy in req.enemies)
                                if (enemy != null && rec.KillProgress != null && rec.KillProgress.TryGetValue(enemy.name, out var c))
                                    current += c;
                        }
                        else
                        {
                            if (rec.KillProgress != null && rec.KillProgress.TryGetValue("ANY", out var any))
                                current = any;
                        }
                    }
                    if (!string.IsNullOrEmpty(req.killName))
                        label = "<size=80%>Kill " + req.killName + ":";
                    else if (req.enemies == null || req.enemies.Count == 0)
                        label = "<size=80%>Kill enemies:";
                    else
                        label = "<size=80%>Kill:";
                    break;
                case QuestData.RequirementType.DistanceRun:
                    current = tracker ? tracker.LongestRun : 0f;
                    label = "<size=80%>Run Distance:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.DistanceTravel:
                    current = rec != null ? rec.DistanceTravelProgress : 0;
                    label = "<size=80%>Steps Taken:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.BuffCast:
                    if (req.buffs == null || req.buffs.Count == 0)
                    {
                        current = tracker ? tracker.BuffsCast : 0;
                        if (rec != null)
                            current -= rec.BuffCastBaseline;
                        label = "<size=80%>Cast Buffs:";
                        appendSpace = true;
                    }
                    else
                    {
                        current = 0;
                        if (rec != null && rec.BuffCastProgress != null)
                        {
                            foreach (var b in req.buffs)
                            {
                                if (b == null) continue;
                                if (rec.BuffCastProgress.TryGetValue(b.name, out var c))
                                    current += c;
                            }
                        }
                        var name = !string.IsNullOrEmpty(req.buffCastName)
                            ? req.buffCastName
                            : string.Join(", ", req.buffs.FindAll(b => b != null).ConvertAll(b => b.GetDisplayName()));
                        label = req.includeAutoCasts ? $"<size=80%>Cast {name}:" : $"<size=80%>Manually cast {name}:";
                        appendSpace = true;
                    }
                    break;
                case QuestData.RequirementType.CriticalStrike:
                    current = tracker ? tracker.CriticalHits : 0;
                    if (rec != null)
                        current -= rec.CriticalBaseline;
                    label = "<size=80%>Critical hits:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.ResourcesGathered:
                    current = tracker ? tracker.TotalResourcesGathered : 0;
                    if (rec != null)
                        current -= rec.ResourcesBaseline;
                    label = "<size=80%>Gather resources:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.TasksCompleted:
                    current = tracker ? tracker.TasksCompleted : 0;
                    label = "<size=80%>Tasks completed:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.CauldronMix:
                    current = rec != null ? rec.CauldronMixProgress : 0;
                    label = "<size=80%>Mix Resources:";
                    appendSpace = true;
                    break;
                case QuestData.RequirementType.Instant:
                    // No row spawned for instant
                    showCounts = false;
                    label = string.Empty;
                    appendSpace = false;
                    break;
                case QuestData.RequirementType.Meet:
                    isMeet = true;
                    // If met, current becomes target
                    if (!string.IsNullOrEmpty(req.meetNpcId) && Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains(req.meetNpcId))
                        current = target > 0 ? target : 1;
                    else
                        current = 0;
                    showCounts = false;
                    label = "<size=80%>Meet an NPC</size>";
                    if (target <= 0) target = 1; // treat as 1/1 when completed
                    appendSpace = false;
                    break;
                default:
                    label = "<size=80%>" + req.type + ":" + "</size>";
                    appendSpace = true;
                    break;
            }
        }

        private static string FormatValue(QuestData data, double value)
        {
            return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0") : CalcUtils.FormatNumber(value, true);
        }
    }
}
