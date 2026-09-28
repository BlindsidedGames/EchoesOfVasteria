using UnityEngine;
using UnityEngine.Serialization;

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
        [Tooltip("Skill level required for this drop (0 = no requirement)")]
        public int requiredSkillLevel;
    }
}
