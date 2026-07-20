#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.CustomGUI
{
    [System.Serializable]
    public class GUIGridOptions
    {
        public Color BackgroundColor = new Color(40f / 256f, 40f / 256f, 40f / 256f);
        public Color LineColor = new Color(81f / 256f, 81f / 256f, 81f / 256f);
        public Color HighlightColor = new Color(168f / 256f, 168f / 256f, 168f / 256f);

        public Vector2Int GridCellSize = new Vector2Int(32, 32);
        public Vector2 GridOffset = Vector2.zero;
        public float ZoomAmount { get; private set; } = 1;
        public bool DrawGrid = true;

        public float MinZoom = .1f;
        public float MaxZoom = 50f; // This value lets the user zoom in way too much, but the default value would cause issues on larger monitors

        public List<int> DragGridMouseButtons = new List<int>() { 1, 2 };

        public bool limitBounds = false;
        /// <summary>
        /// The cell bounds in grid cells, if limit bounds are enabled this the grid will make sure that this area never goes fully off screen;
        /// </summary>
        public RectInt CellBounds;

        public GUIGridOptions()
        {
            GridOffset = GridCellSize * 3;
        }

        #region Methods
        public void SetZoom(float zoomAmount) => ZoomAmount = Mathf.Clamp(zoomAmount, MinZoom, MaxZoom);
        #endregion

        #region Presets
        public static GUIGridOptions Default => new GUIGridOptions();
        #endregion
    }
}
#endif