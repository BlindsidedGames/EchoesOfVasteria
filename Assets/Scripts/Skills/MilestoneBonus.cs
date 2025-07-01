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
    }
}
