using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    [System.Serializable]
    public class ResourceDrop
    {
        public Resource resource;
        public Vector2Int dropRange = new Vector2Int(1, 1);
        [Range(0f, 1f)] public float dropChance = 1f;
    }
}
