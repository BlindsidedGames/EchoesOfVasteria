using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    public enum MilestoneType
    {
        InstantTask,
        DoubleResources,
        DoubleXP,
        StatIncrease
    }

    [Serializable]
    public class MilestoneBonus
    {
        public int levelRequirement;
        public string bonusDescription;
        public string bonusID;

        public MilestoneType type;

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
                default:
                    return string.Empty;
            }
        }
    }
}
