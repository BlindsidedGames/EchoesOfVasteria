using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VinTools.BetterRuleTiles.Runtime.Utilities;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        [System.Serializable]
        public class PresetBlock
        {
            public PresetBlock(Vector2Int corner1, Vector2Int corner2)
            {
                var bounds = new Rect(
                    new Vector2Int(Mathf.Min(corner1.x, corner2.x), Mathf.Min(corner1.y, corner2.y)),
                    new Vector2Int(Mathf.Max(corner1.x, corner2.x) - Mathf.Min(corner1.x, corner2.x), Mathf.Max(corner1.y, corner2.y) - Mathf.Min(corner1.y, corner2.y))
                );

                Bounds.Add(bounds);
                RecalculateEdges();
            }
            public PresetBlock(Rect rect)
            {
                Bounds.Add(rect);
                RecalculateEdges();
            }
            public PresetBlock(List<Rect> rects)
            {
                Bounds.AddRange(rects);
                RecalculateEdges();
            }

            public List<Rect> Bounds = new List<Rect>();
            public List<TileEdge> Edges = new List<TileEdge>();
            public List<BlockEdgeSpan> EdgeSpans = new List<BlockEdgeSpan>();
            public List<CombinedTileEdge> CombinedEdges = new List<CombinedTileEdge>();

            public RuleTile.TilingRuleOutput.Transform BlockTransform = RuleTile.TilingRuleOutput.Transform.Fixed;

            public void AddRect(Rect rect)
            {
                Bounds.Add(rect);
                RecalculateEdges();
            }
            public void AddRects(List<Rect> rects)
            {
                Bounds.AddRange(rects);
                RecalculateEdges();
            }
            public bool InBounds(Vector2 tile) => Bounds.Exists(b => b.Contains(tile));
            public bool Overlaps(Rect rect) => Bounds.Exists(b => b.Overlaps(rect));
            public bool Outside(Rect rect) => Bounds.Exists(b => !b.Overlaps(rect));

            /// <summary>
            /// Removes the area specified from this preset block, and moves it to a new one
            /// </summary>
            /// <param name="other">Area to separate</param>
            /// <returns>The preset block that was split from the original</returns>
            public List<Rect> SplitOverlappingArea(Rect other)
            {
                // list for the separated areas
                var separatedAreas = new List<Rect>();
                
                // loop over a copy so we can modify the original
                foreach (var rect in Bounds.ToList())
                {
                    // if it doesn't overlap, continue
                    if (!rect.GetOverlappingArea(other, out var overlappingArea)) continue;
                    
                    // add overlapping area to new block
                    separatedAreas.Add(overlappingArea);
                    // remove old rect from bounds
                    Bounds.Remove(rect);
                    // split block and add it to the bounds
                    Bounds.AddRange(rect.SubtractOverlappingArea(overlappingArea));
                }

                RecalculateEdges();
                // return new block
                return separatedAreas;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="separatedRects"></param>
            /// <returns>True if the Block contained loose parts</returns>
            public bool SeparateLooseRects(out List<Rect> separatedRects)
            {
                // treat all Rects as separated by default (this only matters if the function returns true)
                separatedRects = Bounds.ToList();
                
                // only run when there are at least two rects
                if (Bounds.Count <= 1) return false;

                // move first rect to the connected rects array
                var connectedRects = new List<Rect> {separatedRects[0]};
                separatedRects.RemoveAt(0);
                
                // find all rects connected to rect 1
                int i = 0;
                while (connectedRects.Count > i)
                {
                    var temp = separatedRects.FindAll(r => r.Touches(connectedRects[i]));
                    connectedRects.AddRange(temp);
                    separatedRects.RemoveAll(temp.Contains);
                    
                    i++;
                }
                
                // return false if there is no separated section
                if (separatedRects.Count == 0) return false;
                
                // keep connected rects and return true
                Bounds = connectedRects;
                RecalculateEdges();
                return true;
            }
            public void MoveArea(Vector2Int by)
            {
                for (int i = 0; i < Bounds.Count; i++)
                    Bounds[i] = new Rect(Bounds[i].position + by, Bounds[i].size);
                
                RecalculateEdges();
            }
            
            public List<Vector3Int> GetPositions()
            {
                var positions = new List<Vector3Int>();

                //check all bounds
                foreach (var bounds in Bounds)
                {
                    //loop through all values of the bounds
                    for (float y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        for (float x = bounds.xMin; x < bounds.xMax; x++)
                        {
                            //add the position if it's not already in the list
                            Vector3Int pos = new Vector3Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y), 0);
                            if (!positions.Contains(pos)) positions.Add(pos);
                        }
                    }
                }

                //return the sorted list
                return positions.OrderBy(t => t.y * -10000 + t.x).ToList();
            }
            public List<Vector3Int> GetPositions(Vector2Int relativeTo) => GetPositions(new Vector3Int(relativeTo.x, relativeTo.y, 0));
            public List<Vector3Int> GetPositions(Vector3Int relativeTo)
            {
                var positions = new List<Vector3Int>();

                //add the offset value
                foreach (var item in GetPositions()) positions.Add(item - relativeTo);

                //return
                return positions;
            }

            public void RecalculateEdges()
            {
                //get all the tiles
                List<Vector3Int> poses = GetPositions();

                //reset arrays
                Edges = new List<TileEdge>();
                EdgeSpans = new List<BlockEdgeSpan>();
                CombinedEdges = new List<CombinedTileEdge>();

                //add all the poses
                foreach (var pos in poses)
                {
                    if (!poses.Contains(pos + Vector3Int.up)) Edges.Add(new TileEdge(new Vector2Int(pos.x, pos.y), Edge.Up));
                    if (!poses.Contains(pos + Vector3Int.down)) Edges.Add(new TileEdge(new Vector2Int(pos.x, pos.y), Edge.Down));
                    if (!poses.Contains(pos + Vector3Int.left)) Edges.Add(new TileEdge(new Vector2Int(pos.x, pos.y), Edge.Left));
                    if (!poses.Contains(pos + Vector3Int.right)) Edges.Add(new TileEdge(new Vector2Int(pos.x, pos.y), Edge.Right));
                }

                //Combine tile edges
                foreach (var item in Edges)
                {
                    var tile = CombinedEdges.Find(e => e.Pos == item.Pos);

                    if (tile == null) CombinedEdges.Add(new CombinedTileEdge(item.Pos, item.Side));
                    else tile.AddEdge(item.Side);
                }

                //reduce individual edges to edge spans
                //separate edges into two temporary lists
                List<TileEdge> horizontalEdges = new List<TileEdge>(Edges.Where(e => e.Side == Edge.Up || e.Side == Edge.Down));
                List<TileEdge> verticalEdges = new List<TileEdge>(Edges.Where(e => e.Side == Edge.Left || e.Side == Edge.Right));

                //horizontal edges
                while (horizontalEdges.Count > 0)
                {
                    var originalEdge = horizontalEdges.First();
                    horizontalEdges.Remove(originalEdge);

                    int leftSpan = 0;
                    int rightSpan = 0;
                    bool leftCheck = true;
                    bool rightCheck = true;

                    while (leftCheck)
                    {
                        var found = horizontalEdges.Find(e => e.Pos == originalEdge.Pos + Vector2Int.left * (leftSpan + 1) && e.Side == originalEdge.Side);

                        if (found != null)
                        {
                            leftSpan++;
                            horizontalEdges.Remove(found);
                        }
                        else leftCheck = false;
                    }

                    while (rightCheck)
                    {
                        var found = horizontalEdges.Find(e => e.Pos == originalEdge.Pos + Vector2Int.right * (rightSpan + 1) && e.Side == originalEdge.Side);

                        if (found != null)
                        {
                            rightSpan++;
                            horizontalEdges.Remove(found);
                        }
                        else rightCheck = false;
                    }

                    EdgeSpans.Add(new BlockEdgeSpan(originalEdge.Pos + Vector2Int.left * leftSpan, originalEdge.Pos + Vector2Int.right * rightSpan, originalEdge.Side));
                }

                //vertical edges
                while (verticalEdges.Count > 0)
                {
                    var originalEdge = verticalEdges.First();
                    verticalEdges.Remove(originalEdge);

                    int upSpan = 0;
                    int downSpan = 0;
                    bool upCheck = true;
                    bool downCheck = true;

                    while (upCheck)
                    {
                        var found = verticalEdges.Find(e => e.Pos == originalEdge.Pos + Vector2Int.up * (upSpan + 1) && e.Side == originalEdge.Side);

                        if (found != null)
                        {
                            upSpan++;
                            verticalEdges.Remove(found);
                        }
                        else upCheck = false;
                    }

                    while (downCheck)
                    {
                        var found = verticalEdges.Find(e => e.Pos == originalEdge.Pos + Vector2Int.down * (downSpan + 1) && e.Side == originalEdge.Side);

                        if (found != null)
                        {
                            downSpan++;
                            verticalEdges.Remove(found);
                        }
                        else downCheck = false;
                    }

                    EdgeSpans.Add(new BlockEdgeSpan(originalEdge.Pos + Vector2Int.up * upSpan, originalEdge.Pos + Vector2Int.down * downSpan, originalEdge.Side));
                }
            }

            [System.Serializable]
            public class BlockEdgeSpan
            {
                public Edge Side;
                public Vector2Int Pos1;
                public Vector2Int Pos2;

                public BlockEdgeSpan(Vector2Int pos1, Vector2Int pos2, Edge side)
                {
                    Side = side;
                    Pos1 = pos1;
                    Pos2 = pos2;
                }
            }
            [System.Serializable]
            public class TileEdge
            {
                public Edge Side;
                public Vector2Int Pos;

                public TileEdge(Vector2Int pos, Edge side)
                {
                    Side = side;
                    Pos = pos;
                }
            }
            [System.Serializable]
            public class CombinedTileEdge
            {
                public Vector2Int Pos;
                public bool EdgeUp = false;
                public bool EdgeDown = false;
                public bool EdgeRight = false;
                public bool EdgeLeft = false;

                public CombinedTileEdge(Vector2Int pos, Edge side)
                {
                    Pos = pos;
                    AddEdge(side);
                }
                public void AddEdge(Edge side)
                {
                    switch (side)
                    {
                        case Edge.Up:
                            EdgeUp = true;
                            break;
                        case Edge.Down:
                            EdgeDown = true;
                            break;
                        case Edge.Left:
                            EdgeLeft = true;
                            break;
                        case Edge.Right:
                            EdgeRight = true;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}