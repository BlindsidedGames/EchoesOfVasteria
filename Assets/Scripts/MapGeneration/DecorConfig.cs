using System;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

// Add the [Flags] attribute to make the enum multi-selectable.
// Assign power-of-two values to each enum member.
[Flags]
public enum SpawnArea
{
    Water = 1 << 0, // 1
    Sand = 1 << 1, // 2
    Grass = 1 << 2 // 4
}

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

        // Change this from a List<SpawnArea> to a single SpawnArea field.
        // Odin will automatically create a multi-select UI for a [Flags] enum.
        [EnumToggleButtons] [LabelWidth(100)] public SpawnArea SpawnOn;
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

            var builder = new StringBuilder();

            if (config.SpawnOn.HasFlag(SpawnArea.Water)) builder.Append("Water, ");
            if (config.SpawnOn.HasFlag(SpawnArea.Sand)) builder.Append("Sand, ");
            if (config.SpawnOn.HasFlag(SpawnArea.Grass)) builder.Append("Grass, ");

            string areaString;
            if (builder.Length > 0)
                // Remove the trailing comma and space
                areaString = builder.ToString(0, builder.Length - 2);
            else
                areaString = "Unset";

            Name = $"{areaString} | {tileName}";
        }

        public float GetWeight(float worldX)
        {
            if (tile == null) return 0f;
            if (worldX < config.minX || worldX > config.maxX) return 0f;
            return Mathf.Max(0f, config.weight);
        }
    }
}