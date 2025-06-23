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