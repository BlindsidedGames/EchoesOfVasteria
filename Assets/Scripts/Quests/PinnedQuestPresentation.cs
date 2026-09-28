using System.Text;
using UnityEngine;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using Blindsided.SaveData;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.Quests
{
    public static class PinnedQuestPresentation
    {
        public static (string text, bool ready) Build(QuestData data, GameData.QuestRecord rec)
        {
            var resourceManager = ResourceManager.Instance;
            var tracker = GameplayStatTracker.Instance;

                var completed = rec != null && rec.Completed;

                var progress = 0f;
                var reqCount = 0;

                var sb = new StringBuilder(QuestTextFormatter.BuildGoalText(data, rec, includeTitleLine: true));

                foreach (var req in data.requirements)
                {
                    var pct = 0f;
                    if (req.type == QuestData.RequirementType.Resource)
                    {
                        var current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)(current / target);
                    }
                    else if (req.type == QuestData.RequirementType.Kill)
                    {
                        double current = 0;
                        if (rec != null)
                        {
                            if (req.enemies != null && req.enemies.Count > 0)
                            {
                                foreach (var enemy in req.enemies)
                                    if (rec.KillProgress != null && rec.KillProgress.TryGetValue(enemy.name, out var c))
                                        current += c;
                            }
                            else if (rec.KillProgress != null && rec.KillProgress.TryGetValue("ANY", out var any))
                            {
                                current = any;
                            }
                        }
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)(current / target);
                    }
                    else if (req.type == QuestData.RequirementType.DistanceRun)
                    {
                        var current = tracker ? tracker.LongestRun : 0f;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.DistanceTravel)
                    {
                        var current = rec != null ? rec.DistanceTravelProgress : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.BuffCast)
                    {
                        double current;
                        if (req.buffs == null || req.buffs.Count == 0)
                        {
                            current = tracker ? tracker.BuffsCast : 0;
                            if (rec != null)
                                current -= rec.BuffCastBaseline;
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
                        }
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.CriticalStrike)
                    {
                        var current = tracker ? tracker.CriticalHits : 0;
                        if (rec != null)
                            current -= rec.CriticalBaseline;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.ResourcesGathered)
                    {
                        var current = tracker ? tracker.TotalResourcesGathered : 0;
                        if (rec != null)
                            current -= rec.ResourcesBaseline;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.TasksCompleted)
                    {
                        var current = tracker ? tracker.TasksCompleted : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.CauldronMix)
                    {
                        double current = rec != null ? rec.CauldronMixProgress : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.Instant)
                    {
                        pct = 1f;
                    }
                    else if (req.type == QuestData.RequirementType.Meet)
                    {
                        if (!string.IsNullOrEmpty(req.meetNpcId) && CompletedNpcTasks.Contains(req.meetNpcId))
                            pct = 1f;
                    }

                    progress += Mathf.Clamp01(pct);
                    reqCount++;
                }

                if (reqCount > 0)
                    progress /= reqCount;
                var ready = progress >= 1f;

            if (completed || ready)
            {
                var title = data.questName.GetLocalizedString();
                return (string.IsNullOrEmpty(title) ? "<size=80%>Ready to turn in</size>" : $"{title}\n<size=80%>Ready to turn in</size>", true);
            }
            return (sb.ToString(), false);
        }
    }
}
