#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles;
using VinTools.Utilities;

namespace VinToolsEditor.BetterRuleTiles
{
    public class HexagonalEditorGrid : EditorGridBase
    {
        public HexagonalEditorGrid(BetterRuleTileEditor window, bool flatTop, bool _new = true) : base(window, _new)
        {
            this.window = window;
            SetUpClass(window, _new);

            if (flatTop) t_lockedTexture = TextureUtils.MaskTexture(TextureUtils.CreateFlatTopHexagonTexture(Color.red, 64, 64), TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAoklEQVR4Ae3WgQ2AIBAEQTH2ben42MasiQWAk3PX3vu94OeGz/4ffY0A+g54AV0A7X8O/8zbX0BW0AbIX/+cvQ7QBbQBuoA6QBfQBugC6gBdQBugC6gDdAFtgC6gDtAFtAG6gDpAF9AG6ALqAF1AG6ALqAN0AW2ALqAO0AW0AbqAOkAX0AboAuoAXUAboAuoA3QBbYAuoA7QBbQBuoA6QBfwAYEyH+RMmXZpAAAAAElFTkSuQmCC"));
            else t_lockedTexture = TextureUtils.MaskTexture(TextureUtils.CreatePointedTopHexagonTexture(Color.red, 64, 64), TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAoklEQVR4Ae3WgQ2AIBAEQTH2ben42MasiQWAk3PX3vu94OeGz/4ffY0A+g54AV0A7X8O/8zbX0BW0AbIX/+cvQ7QBbQBuoA6QBfQBugC6gBdQBugC6gDdAFtgC6gDtAFtAG6gDpAF9AG6ALqAF1AG6ALqAN0AW2ALqAO0AW0AbqAOkAX0AboAuoAXUAboAuoA3QBbYAuoA7QBbQBuoA6QBfwAYEyH+RMmXZpAAAAAElFTkSuQmCC"));

            _flatTop = flatTop;
        }

        private bool _flatTop;
        private Vector2 d_hexScaledCurrentGridCellSize;

        public override void CalculateGridVariables()
        {
            if (_gridCellSize.x < 1) _gridCellSize = new Vector2Int(1, _gridCellSize.y);
            if (_gridCellSize.y < 1) _gridCellSize = new Vector2Int(_gridCellSize.x, 1);

            //calculate variables
            d_hexScaledCurrentGridCellSize = new Vector2(
                Mathf.RoundToInt(_gridCellSize.x * _zoomAmount * (_flatTop ? 1f : 0.8659766f)),
                Mathf.RoundToInt(_gridCellSize.y * _zoomAmount * (_flatTop ? 0.8659766f : 1f))
                );
            d_currentGridCellSize = d_hexScaledCurrentGridCellSize * (_flatTop ? new Vector2(.75f, 1) : new Vector2(1, .75f));

            if (d_currentGridCellSize.x < 1) d_currentGridCellSize.x = 1;
            if (d_currentGridCellSize.y < 1) d_currentGridCellSize.y = 1;

            d_currentGridCellAmount = new Vector2Int(
                (int)window.position.width / (int)d_currentGridCellSize.x + 50,
                (int)window.position.height / (int)d_currentGridCellSize.y + 50
                );
            d_currentGridStart = new Vector2Int(
                (int)_gridOffset.x % (int)d_currentGridCellSize.x - 11 * _gridCellSize.x,
                (int)_gridOffset.y % (int)d_currentGridCellSize.y - 11 * _gridCellSize.y
                );
            d_currentGridOffset = new Vector2Int(
                -(int)_gridOffset.x / (int)d_currentGridCellSize.x,
                -(int)_gridOffset.y / (int)d_currentGridCellSize.y
                );
            d_currentGridOrigin = d_currentGridStart - d_currentGridOffset * d_currentGridCellSize;
        }

        public Vector3[] h_line_seg;
        public Vector3[] h_line;
        public Vector3[] w_line_seg;
        public Vector3[] w_line;

        public override void DrawGrid()
        {
            CalculateGridVariables();

            if (!window._file.settings._renderSmallGrid && _zoomAmount < window._file.settings._zoomTreshold) return;

            Handles.BeginGUI();
            Handles.color = t_lineTexture.GetPixel(0, 0);

            //Get universal variables
            Vector2Int startPos = new Vector2Int(Mathf.Abs(GetTilePos(d_currentGridStart).x), Mathf.Abs(GetTilePos(d_currentGridStart).y));
            Vector2 s = d_hexScaledCurrentGridCellSize;
            int halfcellsize_y = Vector2Int.CeilToInt(d_currentGridCellAmount / 2).y;
            int halfcellsize_x = Vector2Int.CeilToInt(d_currentGridCellAmount / 2).x;

            if (!_flatTop)
            {
                //set values for vertical lines, outside of for loop for performance
                h_line_seg = new Vector3[4];
                h_line = new Vector3[halfcellsize_y * h_line_seg.Length];

                //draw vertical lines
                for (int x = 0; x < d_currentGridCellAmount.x; x++)
                {
                    Vector3 p = new Vector3(d_currentGridStart.x + x * d_currentGridCellSize.x, d_currentGridStart.y) +
                        new Vector3(startPos.x % 2 == 0 ? d_currentGridCellSize.x : 0, startPos.y % 2 == 0 ? d_currentGridCellSize.y : 0);

                    h_line_seg[0] = p;
                    h_line_seg[1] = new Vector3(p.x + -s.x * .5f, p.y + s.y * .25f);
                    h_line_seg[2] = new Vector3(p.x + -s.x * .5f, p.y + s.y * .75f);
                    h_line_seg[3] = new Vector3(p.x + 0, p.y + s.y);

                    int index = 0;
                    for (int i = 0; i < halfcellsize_y; i++)
                    {
                        for (int n = 0; n < h_line_seg.Length; n++)
                        {
                            h_line[index] = new Vector3(h_line_seg[n].x, h_line_seg[n].y + i * d_currentGridCellSize.y * 2);
                            index++;
                        }
                    }

                    Handles.DrawPolyLine(h_line);
                }

                //set values for horizontal lines, outside of for loop for performance
                w_line_seg = new Vector3[2];
                w_line = new Vector3[(int)d_currentGridCellAmount.x * w_line_seg.Length];

                //draw horizontal lines
                for (int y = 0; y < d_currentGridCellAmount.y; y++)
                {
                    Vector3 p = new Vector3(
                        d_currentGridStart.x + (startPos.y % 2 == y % 2 ? d_currentGridCellSize.x / 2 : 0),
                        d_currentGridStart.y + y * d_currentGridCellSize.y);

                    w_line_seg[0] = p;
                    w_line_seg[1] = new Vector3(p.x + s.x * .5f, p.y + s.y * .25f);

                    int index = 0;
                    for (int i = 0; i < d_currentGridCellAmount.x; i++)
                    {
                        for (int n = 0; n < w_line_seg.Length; n++)
                        {
                            w_line[index] = new Vector3(w_line_seg[n].x + i * d_currentGridCellSize.x, w_line_seg[n].y);
                            index++;
                        }
                    }

                    Handles.DrawPolyLine(w_line);
                }
            }
            else
            {
                //set values for vertical lines, outside of for loop for performance
                h_line_seg = new Vector3[2];
                h_line = new Vector3[(int)d_currentGridCellAmount.y * h_line_seg.Length];

                //draw vertical lines
                for (int x = 0; x < d_currentGridCellAmount.x; x++)
                {
                    Vector3 p = new Vector3(
                        d_currentGridStart.x + x * d_currentGridCellSize.x, 
                        d_currentGridStart.y + (startPos.x % 2 == x % 2 ? d_currentGridCellSize.y / 2 : 0)
                        );

                    h_line_seg[0] = p;
                    h_line_seg[1] = new Vector3(p.x + s.x * .25f, p.y + s.y * .5f);

                    int index = 0;
                    for (int i = 0; i < d_currentGridCellAmount.y; i++)
                    {
                        for (int n = 0; n < h_line_seg.Length; n++)
                        {
                            h_line[index] = new Vector3(h_line_seg[n].x, h_line_seg[n].y + i * d_currentGridCellSize.y);
                            index++;
                        }
                    }

                    Handles.DrawPolyLine(h_line);
                }

                //set values for horizontal lines, outside of for loop for performance
                w_line_seg = new Vector3[4];
                w_line = new Vector3[halfcellsize_x * w_line_seg.Length];

                //draw horizontal lines
                for (int y = 0; y < d_currentGridCellAmount.y; y++)
                {
                    Vector3 p = new Vector3(d_currentGridStart.x, d_currentGridStart.y + y * d_currentGridCellSize.y) +
                        new Vector3(startPos.x % 2 == 0 ? d_currentGridCellSize.x : 0, startPos.y % 2 == 0 ? d_currentGridCellSize.y : 0);

                    w_line_seg[0] = p;
                    w_line_seg[1] = new Vector3(p.x + s.x * .25f, p.y - s.y * .5f);
                    w_line_seg[2] = new Vector3(p.x + s.x * .75f, p.y - s.y * .5f);
                    w_line_seg[3] = new Vector3(p.x + s.x, p.y);

                    int index = 0;
                    for (int i = 0; i < halfcellsize_x; i++)
                    {
                        for (int n = 0; n < w_line_seg.Length; n++)
                        {
                            w_line[index] = new Vector3(w_line_seg[n].x + i * d_currentGridCellSize.x * 2, w_line_seg[n].y);
                            index++;
                        }
                    }

                    Handles.DrawPolyLine(w_line);
                }
            }

            Handles.EndGUI();
        }
        public override void Zoom()
        {
            if (_zoomScroll.y != 250)
            {
                float zoom = (_zoomScroll.y - 250) / 1000;
                float prewZoomAmount = _zoomAmount;

                //apply zoom
                _zoomAmount -= zoom * _zoomAmount;
                _zoomAmount = Mathf.Clamp(_zoomAmount, .1f, 6f);

                //reset scroll
                _zoomScroll.y = 250;

                //change zoom origin
                var newGridSize = new Vector2(
                    Mathf.RoundToInt(_gridCellSize.x * _zoomAmount * (_flatTop ? 1f : 0.8659766f)),
                    Mathf.RoundToInt(_gridCellSize.y * _zoomAmount * (_flatTop ? 0.8659766f : 1f))
                    ) * (_flatTop ? new Vector2(.75f, 1) : new Vector2(1, .75f));

                Vector2 dist = Event.current.mousePosition - d_currentGridOrigin;
                Vector2 newDist = dist / d_currentGridCellSize * newGridSize;
                _gridOffset += dist - newDist;
            }
        }
        public override void DrawRuler()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.LowerCenter;

            //draw bottom row
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                GUI.Label(new Rect(d_currentGridStart.x + x * d_currentGridCellSize.x + (_flatTop ? d_currentGridCellSize.x / 6 : 0), window.position.height - 50, d_currentGridCellSize.x, 50), $"{d_currentGridOffset.x + x}", centeredTextStyle);
            }

            //change style
            centeredTextStyle.alignment = TextAnchor.MiddleLeft;

            //draw left row
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                GUI.Label(new Rect(0, d_currentGridStart.y + y * d_currentGridCellSize.y + (!_flatTop ? d_currentGridCellSize.y / 6 : 0), 50, d_currentGridCellSize.y), $"{d_currentGridOffset.y + y}", centeredTextStyle);
            }
        }

        public override Rect GetGridPos(Vector2Int tile)
        {
            Vector2 pos = d_currentGridOrigin + tile * d_currentGridCellSize;

            if (!_flatTop && tile.y % 2 == 0) pos += new Vector2(d_currentGridCellSize.x / 2, 0);
            if (_flatTop && tile.x % 2 == 0) pos += new Vector2(0, d_currentGridCellSize.y / 2);

            return new Rect(pos, d_hexScaledCurrentGridCellSize);
        }
        public override void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Texture2D tex, bool drawAll = true) => DrawGridTileOutline(tile, size, tex.GetPixel(0, 0), drawAll);
        public override void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Color col, bool drawAll = true)
        {
            Handles.BeginGUI();
            Handles.color = col;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    //draw bottom left of the left side if y is odd
                    //draw top right of the right side if y is odd

                    bool L = x == 0; //left edge
                    bool D = y == 0; //down edge
                    bool R = x == (size.x - 1); //right edge
                    bool U = y == (size.y - 1); //upper edge

                    DrawIndividualEdge(tile + new Vector2Int(x, y), U, D, L, R);
                }
            }

            Handles.EndGUI();
        }
        public void DrawIndividualEdge(Vector2Int pos, bool U, bool D, bool L, bool R)
        {
            var rect = GetGridPos(pos);

            if (!_flatTop)
            {
                //get if odd
                bool O = Mathf.Abs(pos.y) % 2 != 0;

                if (D) Handles.DrawPolyLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .25f), new Vector2(rect.x + rect.width / 2, rect.y), new Vector2(rect.x, rect.y + rect.height * .25f));
                if (U) Handles.DrawPolyLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .75f), new Vector2(rect.x + rect.width / 2, rect.y + rect.height), new Vector2(rect.x, rect.y + rect.height * .75f));
                if (L) Handles.DrawLine(new Vector2(rect.x, rect.y + rect.height * .25f), new Vector2(rect.x, rect.y + rect.height * .75f));
                if (R) Handles.DrawLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .25f), new Vector2(rect.x + rect.width, rect.y + rect.height * .75f));

                if (L && O && !U) Handles.DrawLine(new Vector2(rect.x, rect.y + rect.height * .75f), new Vector2(rect.x + rect.width * .5f, rect.y + rect.height));
                if (L && !O && !U) Handles.DrawLine(new Vector2(rect.x - rect.width * .5f, rect.y + rect.height), new Vector2(rect.x, rect.y + rect.height * .75f));
                if (R && !O && !U) Handles.DrawLine(new Vector2(rect.x + rect.width * .5f, rect.y + rect.height), new Vector2(rect.x + rect.width, rect.y + rect.height * .75f));
                if (R && !O && !D) Handles.DrawLine(new Vector2(rect.x + rect.width * .5f, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height * .25f));
            }
            else
            {
                //get if odd
                bool O = Mathf.Abs(pos.x) % 2 != 0;

                if (D) Handles.DrawLine(new Vector2(rect.x + rect.width * .25f, rect.y), new Vector2(rect.x + rect.width * .75f, rect.y));
                if (U) Handles.DrawLine(new Vector2(rect.x + rect.width * .25f, rect.y + rect.height), new Vector2(rect.x + rect.width * .75f, rect.y + rect.height));
                if (L) Handles.DrawPolyLine(new Vector2(rect.x + rect.width * .25f, rect.y), new Vector2(rect.x, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .25f, rect.y + rect.height));
                if (R) Handles.DrawPolyLine(new Vector2(rect.x + rect.width * .75f, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .75f, rect.y + rect.height));

                if (D && O && !R) Handles.DrawLine(new Vector2(rect.x + rect.width * .75f, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height * .5f));
                if (U && !O && !L) Handles.DrawLine(new Vector2(rect.x, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .25f, rect.y + rect.height));
                if (D && O && !L) Handles.DrawLine(new Vector2(rect.x, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .25f, rect.y));
                if (U && !O && !R) Handles.DrawLine(new Vector2(rect.x + rect.width * .75f, rect.y + rect.height), new Vector2(rect.x + rect.width, rect.y + rect.height * .5f));
            }
        }

        public void DrawIndividualTileOutline(Vector2Int tile, Color col, bool drawAll = false)
        {
            Handles.BeginGUI();
            Handles.color = col;

            var rect = GetGridPos(tile);

            //if we only need to draw a single tile, not connect them
            if (drawAll)
            {
                if (!_flatTop)
                {
                    Handles.DrawPolyLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .25f), new Vector2(rect.x + rect.width / 2, rect.y), new Vector2(rect.x, rect.y + rect.height * .25f));
                    Handles.DrawPolyLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .75f), new Vector2(rect.x + rect.width / 2, rect.y + rect.height), new Vector2(rect.x, rect.y + rect.height * .75f));
                    Handles.DrawLine(new Vector2(rect.x, rect.y + rect.height * .25f), new Vector2(rect.x, rect.y + rect.height * .75f));
                    Handles.DrawLine(new Vector2(rect.x + rect.width, rect.y + rect.height * .25f), new Vector2(rect.x + rect.width, rect.y + rect.height * .75f));
                }
                else
                {
                    Handles.DrawLine(new Vector2(rect.x + rect.width * .25f, rect.y), new Vector2(rect.x + rect.width * .75f, rect.y));
                    Handles.DrawLine(new Vector2(rect.x + rect.width * .25f, rect.y + rect.height), new Vector2(rect.x + rect.width * .75f, rect.y + rect.height));
                    Handles.DrawPolyLine(new Vector2(rect.x + rect.width * .25f, rect.y), new Vector2(rect.x, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .25f, rect.y + rect.height));
                    Handles.DrawPolyLine(new Vector2(rect.x + rect.width * .75f, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height * .5f), new Vector2(rect.x + rect.width * .75f, rect.y + rect.height));
                }
            }

            Handles.EndGUI();
        }

        public override void DrawPresetBlocksOverlay(List<BetterRuleTileContainer.PresetBlock> blocks)
        {
            Handles.BeginGUI();
            Handles.color = settings._presetBlockColor;

            foreach (var block in blocks)
            {
                foreach (var item in block.CombinedEdges)
                {
                    DrawIndividualEdge(item.Pos, item.EdgeUp, item.EdgeDown, item.EdgeLeft, item.EdgeRight);
                }
            }

            Handles.EndGUI();
        }

        public override Vector2Int GetTilePos(Vector2 position)
        {
            var p = (position - d_currentGridOrigin - new Vector2(_flatTop ? d_hexScaledCurrentGridCellSize.x / 8f : 0, _flatTop ? 0 : d_hexScaledCurrentGridCellSize.y / 8f)) / d_currentGridCellSize;
            Vector2 offset = Vector2.zero;

            if (!_flatTop && Mathf.FloorToInt(p.y) % 2 == 0) offset.x = .5f; 
            if (_flatTop && Mathf.FloorToInt(p.x) % 2 == 0) offset.y = .5f; 

            return Vector2Int.FloorToInt(p - offset);
        }
        public override Rect DrawInteractiveMiniGrid(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //reset if deleted all neighbors, this is to prevent errors
            if (cell.NeighborPositions.Count <= 0) ResetCellNeighbors(cell);

            //calculate values
            Vector2Int size = new Vector2Int(18, 18);
            Vector2Int min = new Vector2Int(cell.NeighborPositions.Min(t => t.x) - 1, cell.NeighborPositions.Min(t => t.y) - 1);
            Vector2Int max = new Vector2Int(cell.NeighborPositions.Max(t => t.x) + 1, cell.NeighborPositions.Max(t => t.y) + 1);
            Vector2Int gridSize = new Vector2Int(max.x - min.x, max.y - min.y);
            Vector2 start = new Vector2(rect.x + (rect.width - (gridSize.x + 1) * size.x) / 2f, rect.y + 26) + new Vector2(_flatTop ? size.x / 2 : size.x / 4, 0);
            Rect allRect = new Rect(rect.x, rect.y, rect.width, size.y * gridSize.y + 75 + (_flatTop ? size.y / 2 : 0));

            //draw background, title and buttons
            DrawMiniGridBackground(allRect, cell);

            //display grid
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2 index = new Vector2(x - min.x, y - min.y);
                    Rect cellRect = new Rect((start + index * size - Vector2.one) + new Vector2(!_flatTop && (y + cell.Position.y) % 2 == 0 ? 0 : -size.x / 2, _flatTop && (x + cell.Position.x) % 2 == 0 ? size.y / 2 : 0), size + new Vector2Int(2, 2));

                    //center
                    if (x == 0 && y == 0)
                    {
                        GUIContent buttonIcon = new GUIContent("");
                        switch (cell.Transform)
                        {
                            case RuleTile.TilingRuleOutput.Transform.Fixed: buttonIcon = new GUIContent(t_Fixed, "Fixed"); break;
                            case RuleTile.TilingRuleOutput.Transform.Rotated: buttonIcon = new GUIContent(t_Rotated, "Rotated"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorX: buttonIcon = new GUIContent(t_MirrorX, "MirrorX"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorY: buttonIcon = new GUIContent(t_MirrorY, "MirrorY"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorXY: buttonIcon = new GUIContent(t_MirrorXY, "MirrorXY"); break;
                        }

                        if (GUI.Button(cellRect, buttonIcon, _miniGridButtonStyle))
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

                    //has rule
                    if (cell.NeighborPositions.Contains(new Vector3Int(x, y, 0)))
                    {
                        if (GUI.Button(cellRect, t_MiniButtonCheckmark, _miniGridButtonStyle)) cell.NeighborPositions.Remove(new Vector3Int(x, y, 0));
                        continue;
                    }
                    //has no rule
                    else
                    {
                        if (GUI.Button(cellRect, "", _miniGridButtonStyle)) cell.NeighborPositions.Add(new Vector3Int(x, y, 0));
                        continue;
                    }
                }
            }

            //return size of the window
            return new Rect(allRect.x, allRect.y, allRect.width, allRect.height - size.y);
        }

        public override void DrawPreviewSprites(List<BetterRuleTileContainer.GridCell> item, Vector2Int moveBy)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                if (item[i].Sprite == null) continue;



                Vector2Int offset = Vector2Int.zero;

                //get previous pos
                Vector2Int currentPos = item[i].Position;
                Vector2Int prevPos = currentPos - moveBy;

                //shift cells
                if (!_flatTop && prevPos.y % 2 == 0 && currentPos.y % 2 == 1) offset = Vector2Int.right;
                if (_flatTop && prevPos.x % 2 == 0 && currentPos.x % 2 == 1) offset = Vector2Int.up;



                //add sprite to cache if not in it yet
                if (!_spriteTextureCache.ContainsKey(item[i].Sprite) || _spriteTextureCache[item[i].Sprite] == null) CacheSprite(item[i].Sprite);

                GUI.DrawTexture(GetOffsetGridPos(item[i].Position + offset), _spriteTextureCache[item[i].Sprite]);
            }
        }
        public override void DrawPreviewTiles(List<BetterRuleTileContainer.GridCell> item, Vector2Int moveBy)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                if (!window._file.DoesTileExist(item[i].TileID)) continue;


                Vector2Int offset = Vector2Int.zero;

                //get previous pos
                Vector2Int currentPos = item[i].Position;
                Vector2Int prevPos = currentPos - moveBy;

                //shift cells
                if (!_flatTop && prevPos.y % 2 == 0 && currentPos.y % 2 == 1) offset = Vector2Int.right;
                if (_flatTop && prevPos.x % 2 == 0 && currentPos.x % 2 == 1) offset = Vector2Int.up;


                if (item[i].TileID > 0) GUI.DrawTexture(GetOffsetGridPos(item[i].Position + offset), window._file.GetTileTex(item[i].TileID));
                else
                {
                    DrawDefaultTiles(item, i, GetGridPos(item[i].Position + offset));
                }
            }
        }

        public override void DrawLockedOverlay(List<BetterRuleTileContainer.GridCell> item)
        {
            foreach (var cell in item.FindAll(t => t.Locked))
            {
                if (settings._renderLockedOverlay) GUI.DrawTexture(GetGridPos(cell.Position), t_lockedTexture);
                if (settings._renderLockedOutline) DrawIndividualTileOutline(cell.Position, settings._lockedOutlineColor, true);
            }
        }
    }
}
#endif
