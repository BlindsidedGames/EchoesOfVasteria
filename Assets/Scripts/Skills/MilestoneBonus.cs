using System;

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
        [UnityEngine.Range(0f, 1f)] public float chance = 1f;

        public TimelessEchoes.Upgrades.StatUpgrade statUpgrade;
        public bool percentBonus;
        public float statAmount;
    }
}
