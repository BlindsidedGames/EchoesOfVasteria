using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;
using VinTools.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace VinTools.BetterRuleTiles
{
    [CreateAssetMenu(menuName = "2D/Tiles/Better Rule Tile Container", fileName = "Better Rule Tile")]
    public class BetterRuleTileContainer : ScriptableObject
    {
#if UNITY_EDITOR
        public static string CreatedAssetPath = "";

        private void OnEnable()
        {
            //created asset path is set every time before a new asset gets created, if the new asset path matches this asset path it means this container was just created
            if (CreatedAssetPath == AssetDatabase.GetAssetPath(this))
            {
                settings._useUniversalSpriteSettings = true;
            }

            CreatedAssetPath = "";
        }
#endif

        #region Variables
        public EditorSettings settings = new EditorSettings();
        public List<UnityEngine.Object> _imageObjects = new List<Object>();
        [Space]
        public List<BetterRuleTile> _tileObjects = new List<BetterRuleTile>();
        public List<BetterHexagonalRuleTile> _hexTileObjects = new List<BetterHexagonalRuleTile>();
        public List<GridCell> _grid = new List<GridCell>();
        public List<TileData> _tiles = new List<TileData>();
        public List<UniversalSpriteData> _overrideSprites = new List<UniversalSpriteData>();
        public List<PresetBlock> _presetBlocks = new List<PresetBlock>();

        public List<GridCell> Grid { get => _grid; }
        public List<TileData> Tiles { get => _tiles; }

        #region Classes
        [System.Serializable]
        public class TileData
        {
            public int UniqueID;

            public string Name;
            public Texture2D Texture;

            public Color Color;
            public TileShape TextureShape;
            public bool MirrorTexX;
            public bool MirrorTexY;

            public Sprite DefaultSprite;
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public GameObject DefaultGameObject;

            public bool uniqueTile = true;
            public int variationOf = -1;

            public List<int> variations = new List<int>();
            public List<CustomTileProperty> customProperties = new List<CustomTileProperty>();

            public TileData(string name, Color color, int id, GridShape shape)
            {
                Name = name;
                Color = color;
                UniqueID = id;

                switch (shape)
                {
                    case GridShape.Isometric: TextureShape = TileShape.Isometric; break;
                    case GridShape.HexagonalPointedTop: TextureShape = TileShape.HexagonPointedTop; break;
                    case GridShape.HexagonalFlatTop: TextureShape = TileShape.HexagonFlatTop; break;
                    default: TextureShape = TileShape.Square; break;
                }
            }
        }

        [System.Serializable]
        public class GridCell
        {
            public Vector2Int Position;
            public Sprite Sprite;
            public int TileID;
            public bool UseDefaultSettings = true;
            public bool AreNeighborPositionsModified;
            public bool AreOutputSpritesModified;
            public bool Locked = false;

            public bool _overrideUniversalSpriteSettings = false;
            public GameObject GameObject;
            public Sprite[] Sprites = new Sprite[0];
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public ExtendedOutputSprite OutputSprite = ExtendedOutputSprite.Single;
            public RuleTile.TilingRuleOutput.Transform Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
            public bool IncludeSpriteInOutput = false;

            public float NoiseScale = .5f;
            public RuleTile.TilingRuleOutput.Transform RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed;

            public float MinAnimationSpeed = 1f;
            public float MaxAnimationSpeed = 1f;
            public Vector2Int PatternSize = Vector2Int.one;

            public List<Vector3Int> NeighborPositions;

            public GridCell(Vector2Int pos, GridShape shape)
            {
                Position = pos;
                NeighborPositions = DefaultNeighborPositions(shape, pos);
            }
            public GridCell() { }

            public static GridCell Clone(GridCell copy)
            {
                //create a copy of the neighbor positions
                var positionsCopy = new List<Vector3Int>();
                foreach (var item in copy.NeighborPositions) positionsCopy.Add(new Vector3Int(item.x ,item.y, item.z));

                return new GridCell
                {
                    Position = copy.Position,
                    Sprite = copy.Sprite,
                    TileID = copy.TileID,
                    UseDefaultSettings = copy.UseDefaultSettings,
                    AreNeighborPositionsModified = copy.AreNeighborPositionsModified,
                    Locked = copy.Locked,

                    Sprites = copy.Sprites,
                    OutputSprite = copy.OutputSprite,
                    Transform = copy.Transform,
                    GameObject = copy.GameObject,
                    ColliderType = copy.ColliderType,
                    IncludeSpriteInOutput = copy.IncludeSpriteInOutput,

                    NoiseScale = copy.NoiseScale,
                    RandomTransform = copy.RandomTransform,

                    MinAnimationSpeed = copy.MinAnimationSpeed,
                    MaxAnimationSpeed = copy.MaxAnimationSpeed,

                    NeighborPositions = positionsCopy,
                    PatternSize = copy.PatternSize
                };
            }

            public bool CheckModifiedNeighborPositions()
            {
                if (Transform != RuleTile.TilingRuleOutput.Transform.Fixed) return true;

                //TODO Maybe count for hexagonal tiles
                if (NeighborPositions.Count != 8 && NeighborPositions.Count != 6) return true;
                if (NeighborPositions.Min(t => t.x) < -1) return true;
                if (NeighborPositions.Max(t => t.x) < -1) return true;
                if (NeighborPositions.Min(t => t.y) > 1) return true;
                if (NeighborPositions.Max(t => t.y) > 1) return true;

                return false;
            }

            public bool CheckModifiedOutput()
            {
                if (!UseDefaultSettings || ColliderType != Tile.ColliderType.Sprite) return true;
                if (OutputSprite != ExtendedOutputSprite.Single) return true;
                if (Sprites.Length > 1) return true;

                return false;
            }
        }

        [System.Serializable]
        public class EditorSettings
        {
            public GridShape _gridShape = GridShape.Square;

            public float _zoomAmount = 1;
            public Vector2Int _gridCellSize = new Vector2Int(32, 32);
            public Vector2 _tileRenderOffset = Vector2.zero;
            public Vector2 _gridOffset = Vector2.zero;

            public bool _lockWindows = true;
            public bool _hideSprites = false;
            public bool _showRuler = true;
            public bool _showModified = false;
            public bool _renderSmallGrid = false;
            public float _zoomTreshold = .35f;
            public int _drawerSize = 6;
            public bool _displaySpriteDrawer = true;
            public bool _displayUniversalSpriteSettings = false;
            public bool _saveSpriteDrawer = true;

            public int _drawerHeight = 50;
            public int _drawerCollumns = 2;
            public int _expandedDrawerCollumns = 10;

            public bool _renderLockedOverlay = false;
            public bool _renderLockedOutline = true;
            public Color _lockedOutlineColor = Color.red;

            //public bool _generatePalette = true;
            //public bool _addMissingRules = true;
            public bool _collapseSimilarRules = false;
            public bool _useUniversalSpriteSettings = false;
            public bool _treatSimilarTilesAsSame = false;

            public Color _presetBlockColor = new Color(255 / 255f, 147 / 255f, 15 / 255f);

            public void SetDisplaySpriteDrawer(bool set)
            {
                if (set && _displayUniversalSpriteSettings) _displayUniversalSpriteSettings = false;
                _displaySpriteDrawer = set;
            }
            public void SetDisplayUniversalSpriteSettings(bool set)
            {
                if (set && _displaySpriteDrawer) _displaySpriteDrawer = false;
                _displayUniversalSpriteSettings = set;
            }
        }

        public enum TileShape
        {
            Square = 0,
            [InspectorName("1x1 Slope")] Slope_1x1 = 10,
            [InspectorName("2x1 Slope Bottom")] Slope_2x1_Bottom = 11,
            [InspectorName("2x1 Slope Top")] Slope_2x1_Top = 12,
            Diamond = 20,
            Isometric = 22,
            [InspectorName("Hexagon Pointed Top")] HexagonPointedTop = 25,
            [InspectorName("Hexagon Flat Top")] HexagonFlatTop = 26,
            Circle = 30,
        }
        public enum GridShape
        {
            Square = 0,
            Isometric = 1,
            [InspectorName("Hexagonal - Pointed-Top")] HexagonalPointedTop = 2,
            [InspectorName("Hexagonal - Flat-Top")] HexagonalFlatTop = 3,
        }
        public enum Edge
        {
            Up,
            Down,
            Left,
            Right
        }

        [System.Serializable]
        public class UniversalSpriteData
        {
            public Sprite BaseSprite;

            public GameObject GameObject;
            public Sprite[] Sprites = new Sprite[0];
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public ExtendedOutputSprite OutputSprite = ExtendedOutputSprite.Single;
            //public RuleTile.TilingRuleOutput.Transform Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
            //public bool IncludeSpriteInOutput = false;

            public float NoiseScale = .5f;
            public RuleTile.TilingRuleOutput.Transform RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed;

            public float MinAnimationSpeed = 1f;
            public float MaxAnimationSpeed = 1f;
            public Vector2Int PatternSize = Vector2Int.one;

            public UniversalSpriteData(Sprite sprite)
            {
                BaseSprite = sprite;
                Sprites = new Sprite[] { sprite };
            }
        }

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
            public bool InBounds(Vector2 tile) => Bounds.Exists(b => b.Contains(tile));
            public bool Overlaps(Rect rect) => Bounds.Exists(b => b.Overlaps(rect));
            public bool Outside(Rect rect) => Bounds.Exists(b => !b.Overlaps(rect));

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

        public List<Vector3Int> DefaultNeighborPositions(Vector2Int pos) => DefaultNeighborPositions(settings._gridShape, pos);
        public static List<Vector3Int> DefaultNeighborPositions(GridShape shape, Vector2Int pos)
        {
            switch (shape)
            {
                case GridShape.HexagonalFlatTop:
                    if (pos.x % 2 == 0)
                    {
                        return new List<Vector3Int>()
                        {
                            new Vector3Int(-1, 1, 0),
                            new Vector3Int(0, 1, 0),
                            new Vector3Int(-1, 0, 0),
                            new Vector3Int(1, 0, 0),
                            new Vector3Int(0, -1, 0),
                            new Vector3Int(1, 1, 0),
                        };
                    }
                    else
                    {
                        return new List<Vector3Int>()
                        {
                            new Vector3Int(0, 1, 0),
                            new Vector3Int(-1, 0, 0),
                            new Vector3Int(1, 0, 0),
                            new Vector3Int(-1, -1, 0),
                            new Vector3Int(0, -1, 0),
                            new Vector3Int(1, -1, 0),
                        };
                    }
                case GridShape.HexagonalPointedTop:
                    if (pos.y % 2 == 0)
                    {
                        return new List<Vector3Int>()
                        {
                            new Vector3Int(0, 1, 0),
                            new Vector3Int(-1, 0, 0),
                            new Vector3Int(1, 0, 0),
                            new Vector3Int(0, -1, 0),
                            new Vector3Int(1, -1, 0),
                            new Vector3Int(1, 1, 0),
                        };
                    }
                    else
                    {
                        return new List<Vector3Int>()
                        {
                            new Vector3Int(-1, 1, 0),
                            new Vector3Int(0, 1, 0),
                            new Vector3Int(-1, 0, 0),
                            new Vector3Int(1, 0, 0),
                            new Vector3Int(-1, -1, 0),
                            new Vector3Int(0, -1, 0),
                        };
                    }
                default:
                    return new List<Vector3Int>()
                    {
                        new Vector3Int(-1, 1, 0),
                        new Vector3Int(0, 1, 0),
                        new Vector3Int(1, 1, 0),
                        new Vector3Int(-1, 0, 0),
                        new Vector3Int(1, 0, 0),
                        new Vector3Int(-1, -1, 0),
                        new Vector3Int(0, -1, 0),
                        new Vector3Int(1, -1, 0),
                    };
            }
        }
        public Vector2Int EditorToUnityCoord(Vector2Int neighborCoord, Vector2Int tileCoord)
        {
            switch (settings._gridShape)
            {
                case GridShape.Isometric: return VinTools.Utilities.MathUtils.TransformPoint(neighborCoord, new Vector2Int(0, -1), new Vector2Int(-1, 0));
                //case GridShape.HexagonalFlatTop: return Functions.TransformPoint(neighborCoord, new Vector2Int(0, -1), new Vector2Int(1, 0));
                default: return VinTools.Utilities.MathUtils.TransformPoint(neighborCoord, new Vector2Int(1, 0), new Vector2Int(0, -1));
            }
        }
        public List<Vector3Int> DisplaceRules(List<Vector3Int> original, Vector2Int tileCoord)
        {
            Vector3Int[] _new = original.ToArray();

            for (int i = 0; i < _new.Length; i++)
            {
                switch (settings._gridShape)
                {
                    case GridShape.HexagonalPointedTop:
                        if (tileCoord.y % 2 == 0 && original[i].y % 2 != 0) _new[i] = new Vector3Int(original[i].x - 1, original[i].y, original[i].z);
                        break;
                    case GridShape.HexagonalFlatTop:
                        if (tileCoord.x % 2 != 0 && original[i].x % 2 != 0) _new[i] = new Vector3Int(original[i].x, original[i].y - 1, original[i].z);
                        _new[i] = (Vector3Int)Vector2Int.RoundToInt(VinTools.Utilities.MathUtils.TransformPoint((Vector3)_new[i], new Vector2(0, 1), new Vector2Int(1, 0)));
                        break;
                    default:
                        _new[i] = original[i];
                        break;
                }
            }

            //default
            return _new.ToList();
        }
        #endregion
        #endregion

#if UNITY_EDITOR
        #region Set methods
        public void SetSprite(Vector2Int p, Sprite s)
        {
            RecordObject($"Added sprite ({this.name})");
            bool n = false;

            //get or create cell
            GridCell cell = _grid.Find(t => t.Position == p);
            if (cell == null)
            {
                cell = new GridCell(p, settings._gridShape);
                n = true;
            }
            else if (cell.Locked) return;

            //set sprite
            if (cell.Sprite == s) return;
            cell.Sprite = s;

            //add cell
            if (n) _grid.Add(cell);

            SaveAsset();
        }
        public void SetTile(Vector2Int p, int id)
        {
            if (id < -10) return;

            RecordObject($"Set tile ({this.name})");
            bool n = false;

            //get or create cell
            GridCell cell = _grid.Find(t => t.Position == p);
            if (cell == null)
            {
                cell = new GridCell(p, settings._gridShape);
                n = true;
            }
            else if (cell.Locked) return;

            //set tile
            if (cell.TileID == id) return;
            cell.TileID = id;

            //add cell
            if (n) _grid.Add(cell);

            SaveAsset();
        }

        public void RemoveSprite(Vector2Int p)
        {
            RecordObject($"Deleted sprite ({this.name})");

            GridCell cell = _grid.Find(t => t.Position == p);
            if (cell != null)
            {
                if (cell.Locked) return;

                cell.Sprite = null;

                if (cell.Sprite == null && cell.TileID == 0) _grid.Remove(cell);

                SaveAsset();
            }
        }
        public void RemoveTile(Vector2Int p)
        {
            RecordObject($"Deleted tile ({this.name})");

            GridCell cell = _grid.Find(t => t.Position == p);
            if (cell != null)
            {
                if (cell.Locked) return;

                cell.TileID = 0;

                if (cell.Sprite == null && cell.TileID == 0) _grid.Remove(cell);

                SaveAsset();
            }
        }

        #region Tile drawer
        public void CreateTile()
        {
            RecordObject($"Created new tile ({this.name})");

            //get an id which is not used yet
            int tileId = 1;
            while (_tiles.Exists(t => t.UniqueID == tileId)) tileId++;

            _tiles.Add(new TileData("New Tile", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), tileId, settings._gridShape));

            _tempTex = null;

            SaveAsset();
        }

        public void ModifyTile(int index, string name, Color color, Texture2D texture, Sprite sprite, Tile.ColliderType collider, GameObject gameObject, TileShape shape, bool mirrorX, bool mirrorY, bool unique, int variation, List<int> variations, List<CustomTileProperty> customTileProperties)
        {
            RecordObject($"Modified tile ({this.name})");

            Tiles[index].Name = name;
            Tiles[index].Color = color;
            Tiles[index].Texture = texture;
            Tiles[index].DefaultSprite = sprite;
            Tiles[index].ColliderType = collider;
            Tiles[index].DefaultGameObject = gameObject;
            Tiles[index].TextureShape = shape;
            Tiles[index].MirrorTexX = mirrorX;
            Tiles[index].MirrorTexY = mirrorY;
            Tiles[index].uniqueTile = unique;
            Tiles[index].variationOf = variation;
            Tiles[index].variations = variations;
            Tiles[index].customProperties = customTileProperties;

            _tempTex = null;
            SaveAsset();
        }
        public void DeleteTile(TileData tile)
        {
            RecordObject($"Deleted tile ({this.name})");
            //Debug.Log($"Deleting tile: {tile.Name}");

            Tiles.Remove(tile);

            _tempTex = null;
            SaveAsset();
        }
        #endregion

        public void CreatePresetBlock(Rect block)
        {
            //if size is 1x1 or less
            if (block.size.x <= 1 && block.size.y <= 1)
            {
                EditorUtility.DisplayDialog("Invalid block size!", "A preset block must be larger than a 1x1 area!", "OK");
                return;
            }

            int overlappingBlocks = _presetBlocks.FindAll(b => b.Overlaps(block)).Count;

            //if overlaps two or more already existing blocks
            if (overlappingBlocks > 1)
            {
                EditorUtility.DisplayDialog("Invalid block placement!", "The selected area overlaps with two or more already existing preset blocks!", "OK");
                return;
            }

            //if only overlaps one block
            if (overlappingBlocks > 0)
            {
                //EditorUtility.DisplayDialog("Invalid block placement!", "The selected area overlaps with another preset block!", "OK");
                
                if (EditorUtility.DisplayDialog("Overlapping bounds!", "The selected area overlaps with an existing preset block, do you want to add it to that block?", "Yes", "No"))
                {
                    _presetBlocks.Find(b => b.Overlaps(block)).AddRect(block);
                }

                return;
            }

            _presetBlocks.Add(new PresetBlock(block));
        }
        public void DeletePresetBlock(Vector2 pos)
        {
            //find block of the tile
            var block = _presetBlocks.Find(b => b.InBounds(pos));

            if (block != null)
            {
                _presetBlocks.Remove(block);
            }
        }

        public void MoveArea(Rect area, Vector2Int moveBy)
        {
            RecordObject($"Moved selection ({this.name})");


            //get tiles in the area
            var tiles = _grid.Where(t => area.Contains(t.Position) && !t.Locked).ToArray();
            //remove tiles temporarily
            _grid.RemoveAll(t => area.Contains(t.Position) && !t.Locked);

            //change their position
            for (int i = 0; i < tiles.Length; i++) tiles[i].Position += moveBy;

            //shift neighbor position on hex tiles, pointed top
            if (settings._gridShape == GridShape.HexagonalPointedTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnPointedHexGrid(ref tiles[i], moveBy);
            }

            //shift neighbor position on hex tiles, flat top
            if (settings._gridShape == GridShape.HexagonalFlatTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnFlattopHexGrid(ref tiles[i], moveBy);
            }

            //put them back
            for (int i = 0; i < tiles.Length; i++)
            {
                var cell = _grid.Find(t => t.Position == tiles[i].Position);

                if (cell != null) _grid[_grid.IndexOf(cell)] = tiles[i];
                if (cell == null) _grid.Add(tiles[i]);
            }

            //save
            SaveAsset();
        }

        public void PasteGrid(List<GridCell> clipboard, Vector2Int moveBy)
        {
            RecordObject($"Pasted selection ({this.name})");

            //convert to an array we can modify and is not a reference of the original copied object
            GridCell[] tiles = new GridCell[clipboard.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = GridCell.Clone(clipboard[i]);

            //fix clipboard on hexagonal grids
            if (settings._gridShape == GridShape.HexagonalPointedTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnPointedHexGrid(ref tiles[i], moveBy);
            }
            if (settings._gridShape == GridShape.HexagonalFlatTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnFlattopHexGrid(ref tiles[i], moveBy);
            }

            //paste the tiles
            for (int i = 0; i < tiles.Length; i++)
            {
                var cell = _grid.Find(t => t.Position == tiles[i].Position);

                if (cell != null && cell.Locked) break;
                if (cell != null) _grid[_grid.IndexOf(cell)] = tiles[i];
                else _grid.Add(tiles[i]);
            }

            //save
            SaveAsset();
        }

        static void FixMoveOnPointedHexGrid(ref GridCell cell, Vector2Int moveBy, bool neighborFix = true, bool moveFix = true)
        {
            //get previous pos
            Vector2Int currentPos = cell.Position;
            Vector2Int prevPos = currentPos - moveBy;

            //if moved even amount of tiles do nothing
            if (prevPos.y % 2 == currentPos.y % 2) return;

            //every neighbor
            if (neighborFix)
            {
                for (int p = 0; p < cell.NeighborPositions.Count; p++)
                {
                    //ignore even rows
                    if (cell.NeighborPositions[p].y % 2 == 0) continue;

                    //odd to even, shift to right
                    if (prevPos.y % 2 == 1)
                    {
                        cell.NeighborPositions[p] = cell.NeighborPositions[p] + Vector3Int.right;
                    }

                    //even to odd, shift to left
                    if (prevPos.y % 2 == 0)
                    {
                        cell.NeighborPositions[p] = cell.NeighborPositions[p] + Vector3Int.left;
                    }
                }
            }

            //move if moved from even to odd
            if (moveFix && currentPos.y % 2 == 1)
            {
                cell.Position += Vector2Int.right;
            }
        }
        static void FixMoveOnFlattopHexGrid(ref GridCell cell, Vector2Int moveBy, bool neighborFix = true, bool moveFix = true)
        {
            //get previous pos
            Vector2Int currentPos = cell.Position;
            Vector2Int prevPos = currentPos - moveBy;

            //if moved even amount of tiles do nothing
            if (prevPos.x % 2 == currentPos.x % 2) return;

            //every neighbor
            if (neighborFix)
            {
                for (int p = 0; p < cell.NeighborPositions.Count; p++)
                {
                    //ignore even columns
                    if (cell.NeighborPositions[p].x % 2 == 0) continue;

                    //odd to even, shift to right
                    if (prevPos.x % 2 == 1)
                    {
                        cell.NeighborPositions[p] = cell.NeighborPositions[p] + Vector3Int.up;
                    }

                    //even to odd, shift to left
                    if (prevPos.x % 2 == 0)
                    {
                        cell.NeighborPositions[p] = cell.NeighborPositions[p] + Vector3Int.down;
                    }
                }
            }

            //move if moved from even to odd
            if (moveFix && currentPos.x % 2 == 1)
            {
                cell.Position += Vector2Int.up;
            }
        }

        public void DeleteArea(Rect area)
        {
            RecordObject($"Deleted selection ({this.name})");

            //remove
            _grid.RemoveAll(t => area.Contains(t.Position) && !t.Locked);

            //save
            SaveAsset();
        }

        public void LockArea(Rect area)
        {
            RecordObject($"Locked area ({this.name})");

            //lock
            foreach (var item in _grid.FindAll(t => area.Contains(t.Position))) item.Locked = true;

            //save
            SaveAsset();
        }
        public void UnlockArea(Rect area)
        {
            RecordObject($"Unlocked area ({this.name})");

            //unlock
            foreach (var item in _grid.FindAll(t => area.Contains(t.Position))) item.Locked = false;

            //save
            SaveAsset();
        }

        public void RecolorSelection(Rect selection, int from, int to)
        {
            RecordObject($"Replaced tiles \"{_tiles[from].Name}\" to \"{_tiles[to].Name}\" in selection ({this.name})");

            //replace
            foreach (var item in _grid.Where(t => selection.Contains(t.Position) && t.TileID == _tiles[from].UniqueID)) item.TileID = _tiles[to].UniqueID;

            //save
            SaveAsset();
        }
        public void RecolorSelection(Rect selection, string from, string to)
        {
            RecordObject($"Replaced sprites \"{from}\" to \"{to}\" in selection ({this.name})");

            //replace
            foreach (var item in _grid.Where(t => selection.Contains(t.Position) && t.Sprite != null))
            {
                string path = AssetDatabase.GetAssetPath(item.Sprite);
                var splitPath = path.Split('/');
                splitPath[splitPath.Length - 1] = TextUtils.ReplaceFirstOccurrence(splitPath[splitPath.Length - 1], from, to);

                //create altered path
                string newPath = "";
                for (int i = 0; i < splitPath.Length; i++)
                {
                    newPath += splitPath[i];
                    if (i + 1 < splitPath.Length) newPath += "/";
                }

                //replace sprite name 
                string spriteName = TextUtils.ReplaceFirstOccurrence(item.Sprite.name, from, to);

                //find 
                if (AssetDatabase.LoadMainAssetAtPath(newPath) != null)
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(newPath))
                    {
                        if (asset.name.Equals(spriteName) && asset is Sprite)
                        {
                            item.Sprite = asset as Sprite;
                            break;
                        }
                    }
                }
            }

            //save
            SaveAsset();
        }

        public bool ChangeGridShape(GridShape to)
        {
            if (settings._gridShape == to) return false;

            foreach (var item in _grid)
            {
                if (!item.AreNeighborPositionsModified) item.NeighborPositions = DefaultNeighborPositions(to, item.Position);
            }
            settings._gridShape = to;

            return true;
        }

        #endregion

        #region Get methods
        public string GetTileName(int i) => GetTileNames()[i];
        public string[] GetTileNames()
        {
            string[] names = new string[_tiles.Count];

            for (int i = 0; i < _tiles.Count; i++)
            {
                names[i] = _tiles[i].Name;
            }

            return names;
        }
        public string[] GetTileNamesSorted()
        {
            List<string> names = new List<string>();

            foreach (var item in _tiles.OrderBy(t => t.UniqueID)) names.Add(item.Name);

            return names.ToArray();
        }
        public string GetUniqueTileName(int id) => _tiles.Find(t => t.UniqueID == id).Name;
        public Color GetTileColor(int i) => GetTileColors()[i];
        public Color[] GetTileColors()
        {
            Color[] colors = new Color[_tiles.Count];

            for (int i = 0; i < _tiles.Count; i++)
            {
                colors[i] = _tiles[i].Color;
            }

            return colors;
        }


        public Texture2D[] GetTileTextures()
        {
            CheckTextures();
            return _tempTex;
        }
        public Texture2D GetTileTexture(int i)
        {
            var tex = GetTileTextures()[i];
            if (tex == null) tex = SetTileTexture(i);

            return tex;
        }

        private Texture2D[] _tempTex = null;
        public void SetTileTextures()
        {
            _tempTex = new Texture2D[_tiles.Count];

            for (int i = 0; i < _tiles.Count; i++) SetTileTexture(i);
        }
        public Texture2D SetTileTexture(int i)
        {
            //set tile texture
            if (_tiles[i].Texture != null) _tempTex[i] = _tiles[i].Texture;
            //else 
            else
            {
                switch (_tiles[i].TextureShape)
                {
                    case TileShape.Square: _tempTex[i] = TextureUtils.CreateFilledTexture(_tiles[i].Color, 64, 64); break;
                    case TileShape.Slope_1x1: _tempTex[i] = TextureUtils.CreateSlopeTexture(_tiles[i].Color, 1, 0, 64, 64); break;
                    case TileShape.Slope_2x1_Bottom: _tempTex[i] = TextureUtils.CreateSlopeTexture(_tiles[i].Color, .5f, 0, 64, 64); break;
                    case TileShape.Slope_2x1_Top: _tempTex[i] = TextureUtils.CreateSlopeTexture(_tiles[i].Color, 1, .5f, 64, 64); break;
                    case TileShape.Diamond: _tempTex[i] = TextureUtils.CreateDiamondTexture(_tiles[i].Color, 64, 64); break;
                    case TileShape.Isometric: _tempTex[i] = TextureUtils.CreateIsometricTexture(_tiles[i].Color, 64, 64); break;
                    case TileShape.HexagonPointedTop: _tempTex[i] = TextureUtils.CreatePointedTopHexagonTexture(_tiles[i].Color, 64, 64); break;
                    case TileShape.HexagonFlatTop: _tempTex[i] = TextureUtils.CreateFlatTopHexagonTexture(_tiles[i].Color, 64, 64); break;
                    case TileShape.Circle: _tempTex[i] = TextureUtils.CreateCircleTexture(_tiles[i].Color, 64, 64); break;
                }

                //mirror if needed
                _tempTex[i] = TextureUtils.MirrorTexture(_tempTex[i], _tiles[i].MirrorTexX, _tiles[i].MirrorTexY);
            }

            return _tempTex[i];
        }
        void CheckTextures()
        {
            if (_tempTex == null || (_tempTex.Length != _tiles.Count)) SetTileTextures();
        }

        public Texture2D GetTileTex(int id)
        {
            var tile = _tiles.Find(t => t.UniqueID == id);

            if (tile == null) return new Texture2D(1, 1);
            else return GetTileTexture(_tiles.IndexOf(tile));
        }
        public bool DoesTileExist(int id) => (_tiles.Exists(t => t.UniqueID == id) || (id < -1 && id > -5));
        #endregion

        #region UnityEditor
        void SaveAsset() => EditorUtility.SetDirty(this);
        public void RecordObject(string action)
        {
            //Debug.Log("Record");
            Undo.RecordObject(this, action);
        }

        public void CreateNewTileObject() => CreateNewTileObject("RuleTile");
        public void CreateNewTileObject(string name)
        {
            RecordObject($"Created tile objects ({this.name})");

            BetterRuleTile obj = ScriptableObject.CreateInstance<BetterRuleTile>();
            obj.name = name;

            _tileObjects.Add(obj);

            AssetDatabase.AddObjectToAsset(obj, this);
            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(obj);
        }
        public void SaveObjectToAsset(BetterRuleTile tile)
        {
            RecordObject($"Created better rule tile ({this.name})");

            if (!_tileObjects.Exists(t => t.UniqueID == tile.UniqueID))
            {
                _tileObjects.Add(tile);
                AssetDatabase.AddObjectToAsset(tile, this);
            }

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(tile);
        }
        public void SaveObjectToAsset(BetterHexagonalRuleTile tile)
        {
            RecordObject($"Created better rule tile ({this.name})");

            if (!_hexTileObjects.Exists(t => t.UniqueID == tile.UniqueID))
            {
                _hexTileObjects.Add(tile);
                AssetDatabase.AddObjectToAsset(tile, this);
            }

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(tile);
        }

        public void DeleteAllTileObjects()
        {
            RecordObject($"Deleted all child tiles ({this.name})");

            for (int i = _tileObjects.Count - 1; i >= 0; i--)
            {
                DeleteTileObject(_tileObjects[i], false);
            }
            for (int i = _hexTileObjects.Count - 1; i >= 0; i--)
            {
                DeleteTileObject(_hexTileObjects[i], false);
            }

            AssetDatabase.SaveAssets();
        }
        public void DeleteUnusedSquareTileObjects()
        {
            RecordObject($"Deleted unused objects ({this.name})");

            for (int i = _tileObjects.Count - 1; i >= 0; i--)
            {
                if (!_tiles.Exists(t => t.UniqueID == _tileObjects[i].UniqueID)) DeleteTileObject(_tileObjects[i]);
            }
        }
        public void DeleteUnusedHexTileObjects()
        {
            RecordObject($"Deleted unused objects ({this.name})");

            for (int i = _hexTileObjects.Count - 1; i >= 0; i--)
            {
                if (!_tiles.Exists(t => t.UniqueID == _hexTileObjects[i].UniqueID)) DeleteTileObject(_hexTileObjects[i]);
            }
        }
        void DeleteTileObject(BetterRuleTile tile, bool save = true)
        {
            _tileObjects.Remove(tile);
            Undo.DestroyObjectImmediate(tile);

            if (save) AssetDatabase.SaveAssets();
        }
        
        void DeleteTileObject(BetterHexagonalRuleTile tile, bool save = true)
        {
            _hexTileObjects.Remove(tile);
            Undo.DestroyObjectImmediate(tile);

            if (save) AssetDatabase.SaveAssets();
        }


        [ContextMenu("Delete All Objects")]
        public void DeleteAllNestedObjects()
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));

            foreach (var item in objs)
            {
                if (item != null && !(item is BetterRuleTileContainer)) Undo.DestroyObjectImmediate(item);
            }

            _tileObjects.Clear();
            _hexTileObjects.Clear();

            AssetDatabase.SaveAssets();
        }
        #endregion
#endif
    }
}
