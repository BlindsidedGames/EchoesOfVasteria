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
        public List<Skill> capableSkills = new();
    }
}
