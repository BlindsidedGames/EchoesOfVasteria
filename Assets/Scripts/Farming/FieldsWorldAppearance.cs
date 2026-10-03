using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using TimelessEchoes.UI.Toolkit;

namespace TimelessEchoes.Farming
{
    /// <summary>Explicit references to existing art; no Resources-wide or editor asset lookup at runtime.</summary>
    public sealed class FieldsWorldAppearance : ScriptableObject
    {
        public Sprite[] dry = new Sprite[4]; // NW corner, north edge, west edge, centre.
        public Sprite[] wet = new Sprite[4];
        public Sprite[] fence = new Sprite[8]; // rail, side, NW, NE, SW, SE, north gate, south gate.
        public Sprite[] treeStages = new Sprite[3];
        public Tile[] pathTiles;
        public Material material;
        public ToolkitTheme theme;
        public ThemeStyleSheet runtimeTheme;
        public PanelTextSettings textSettings;
    }
}
