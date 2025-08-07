using System;
using System.Collections.Generic;
using TimelessEchoes.Quests;

namespace TimelessEchoes.Buffs
{
    public enum BuffEffectType
    {
        MoveSpeedPercent,
        DamagePercent,
        DefensePercent,
        AttackSpeedPercent,
        TaskSpeedPercent,
        LifestealPercent,
        MaxDistancePercent,
        InstantTasks
    }

    public enum BuffDurationType
    {
        Time,
        DistancePercent,
        ExtraDistancePercent
    }

    [Serializable]
    public struct BuffEffect
    {
        public BuffEffectType type;
        public float value;
    }

    [Serializable]
    public class BuffUpgrade
    {
        public QuestData quest;
        public List<BuffEffect> additionalEffects = new();
        public float durationDelta;
        public int echoCountDelta;
    }
}
