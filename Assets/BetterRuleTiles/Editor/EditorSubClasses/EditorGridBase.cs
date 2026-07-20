#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;
using Clipboard = VinTools.BetterRuleTiles.Editor.Data.TileEditorClipboard;

namespace VinTools.BetterRuleTiles.Editor.Grid
{
    public class EditorGridBase
    {
        #region Constructors
        public EditorGridBase(BetterRuleTileEditor window, bool _new = true)
        {
            this.window = window;
            SetUpClass(window, _new);
            
            //cache all sprites
            foreach (var item in window._file._grid) Cache.Add(item.Sprite);
        }

        public void SetUpClass(BetterRuleTileEditor window, bool _new)
        {

            if (_new)
            {
                window._file.settings._cellSize = Vector2.one;
                window._tileRenderOffset = Vector2.zero;
            }
        }
        #endregion

        #region variables
        public BetterRuleTileEditor window;
        
        public virtual Texture2D t_lockedTexture => Tex.Get(Tex.T_ID.t_lockedTexture);
        public virtual Vector2 _defaultTileScale => Vector2.one;

        public Vector2 d_currentGridCellSize;
        public Vector2 d_currentUnscaledGridCellSize;
        public Vector2 d_currentGridStart;
        public Vector2 d_currentGridCellAmount;
        public Vector2 d_currentGridOffset;
        public Vector2 d_currentGridOrigin;

        public Vector2 _dragStartPos = Vector2.zero;
        public Vector2 _zoomScroll = new Vector2(0, 250);

        public float _zoomAmount { get => window._zoomAmount; set => window._zoomAmount = value; }

        public Vector2Int _gridCellSize => Vector2Int.RoundToInt(window._file.settings._cellSize * BetterRuleTileContainer.EditorSettings.GridUnitPixelSize);
        public Vector2 _tileRenderOffset { get => window._tileRenderOffset; set => window._tileRenderOffset = value; }
        public Vector2 _gridOffset { get => window._gridOffset; set => window._gridOffset = value; }
        public BetterRuleTileContainer.EditorSettings settings => window._file.settings;
        #endregion

        public void DrawAll()
        {
            //draw base grid, tiles and spritess
            DrawgGridBG();
            DrawTiles(window._file.Grid);
            ManageGrid();
            if (!window._hideSprites) DrawSprites(window._file.Grid);

            //draw ruler
            if (window._showRuler && _zoomAmount > .8f) DrawRuler();

            //draw paste tiles, sprites and selection
            if (Clipboard.CurrentlyPastingInEditor == window)
            {
                var clipboardMovedBy = new Vector2Int((int)Clipboard.Rect.position.x, (int)Clipboard.Rect.position.y) - Clipboard.Origin;
                DrawClipboardPreviewTiles(Clipboard.Contents, clipboardMovedBy);
                DrawClipboardPreviewSprites(Clipboard.Contents, clipboardMovedBy);
                DrawGridTileOutline(Vector2Int.RoundToInt(Clipboard.Rect.position), Vector2Int.RoundToInt(Clipboard.Rect.size), Tex.Get(Tex.C_ID.clipboardColor));
            }


            //highlight modified cells
            if (window._showModified)
            {
                foreach (var tile in window._file.Grid.Where(t => t.AreNeighborPositionsModified || t.AreOutputSpritesModified))
                {
                    bool cycle = Mathf.FloorToInt((float)EditorApplication.timeSinceStartup * 5f) % 2 == 0;


                    if (cycle)
                    {
                        if (tile.AreOutputSpritesModified) DrawGridTileOutline(tile.Position, Vector2Int.one, new Color(1f, 210f / 255f, 1f));
                    }
                    else
                    {
                        if (tile.AreNeighborPositionsModified) DrawGridTileOutline(tile.Position, Vector2Int.one, new Color(1f, 1f, 210f / 255f));
                    }
                }
            }
            
            //draw preset blocks
            DrawPresetBlocksOverlay(window._file._presetBlocks);
            //draw locked tile overlay
            DrawLockedOverlay(window._file.Grid);

            //draw selection
            if (window._inspectingCell != null) DrawGridTileOutline(window._inspectingCell.Position, Tex.Get(Tex.C_ID.inspectingColor));
            if (window._hasSelection) DrawGridTileOutline(Vector2Int.RoundToInt(window._selectionRect.position), Vector2Int.RoundToInt(window._selectionRect.size), Tex.Get(Tex.C_ID.highlightColor));
            if (window.HasInspectingSelection(out var rect)) DrawGridTileOutline(rect.position, rect.size, Tex.Get(Tex.C_ID.inspectingColor));
            if (window._movingSelection) DrawGridTileOutline(Vector2Int.RoundToInt(window._selectionRect.position) + window._moveToPos - window._moveFromPos, Vector2Int.RoundToInt(window._selectionRect.size), Tex.Get(Tex.C_ID.selectedColor));
            if (window._movingPasteSelection) DrawGridTileOutline(Vector2Int.RoundToInt(Clipboard.Rect.position) + window._moveToPos - window._moveFromPos, Vector2Int.RoundToInt(Clipboard.Rect.size), Tex.Get(Tex.C_ID.selectedColor));

            // Moved to BetterRuleTileEditor.cs
            //window.Repaint();
        }

        #region Grid value functions
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

        /// <summary>
        /// Returns the coordinate of a tile at a given screen position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector2Int GetTilePos(Vector2 position) => Vector2Int.FloorToInt((position - d_currentGridOrigin) / d_currentGridCellSize);

        /// <summary>
        /// Returns the screen coordinate of a given tile
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual Rect GetGridPos(Vector2Int tile) => new Rect(
                d_currentGridOrigin.x + tile.x * d_currentGridCellSize.x,
                d_currentGridOrigin.y + tile.y * d_currentGridCellSize.y,
                d_currentGridCellSize.x,
                d_currentGridCellSize.y
                );

        /// <summary>
        /// Check whether a tile is visible in the window or not
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual bool IsTileInWindow(Vector2Int tile)
        {
            Rect rect = GetGridPos(tile);

            if (
                rect.x > -d_currentGridCellSize.x &&
                rect.x < window.position.width + d_currentGridCellSize.x &&
                rect.y > -d_currentGridCellSize.y &&
                rect.y < window.position.height + d_currentGridCellSize.y
                )
                return true;
            else return false;
        }
        #endregion

        #region Draw grid functions
        //universal methods
        public void DrawgGridBG()
        {
            if (!window.IsMouseOverWindow())
            {
                //get scrollwheel
                _zoomScroll = GUI.BeginScrollView(new Rect(Vector2.zero, window.position.size), _zoomScroll, new Rect(Vector2.zero, window.position.size + new Vector2(0, 500)));
                GUI.EndScrollView();
            }

            //draw background
            Color bgColor = Tex.Get(Tex.C_ID.c_backgroundColor);
            EditorGUI.DrawRect(new Rect(Vector2.zero, new Vector2(window.position.width, window.position.height)), bgColor);
            // draw warning background for the tileset location
            if (window._file.UseTileSet)
            {
                // get pos of tile -1 -1
                var startPos = GetGridPos(new Vector2Int(-1, -1));
                // calculate coordinates
                var drawStartPos = new Vector2(startPos.x < 0 ? 0 : startPos.x, startPos.y < 0 ? 0 : startPos.y);
                var drawSize = new Vector2(window.position.width - drawStartPos.x, window.position.height - drawStartPos.y);
                // create bg color
                var warningBG = new Color(bgColor.r + .2f, bgColor.g, bgColor.b, bgColor.a);
                // draw BG
                EditorGUI.DrawRect(new Rect(drawStartPos, drawSize), warningBG);
            }
        }
        public void ManageGrid()
        {
            //calculate values and draw gridlines
            DrawGrid();

            //quit if mouse is over a window
            if (window.IsMouseOverWindow()) return;

            //drag move grid
            DragMoveGrid();

            //zoom
            Zoom();
        }

        public virtual void DragMoveGrid()
        {
            if (Event.current.button > 0 && Event.current.type == EventType.MouseDown) _dragStartPos = Vector2Int.FloorToInt(Event.current.mousePosition);
            if (Event.current.button > 0 && Event.current.type == EventType.MouseDrag)
            {
                _gridOffset += Vector2Int.CeilToInt(Event.current.mousePosition) - _dragStartPos;
                _dragStartPos = Vector2Int.CeilToInt(Event.current.mousePosition);
            }
        }
        public virtual void Zoom()
        {
            if (_zoomScroll.y != 250)
            {
                float zoom = (_zoomScroll.y - 250) / 1000;
                float prewZoomAmount = _zoomAmount;

                //apply zoom
                _zoomAmount -= zoom * _zoomAmount;
                _zoomAmount = Mathf.Clamp(_zoomAmount, .1f, 24f);

                //reset scroll
                _zoomScroll.y = 250;

                //change zoom origin
                Vector2Int newGridSize = new Vector2Int(
                (int)(_gridCellSize.x * _zoomAmount),
                (int)(_gridCellSize.y * _zoomAmount)
                );

                Vector2 dist = Event.current.mousePosition - d_currentGridOrigin;
                Vector2 newDist = new Vector2(dist.x / d_currentGridCellSize.x * newGridSize.x, dist.y / d_currentGridCellSize.y * newGridSize.y);
                _gridOffset += dist - newDist;
            }
        }
        public virtual void CalculateGridVariables()
        {
            //calculate variables
            d_currentGridCellSize = Vector2Int.FloorToInt((Vector2)_gridCellSize * _zoomAmount);
            d_currentUnscaledGridCellSize = Vector2Int.FloorToInt(_zoomAmount * BetterRuleTileContainer.EditorSettings.GridUnitPixelSize * Vector2.one);

            if (d_currentGridCellSize.x < 1) d_currentGridCellSize.x = 1;
            if (d_currentGridCellSize.y < 1) d_currentGridCellSize.y = 1;

            d_currentGridCellAmount = new Vector2Int(
                (int)window.position.width / (int)d_currentGridCellSize.x + 40,
                (int)window.position.height / (int)d_currentGridCellSize.y + 40
                );
            d_currentGridStart = new Vector2Int(
                (int)_gridOffset.x % (int)d_currentGridCellSize.x - 3 * _gridCellSize.x,
                (int)_gridOffset.y % (int)d_currentGridCellSize.y - 3 * _gridCellSize.y
                );
            d_currentGridOffset = new Vector2Int(
                -(int)_gridOffset.x / (int)d_currentGridCellSize.x,
                -(int)_gridOffset.y / (int)d_currentGridCellSize.y
                );
            d_currentGridOrigin = d_currentGridStart - d_currentGridOffset * d_currentGridCellSize;
        }
        public virtual void DrawGrid()
        {
            CalculateGridVariables();

            if (!window._file.settings._renderSmallGrid && _zoomAmount < window._file.settings._zoomTreshold) return;

            //draw grid lines
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                EditorGUI.DrawRect(new Rect(
                    d_currentGridStart.x + x * d_currentGridCellSize.x,
                    d_currentGridStart.y,
                    1,
                    d_currentGridCellAmount.y * d_currentGridCellSize.y
                    ),
                    Tex.Get(Tex.C_ID.lineColor));
            }
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                EditorGUI.DrawRect(new Rect(
                    d_currentGridStart.x,
                    d_currentGridStart.y + y * d_currentGridCellSize.y,
                    d_currentGridCellAmount.x * d_currentGridCellSize.x,
                    1
                    ),
                    Tex.Get(Tex.C_ID.lineColor));
            }
        }
        public virtual void DrawRuler()
        {
            //draw bottom row
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                GUI.Label(new Rect(d_currentGridStart.x + x * d_currentGridCellSize.x, window.position.height - 50, d_currentGridCellSize.x, 50), $"{d_currentGridOffset.x + x}", Style.LowerCenteredLabel);
            }

            //draw left row
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                GUI.Label(new Rect(0, d_currentGridStart.y + y * d_currentGridCellSize.y, 50, d_currentGridCellSize.y), $"{d_currentGridOffset.y + y}", Style.MiddleLeftLabel);
            }
        }

        public void DrawGridTileOutline(Vector2Int tile, Texture2D tex, bool drawAll = true) => DrawGridTileOutline(tile, Vector2Int.one, tex, drawAll);
        public void DrawGridTileOutline(Vector2Int tile, Color col, bool drawAll = true) => DrawGridTileOutline(tile, Vector2Int.one, col, drawAll);
        public virtual void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Texture2D tex, bool drawAll = true)
        {
            Rect rect = new Rect(GetGridPos(tile).position, size * d_currentGridCellSize);

            GUI.DrawTexture(new Rect(rect.x + 0, rect.y + 0, 1, rect.height), tex); // Left
            GUI.DrawTexture(new Rect(rect.x + 0, rect.y + rect.height, rect.width, 1), tex); // Down

            if (drawAll)
            {
                GUI.DrawTexture(new Rect(rect.x + 0, rect.y, rect.width, 1), tex); // Up
                GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y + 0, 1, rect.height), tex); // Right
            }
        }
        public void DrawGridTileOutline(Vector2 tile, Vector2 size, Color col, bool drawAll = true) => DrawGridTileOutline(new Vector2Int(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y)), new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)), col, drawAll);
        public virtual void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Color col, bool drawAll = true)
        {
            Rect rect = new Rect(GetGridPos(tile).position, size * d_currentGridCellSize);

            Handles.BeginGUI();
            Handles.color = col;

            Handles.DrawLine(rect.position + Vector2.one, rect.position + Vector2.up * rect.height + Vector2.one); // Left
            Handles.DrawLine(rect.position + Vector2.up * rect.height + Vector2.one, rect.position + rect.size + Vector2.one); // Down

            if (drawAll)
            {
                Handles.DrawLine(rect.position + Vector2.one, rect.position + Vector2.right * rect.width + Vector2.one);
                Handles.DrawLine(rect.position + Vector2.right * rect.width + Vector2.one, rect.position + rect.size + Vector2.one);
            }

            Handles.EndGUI();
        }
        public void DrawEdge(BetterRuleTileContainer.PresetBlock.TileEdge edge, Color col) => DrawEdge(edge.Pos, edge.Pos, edge.Side, col);
        public void DrawEdge(BetterRuleTileContainer.PresetBlock.BlockEdgeSpan edge, Color col) => DrawEdge(edge.Pos1, edge.Pos2, edge.Side, col);
        public virtual void DrawEdge(Vector2Int pos1, Vector2Int pos2, BetterRuleTileContainer.Edge side, Color col)
        {
            Vector2 startGridPos = GetGridPos(Min(pos1, pos2)).position + Vector2.one;

            Handles.BeginGUI();
            Handles.color = col;

            switch (side)
            {
                case BetterRuleTileContainer.Edge.Up:
                    Handles.DrawLine(startGridPos + Vector2.up * d_currentGridCellSize, startGridPos + Vector2.up * d_currentGridCellSize + (Abs(pos2 - pos1) + Vector2.right) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Down:
                    Handles.DrawLine(startGridPos, startGridPos + (Abs(pos2 - pos1) + Vector2.right) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Left:
                    Handles.DrawLine(startGridPos, startGridPos + (Abs(pos2 - pos1) + Vector2.up) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Right:
                    Handles.DrawLine(startGridPos + Vector2.right * d_currentGridCellSize, startGridPos + Vector2.right * d_currentGridCellSize + (Abs(pos2 - pos1) + Vector2.up) * d_currentGridCellSize);
                    break;
                default:
                    break;
            }

            Handles.EndGUI();

            Vector2 Abs(Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
            Vector2Int Min(Vector2Int v1, Vector2Int v2) => new Vector2Int(Mathf.Min(v1.x, v2.x), Mathf.Min(v1.y, v2.y));
        }
        public virtual void DrawLockedOverlay(List<BetterRuleTileContainer.GridCell> item)
        {
            foreach (var cell in item.FindAll(t => t.Locked))
            {
                if (settings._renderLockedOverlay) GUI.DrawTexture(RectTools.SquareifyRect(GetGridPos(cell.Position)), t_lockedTexture, ScaleMode.ScaleToFit);
                if (settings._renderLockedOutline) DrawGridTileOutline(cell.Position, Vector2Int.one, settings._lockedOutlineColor, true);
            }
        }
        public virtual void DrawPresetBlocksOverlay(List<BetterRuleTileContainer.PresetBlock> blocks)
        {
            foreach (var block in blocks)
            {
                foreach (var item in block.EdgeSpans) DrawEdge(item, settings._presetBlockColor);

                /*foreach (var bounds in block.Bounds)
                {
                    DrawGridTileOutline(bounds.position, bounds.size, settings._presetBlockColor, true);
                }*/
            }
        }

        public virtual void DrawSprites(List<BetterRuleTileContainer.GridCell> item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                if (item[i].Sprite == null) continue;

                GridSpriteRenderer.DrawSpriteOnGrid(this, item[i]);
            }
        }
        public virtual void DrawClipboardPreviewSprites(List<BetterRuleTileContainer.GridCell> item, Vector2Int moveBy) => DrawSprites(item);
        
        public virtual void DisplayMissingTextureError(Sprite sprite) => Debug.LogWarning($"Texture data of sprite \"{sprite}\" couldn't be read as the image is not readable. Enable the \"Read/Write\" option in the texture import settings, than reload the editor window.", sprite.texture);

        public virtual void DrawTiles(List<BetterRuleTileContainer.GridCell> item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                
                GridSpriteRenderer.DrawTileOnGrid(this, item[i], window._drawerData);
            }
        }
        public virtual void DrawClipboardPreviewTiles(List<BetterRuleTileContainer.GridCell> item, Vector2Int movedBy) => DrawTiles(item);
        
        public Rect GetOffsetGridPos(Vector2Int tile)
        {
            Rect original = GetGridPos(tile);
            return new Rect(
                original.x + d_currentGridCellSize.x * _tileRenderOffset.x,
                original.y + d_currentGridCellSize.y * _tileRenderOffset.y,
                original.width,
                original.height
                );
        }

        #endregion

        #region windows
        public virtual Rect DrawInteractiveMiniGrid(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //reset if deleted all neighbors, this is to prevent errors
            if (cell.NeighborPositions.Count <= 0) ResetCellNeighbors(cell);

            //calculate values
            Vector2Int size = new Vector2Int(18, 18);
            Vector2Int min = new Vector2Int(cell.NeighborPositions.Min(t => t.x) - 1, cell.NeighborPositions.Min(t => t.y) - 1);
            Vector2Int max = new Vector2Int(cell.NeighborPositions.Max(t => t.x) + 1, cell.NeighborPositions.Max(t => t.y) + 1);
            Vector2 start = new Vector2(rect.x + (rect.width - (max.x - min.x + 1) * size.x) / 2f, rect.y + 26);
            Rect allRect = new Rect(rect.x, rect.y, rect.width, size.y * (max.y - min.y) + 75);

            //draw background, title and buttons
            DrawMiniGridBackground(allRect, cell);

            //display grid
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2 index = new Vector2(x - min.x, y - min.y);
                    Rect cellRect = new Rect(start + index * size - Vector2.one, size + new Vector2Int(2, 2));

                    //center
                    if (x == 0 && y == 0)
                    {
                        GUIContent buttonIcon = new GUIContent("");
                        switch (cell.Transform)
                        {
                            case RuleTile.TilingRuleOutput.Transform.Fixed: buttonIcon = new GUIContent(Tex.Get(Tex.T_ID.t_Fixed), "Fixed"); break;
                            case RuleTile.TilingRuleOutput.Transform.Rotated: buttonIcon = new GUIContent(Tex.Get(Tex.T_ID.t_Rotated), "Rotated"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorX: buttonIcon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorX), "MirrorX"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorY: buttonIcon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorY), "MirrorY"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorXY: buttonIcon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorXY), "MirrorXY"); break;
                        }

                        if (GUI.Button(cellRect, buttonIcon, Style.MiniGridButtonStyle))
                        {
                            switch (cell.Transform)
                            {
                                case RuleTile.TilingRuleOutput.Transform.Fixed: cell.Transform = RuleTile.TilingRuleOutput.Transform.Rotated; continue;
                                case RuleTile.TilingRuleOutput.Transform.Rotated: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorX; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorX: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorY; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorY: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorXY; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorXY: cell.Transform = RuleTile.TilingRuleOutput.Transform.Fixed; continue;
                            }
                        }
                        continue;
                    }

                    // has rule
                    if (cell.NeighborPositions.Contains(new Vector3Int(x, y, 0)))
                    {
                        if (GUI.Button(cellRect, Tex.Get(Tex.T_ID.t_MiniButtonCheckmark), Style.MiniGridButtonStyle))
                        {
                            //window._file.RecordObject($"Changed neighbor position checking at position: {x}, {y}");
                            cell.NeighborPositions.Remove(new Vector3Int(x, y, 0));
                        }
                        continue;
                    }
                    // has no rule
                    else
                    {
                        if (GUI.Button(cellRect, "", Style.MiniGridButtonStyle))
                        {
                            //window._file.RecordObject($"Changed neighbor position checking at position: {x}, {y}");
                            cell.NeighborPositions.Add(new Vector3Int(x, y, 0));
                        }
                        continue;
                    }
                }
            }

            //return size of the window
            return new Rect(allRect.x, allRect.y, allRect.width, allRect.height - size.y);
        }
        public void DrawMiniGridBackground(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //draw background
            GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_windowBorderTexture), ScaleMode.StretchToFill, true, 0, Color.white, 0, 5);
            GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, Color.white, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.Get(Tex.T_ID.t_fieldBoxTexture), ScaleMode.StretchToFill, true, 0, Color.white, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.Get(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, Color.white, 1, 5);
            
            if (EditorGUIUtility.isProSkin) GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), "Neighbor positions", "boldlabel");
            else GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), "Neighbor positions", Style.GridWhiteTextStyle);

            //bottom buttons
            if (!window._hasSelection) GUI.Box(new Rect(rect.position + new Vector2(5, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Select an area to add it");
            if (window._hasSelection && GUI.Button(new Rect(rect.position + new Vector2(5, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Add selection")) AddSelectionToRules(cell);
            if (GUI.Button(new Rect(rect.position + new Vector2(rect.width / 2 + 2, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Reset to default")) ResetCellNeighbors(cell);

        }
        public virtual void AddSelectionToRules(BetterRuleTileContainer.GridCell cell)
        {
            List<Vector3Int> pos = new List<Vector3Int>();
            for (int x = Mathf.Min(window._selectionStart.x, window._selectionEnd.x); x <= Mathf.Max(window._selectionStart.x, window._selectionEnd.x); x++)
            {
                for (int y = Mathf.Min(window._selectionStart.y, window._selectionEnd.y); y <= Mathf.Max(window._selectionStart.y, window._selectionEnd.y); y++)
                {
                    var p = new Vector3Int(x - cell.Position.x, y - cell.Position.y, 0);
                    if (p != Vector3Int.zero && !cell.NeighborPositions.Contains(p)) pos.Add(p);
                }
            }
            cell.NeighborPositions.AddRange(pos);
        }
        public void ResetCellNeighbors(BetterRuleTileContainer.GridCell cell)
        {
            cell.NeighborPositions = window._file.DefaultNeighborPositions(cell.Position);

            cell.Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
        }
        #endregion
    }
}
#endif