using UnityEngine;
using UnityEngine.Serialization;
using TimelessEchoes.Quests;

namespace TimelessEchoes.Upgrades
{
    [System.Serializable]
    public class ResourceDrop
    {
        public Resource resource;
        public Vector2Int dropRange = new Vector2Int(1, 1);
        [FormerlySerializedAs("dropChance")]
        [Tooltip("Relative weight used when selecting this drop." )]
        [Min(0f)] public float weight = 1f;
        // Minimum world X position required for this drop to occur
        public float minX;
        // Maximum world X position allowed for this drop to occur
        public float maxX = float.PositiveInfinity;
        // Quest required for this drop to occur
        public QuestData requiredQuest;
    }
}
