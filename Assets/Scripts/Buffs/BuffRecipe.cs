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
        [MinValue(0f)]
        public float baseDuration = 30f;

        [TitleGroup("General")]
        [MinValue(0f)]
        public float baseCooldown = 60f;

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

        // Removed Extra Distance-specific fields

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

        private struct BuffPower
        {
            public float durationMultiplier;          // Multiplier for time-based durations
            public float distanceFractionAdd;         // Additive fraction for DistancePercent (e.g., +0.3)
            public float effectValueMultiplier;       // Multiplier for non-distance effect values
        }

        private BuffPower ComputePowerPolicy()
        {
            var power = 0f;
            var cm = TimelessEchoes.Upgrades.CauldronManager.Instance;
            if (cm != null)
                power = cm.GetBuffPowerPercent(name);

            var policy = new BuffPower
            {
                durationMultiplier = 1f,
                distanceFractionAdd = 0f,
                effectValueMultiplier = 1f
            };

            if (power <= 0f)
                return policy;

            if (durationType == BuffDurationType.DistancePercent)
            {
                policy.distanceFractionAdd = power / 100f; // add absolute percent points to fraction
                policy.effectValueMultiplier = 1f + power / 100f;
            }
            else
            {
                policy.durationMultiplier = 1f + power / 100f; // extend time-based duration
                policy.effectValueMultiplier = 1f + power / 100f;
            }

            return policy;
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
            // Apply power policy: multiply only non-distance effects
            var policy = ComputePowerPolicy();
            var list = new List<BuffEffect>();
            foreach (var pair in dict)
            {
                var val = pair.Value;
                var isDistanceEffect = pair.Key == BuffEffectType.MaxDistancePercent ||
                                       pair.Key == BuffEffectType.MaxDistanceIncrease;
                if (!isDistanceEffect)
                    val *= policy.effectValueMultiplier;
                list.Add(new BuffEffect { type = pair.Key, value = val });
            }
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
            // Apply power via policy
            var policy = ComputePowerPolicy();
            if (durationType == BuffDurationType.DistancePercent)
                duration = Mathf.Clamp01(duration + policy.distanceFractionAdd);
            else
                duration *= policy.durationMultiplier;
            return duration;
        }

        public float GetCooldown()
        {
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
            // Apply Cauldron tier-based cooldown reduction
            var cm = TimelessEchoes.Upgrades.CauldronManager.Instance;
            if (cm != null)
            {
                var reducePercent = cm.GetBuffCooldownReductionPercent(name);
                cooldown *= Mathf.Max(0f, 1f - reducePercent / 100f);
            }

            // Cooldown now starts after the buff duration finishes.
            return cooldown;
        }

        // Removed GetExtraDistance; extra distance percent duration is no longer supported

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

            // Removed Extra Distance-specific description

            var echoCount = GetEchoCount();
            if (echoCount > 0)
                lines.Add($"Echoes: {echoCount}");
            if (durationType == BuffDurationType.DistancePercent)
                lines.Add($"Distance: {Mathf.CeilToInt(GetDuration() * 100f)}%");
            else
                lines.Add(
                    $"Duration: {CalcUtils.FormatTime(GetDuration(), shortForm: true)}, " +
                    $"Cooldown: {CalcUtils.FormatTime(GetCooldown(), shortForm: true)}");
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
                BuffEffectType.DoubleResources => "Resources doubled",
                BuffEffectType.CritChancePercent => $"Crit Chance +{eff.value}%",
                _ => string.Empty
            };
        }
    }
}
