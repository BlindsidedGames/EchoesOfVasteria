using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TimelessEchoes.Skills;

namespace TimelessEchoes
{
    [Serializable]
    public class EchoSpawnConfig
    {
        [Min(1)]
        public int echoCount = 1;
        [HideIf(nameof(disableSkills))]
        public List<Skill> capableSkills = new();

        /// <summary>
        /// When true, spawned Echoes ignore all task related behaviour.
        /// </summary>
        public bool disableSkills;
        /// <summary>
        /// When true, spawned Echoes can perform combat actions.
        /// </summary>
        public bool combatEnabled = true;
    }
}
