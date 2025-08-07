using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.Buffs
{
    [ManageableData]
    [CreateAssetMenu(fileName = "BuffRecipe", menuName = "SO/Buff Recipe")]
    public class BuffRecipe : ScriptableObject
    {
        [TitleGroup("General")]
        [Tooltip("Display name for this buff. If empty the asset name will be used.")]
        public string title;

        [TitleGroup("General")]
        [TextArea]
        public string description;

        [TitleGroup("General")]
        public Sprite buffIcon;

        [TitleGroup("General")]
        public BuffDurationType durationType = BuffDurationType.Time;

        [TitleGroup("General")]
        [MinValue(0f)]
        public float baseDuration = 30f;

        [TitleGroup("General")]
        [MinValue(0)]
        public int baseEchoCount;

        [TitleGroup("General")]
        [SerializeField]
        public TimelessEchoes.EchoSpawnConfig echoConfig;

        [TitleGroup("General")]
        public QuestData requiredQuest;

        [TitleGroup("Effects")]
        public List<BuffEffect> baseEffects = new();

        [TitleGroup("Upgrades")]
        public List<BuffUpgrade> upgrades = new();

        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(title) ? name : title;
        }

        public int GetCurrentLevel()
        {
            var level = 0;
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm == null) return 0;
            foreach (var up in upgrades)
            {
                if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                    level++;
            }
            return level;
        }

        public List<BuffEffect> GetAggregatedEffects()
        {
            var dict = new Dictionary<BuffEffectType, float>();
            foreach (var eff in baseEffects)
            {
                if (dict.ContainsKey(eff.type))
                    dict[eff.type] += eff.value;
                else
                    dict[eff.type] = eff.value;
            }
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest == null || !qm.IsQuestCompleted(up.quest))
                        continue;
                    foreach (var eff in up.additionalEffects)
                    {
                        if (dict.ContainsKey(eff.type))
                            dict[eff.type] += eff.value;
                        else
                            dict[eff.type] = eff.value;
                    }
                }
            }
            var list = new List<BuffEffect>();
            foreach (var pair in dict)
                list.Add(new BuffEffect { type = pair.Key, value = pair.Value });
            return list;
        }

        public int GetEchoCount()
        {
            var count = baseEchoCount;
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                        count += up.echoCountDelta;
                }
            }
            return Mathf.Max(0, count);
        }

        public float GetDuration()
        {
            var duration = baseDuration;
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                        duration += up.durationDelta;
                }
            }
            return duration;
        }

        public List<string> GetDescriptionLines()
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(description))
                lines.Add(description);

            var effectStrings = new List<string>();
            foreach (var eff in GetAggregatedEffects())
            {
                var text = DescribeEffect(eff);
                if (!string.IsNullOrEmpty(text))
                    effectStrings.Add(text);
            }

            for (var i = 0; i < effectStrings.Count; i += 3)
            {
                var count = Math.Min(3, effectStrings.Count - i);
                lines.Add(string.Join(", ", effectStrings.GetRange(i, count)));
            }

            var echoCount = GetEchoCount();
            if (echoCount > 0)
                lines.Add($"Echoes: {echoCount}");
            if (durationType == BuffDurationType.DistancePercent)
                lines.Add($"Distance: {Mathf.CeilToInt(GetDuration() * 100f)}%");
            else if (durationType == BuffDurationType.ExtraDistancePercent)
                lines.Add($"Extra Distance: {Mathf.CeilToInt(GetDuration() * 100f)}%");
            else
                lines.Add($"Duration: {CalcUtils.FormatTime(GetDuration(), shortForm: true)}");
            return lines;
        }

        private static string DescribeEffect(BuffEffect eff)
        {
            return eff.type switch
            {
                BuffEffectType.MoveSpeedPercent => $"Move Speed +{eff.value}%",
                BuffEffectType.DamagePercent => $"Damage +{eff.value}%",
                BuffEffectType.DefensePercent => $"Defense +{eff.value}%",
                BuffEffectType.AttackSpeedPercent => $"Attack Speed +{eff.value}%",
                BuffEffectType.TaskSpeedPercent => $"Task Speed +{eff.value}%",
                BuffEffectType.LifestealPercent => $"Lifesteal {eff.value}%",
                BuffEffectType.MaxDistancePercent => $"Max Reap Distance +{eff.value}%",
                BuffEffectType.InstantTasks => "Tasks complete instantly",
                _ => string.Empty
            };
        }
    }
}
