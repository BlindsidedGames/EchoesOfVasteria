using System;
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes
{
    /// <summary>
    ///     Serializable configuration object for data-driven echo spawners (skills, quests, cut-scenes, etc.).
    ///     Allows designers to specify the skills available to spawned echoes as well as the intended behaviour type.
    /// </summary>
    [Serializable]
    public class EchoSpawnConfig
    {
        /// <summary>
        ///     Skills this Echo can perform. Leave empty to allow all skills.
        /// </summary>
        [Tooltip("Skills this Echo can perform. Leave empty to allow all skills.")]
        public List<Skill> capableSkills = new();

        /// <summary>
        ///     Overall behaviour for spawned Echoes.
        /// </summary>
        [Tooltip("Overall behaviour for spawned Echoes.")]
        public TimelessEchoes.Hero.EchoType echoType = TimelessEchoes.Hero.EchoType.All;
    }
}
