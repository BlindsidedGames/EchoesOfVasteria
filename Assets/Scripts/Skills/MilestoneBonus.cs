using System;
using Sirenix.OdinInspector;
using UnityEngine;
using TimelessEchoes.Upgrades;
using System.Linq;

namespace TimelessEchoes.Skills
{
    public enum MilestoneType
    {
        InstantTask,
        InstantKill,
        DoubleResources,
        DoubleXP,
        StatIncrease,
        SpawnEcho
    }

    [Serializable]
    public class MilestoneBonus
    {
        public int levelRequirement;
        public string bonusDescription;
        public string bonusID;

        public MilestoneType type;

        [ShowIf("type", MilestoneType.SpawnEcho)]
        public TimelessEchoes.EchoSpawnConfig echoSpawnConfig;
        [ShowIf("type", MilestoneType.SpawnEcho)]
        [Min(0f)]
        public float echoDuration = 10f;

        [Range(0f, 1f)]
        [HideIf("type", MilestoneType.StatIncrease)]
        public float chance = 1f;

        [ShowIf("type", MilestoneType.StatIncrease)]
        public TimelessEchoes.Upgrades.StatUpgrade statUpgrade;
        [ShowIf("type", MilestoneType.StatIncrease)]
        public bool percentBonus;
        [ShowIf("type", MilestoneType.StatIncrease)]
        public float statAmount;

        /// <summary>
        /// Returns the milestone description based on the configured settings
        /// if <see cref="bonusDescription"/> is empty.
        /// </summary>
        /// <param name="skillName">Optional skill name for instant task bonuses.</param>
        public string GetDescription(string skillName = null)
        {
            if (!string.IsNullOrWhiteSpace(bonusDescription))
                return bonusDescription;

            switch (type)
            {
                case MilestoneType.InstantTask:
                    string taskLabel = string.IsNullOrWhiteSpace(skillName)
                        ? "tasks"
                        : $"{skillName.ToLowerInvariant()} tasks";
                    return $"Provides a {chance * 100f:0.#}% chance to instantly complete {taskLabel}.";
                case MilestoneType.InstantKill:
                    return $"Provides a {chance * 100f:0.#}% chance to instantly kill enemies.";
                case MilestoneType.DoubleResources:
                    return $"Provides a {chance * 100f:0.#}% chance to double resources gained.";
                case MilestoneType.DoubleXP:
                    return $"Provides a {chance * 100f:0.#}% chance to gain double XP.";
                case MilestoneType.StatIncrease:
                    string statName = statUpgrade != null ? statUpgrade.name : "stat";
                    string amountText = string.Empty;
                    if (statAmount != 0f)
                        amountText = percentBonus ? $" by {statAmount * 100f:0.#}%" : $" by {statAmount:0.#}";
                    return $"Increases {statName}{amountText}.";
                case MilestoneType.SpawnEcho:
                    string skillText = skillName;
                    if (echoSpawnConfig != null && echoSpawnConfig.capableSkills != null && echoSpawnConfig.capableSkills.Count > 0)
                    {
                        if (echoSpawnConfig.capableSkills.Count == 1)
                            skillText = echoSpawnConfig.capableSkills[0]?.skillName ?? skillName;
                        else
                            skillText = "various";
                    }

                    var controller = StatUpgradeController.Instance;
                    var echoUpgrade = controller?.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Echo Lifetime");
                    float bonus = echoUpgrade != null ? controller.GetTotalValue(echoUpgrade) : 0f;
                    float totalDuration = echoDuration + bonus;
                    return $"Provides a {chance * 100f:0.#}% chance to summon an Echo that performs {skillText} tasks for {totalDuration:0.#} seconds.";
                default:
                    return string.Empty;
            }
        }
    }
}
