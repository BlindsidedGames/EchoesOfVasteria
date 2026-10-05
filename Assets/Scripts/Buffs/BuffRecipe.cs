using TimelessEchoes.UI.Toolkit;
using System;
using System.Linq;
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
        // Keep serialized effect IDs and old card counts readable after retirement.
        public static bool IsRetiredName(string value) => value == "Prospector" || value == "Slipstream";
        public bool IsRetired => IsRetiredName(name) || HasEffect(BuffEffectType.ProspectorWeightPercent);
        private static BuffRecipe[] availableSource, availableBuffs;
        public static BuffRecipe[] LoadAvailable(string path = "")
        {
            var source = AssetCache.GetAll<BuffRecipe>(path);
            if (!ReferenceEquals(source, availableSource))
            {
                availableSource = source;
                availableBuffs = source.Where(buff => buff && !buff.IsRetired).ToArray();
            }
            return availableBuffs;
        }

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

        // Cached card ID to avoid string allocations in hot paths
        [System.NonSerialized] private string _cachedCardId;
        /// <summary>
        /// Returns the Cauldron card ID for this buff (e.g., "BUFF:Haste").
        /// Cached after first access to avoid allocations.
        /// </summary>
        public string CardId
        {
            get
            {
                if (_cachedCardId == null)
                    _cachedCardId = "BUFF:" + name;
                return _cachedCardId;
            }
        }

        [TitleGroup("Effects")]
        public List<BuffEffect> baseEffects = new();

        [TitleGroup("Upgrades")]
        public List<BuffUpgrade> upgrades = new();

        // Removed Extra Distance-specific fields

        public bool HasEffect(BuffEffectType type) => baseEffects.Exists(e => e.type == type);

        public string GetDisplayName()
        {
            return ToolkitLocalization.Text("buff." + name, string.IsNullOrEmpty(title) ? name : title);
        }

        public int GetCurrentLevel()
        {
            var level = 0;
            var qm = QuestManager.Instance;
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
            public float effectValueMultiplier;       // Multiplier applied to effect values
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
                effectValueMultiplier = 1f
            };

            if (power > 0f)
            {
                var multiplier = 1f + power / 100f;
                policy.durationMultiplier = multiplier;
                policy.effectValueMultiplier = multiplier;
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

            var qm = QuestManager.Instance;
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
            // Apply power policy: multiply only non-distance distance-duration effects
            var policy = ComputePowerPolicy();
            var list = new List<BuffEffect>();
            foreach (var pair in dict)
            {
                var val = pair.Value;
                var isDistanceEffect = pair.Key == BuffEffectType.MaxDistancePercent ||
                                       pair.Key == BuffEffectType.MaxDistanceIncrease ||
                                       pair.Key == BuffEffectType.DistanceDurationPercent;
                if (!isDistanceEffect)
                    val *= policy.effectValueMultiplier;
                list.Add(new BuffEffect { type = pair.Key, value = val });
            }
            return list;
        }

        public int GetEchoCount()
        {
            var count = baseEchoCount;
            var qm = QuestManager.Instance;
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
            var qm = QuestManager.Instance;
            if (qm != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest != null && qm.IsQuestCompleted(up.quest))
                        duration += up.durationDelta;
                }
            }

            if (durationType == BuffDurationType.DistancePercent)
            {
                var fraction = 0f;
                foreach (var eff in baseEffects)
                {
                    if (eff.type == BuffEffectType.DistanceDurationPercent)
                        fraction += eff.value;
                }

                if (qm != null)
                {
                    foreach (var up in upgrades)
                    {
                        if (up?.quest == null || !qm.IsQuestCompleted(up.quest))
                            continue;
                        if (up.additionalEffects == null)
                            continue;
                        foreach (var eff in up.additionalEffects)
                        {
                            if (eff.type == BuffEffectType.DistanceDurationPercent)
                                fraction += eff.value;
                        }
                    }
                }

                if (fraction > 0f)
                    duration = fraction;

                duration = Mathf.Clamp01(duration);
            }
            else
            {
                var policy = ComputePowerPolicy();
                duration *= policy.durationMultiplier;
            }
            return duration;
        }

        public float GetCooldown()
        {
            var cooldown = baseCooldown;
            var qm = QuestManager.Instance;
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

        public List<string> GetDescriptionLines(bool includeTiming = true)
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(description))
                lines.Add(ToolkitLocalization.Text("buff." + name + ".description", description));

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
                lines.Add(ToolkitLocalization.Text("buffs.echo-count", "Echoes: {0}", echoCount));
            if (!includeTiming) return lines;
            if (durationType == BuffDurationType.DistancePercent)
                lines.Add(ToolkitLocalization.Text("buffs.description-distance", "Distance: {0}%", Mathf.CeilToInt(GetDuration() * 100f)));
            else
                lines.Add(
                    ToolkitLocalization.Text("buffs.description-duration", "Duration: {0}, Cooldown: {1}", CalcUtils.FormatTime(GetDuration(), shortForm: true), CalcUtils.FormatTime(GetCooldown(), shortForm: true)));
            return lines;
        }

        private static string DescribeEffect(BuffEffect eff)
        {
            return eff.type switch
            {
                BuffEffectType.MoveSpeedPercent => ToolkitLocalization.Text("buffs.effect-move-speed-percent", "Move Speed +{0}%", eff.value),
                BuffEffectType.DamagePercent => ToolkitLocalization.Text("buffs.effect-damage-percent", "Damage +{0}%", eff.value),
                BuffEffectType.DefensePercent => ToolkitLocalization.Text("buffs.effect-defense-percent", "Defense +{0}%", eff.value),
                BuffEffectType.AttackSpeedPercent => ToolkitLocalization.Text("buffs.effect-attack-speed-percent", "Attack Speed +{0}%", eff.value),
                BuffEffectType.TaskSpeedPercent => ToolkitLocalization.Text("buffs.effect-task-speed-percent", "Task Speed +{0}%", eff.value),
                BuffEffectType.HealthRegenPercent => ToolkitLocalization.Text("buffs.effect-health-regen-percent", "Health Regen +{0}%", eff.value),
                BuffEffectType.MaxDistancePercent => ToolkitLocalization.Text("buffs.effect-max-distance-percent", "Max Reap Distance +{0}%", eff.value),
                BuffEffectType.MaxDistanceIncrease => ToolkitLocalization.Text("buffs.effect-max-distance-increase", "Max Reap Distance +{0}", Mathf.CeilToInt(eff.value)),
                BuffEffectType.InstantTasks => ToolkitLocalization.Text("buffs.effect-instant-tasks", "Tasks complete instantly"),
                // ResourceMultiplier represents percent gain: use +X% text
                BuffEffectType.ResourceMultiplier => ToolkitLocalization.Text("buffs.effect-resource-multiplier", "Resource Gains +{0}%", eff.value),
                BuffEffectType.ExperienceBonusFraction => ToolkitLocalization.Text("buffs.effect-experience-bonus-fraction", "{0:0.#}% Bonus Experience", eff.value * 100f),
                BuffEffectType.CritChancePercent => ToolkitLocalization.Text("buffs.effect-crit-chance-percent", "Crit Chance +{0}%", eff.value),
                BuffEffectType.CritDamagePercent => ToolkitLocalization.Text("buffs.effect-crit-damage-percent", "Crit Damage +{0}%", eff.value),
                BuffEffectType.ProspectorWeightPercent => ToolkitLocalization.Text("buffs.effect-prospector-weight-percent", "Target Spawn Weight +{0:0.#}%", Mathf.Min(200f, eff.value)),
                BuffEffectType.CollectorWeightPercent => ToolkitLocalization.Text("buffs.effect-collector-weight-percent", "Unfamiliar Task Weight up to +{0:0.#}%", Mathf.Min(200f, eff.value)),
                BuffEffectType.WindfallRewardPercent => ToolkitLocalization.Text("buffs.effect-windfall-reward-percent", "Resource Bonus +{0:0.#}% every 10 tasks", Mathf.Min(100f, eff.value)),
                BuffEffectType.EchoResonanceRewardPercent => ToolkitLocalization.Text("buffs.effect-echo-resonance-reward-percent", "Echo Follow-up Resources +{0:0.#}% (10s)", Mathf.Min(100f, eff.value)),
                BuffEffectType.TimeScalePercent => ToolkitLocalization.Text("buffs.effect-time-scale-percent", "Game Speed +{0}", eff.value),
                BuffEffectType.DistanceDurationPercent => string.Empty,
                _ => string.Empty
            };
        }
    }
}

