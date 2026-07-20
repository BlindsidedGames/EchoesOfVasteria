#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;
using VinTools.BetterRuleTiles;
using VinTools.BetterRuleTiles.Editor.Utilities;

namespace VinTools.BetterRuleTiles.Editor.Generation
{
    public partial class BetterRuleTileGenerator
    {
        public static Type GetDefaultTileType(BetterRuleTileContainer.GridShape gridShape)
        {
            // if that failed, generate the tiles based on shape
            switch (gridShape)
            {
                case BetterRuleTileContainer.GridShape.Square: return typeof(BetterRuleTile);
                case BetterRuleTileContainer.GridShape.Isometric: return typeof(BetterRuleTile);
                case BetterRuleTileContainer.GridShape.HexagonalPointedTop: return typeof(BetterHexagonalRuleTile);
                case BetterRuleTileContainer.GridShape.HexagonalFlatTop: return typeof(BetterHexagonalRuleTile);
                default: return typeof(BetterRuleTile);
            }
        }
        
        public static void GenerateTiles(BetterRuleTileContainer container)
        {
            // if successfully generated using custom tiles, don't continue
            if (GenerateCustomTiles(container)) return;
            
            // if that failed, generate the tiles based on shape
            switch (container.settings._gridShape)
            {
                case BetterRuleTileContainer.GridShape.Square: GenerateTiles<BetterRuleTileBase>(container); break;
                case BetterRuleTileContainer.GridShape.Isometric: GenerateTiles<BetterRuleTileBase>(container); break;
                case BetterRuleTileContainer.GridShape.HexagonalPointedTop: GenerateTiles<BetterHexagonalRuleTileBase>(container); break;
                case BetterRuleTileContainer.GridShape.HexagonalFlatTop: GenerateTiles<BetterHexagonalRuleTileBase>(container); break;
            }
        }

        public static void GenerateTiles<T>(BetterRuleTileContainer container) where T : TileBase, IBetterRuleTile
        {
            // Updated package, hex tiles are now handled together with regular tiles
            // move all old hex tiles to the regular tile list
            foreach (var hexTile in container._oldHexTiles) container._tileObjects.Add(hexTile);
            container._oldHexTiles.Clear();

            // create list of tiles
            List<T> tiles = new List<T>();

            // generate tiles
            for (int i = 0; i < container.Tiles.Count; i++)
            {
                tiles.Add(GenerateTile<T>(container, container.Tiles[i].UniqueID));
            }

            // assign the other tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                //set tiles to their correct place
                foreach (T item in tiles) 
                    tiles[i].otherTiles[item.UniqueIdentifier - 1] = item.Tile;
            }
            
            // set variations
            SetVariations(container, tiles);
            foreach (var tile in tiles) tile.HasVariations = tile.variations.Count > 0;
            
            // copy extended tiling rule to regular tiling rule
            foreach (var tile in tiles)
            {
                // check if there are extra tiling rules
                tile.HasExtraTilingRules = tile.TempTilingRules.Any(t => t.IsCustomOutputSprite);

                // split tiling rules to default and extra
                ExportTilingRules(tile.TempTilingRules, out var tilingRules, out var extras);
                tile.TilingRules = tilingRules;
                tile.m_ExtraTilingRules = extras;
                tile.TempTilingRules.Clear();

                // add settings
                tile.TreatSimilarTilesAsSame = container.settings._treatSimilarTilesAsSame;
            }

            // delete unused previous tiles 
            // container.DeleteUnusedTileObjects();

            // create Tiles
            foreach (var tile in tiles) container.SaveObjectToAsset(tile);
            
            
            // ------ edit asset to use the actual tiles ------
            // Get the guids to replace
            List<string> exportedScriptGuid = new List<string>{ TypeUtils.GetTypeGuid(typeof(T)) };
            foreach (var obj in container._tileObjects)
            {
                string guid = TypeUtils.GetTypeGuid(obj.GetType());
                if (!exportedScriptGuid.Contains(guid)) exportedScriptGuid.Add(guid);
            }
            
            // get the correct script guid
            string correctScriptGuid = container.settings._selectedExportScriptGUID;
            if (correctScriptGuid == "Default")
            {
                if (typeof(T).IsAssignableFrom(typeof(BetterRuleTileBase))) correctScriptGuid = TypeUtils.GetTypeGuid(typeof(BetterRuleTile));
                if (typeof(T).IsAssignableFrom(typeof(BetterHexagonalRuleTileBase))) correctScriptGuid = TypeUtils.GetTypeGuid(typeof(BetterHexagonalRuleTile));
            }
                
            // edit file
            string path = AssetDatabase.GetAssetPath(container);
            string content = System.IO.File.ReadAllText(path);
            foreach (var replaceFrom in exportedScriptGuid)
            {
                if (replaceFrom == correctScriptGuid) continue;
                
                //Debug.Log($"Replaced from {replaceFrom} to {correctScriptGuid}");
                content = content.Replace(replaceFrom, correctScriptGuid);
            }

            // save edited file
            System.IO.File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            
            // highlight object
            Selection.activeObject = container;
            EditorGUIUtility.PingObject(container);
        }
        static T GenerateTile<T>(BetterRuleTileContainer container, int uniqueID) where T : TileBase, IBetterRuleTile
        {
            T tile = (T)container._tileObjects.Find(t => ((IBetterRuleTile)t).UniqueIdentifier == uniqueID);
            if (tile == null) tile = ScriptableObject.CreateInstance<T>();

            var templateTile = container.Tiles.Find(t => t.UniqueID == uniqueID);

            tile.name = templateTile.Name;
            tile.UniqueIdentifier = uniqueID;
            //tile.variationParent = null;
            tile.otherTiles = new TileBase[container._tiles.Max(t => t.UniqueID)];
            tile.DefaultColliderType = templateTile.ColliderType;

            tile.TempTilingRules = GenerateRules(container, uniqueID);
            tile.DefaultSprite = templateTile.DefaultSprite;
            tile.DefaultGameObject = templateTile.DefaultGameObject;

            tile.CustomTileProperties.Clear();
            for (int i = 0; i < templateTile.customProperties.Count; i++)
            {
                tile.CustomTileProperties.Add(new CustomTileProperty(templateTile.customProperties[i]));
            }

            if (tile.DefaultSprite == null)
            {
                //Debug.LogWarning($"Default sprite of tile \"{tile}\" is not set, a sprite was automatically assigned.");
                tile.DefaultSprite = tile.TempTilingRules.Last().m_Sprites[0];

                // if it's still null
                if (tile.DefaultSprite == null)
                {
                    Debug.LogWarning($"Default sprite of tile \"{tile}\" is not set, which could result in the tile displaying as a blank space.");
                }
            }

            // for tilesets assign the defaults based on the simplest tiling rule, since you can't assign those there
            if (container.UseTileSet)
            {
                tile.DefaultGameObject = tile.TempTilingRules.Last().m_GameObject;
                tile.DefaultColliderType = tile.TempTilingRules.Last().m_ColliderType;
            }

            return tile;
        }
        
        static void SetVariations<T>(BetterRuleTileContainer container, List<T> tiles) where T : TileBase, IBetterRuleTile
        {
            //for every tile
            for (int i = 0; i < tiles.Count; i++)
            {
                //find tiles where the variated tile is this tile
                var variations = container._tiles.Where(t => !t.uniqueTile && t.variationOf == tiles[i].UniqueIdentifier).ToArray();
                //Debug.Log($"{tiles[i].name} variation count: {variations.Length}");

                //add those tiles
                foreach (var item in variations)
                {
                    var obj = tiles.Find(t => t.UniqueIdentifier == item.UniqueID);

                    //obj.variationParent = tiles[i];
                    obj.TempTilingRules = AddParentRules(obj.TempTilingRules, tiles[i].TempTilingRules);
                    //tiles[i].variations.Add(obj); //not needed since the parent is only for adding the missing rules
                }

                //add variations set up manually by the user
                tiles[i].variations.Clear(); //reset
                var tileOf = container.Tiles.Find(t => t.UniqueID == tiles[i].UniqueIdentifier);
                foreach (var item in tileOf.variations)
                {
                    var brTile = tiles.Find(t => t.UniqueIdentifier == item);
                    if (brTile != null && !tiles[i].variations.Contains(brTile.Tile)) tiles[i].variations.Add(brTile.Tile);
                }
            }
        }
        
        

        #region Generic: tiling rules
        static List<ExtendedTilingRule> GenerateRules(BetterRuleTileContainer container, int uniqueID)
        {
            // create new tiling rule list
            List<ExtendedTilingRule> tilingRules = new List<ExtendedTilingRule>();

            // find the tile object
            var tileOf = container.Tiles.Find(t => t.UniqueID == uniqueID);

            foreach (var item in container.Grid.FindAll(t => t.TileID == uniqueID))
            {
                // ignore tile if it has no sprite
                if (item.Sprite == null) continue;
                // order neighbor positions by grid order
                item.NeighborPositions = item.NeighborPositions.OrderBy(t => t.y * -10000 + t.x).ToList();
                
                // create a new list for the neighbor position because it needs to be converted from editor to tilemap coords
                List<Vector3Int> NeighborPositions = new List<Vector3Int>();

                // check if the tile is part of a preset block
                var presetBlock = container._presetBlocks.Find(p => p.InBounds(item.Position));
                if (presetBlock != null)
                {
                    foreach (var neighbor in presetBlock.GetPositions(item.Position))
                    {
                        NeighborPositions.Add((Vector3Int)container.EditorToUnityCoord(new Vector2Int(neighbor.x, neighbor.y), item.Position));
                    }
                }
                // convert default neighbor positions from editor coordinates to tilemap coordinates
                else
                {
                    foreach (var neighbor in item.NeighborPositions)
                    {
                        NeighborPositions.Add((Vector3Int)container.EditorToUnityCoord(new Vector2Int(neighbor.x, neighbor.y), item.Position));
                    }
                }
                
                // make sure there are no duplicates in the neighborPositions
                NeighborPositions = RemoveDuplacePositions(NeighborPositions);
                
                // create tiling
                int[] neighbors = new int[NeighborPositions.Count];

                // set neighbors
                for (int i = 0; i < neighbors.Length; i++)
                {
                    //neighbors[i] = GetNeighborRule(container, container.Grid.Find(t => t.Position == new Vector2Int(item.Position.x + NeighborPositions[i].x, item.Position.y - NeighborPositions[i].y)), UniqueID);
                    neighbors[i] = GetNeighborRule(container, container.Grid.Find(t => t.Position == item.Position + container.EditorToUnityCoord((Vector2Int)NeighborPositions[i], item.Position)), uniqueID);
                }

                // create tiling rule
                ExtendedTilingRule tilingRule = CreateTilingRule(container, item, tileOf, presetBlock);
                tilingRule.m_NeighborPositions = container.DisplaceRules(NeighborPositions, item.Position);
                tilingRule.m_Neighbors = neighbors.ToList();
                tilingRule.brtCell = item;

                // add tiling rule to list
                tilingRules.Add(tilingRule);
            }

            // Handle duplicates and sorting
            tilingRules = RemoveDuplicates(tilingRules);
            if (container.settings._collapseSimilarRules) tilingRules = SimplifyNeighborRules(tilingRules);
            tilingRules = SortRules(tilingRules);
            
            // return rules
            return tilingRules;
        }
        static ExtendedTilingRule CreateTilingRule(BetterRuleTileContainer container, BetterRuleTileContainer.GridCell cell, BetterRuleTileContainer.TileData parentTileData, BetterRuleTileContainer.PresetBlock presetBlock)
        {
            //create stuff array
            List<Sprite> sprites = new List<Sprite>();

            //create tiling rule
            ExtendedTilingRule tilingRule = new ExtendedTilingRule();

            //if using the universal sprite settings
            if (container.settings._useUniversalSpriteSettings)
            {
                //get the override
                var spriteOverride = container._overrideSprites.Find(t => t.BaseSprite == cell.Sprite);

                //if there is an override
                if (spriteOverride != null)
                {
                    tilingRule.m_Sprites = spriteOverride.Sprites;
                    tilingRule.m_ColliderType = spriteOverride.ColliderType;
                    tilingRule.m_GameObject = spriteOverride.GameObject;
                    tilingRule.m_ExtendedOutputSprite = spriteOverride.OutputSprite;
                    tilingRule.m_PatternSize = spriteOverride.PatternSize;
                    if (spriteOverride.OutputSprite == ExtendedOutputSprite.Random)
                    {
                        tilingRule.m_PerlinScale = spriteOverride.NoiseScale;
                        tilingRule.m_RandomTransform = spriteOverride.RandomTransform;
                    }
                    if (spriteOverride.OutputSprite == ExtendedOutputSprite.Animation)
                    {
                        tilingRule.m_MaxAnimationSpeed = spriteOverride.MaxAnimationSpeed;
                        tilingRule.m_MinAnimationSpeed = spriteOverride.MinAnimationSpeed;
                    }
                }
                //if there is no override
                else
                {
                    tilingRule.m_Sprites = new Sprite[] { cell.Sprite };
                    tilingRule.m_ColliderType = parentTileData.ColliderType;
                    tilingRule.m_GameObject = parentTileData.DefaultGameObject;
                    tilingRule.m_ExtendedOutputSprite = ExtendedOutputSprite.Single;
                }
            }
            //if using the cell sprite settings
            else
            {
                if (cell.OutputSprite == ExtendedOutputSprite.Single || cell.IncludeSpriteInOutput || cell.Sprites.Length <= 0) sprites.Add(cell.Sprite);
                if (cell.OutputSprite != ExtendedOutputSprite.Single) sprites.AddRange(cell.Sprites);

                tilingRule.m_Sprites = sprites.ToArray();
                tilingRule.m_ColliderType = cell.UseDefaultSettings ? parentTileData.ColliderType : cell.ColliderType;
                tilingRule.m_GameObject = cell.UseDefaultSettings ? parentTileData.DefaultGameObject : cell.GameObject;
                tilingRule.m_ExtendedOutputSprite = cell.OutputSprite;
                tilingRule.m_PatternSize = cell.PatternSize;
                if (cell.OutputSprite == ExtendedOutputSprite.Random)
                {
                    tilingRule.m_PerlinScale = cell.NoiseScale;
                    tilingRule.m_RandomTransform = cell.RandomTransform;
                }
                if (cell.OutputSprite == ExtendedOutputSprite.Animation)
                {
                    tilingRule.m_MaxAnimationSpeed = cell.MaxAnimationSpeed;
                    tilingRule.m_MinAnimationSpeed = cell.MinAnimationSpeed;
                }
                //tilingRule.m_Id
            }

            //if inside preset block
            if (presetBlock != null)
            {
                tilingRule.m_RuleTransform = presetBlock.BlockTransform;
            }
            else
            {
                tilingRule.m_RuleTransform = cell.Transform;
            }

            // settings which are not tied to the universal sprite settings or preset blocks
            tilingRule.priorityModifier = cell.PriorityModifier;
            tilingRule.priorityGroup = cell.Priority;

            //return
            return tilingRule;
        }
        static List<Vector3Int> RemoveDuplacePositions(List<Vector3Int> neighborPositions)
        {
            //create new empty list
            List<Vector3Int> newList = new List<Vector3Int>();

            //add positions to the new list if it doesn't contain it yet
            foreach (var pos in neighborPositions)
            {
                if (!newList.Contains(pos)) newList.Add(pos);
            }

            //return list with no duplicates
            return newList;
        }
        static List<ExtendedTilingRule> RemoveDuplicates(List<ExtendedTilingRule> tilingRules)
        {
            //for loop, but lets me remove things from the list
            int i = 0;
            while (i < tilingRules.Count)
            {
                for (int b = tilingRules.Count - 1; b > i; b--)
                {
                    if (CheckSame(tilingRules[i], tilingRules[b])) tilingRules.RemoveAt(b);
                }

                //increase index
                i++;
            }

            //return
            return tilingRules;
        }
        static List<ExtendedTilingRule> SimplifyNeighborRules(List<ExtendedTilingRule> tilingRules)
        {
            //Debug.Log("Simplifying");

            //create a temporary list to store tiles
            List<ExtendedTilingRule> rulesToAdd = new List<ExtendedTilingRule>();

            //for loop, but lets me remove things from the list
            int i = 0;
            while (i < tilingRules.Count)
            {
                //find sprites with the same 
                var match = tilingRules.FindAll(t => Equals(t, tilingRules[i]));

                //if there ar more than one tiles with the same sprite
                if (match.Count > 1)
                {
                    //Debug.Log("Found match");

                    //remove duplicate sprites
                    foreach (var item in match) tilingRules.Remove(item);

                    //add simplified version
                    rulesToAdd.Add(SimlifyRules(match));
                }
                else
                {
                    //increase index
                    i++;
                }
            }

            //add rules and return it
            tilingRules.AddRange(rulesToAdd);
            return tilingRules;
        }

        static bool Equals(RuleTile.TilingRule t1, RuleTile.TilingRule t2)
        {
            //TODO there should be a more specific check to prevent accidental simplification

            if (t1.m_Sprites[0] != t2.m_Sprites[0]) return false;
            if (t2.m_Sprites.Length > 1) return false;
            if (t1.m_NeighborPositions.Count != t2.m_NeighborPositions.Count) return false;

            for (int i = 0; i < t1.m_NeighborPositions.Count; i++) if (t1.m_NeighborPositions[i] != t2.m_NeighborPositions[i]) return false;

            return true;
        }
        static ExtendedTilingRule SimlifyRules(List<ExtendedTilingRule> tilingRules)
        {
            //create a tiling rule for the 
            ExtendedTilingRule tilingRule = tilingRules[0];

            //check every neighbor
            for (int i = 0; i < tilingRule.m_NeighborPositions.Count; i++)
            {
                //check in every tile at that position
                for (int t = 0; t < tilingRules.Count; t++)
                {
                    //if not same just make the tile ignore that position
                    if (tilingRules[t].m_Neighbors[i] != tilingRule.m_Neighbors[i])
                    {
                        tilingRule.m_Neighbors[i] = -1;
                        break;
                    }
                }
            }

            //return that tile
            return tilingRule;
        }
        static bool CheckSame(RuleTile.TilingRule tr1, RuleTile.TilingRule tr2, bool checkSprites = false)
        {
            if (tr1.m_Neighbors.Count != tr2.m_Neighbors.Count) return false;
            if (tr1.m_NeighborPositions.Count != tr2.m_NeighborPositions.Count) return false;
            if (checkSprites && tr1.m_Sprites.Length != tr2.m_Sprites.Length) return false;

            for (int i = 0; i < tr1.m_Neighbors.Count; i++) if (tr1.m_Neighbors[i] != tr2.m_Neighbors[i]) return false;
            for (int i = 0; i < tr1.m_NeighborPositions.Count; i++) if (tr1.m_NeighborPositions[i] != tr2.m_NeighborPositions[i]) return false;
            if (checkSprites) for (int i = 0; i < tr1.m_Sprites.Length; i++) if (tr1.m_Sprites[i] != tr2.m_Sprites[i]) return false;

            return true;
        }

        static List<ExtendedTilingRule> SortRules(List<ExtendedTilingRule> tilingRules)
        {
            List<List<ExtendedTilingRule>> tempGrouped = tilingRules
                .GroupBy(t => t.priorityGroup) // Group by priority
                .OrderByDescending(g => g.Key) // Order groups in descending order
                .Select(g => g.OrderByDescending(t => GetNeighborPriority(t)).ToList()) // order each group separately
                .ToList();

            int i = 0;
            // for all groups
            foreach (var group in tempGrouped)
            {
                // save the initial indexes
                foreach (var rule in group)
                {
                    rule.brtCell.info_InitialRuleOrder = i;
                    i++;
                }
                
                // adjust priority by group
                ApplyCustomPriority(group);
            }
            
            // combine groups into a full list
            var temp = tempGrouped.SelectMany(g => g).ToList();
            
            // save final priority in cell
            for (int j = 0; j < tilingRules.Count; j++) temp[j].brtCell.info_ExportedRuleOrder = j;
            
            // return sorted list
            return temp;
        }
        static int GetNeighborPriority(RuleTile.TilingRule tr)
        {
            int num = 0;
            for (int i = 0; i < tr.m_Neighbors.Count; i++)
            {
                if (tr.m_Neighbors[i] != -1) num += 100; //add 100 for every non space
                if (tr.m_Neighbors[i] > 0) num += 5; //add +5 for every connection tile
            }
            return num;
        }
        
        /// <summary>
        /// Changes the order of the tiles based on their priorityModifier property
        /// </summary>
        /// <param name="tilingRules"></param>
        static void ApplyCustomPriority(List<ExtendedTilingRule> tilingRules)
        {
            // returns true if priority changed
            bool ChangePriority(int i, Predicate<int> predicate)
            {
                // This is needed since the index changes during operation
                var tileToCheck = tilingRules[i];
                // change priority
                if (predicate(tileToCheck.priorityModifier))
                {
                    var moved = BumpListPriority(tilingRules, i, i + tileToCheck.priorityModifier);
                    tileToCheck.priorityModifier -= moved;
                    
                    // if haven't modified the priority, continue iterating
                    if (moved == 0) return false;
                    // else priority change and this index contains a new item
                    return true;
                }
                
                // no change
                return false;
            }
            
            // decrease priority
            int i = 0;
            while (i < tilingRules.Count) if (!ChangePriority(i, (m) => m > 0)) i++;
            
            // increase priority
            // iterate from lowest to highest priority rules
            i = tilingRules.Count - 1;
            while (i >= 0) if (!ChangePriority(i, (m) => m < 0)) i--;
        }
        /// <summary>
        /// Move up an item in the specified list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Amount of spaces moved</returns>
        static int BumpListPriority<T>(List<T> list, int from, int to)
        {
            if (to == from) return 0;
            //Debug.Log($"Moving rule from {from} to {to}");
            
            // make sure we don't go out of range
            if (to < 0) to = 0;
            if (to >= list.Count) to = list.Count - 1;
            
            // save the item we're moving
            T tempObj = list[from];
            
            // move items in-between
            // increasing priority (index with lower index has higher priority)
            if (to < from) for (int i = from; i > to; i--) list[i] = list[i - 1];
            // decrease priority (increase index)
            else for (int i = from; i < to; i++) list[i] = list[i + 1];
            
            // move temp item
            list[to] = tempObj;
            return to - from;
        }
        
        static int GetNeighborRule(BetterRuleTileContainer container, BetterRuleTileContainer.GridCell cell, int TileID)
        {
            if (cell == null) return -1;
            if (cell.TileID == TileID) return 0;
            if (cell.TileID < 0) return cell.TileID;

            if (container.Tiles.Exists(t => t.UniqueID == cell.TileID)) return cell.TileID;

            return -1;
        }

        static List<ExtendedTilingRule> AddParentRules(List<ExtendedTilingRule> original, List<ExtendedTilingRule> parentRules)
        {
            //add extra rules for parent tile
            foreach (var item in parentRules) original.Add(CopyTilingRule(item));
            //clear duplicate rules
            original = RemoveDuplicates(original);

            return original;
        }
        static ExtendedTilingRule CopyTilingRule(ExtendedTilingRule from)
        {
            return new ExtendedTilingRule
            {
                m_Neighbors = from.m_Neighbors,
                m_NeighborPositions = from.m_NeighborPositions,
                m_Sprites = from.m_Sprites,
                m_ColliderType = from.m_ColliderType,
                m_GameObject = from.m_GameObject,
                m_MaxAnimationSpeed = from.m_MaxAnimationSpeed,
                m_MinAnimationSpeed = from.m_MinAnimationSpeed,
                m_Output = from.m_Output,
                m_Id = from.m_Id,
                m_PerlinScale = from.m_PerlinScale,
                m_RandomTransform = from.m_RandomTransform,
                m_RuleTransform = from.m_RuleTransform,
            };
        }
        static void ExportTilingRules(List<ExtendedTilingRule> original, out List<RuleTile.TilingRule> tempTilingRules, out List<ExtraTilingRule> tempExtras)
        {
            int count = original.Count();

            tempTilingRules = new RuleTile.TilingRule[count].ToList();
            tempExtras = new ExtraTilingRule[count].ToList();

            for (int i = 0; i < count; i++)
            {
                tempTilingRules[i] = original[i].ExportTilingRule();
                tempExtras[i] = original[i].ExportExtras();
            }
        }
        #endregion
    }
}
#endif