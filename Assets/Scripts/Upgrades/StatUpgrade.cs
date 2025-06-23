using System.Collections.Generic;
using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "StatUpgrade", menuName = "SO/Stat Upgrade")]
    public class StatUpgrade : ScriptableObject
    {
        [System.Serializable]
        public class Threshold
        {
            public int minLevel;
            public int maxLevel;
            public List<ResourceRequirement> requirements;
        }

        [System.Serializable]
        public class ResourceRequirement
        {
            public Resource resource;
            public int amount;
        }

        public List<Threshold> thresholds;
        public int statIncreasePerLevel = 1;
    }
}
