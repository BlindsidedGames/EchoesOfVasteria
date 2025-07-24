using System;
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes
{
    [Serializable]
    public class EchoSpawnConfig
    {
        [Min(1)]
        public int echoCount = 1;
        [Tooltip("Skills this Echo can perform. Leave empty to allow all skills.")]
        public List<Skill> capableSkills = new();

        [Tooltip("Overall behaviour for spawned Echoes.")]
        public TimelessEchoes.Hero.EchoType echoType = TimelessEchoes.Hero.EchoType.All;
    }
}
