#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.Grid
{
    public class IsometricEditorGrid : EditorGridBase
    {
        public override Texture2D t_lockedTexture => Tex.Get(Tex.T_ID.t_isometricLockedTexture);
        public override Vector2 _defaultTileScale => Vector2.one / 2;
        
        public IsometricEditorGrid(BetterRuleTileEditor window, bool _new = true) : base(window, _new)
        {
            this.window = window;
            SetUpClass(window, _new);
            
            if (_new)
            {
                window._file.settings._cellSize = new Vector2(1, .5f);
                window._tileRenderOffset = new Vector2(0, 0);
            }
        }

        public override void DrawGrid()
        {
            CalculateGridVariables();

            if (!window._file.settings._renderSmallGrid && _zoomAmount < window._file.settings._zoomTreshold) return;

            //set up drawing lines
            Handles.BeginGUI();
            Handles.color = new Color(81f / 256f, 81f / 256f, 81f / 256f);

            //calculate line direction
            Vector2 lineDir = Mathf.Max(d_currentGridCellAmount.x, d_currentGridCellAmount.y) * d_currentGridCellSize;

            //draw grid lines
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                Vector2 startPos = new Vector2(d_currentGridStart.x + (x + .5f) * d_currentGridCellSize.x, d_currentGridStart.y);
                Vector2 offset1 = lineDir;
                Vector2 offset2 = lineDir * new Vector2(-1, 1);

                Handles.DrawLine(startPos, startPos + offset1);
                Handles.DrawLine(startPos, startPos + offset2);
            }
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                Vector2 startPos1 = new Vector2(d_currentGridStart.x, d_currentGridStart.y + (y + .5f) * d_currentGridCellSize.y);
                Vector2 startPos2 = new Vector2(d_currentGridStart.x + d_currentGridCellAmount.x * d_currentGridCellSize.x, d_currentGridStart.y + (y + .5f) * d_currentGridCellSize.y);
                Vector2 offset1 = lineDir;
                Vector2 offset2 = lineDir * new Vector2(-1, 1);

                Handles.DrawLine(startPos1, startPos1 + offset1);
                Handles.DrawLine(startPos2, startPos2 + offset2);
            }

            //end drawing lines
            Handles.EndGUI();
        }
        public override void Zoom()
        {
            if (_zoomScroll.y != 250)
            {
                float zoom = (_zoomScroll.y - 250) / 1000;
                float prewZoomAmount = _zoomAmount;

                //apply zoom
                _zoomAmount -= zoom * (_zoomAmount > 1 ? _zoomAmount : 1);
                _zoomAmount = Mathf.Clamp(_zoomAmount, .1f, 6f);
                _zoomAmount = Mathf.Round(_zoomAmount * 16) / 16;

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

        public override Vector2Int GetTilePos(Vector2 position) => IsometricToSquareGridPoint(position);
        public override Rect GetGridPos(Vector2Int tile)
        {
            return new Rect(
                SquareToIsometricGridPoint(tile) + d_currentGridOrigin,
                d_currentGridCellSize
                );
        }

        public override void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Texture2D tex, bool drawAll = true)
        {
            DrawGridTileOutline(tile, size, tex.GetPixel(0, 0), drawAll);
        }
        public override void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Color col, bool drawAll = true)
        {
            Vector2 topCorner = GetGridPos(tile).position + new Vector2(d_currentGridCellSize.x / 2, 0);
            Vector2 rightDown = d_currentGridCellSize / 2 * size.x;
            Vector2 leftDown = new Vector2(-d_currentGridCellSize.x, d_currentGridCellSize.y) / 2 * size.y;

            Handles.BeginGUI();
            Handles.color = col;

            Handles.DrawLine(topCorner, topCorner + rightDown);
            Handles.DrawLine(topCorner, topCorner + leftDown);

            if (drawAll)
            {
                Handles.DrawLine(topCorner + rightDown, topCorner + rightDown + leftDown);
                Handles.DrawLine(topCorner + leftDown, topCorner + leftDown + rightDown);
            }

            Handles.EndGUI();
        }
        public override void DrawEdge(Vector2Int pos1, Vector2Int pos2, BetterRuleTileContainer.Edge side, Color col)
        {
            Vector2 offset = new Vector2(d_currentGridCellSize.x / 2, 0);
            Vector2 startGridPos = GetGridPos(Min(pos1, pos2)).position + offset;
            Vector2 rightDown = d_currentGridCellSize / 2;
            Vector2 leftDown = new Vector2(-d_currentGridCellSize.x, d_currentGridCellSize.y) / 2;

            Handles.BeginGUI();
            Handles.color = col;

            switch (side)
            {
                case BetterRuleTileContainer.Edge.Up:
                    Handles.DrawLine(startGridPos + leftDown, GetGridPos(Max(pos1, pos2) + Vector2Int.right).position + offset + leftDown);
                    break;
                case BetterRuleTileContainer.Edge.Down:
                    Handles.DrawLine(startGridPos, GetGridPos(Max(pos1, pos2) + Vector2Int.right).position + offset);
                    break;
                case BetterRuleTileContainer.Edge.Left:
                    Handles.DrawLine(startGridPos, GetGridPos(Max(pos1, pos2) + Vector2Int.up).position + offset);
                    break;
                case BetterRuleTileContainer.Edge.Right:
                    Handles.DrawLine(startGridPos + rightDown, GetGridPos(Max(pos1, pos2) + Vector2Int.up).position + offset + rightDown); 
                    break;
                default:
                    break;
            }

            Handles.EndGUI();
            
            Vector2Int Min(Vector2Int v1, Vector2Int v2) => new Vector2Int(Mathf.Min(v1.x, v2.x), Mathf.Min(v1.y, v2.y));
            Vector2Int Max(Vector2Int v1, Vector2Int v2) => new Vector2Int(Mathf.Max(v1.x, v2.x), Mathf.Max(v1.y, v2.y));
        }

        public override void DrawTiles(List<BetterRuleTileContainer.GridCell> item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                
                GridSpriteRenderer.DrawTileOnGrid(this, item[i], window._drawerData);
            }
        }
        
        public override Rect DrawInteractiveMiniGrid(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //reset if deleted all neighbors, this is to prevent errors
            if (cell.NeighborPositions.Count <= 0) ResetCellNeighbors(cell);

            //calculate values
            //Vector2Int size = new Vector2Int(20, 20 * _gridCellSize.y / _gridCellSize.x);
            Vector2Int size = new Vector2Int(28, 28);
            Vector2Int min = new Vector2Int(cell.NeighborPositions.Min(t => t.x) - 1, cell.NeighborPositions.Min(t => t.y) - 1);
            Vector2Int max = new Vector2Int(cell.NeighborPositions.Max(t => t.x) + 1, cell.NeighborPositions.Max(t => t.y) + 1);
            Vector2Int cellAmount = new Vector2Int(max.x - min.x, max.y - min.y);

            Vector2 start = new Vector2(
                rect.x - size.x / 2 + rect.width / 2 + ((cellAmount.y - cellAmount.x) / 4f * size.x),
                rect.y + 26);
            Rect allRect = new Rect(
                rect.x, 
                rect.y, 
                rect.width, 
                size.y * (Mathf.Abs(cellAmount.x - cellAmount.y) / 2f + Mathf.Min(cellAmount.x, cellAmount.y)) + 85
                );

            //draw background, title and buttons
            DrawMiniGridBackground(allRect, cell);

            //display grid
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2Int index = new Vector2Int(x - min.x, y - min.y);
                    Rect cellRect = new Rect(start + SquareToIsometricGridPoint(index, size) - Vector2.one, size + new Vector2Int(2, 2));

                    //draw background
                    GUI.DrawTexture(cellRect, Tex.Get(Tex.T_ID.t_IsometricMiniButtonBG));

                    //create icons and tooltips
                    GUIContent icon = new GUIContent("");
                    if (x == 0 && y == 0)
                    {
                        switch (cell.Transform)
                        {
                            case RuleTile.TilingRuleOutput.Transform.Fixed: icon = new GUIContent(Tex.Get(Tex.T_ID.t_Fixed), "Fixed"); break;
                            case RuleTile.TilingRuleOutput.Transform.Rotated: icon = new GUIContent(Tex.Get(Tex.T_ID.t_Rotated), "Rotated"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorX: icon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorX), "MirrorX"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorY: icon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorY), "MirrorY"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorXY: icon = new GUIContent(Tex.Get(Tex.T_ID.t_MirrorXY), "MirrorXY"); break;
                        }
                    }
                    else if(cell.NeighborPositions.Contains(new Vector3Int(x, y, 0))) icon = new GUIContent(Tex.Get(Tex.T_ID.t_MiniButtonCheckmark), "");

                    //display icons
                    if (icon.image != null)
                    {
                        Vector2 imgSize = new Vector2(icon.image.width, icon.image.height);
                        Rect drawRect = new Rect(
                            cellRect.position + (cellRect.size - imgSize) / 2,
                            imgSize
                            );

                        GUI.Label(drawRect, icon, GUIStyle.none);
                    }
                }
            }

            //get the coordinate of what tile was clicked
            Vector2Int gridPos = IsometricToSquareGridPoint(Event.current.mousePosition, size, start) + min;
            if (gridPos.x >= min.x && gridPos.x <= max.x && gridPos.y >= min.y && gridPos.y <= max.y && Event.current.type == EventType.MouseDown)
            {
                //convert it to vector3int
                Vector3Int neighborPos = new Vector3Int(gridPos.x, gridPos.y, 0);

                //if clicked middle
                if (neighborPos == Vector3Int.zero)
                {
                    switch (cell.Transform)
                    {
                        case RuleTile.TilingRuleOutput.Transform.Fixed: cell.Transform = RuleTile.TilingRuleOutput.Transform.Rotated; break;
                        case RuleTile.TilingRuleOutput.Transform.Rotated: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorX; break;
                        case RuleTile.TilingRuleOutput.Transform.MirrorX: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorY; break;
                        case RuleTile.TilingRuleOutput.Transform.MirrorY: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorXY; break;
                        case RuleTile.TilingRuleOutput.Transform.MirrorXY: cell.Transform = RuleTile.TilingRuleOutput.Transform.Fixed; break;
                    }
                }
                //if clicked anywhere else
                else
                {
                    if (cell.NeighborPositions.Contains(neighborPos)) cell.NeighborPositions.Remove(neighborPos);
                    else cell.NeighborPositions.Add(neighborPos);
                }
            }

            //return size of the window
            return new Rect(allRect.x, allRect.y, allRect.width, allRect.height - size.y);
        }

        public Vector2 SquareToIsometricGridPoint(Vector2Int point) => SquareToIsometricGridPoint(point, d_currentGridCellSize);
        public Vector2 SquareToIsometricGridPoint(Vector2Int point, Vector2 cellSize)
        {
            float H = cellSize.x / 2;
            float M = cellSize.y / cellSize.x;

            return new Vector2
                (
                    point.x * 1f * H + //A
                    point.y * -1f * H, //B
                    point.x * M * H + //C
                    point.y * M * H //D
                );
        }
        public Vector2Int IsometricToSquareGridPoint(Vector2 point) => IsometricToSquareGridPoint(point, d_currentGridCellSize, d_currentGridOrigin);
        public Vector2Int IsometricToSquareGridPoint(Vector2 point, Vector2 cellSize) => IsometricToSquareGridPoint(point, cellSize, Vector2.zero);
        public Vector2Int IsometricToSquareGridPoint(Vector2 point, Vector2 cellSize, Vector2 origin)
        {
            float H = cellSize.x / 2;
            float M = cellSize.y / cellSize.x;

            float D = 1 / ((1 * H * M * H) - (-1 * H * M * H));

            var val = new Vector2
                (
                    (point.x - origin.x) * D * M * H + //A
                    (point.y - origin.y) * D * 1f * H, //B
                    (point.x - origin.x) * D * -M * H + //C
                    (point.y - origin.y) * D * 1f * H //D
                );

            return Vector2Int.FloorToInt(val + new Vector2(-.5f, .5f));
        }
    }
}
#endif
