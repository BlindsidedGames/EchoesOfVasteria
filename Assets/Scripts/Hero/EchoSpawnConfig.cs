using System;
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes
{
    [Serializable]
    public class EchoSpawnConfig
    {
        [Tooltip("Skills this Echo can perform. Leave empty to allow all skills.")]
        public List<Skill> capableSkills = new();

        [Tooltip("Overall behaviour for spawned Echoes.")]
        public TimelessEchoes.Hero.EchoType echoType = TimelessEchoes.Hero.EchoType.All;
    }
}
