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
        public bool borderOnly;
        public int topBorderOffset;
        public int bottomBorderOffset;
        public int leftBorderOffset;
        public int rightBorderOffset;

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
        public TileBase tile;

        // When the prefab is changed, call the UpdateName method.
        [OnValueChanged("UpdateName")]
        [HorizontalGroup("Entry", 55)]
        [PreviewField(50, ObjectFieldAlignment.Left)]
        [HideLabel]
        public GameObject prefab;

        // When any value inside the config is changed, call the UpdateName method.
        [OnValueChanged("UpdateName", true)] [HorizontalGroup("Entry")] [InlineProperty] [HideLabel]
        public DecorConfig config = new();

        /// <summary>
        ///     This method builds the string for the 'Name' field based on the current settings.
        /// </summary>
        public void UpdateName()
        {
            if (tile != null)
                Name = tile.name;
            else if (prefab != null)
                Name = prefab.name;
            else
                Name = "None";
        }

        public float GetWeight(float worldX)
        {
            if (tile == null && prefab == null) return 0f;
            if (worldX < config.minX || worldX > config.maxX) return 0f;
            return Mathf.Max(0f, config.weight);
        }
    }
}