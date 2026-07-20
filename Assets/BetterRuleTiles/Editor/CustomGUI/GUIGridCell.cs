#if UNITY_EDITOR
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.CustomGUI
{
    [System.Serializable]
    public class GUIGridCell
    {
        public Vector2Int Cell;

        public Texture2D Texture;
        public Color BackgroundColor;

        public int x
        {
            get => Cell.x;
            set => Cell.x = value; 
        }
        public int y
        {
            get => Cell.y;
            set => Cell.y = value; 
        }

        public GUIGridCell(int x, int y) => new GUIGridCell(new Vector2Int(x, y));
        public GUIGridCell(int x, int y, Texture2D tex) => new GUIGridCell(new Vector2Int(x, y), tex);
        public GUIGridCell(int x, int y, Color color) => new GUIGridCell(new Vector2Int(x, y), color);
        public GUIGridCell(int x, int y, Texture2D tex, Color color) => new GUIGridCell(new Vector2Int(x, y), tex, color);
        public GUIGridCell(Vector2Int cell) => new GUIGridCell(cell, null, Color.clear);
        public GUIGridCell(Vector2Int cell, Texture2D tex) => new GUIGridCell(cell, tex, Color.clear);
        public GUIGridCell(Vector2Int cell, Color color) => new GUIGridCell(cell, null, color);
        public GUIGridCell(Vector2Int cell, Texture2D tex, Color color)
        {
            this.Cell = cell;
            this.Texture = tex;
            this.BackgroundColor = color;
        }
    }
}
#endif