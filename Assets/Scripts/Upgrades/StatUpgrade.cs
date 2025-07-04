using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "StatUpgrade", menuName = "SO/Stat Upgrade")]
    public class StatUpgrade : ScriptableObject
    {
        /// <summary>
        ///     Base value of the stat before any upgrades are applied.
        /// </summary>
        public float baseValue = 0f;
        /// <summary>
        ///     Optional description displayed in the upgrade UI.
        /// </summary>
        public string description = string.Empty;
        public List<Threshold> thresholds;
        public float statIncreasePerLevel = 1;

        [Serializable]
        public class Threshold
        {
            public int minLevel;
            public int maxLevel;
            public List<ResourceRequirement> requirements;
        }

        [Serializable]
        public class ResourceRequirement
        {
            public Resource resource;
            public int amount;
            public int amountIncreasePerLevel = 0;
        }
    }
}