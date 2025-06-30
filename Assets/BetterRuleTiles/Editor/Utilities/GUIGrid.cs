#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VinToolsEditor.Utilities
{
    /// <summary>
    /// A class to draw fullscreen interactable grids in an editor window
    /// </summary>
    public class GUIGrid
    {
#region variables
        public delegate void DrawMethod();

        public GUIGridOptions GridOptions = new GUIGridOptions();

        public bool NeedsRepaint { get; private set; } = false;

        private Vector2 _dragStartPos = Vector2.zero;
        private Vector2 _zoomScroll = new Vector2(0, 250);

        private Vector2 _currentGridCellSize;
        private Vector2 _currentGridStart;
        private Vector2 _currentGridCellAmount;
        private Vector2 _currentGridOffset;
        private Vector2 _currentGridOrigin;
#endregion

#region Main functions
        /// <summary>
        /// Draws the grid with the saved parameters
        /// </summary>
        /// <param name="size">size of the window</param>
        /// <param name="drawUnderLines">method to draw UI under the grid lines</param>
        /// <param name="drawOverLines">method to draw UI over the grid</param>
        public void DrawGrid(Vector2 size, DrawMethod drawUnderLines = null, DrawMethod drawOverLines = null)
        {
            //reset needs repaint
            NeedsRepaint = false;

            //background
            DrawBackground(size, GridOptions.BackgroundColor);

            //drag grid around
            DragMoveGrid();
            //limit bounds
            if (GridOptions.limitBounds) LimitBounds(size);
            //change zoom based on scrool
            Zoom();

            //calculate variables
            CalculateGridVariables(size);

            //draw UI
            drawUnderLines?.Invoke();

            //draw lines
            if (GridOptions.DrawGrid) DrawGridlines(size, GridOptions.LineColor);

            //draw UI
            drawOverLines?.Invoke();
        }

        /// <summary>
        /// Draw the solid background color with scrolling enabled
        /// </summary>
        /// <param name="size"></param>
        /// <param name="color"></param>
        private void DrawBackground(Vector2 size, Color color)
        {
            //get scrollwheel
            _zoomScroll = GUI.BeginScrollView(new Rect(Vector2.zero, size), _zoomScroll, new Rect(Vector2.zero, size + new Vector2(0, 500)));
            GUI.EndScrollView();

            //draw background
            EditorGUI.DrawRect(new Rect(Vector2.zero, size), color);
        }

        /// <summary>
        /// Calculates the variables needed to draw the grid
        /// </summary>
        /// <param name="rect"></param>
        protected virtual void CalculateGridVariables(Vector2 size)
        {
            //make sure the grid cells are not 0 in size
            if (GridOptions.GridCellSize.x < 1) GridOptions.GridCellSize.x = 1;
            if (GridOptions.GridCellSize.y < 1) GridOptions.GridCellSize.y = 1;

            //calculate variables
            _currentGridCellSize = Vector2Int.FloorToInt((Vector2)GridOptions.GridCellSize * GridOptions.ZoomAmount);

            if (_currentGridCellSize.x < 1) _currentGridCellSize.x = 1;
            if (_currentGridCellSize.y < 1) _currentGridCellSize.y = 1;

            _currentGridCellAmount = new Vector2Int(
                (int)size.x / (int)_currentGridCellSize.x + 40,
                (int)size.y / (int)_currentGridCellSize.y + 40
                );
            _currentGridStart = new Vector2Int(
                (int)GridOptions.GridOffset.x % (int)_currentGridCellSize.x - 3 * GridOptions.GridCellSize.x,
                (int)GridOptions.GridOffset.y % (int)_currentGridCellSize.y - 3 * GridOptions.GridCellSize.y
                );
            _currentGridOffset = new Vector2Int(
                -(int)GridOptions.GridOffset.x / (int)_currentGridCellSize.x,
                -(int)GridOptions.GridOffset.y / (int)_currentGridCellSize.y
                );
            _currentGridOrigin = _currentGridStart - _currentGridOffset * _currentGridCellSize;
        }

        /// <summary>
        /// Draw grid lines using the color from the grid options
        /// </summary>
        /// <param name="size"></param>
        /// <param name="color"></param>
        /// <summary>
        public void DrawGridlines(Vector2 size) => DrawGridlines(size, GridOptions.LineColor);
        /// Draw grid lines
        /// </summary>
        /// <param name="size"></param>
        /// <param name="color"></param>
        protected virtual void DrawGridlines(Vector2 size, Color color)
        {
            //draw grid lines
            for (int x = 0; x < _currentGridCellAmount.x; x++)
            {
                EditorGUI.DrawRect(new Rect(
                    _currentGridStart.x + x * _currentGridCellSize.x,
                    _currentGridStart.y,
                    1,
                    _currentGridCellAmount.y * _currentGridCellSize.y
                    ),
                    color);
            }
            for (int y = 0; y < _currentGridCellAmount.y; y++)
            {
                EditorGUI.DrawRect(new Rect(
                    _currentGridStart.x,
                    _currentGridStart.y + y * _currentGridCellSize.y,
                    _currentGridCellAmount.x * _currentGridCellSize.x,
                    1
                    ),
                    color);
            }
        }

        /// <summary>
        /// Drag and move the grid around
        /// </summary>
        private void DragMoveGrid()
        {
            bool canDrag = GridOptions.DragGridMouseButtons.Contains(Event.current.button);

            if (canDrag && Event.current.type == EventType.MouseDown) _dragStartPos = Vector2Int.FloorToInt(Event.current.mousePosition);
            if (canDrag && Event.current.type == EventType.MouseDrag)
            {
                GridOptions.GridOffset += Vector2Int.CeilToInt(Event.current.mousePosition) - _dragStartPos;
                _dragStartPos = Vector2Int.CeilToInt(Event.current.mousePosition);

                NeedsRepaint = true;
            }
        }

        /// <summary>
        /// Limit bounds based on the grid options, makes sure that there's at least 1 pixel visible from the specified bounds
        /// </summary>
        /// <param name="size"></param>
        protected virtual void LimitBounds(Vector2 size)
        {
            Vector2 topLeft = GetGridPos(GridOptions.CellBounds.position).position + Vector2.one;
            Vector2 bottomRight = GetGridPos(GridOptions.CellBounds.position + GridOptions.CellBounds.size).position;

            if (topLeft.x > size.x) GridOptions.GridOffset.x -= topLeft.x - size.x; 
            if (topLeft.y > size.y) GridOptions.GridOffset.y -= topLeft.y - size.y;
            if (bottomRight.x < 0) GridOptions.GridOffset.x -= bottomRight.x;
            if (bottomRight.y < 0) GridOptions.GridOffset.y -= bottomRight.y;
        }

        /// <summary>
        /// Change the zoom of the grid based on the scrolling wheel
        /// </summary>
        protected virtual void Zoom()
        {
            if (_zoomScroll.y != 250)
            {
                float zoom = (_zoomScroll.y - 250) / 1000;

                //apply zoom
                GridOptions.ZoomAmount -= zoom * GridOptions.ZoomAmount;
                GridOptions.ZoomAmount = Mathf.Clamp(GridOptions.ZoomAmount, GridOptions.MinZoom, GridOptions.MaxZoom);

                //reset scroll
                _zoomScroll.y = 250;

                //change zoom origin
                Vector2Int newGridSize = new Vector2Int(
                (int)(GridOptions.GridCellSize.x * GridOptions.ZoomAmount),
                (int)(GridOptions.GridCellSize.y * GridOptions.ZoomAmount)
                );

                Vector2 dist = Event.current.mousePosition - _currentGridOrigin;
                Vector2 newDist = new Vector2(dist.x / _currentGridCellSize.x * newGridSize.x, dist.y / _currentGridCellSize.y * newGridSize.y);
                GridOptions.GridOffset += dist - newDist;

                NeedsRepaint = true;
            }
        }
#endregion

#region Draw functions
        /// <summary>
        /// Draw an outline around one grid cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="color"></param>
        /// <param name="drawAll"></param>
        public void DrawCellOutline(Vector2Int cell, Color color, bool drawAll = true) => DrawCellOutline(cell, Vector2Int.one, color, drawAll);
        /// <summary>
        /// Draw an outline around one grid cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="drawAll"></param>
        public void DrawCellOutline(int x, int y, Color color, bool drawAll = true) => DrawCellOutline(x, y, 0, 0, color, drawAll);
        /// <summary>
        /// Draw an outline around multiple grid cells
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="color"></param>
        /// <param name="drawAll"></param>
        public void DrawCellOutline(int x, int y, int width, int height, Color color, bool drawAll = true) => DrawCellOutline(new Vector2Int(x, y), new Vector2Int(width, height), color, drawAll);
        /// <summary>
        /// Draw an outline around multiple grid cells
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="size"></param>
        /// <param name="color"></param>
        /// <param name="drawAll"></param>
        public virtual void DrawCellOutline(Vector2Int cell, Vector2Int size, Color color, bool drawAll = true)
        {
            Rect rect = new Rect(GetGridPos(cell).position, size * _currentGridCellSize);

            EditorGUI.DrawRect(new Rect(rect.x + 0, rect.y + 0, 1, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.x + 0, rect.y + rect.height, rect.width, 1), color); // Down

            if (drawAll)
            {
                EditorGUI.DrawRect(new Rect(rect.x + 0, rect.y, rect.width, 1), color); // Up
                EditorGUI.DrawRect(new Rect(rect.x + rect.width, rect.y + 0, 1, rect.height), color); // Right
            }
        }

        public void DrawCell(int x, int y, Color color) => DrawCell(new Vector2Int(x, y), color);
        public virtual void DrawCell(Vector2Int cell, Color color) => EditorGUI.DrawRect(GetGridPos(cell), color);
        public void DrawCell(int x, int y, Texture2D tex) => DrawCell(new Vector2Int(x, y), tex);
        public virtual void DrawCell(Vector2Int cell, Texture2D tex) => GUI.DrawTexture(GetGridPos(cell), tex);
        public void DrawCell(GUIGridCell cell)
        {
            DrawCell(cell.Cell, cell.BackgroundColor);
            if (cell.Texture != null) DrawCell(cell.Cell, cell.Texture);
        }

        public void DrawCells(int x, int y, int columns, int rows, Color color) => DrawCells(new Vector2Int(x, y), new Vector2Int(columns, rows), color);
        public virtual void DrawCells(Vector2Int origin, Vector2Int amount, Color color)
        {
            var olRect = GetGridPos(origin);
            var rect = new Rect(olRect.x, olRect.y, olRect.width * amount.x, olRect.height * amount.y);

            EditorGUI.DrawRect(rect, color);
        }
        public void DrawCells(int x, int y, int columns, int rows, Texture2D tex) => DrawCells(new Vector2Int(x, y), new Vector2Int(columns, rows), tex);
        public virtual void DrawCells(Vector2Int origin, Vector2Int amount, Texture2D tex)
        {
            var olRect = GetGridPos(origin);
            var rect = new Rect(olRect.x, olRect.y, olRect.width * amount.x, olRect.height * amount.y);

            GUI.DrawTexture(rect, tex);
        }
        public virtual void DrawCells(List<GUIGridCell> cells)
        {
            foreach (var item in cells) DrawCell(item);
        }
#endregion

#region Get data
        /// <summary>
        /// Returns the screen coordinate of a given tile coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Rect GetGridPos(int x, int y) => GetGridPos(new Vector2Int(x, y));
        /// <summary>
        /// Returns the screen coordinate of a given tile coordinate
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual Rect GetGridPos(Vector2Int tile) => new Rect(
                _currentGridOrigin.x + tile.x * _currentGridCellSize.x,
                _currentGridOrigin.y + tile.y * _currentGridCellSize.y,
                _currentGridCellSize.x,
                _currentGridCellSize.y
                );

        /// <summary>
        /// Check whether a tile is visible in the window or not
        /// </summary>
        /// <param name="windowSize"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsTileInWindow(Vector2 windowSize, int x, int y) => IsTileInWindow(windowSize, new Vector2Int(x, y));
        /// <summary>
        /// Check whether a tile is visible in the window or not
        /// </summary>
        /// <param name="windowSize"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual bool IsTileInWindow(Vector2 windowSize, Vector2Int tile)
        {
            Rect rect = GetGridPos(tile);

            if (
                rect.x > -_currentGridCellSize.x &&
                rect.x < windowSize.x + _currentGridCellSize.x &&
                rect.y > -_currentGridCellSize.y &&
                rect.y < windowSize.y + _currentGridCellSize.y
                )
                return true;
            else return false;
        }

        /// <summary>
        /// Returns the coordinate of a tile at a given screen position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector2Int GetTilePos(Vector2 position) => Vector2Int.FloorToInt((position - _currentGridOrigin) / _currentGridCellSize);
        /// <summary>
        /// Returns the tile coordinate of the mouse cursor
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetMouseTilePos() => GetTilePos(Event.current.mousePosition);
        /// <summary>
        /// Returns the screen position of the tile that's under the mouse cursor
        /// </summary>
        /// <returns></returns>
        public Rect GetMouseGridPos() => GetGridPos(GetMouseTilePos());


#endregion

        public GUIGrid()
        {
            GridOptions = new GUIGridOptions();
        }
        public GUIGrid(GUIGridOptions options)
        {
            GridOptions = options;
        }
    }

    [System.Serializable]
    public class GUIGridOptions
    {
        public Color BackgroundColor = new Color(40f / 256f, 40f / 256f, 40f / 256f);
        public Color LineColor = new Color(81f / 256f, 81f / 256f, 81f / 256f);
        public Color HighlightColor = new Color(168f / 256f, 168f / 256f, 168f / 256f);

        public Vector2Int GridCellSize = new Vector2Int(32, 32);
        public Vector2 GridOffset = Vector2.zero;
        public float ZoomAmount = 1;
        public bool DrawGrid = true;

        public float MinZoom = .1f;
        public float MaxZoom = 6f;

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

#region Presets
        public static GUIGridOptions Default => new GUIGridOptions();
        
#endregion
    }

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