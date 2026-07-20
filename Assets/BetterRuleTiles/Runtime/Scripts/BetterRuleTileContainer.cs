using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using static UnityEngine.RuleTile.TilingRuleOutput;

using static UnityEngine.Tilemaps.Tile;
using MathUtils = VinTools.BetterRuleTiles.Runtime.Utilities.MathUtils;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer : ScriptableObject
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

        [MenuItem("Assets/Create/2D/Tiles/Better Rule Tile Container")]
        [MenuItem("Tools/Better Rule Tiles/Create BRT Container", false, 1)]
        public static void CreateBetterRuleTileContainer() => EditorUtils.CreateScriptableObjectAsset<BetterRuleTileContainer>("New Better Rule Tile Container");
        
        [MenuItem("Assets/Create/2D/Tiles/Tileset Container")]
        [MenuItem("Tools/Better Rule Tiles/Create Tileset Container", false, 2)]
        public static void CreateTileSetContainer()
        {
            var asset = EditorUtils.CreateScriptableObjectAsset<BetterRuleTileContainer>("New Tileset Container");
            asset.UseTileSet = true;
        }
#endif

        #region Variables
        [Header("Variables")]
        public EditorSettings settings = new EditorSettings();
        //[Space]
        public List<TileBase> _tileObjects = new List<TileBase>();
        [FormerlySerializedAs("_hexTileObjects")] public List<BetterHexagonalRuleTile> _oldHexTiles = new List<BetterHexagonalRuleTile>();
        public List<GridCell> _grid = new List<GridCell>();
        public List<TileData> _tiles = new List<TileData>();
        public List<UniversalSpriteData> _overrideSprites = new List<UniversalSpriteData>();
        public List<PresetBlock> _presetBlocks = new List<PresetBlock>();

        public List<GridCell> Grid { get => _grid; }
        public List<TileData> Tiles { get => _tiles; }

        #region Classes

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
                case GridShape.Isometric: return MathUtils.TransformPoint(neighborCoord, new Vector2Int(0, -1), new Vector2Int(-1, 0));
                //case GridShape.HexagonalFlatTop: return Functions.TransformPoint(neighborCoord, new Vector2Int(0, -1), new Vector2Int(1, 0));
                default: return MathUtils.TransformPoint(neighborCoord, new Vector2Int(1, 0), new Vector2Int(0, -1));
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
                        _new[i] = (Vector3Int)Vector2Int.RoundToInt(MathUtils.TransformPoint((Vector3)_new[i], new Vector2(0, 1), new Vector2Int(1, 0)));
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
        public GridCell SetSprite(Vector2Int p, Sprite s)
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
            else if (cell.Locked) return cell;

            //set sprite
            if (cell.Sprite == s) return cell;
            cell.Sprite = s;

            //add cell
            if (n) _grid.Add(cell);

            SaveAsset();
            return cell;
        }
        public void SetTile(Vector2Int p, int id)
        {
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
        public void CreateTile(int tileId = 1)
        {
            RecordObject($"Created new tile ({this.name})");

            //get an id which is not used yet
            while (_tiles.Exists(t => t.UniqueID == tileId)) tileId++;

            _tiles.Add(new TileData("New Tile", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), tileId, settings._gridShape));
            _tempTex = null;

            SaveAsset();
        }

        public void ModifyTile(int index, TileData tempTile)
        {
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
            RecordObject($"Created Preset Block ({this.name})");

            // if size is 1x1 or less
            if (block.size.x <= 1 && block.size.y <= 1)
            {
                EditorUtility.DisplayDialog("Invalid block size!", "A preset block must be larger than a 1x1 area!", "OK");
                return;
            }

            var overlappingBlocks = _presetBlocks.FindAll(b => b.Overlaps(block));
            int overlappingBlocksCount = overlappingBlocks.Count;

            // if overlaps two or more already existing blocks
            if (overlappingBlocksCount > 1)
            {
                if (EditorUtility.DisplayDialog("Invalid block placement!", "The selected area overlaps with two or more already existing preset blocks. Do you want to merge them together?", "Yes", "No"))
                {
                    overlappingBlocks[0].AddRect(block);
                    for (int i = 1; i < overlappingBlocksCount; i++)
                    {
                        overlappingBlocks[0].AddRects(overlappingBlocks[i].Bounds);
                        _presetBlocks.Remove(overlappingBlocks[i]);
                    }
                }
                return;
            }

            // if only overlaps one block
            if (overlappingBlocksCount == 1)
            {
                if (EditorUtility.DisplayDialog("Overlapping bounds!", "The selected area overlaps with an existing preset block, do you want to add it to that block?", "Yes", "No"))
                    overlappingBlocks[0].AddRect(block);

                return;
            }
            
            // no issues, add a new preset block
            _presetBlocks.Add(new PresetBlock(block));
        }
        public void DeletePresetBlock(Vector2 pos)
        {
            RecordObject($"Deleted Preset Block ({this.name})");

            // find block of the tile
            var block = _presetBlocks.Find(b => b.InBounds(pos));

            if (block != null)
            {
                _presetBlocks.Remove(block);
            }
        }

        public bool MoveArea(Rect area, Vector2Int moveBy)
        {
            RecordObject($"Moved selection ({this.name})");

            // handle preset blocks
            var overlappingBlocks = _presetBlocks.FindAll(b => b.Overlaps(area));
            if (overlappingBlocks.Count > 0)
            {
                // variables so the operation can be reverted
                List<PresetBlock> newBlocks = new List<PresetBlock>();
                List<Rect>[] oldRects = new List<Rect>[overlappingBlocks.Count];
                for (int i = 0; i < overlappingBlocks.Count; i++) oldRects[i] = overlappingBlocks[i].Bounds.ToList();
                
                // get the area that was separated from the preset blocks
                foreach (var block in overlappingBlocks)
                {
                    // create a new offset block
                    var newBlock = new PresetBlock(block.SplitOverlappingArea(area));
                    newBlock.MoveArea(moveBy);
                    newBlocks.Add(newBlock);
                }
                
                // check if there are no overlaps
                bool overlaps = false;
                foreach (var block in _presetBlocks) {
                    foreach (var newblock in newBlocks) {
                        foreach (var rect in newblock.Bounds) {
                            if (block.Overlaps(rect)) {
                                overlaps = true;
                                break;
                            }
                        }
                    }
                }
                
                // if there are overlap, revert to original and cancel the move operation
                if (overlaps)
                {
                    for (int i = 0; i < overlappingBlocks.Count; i++)
                    {
                        overlappingBlocks[i].Bounds = oldRects[i];
                        overlappingBlocks[i].RecalculateEdges();
                    }
                    
                    // error
                    EditorUtility.DisplayDialog("Overlapping Preset Blocks!", 
                        "Move operation was cancelled because Preset Blocks were overlapping. " +
                        "Make sure Preset Blocks are not overlapping when moving a selection.", "OK");
                    return false;
                }
                
                // if everything is fine, create the new preset blocks
                _presetBlocks.AddRange(newBlocks);
                // remove old block if it's empty
                foreach (var block in _presetBlocks.ToArray())
                    if (block.Bounds.Count == 0) _presetBlocks.Remove(block);
                
                // Check if the blocks that were modified have detached parts. If they do, split them
                foreach (var block in overlappingBlocks)
                {
                    if (!block.SeparateLooseRects(out List<Rect> separatedRects)) continue;

                    // true loop in case the separated section still has loose parts
                    while (true)
                    {
                        var newBlock = new PresetBlock(separatedRects);
                        _presetBlocks.Add(newBlock);
                        
                        if (!newBlock.SeparateLooseRects(out separatedRects)) break;
                    }
                }
            }

            // get tiles in the area
            var tiles = _grid.Where(t => area.Contains(t.Position) && !t.Locked).ToArray();
            // remove tiles temporarily
            _grid.RemoveAll(t => area.Contains(t.Position) && !t.Locked);

            // change their position
            for (int i = 0; i < tiles.Length; i++) tiles[i].Position += moveBy;

            // shift neighbor position on hex tiles, pointed top
            if (settings._gridShape == GridShape.HexagonalPointedTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnPointedHexGrid(ref tiles[i], moveBy);
            }

            // shift neighbor position on hex tiles, flat top
            if (settings._gridShape == GridShape.HexagonalFlatTop)
            {
                for (int i = 0; i < tiles.Length; i++) FixMoveOnFlattopHexGrid(ref tiles[i], moveBy);
            }

            // put them back
            for (int i = 0; i < tiles.Length; i++)
            {
                var cell = _grid.Find(t => t.Position == tiles[i].Position);

                if (cell != null) _grid[_grid.IndexOf(cell)] = tiles[i];
                if (cell == null) _grid.Add(tiles[i]);
            }

            //save
            SaveAsset();
            return true;
        }

        public static void FixMoveOnPointedHexGrid(ref GridCell cell, Vector2Int moveBy, bool neighborFix = true, bool moveFix = true)
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

        public static void FixMoveOnFlattopHexGrid(ref GridCell cell, Vector2Int moveBy, bool neighborFix = true, bool moveFix = true)
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
        public string GetTileName(int i) => _tiles[i].Name;
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

        public Texture2D[] _tempTex = null;
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
        #endregion

        #region UnityEditor
        public void SaveAsset() => EditorUtility.SetDirty(this);
        public void RecordObject(string action)
        {
            //Debug.Log("Record");
            Undo.RecordObject(this, action);
        }

        public void CreateNewTileObject<T>(string name) where T : TileBase
        {
            RecordObject($"Created tile objects ({this.name})");

            TileBase obj = ScriptableObject.CreateInstance<T>();
            obj.name = name;

            _tileObjects.Add(obj);

            AssetDatabase.AddObjectToAsset(obj, this);
            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(obj);
        }
        public void SaveObjectToAsset(TileBase tile)
        {
            RecordObject($"Created better rule tile ({this.name})");

            if (!_tileObjects.Exists(t => ((IBetterRuleTile)t).UniqueIdentifier == ((IBetterRuleTile)tile).UniqueIdentifier))
            {
                _tileObjects.Add(tile);
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
            for (int i = _oldHexTiles.Count - 1; i >= 0; i--)
            {
                DeleteTileObject(_oldHexTiles[i], false);
            }

            AssetDatabase.SaveAssets();
        }
        public void DeleteUnusedTileObjects()
        {
            RecordObject($"Deleted unused objects ({this.name})");

            for (int i = _tileObjects.Count - 1; i >= 0; i--)
            {
                if (!_tiles.Exists(t => t.UniqueID == ((IBetterRuleTile)_tileObjects[i]).UniqueIdentifier)) DeleteTileObject(_tileObjects[i]);
            }
        }
        void DeleteTileObject(TileBase tile, bool save = true)
        {
            _tileObjects.Remove(tile);
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
            _oldHexTiles.Clear();

            AssetDatabase.SaveAssets();
        }
        #endregion
#endif

        // ---------------------------------------------------------------------------------------------------------------------------------------- //
    }
}
