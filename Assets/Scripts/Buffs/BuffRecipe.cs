using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
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
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        [MinValue(0f)]
        public float baseDuration = 30f;

        [TitleGroup("General")]
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        [MinValue(0f)]
        public float baseCooldown = 60f;

        [TitleGroup("General")]
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        [MinValue(0)]
        public int baseEchoCount;

        [TitleGroup("General")]
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        [SerializeField]
        public TimelessEchoes.EchoSpawnConfig echoConfig;

        [TitleGroup("General")]
        public QuestData requiredQuest;

        [TitleGroup("Effects")]
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        public List<BuffEffect> baseEffects = new();

        [TitleGroup("Upgrades")]
        [HideIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        public List<BuffUpgrade> upgrades = new();

        [TitleGroup("Extra Distance")]
        [ShowIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        public List<BuffEffect> extraDistanceEffects = new();

        [TitleGroup("Extra Distance")]
        [ShowIf("@durationType == BuffDurationType.ExtraDistancePercent")]
        public List<BuffUpgrade> extraDistanceUpgrades = new();

        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(title) ? name : title;
        }

        public int GetCurrentLevel()
        {
            var level = 0;
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm == null) return 0;
            var upgradeList = durationType == BuffDurationType.ExtraDistancePercent ? extraDistanceUpgrades : upgrades;
            foreach (var up in upgradeList)
            {
                if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                    level++;
            }
            return level;
        }

        public List<BuffEffect> GetAggregatedEffects()
        {
            var dict = new Dictionary<BuffEffectType, float>();
            var effList = durationType == BuffDurationType.ExtraDistancePercent ? extraDistanceEffects : baseEffects;
            foreach (var eff in effList)
            {
                if (dict.ContainsKey(eff.type))
                    dict[eff.type] += eff.value;
                else
                    dict[eff.type] = eff.value;
            }

            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                var upgradeList = durationType == BuffDurationType.ExtraDistancePercent ? extraDistanceUpgrades : upgrades;
                foreach (var up in upgradeList)
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
            if (durationType == BuffDurationType.ExtraDistancePercent)
                return 0;
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
            if (durationType == BuffDurationType.ExtraDistancePercent)
                return 0f;
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

        public float GetCooldown()
        {
            if (durationType == BuffDurationType.ExtraDistancePercent)
                return 0f;
            var cooldown = baseCooldown;
            var qm = QuestManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                        cooldown += up.cooldownDelta;
                }
            }
            // Cooldown now starts after the buff duration finishes, so it is not
            // forced to be longer than the duration.
            return cooldown;
        }

        public float GetExtraDistance(float baseDistance)
        {
            var percent = 0f;
            var flat = 0f;
            foreach (var eff in GetAggregatedEffects())
            {
                if (eff.type == BuffEffectType.MaxDistancePercent)
                    percent += eff.value;
                else if (eff.type == BuffEffectType.MaxDistanceIncrease)
                    flat += eff.value;
            }
            return baseDistance * percent / 100f + flat;
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

            if (durationType == BuffDurationType.ExtraDistancePercent)
            {
                var tracker = GameplayStatTracker.Instance ??
                              UnityEngine.Object.FindFirstObjectByType<GameplayStatTracker>();
                var baseDist = tracker != null ? tracker.MaxRunDistance : 0f;
                var extra = Mathf.CeilToInt(GetExtraDistance(baseDist));
                lines.Add($"Total Extra Distance: +{extra}");
            }

            var echoCount = GetEchoCount();
            if (echoCount > 0)
                lines.Add($"Echoes: {echoCount}");
            if (durationType == BuffDurationType.DistancePercent)
                lines.Add($"Distance: {Mathf.CeilToInt(GetDuration() * 100f)}%");
            else if (durationType != BuffDurationType.ExtraDistancePercent)
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
                BuffEffectType.MaxDistanceIncrease => $"Max Reap Distance +{Mathf.CeilToInt(eff.value)}",
                BuffEffectType.InstantTasks => "Tasks complete instantly",
                _ => string.Empty
            };
        }
    }
}
