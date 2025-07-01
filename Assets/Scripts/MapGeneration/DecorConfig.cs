using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class DecorConfig
    {
        [MinValue(0f)] public float weight = 1f;
        public float minX;
        public float maxX = float.PositiveInfinity;
        [MinValue(0)] public int topBuffer;
        [MinValue(0)] public int bottomBuffer;
        [MinValue(0)] public int sideBuffer;
        public bool spawnOnWater;
        public bool spawnOnSand;
        public bool spawnOnGrass;
    }

    [Serializable]
    [InlineProperty]
    [HideLabel]
    public class DecorEntry
    {
        [Required] public TileBase tile;
        [InlineProperty] public DecorConfig config = new();

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Areas")]
        private string AreasDisplay =>
            (config.spawnOnWater ? "Water " : string.Empty) +
            (config.spawnOnSand ? "Sand " : string.Empty) +
            (config.spawnOnGrass ? "Grass " : string.Empty) == string.Empty
                ? "None"
                : (config.spawnOnWater ? "Water " : string.Empty) +
                  (config.spawnOnSand ? "Sand " : string.Empty) +
                  (config.spawnOnGrass ? "Grass" : string.Empty);

        public float GetWeight(float worldX)
        {
            if (tile == null) return 0f;
            if (worldX < config.minX || worldX > config.maxX) return 0f;
            return Mathf.Max(0f, config.weight);
        }
    }
}
