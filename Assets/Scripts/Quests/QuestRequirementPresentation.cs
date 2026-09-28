using System;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using TimelessEchoes.Stats;
using UnityEngine;
namespace TimelessEchoes.Quests
{
    public static class QuestRequirementPresentation
    {
        public static (string text, float progress) Build(QuestData data, QuestData.Requirement req)
        {
            GameData.QuestRecord record = null;
            Oracle.oracle?.saveData?.Quests?.TryGetValue(data.questId, out record);
            ComputeRequirementProgress(data, req, record, out var current, out var target, out var label, out var counts, out var meet, out var appendSpace);
            string text;
            if (meet) text = current >= target && target > 0 ? "<size=80%>Complete 1/1</size>" : "<size=80%>Meet an NPC</size>";
            else
            {
                var space = appendSpace ? " " : string.Empty;
                text = counts && target > 0 ? $"{label}{space}{FormatValue(data, Math.Min(current, target))} / {FormatValue(data, target)}</size>"
                    : counts ? $"{label}{space}{FormatValue(data, current)}</size>" : label + "</size>";
            }
            var progress = target > 0 ? (float)(current / target) : 0;
            if (meet) progress = current >= target && target > 0 ? 1 : 0;
            return (text, Mathf.Clamp01(progress));
        }
        public static void ComputeRequirementProgress(
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

        public static string FormatValue(QuestData data, double value)
        {
            return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0") : CalcUtils.FormatNumber(value, true);
        }
    }
}
