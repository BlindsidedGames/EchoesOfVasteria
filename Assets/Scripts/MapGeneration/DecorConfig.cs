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
        [MinValue(0)] public int topBuffer = 1;
        [MinValue(0)] public int bottomBuffer;
        [MinValue(0)] public int sideBuffer = 1;

    }

    [Serializable]
    public class DecorEntry
    {
        // This field will be used by the list for its label and for searching.
        // It's hidden in the regular inspector view because we see the result in the list label.
        [HideInInspector] public string Name;

        // When the tile is changed, call the UpdateName method.
        [OnValueChanged("UpdateName")]
        [HorizontalGroup("Entry", 55)]
        [PreviewField(50, ObjectFieldAlignment.Left)]
        [HideLabel]
        [Required]
        public TileBase tile;

        // When any value inside the config is changed, call the UpdateName method.
        [OnValueChanged("UpdateName", true)] [HorizontalGroup("Entry")] [InlineProperty] [HideLabel]
        public DecorConfig config = new();

        /// <summary>
        ///     This method builds the string for the 'Name' field based on the current settings.
        /// </summary>
        public void UpdateName()
        {
            var tileName = tile != null ? tile.name : "No Tile";

            Name = tileName;
        }

        public float GetWeight(float worldX)
        {
            if (tile == null) return 0f;
            if (worldX < config.minX || worldX > config.maxX) return 0f;
            return Mathf.Max(0f, config.weight);
        }
    }
}