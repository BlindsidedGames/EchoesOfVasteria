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
        public float durationMagnitude = 30f;

        [TitleGroup("General")]
        [SerializeField]
        public TimelessEchoes.EchoSpawnConfig echoSpawnConfig;

        [TitleGroup("General")]
        [Tooltip("Quest required to unlock this buff.")]
        public QuestData requiredQuest;

        [TitleGroup("Effects")]
        public List<BuffEffect> baseEffects = new();

        [TitleGroup("Upgrades")]
        public List<BuffUpgrade> upgrades = new();

        public string Title => string.IsNullOrEmpty(title) ? name : title;

        /// <summary>
        ///     Returns the current level based on completed upgrade quests.
        /// </summary>
        public int GetLevel()
        {
            var lvl = 1;
            if (upgrades == null) return lvl;
            foreach (var up in upgrades)
            {
                if (up?.quest == null) continue;
                if (QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(up.quest))
                    lvl++;
            }
            return lvl;
        }

        /// <summary>
        ///     Returns true if the required quest to unlock this buff has been completed.
        /// </summary>
        public bool IsUnlocked()
        {
            if (requiredQuest == null) return true;
            return QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(requiredQuest);
        }

        /// <summary>
        ///     Combine base effects with effects from completed upgrades.
        /// </summary>
        public IEnumerable<BuffEffect> GetActiveEffects()
        {
            var effects = new List<BuffEffect>();
            if (baseEffects != null)
                effects.AddRange(baseEffects);
            if (upgrades != null)
            {
                foreach (var up in upgrades)
                {
                    if (up?.quest == null || up.effects == null) continue;
                    if (QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(up.quest))
                        effects.AddRange(up.effects);
                }
            }
            return effects;
        }

        /// <summary>
        ///     Builds a display name including current level.
        /// </summary>
        public string GetDisplayName()
        {
            return $"{Title} Lvl {GetLevel()}";
        }

        /// <summary>
        ///     Generates human readable description lines for the aggregated effects.
        /// </summary>
        public List<string> GetDescriptionLines()
        {
            var totals = new Dictionary<BuffEffectType, float>();
            foreach (var eff in GetActiveEffects())
            {
                if (totals.ContainsKey(eff.type))
                    totals[eff.type] += eff.magnitude;
                else
                    totals[eff.type] = eff.magnitude;
            }

            if (echoSpawnConfig != null && echoSpawnConfig.echoCount > 0)
            {
                if (totals.ContainsKey(BuffEffectType.EchoCount))
                    totals[BuffEffectType.EchoCount] += echoSpawnConfig.echoCount;
                else
                    totals[BuffEffectType.EchoCount] = echoSpawnConfig.echoCount;
            }

            var lines = new List<string>();
            foreach (var kv in totals)
            {
                switch (kv.Key)
                {
                    case BuffEffectType.MoveSpeed:
                        lines.Add($"Move Speed {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.Damage:
                        lines.Add($"Damage {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.Defense:
                        lines.Add($"Defense {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.AttackSpeed:
                        lines.Add($"Attack Speed {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.TaskSpeed:
                        lines.Add($"Task Speed {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.Lifesteal:
                        lines.Add($"Lifesteal {kv.Value:+0;-0;0}%");
                        break;
                    case BuffEffectType.InstantTasks:
                        lines.Add("Tasks complete instantly");
                        break;
                    case BuffEffectType.EchoCount:
                        lines.Add($"Echo Count {kv.Value:+0;-0;0}");
                        break;
                    case BuffEffectType.Duration:
                        lines.Add($"Duration {kv.Value:+0;-0;0}s");
                        break;
                }
            }

            return lines;
        }

        [Serializable]
        public struct BuffEffect
        {
            public BuffEffectType type;
            [Range(-100f, 100f)] public float magnitude;
        }

        [Serializable]
        public class BuffUpgrade
        {
            public QuestData quest;
            public List<BuffEffect> effects = new();
        }
    }
}
