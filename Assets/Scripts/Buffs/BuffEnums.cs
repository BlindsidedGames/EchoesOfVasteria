using System;

namespace TimelessEchoes.Buffs
{
    /// <summary>
    /// Types of buff effects supported by the game.
    /// </summary>
    public enum BuffEffectType
    {
        MoveSpeed,
        Damage,
        Defense,
        AttackSpeed,
        TaskSpeed,
        Lifesteal,
        InstantTasks,
        EchoCount,
        Duration
    }

    /// <summary>
    /// How a buff's duration is measured.
    /// </summary>
    public enum BuffDurationType
    {
        Time,
        Distance
    }
}
