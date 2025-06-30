#if UNITY_EDITOR
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using VinTools.BetterRuleTiles;
using VinTools.BetterRuleTiles.Internal;

namespace VinToolsEditor.BetterRuleTiles
{
    public class BetterRuleTileGenerator
    {
        public static void GenerateTiles(BetterRuleTileContainer container)
        {
            //generate the tiles, based on shape
            switch (container.settings._gridShape)
            {
                case BetterRuleTileContainer.GridShape.Square: GenerateSquareTiles(container); break;
                case BetterRuleTileContainer.GridShape.Isometric: GenerateSquareTiles(container); break;
                case BetterRuleTileContainer.GridShape.HexagonalPointedTop: GenerateHexTiles(container); break;
                case BetterRuleTileContainer.GridShape.HexagonalFlatTop: GenerateHexTiles(container); break;
            }

            //highlight object
            Selection.activeObject = container;
            EditorGUIUtility.PingObject(container);
        }

        #region Per-type functions
        public static void GenerateSquareTiles(BetterRuleTileContainer container)
        {
            //create list of tiles
            List<BetterRuleTile> tiles = new List<BetterRuleTile>();

            //generate tiles
            for (int i = 0; i < container.Tiles.Count; i++)
            {
                tiles.Add(GenerateTile(container, container.Tiles[i].UniqueID));
            }

            //assign the other tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                //set tiles to their correct place
                foreach (var item in tiles) tiles[i].otherTiles[item.UniqueID - 1] = item;
            }

            //set variations
            SetVariations(container, tiles);
            foreach (var tile in tiles) tile.HasVariations = tile.variations.Count > 0;

            //copy extended tiling rule to regular tiling rule
            foreach (var tile in tiles)
            {
                ExportTilingRules(ref tile.m_ExtendedTilingRules, out var tilingRules, out var extras);
                tile.m_TilingRules = tilingRules;
                tile.m_ExtraTilingRules = extras;

                //check if there are extra tiling rules
                tile.HasExtraTilingRules = tile.m_ExtendedTilingRules.Any(t => t.IsCustomOutputSprite);

                //add settings
                tile.TreatSimilarTilesAsSame = container.settings._treatSimilarTilesAsSame;
            }

            //delete unused previous tiles 
            container.DeleteUnusedSquareTileObjects();

            //create Tiles
            foreach (var tile in tiles) container.SaveObjectToAsset(tile);
        }
        public static void GenerateHexTiles(BetterRuleTileContainer container)
        {
            //create list of tiles
            List<BetterHexagonalRuleTile> tiles = new List<BetterHexagonalRuleTile>();

            //generate tiles
            for (int i = 0; i < container.Tiles.Count; i++)
            {
                tiles.Add(GenerateHexTile(container, container.Tiles[i].UniqueID));
            }

            //assign the other tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                //set tiles to their correct place
                foreach (var item in tiles) tiles[i].otherTiles[item.UniqueID - 1] = item;
            }

            //set variations
            SetVariations(container, tiles);
            foreach (var tile in tiles) tile.HasVariations = tile.variations.Count > 0;

            //copy extended tiling rule to regular tiling rule
            foreach (var tile in tiles)
            {
                ExportTilingRules(ref tile.m_ExtendedTilingRules, out var tilingRules, out var extras);
                tile.m_TilingRules = tilingRules;
                tile.m_ExtraTilingRules = extras;

                //check if there are extra tiling rules
                tile.HasExtraTilingRules = tile.m_ExtendedTilingRules.Any(t => t.IsCustomOutputSprite);

                //add settings
                tile.TreatSimilarTilesAsSame = container.settings._treatSimilarTilesAsSame;
            }

            //delete unused previous tiles 
            container.DeleteUnusedHexTileObjects();

            //create Tiles
            foreach (var tile in tiles) container.SaveObjectToAsset(tile);
        }

        static BetterRuleTile GenerateTile(BetterRuleTileContainer container, int UniqueID)
        {
            BetterRuleTile tile = container._tileObjects.Find(t => t.UniqueID == UniqueID);
            if (tile == null) tile = ScriptableObject.CreateInstance<BetterRuleTile>();

            var templateTile = container.Tiles.Find(t => t.UniqueID == UniqueID);

            tile.name = templateTile.Name;
            tile.UniqueID = UniqueID;
            //tile.variationParent = null;
            tile.otherTiles = new BetterRuleTile[container._tiles.Max(t => t.UniqueID)];
            tile.m_DefaultColliderType = templateTile.ColliderType;

            tile.m_ExtendedTilingRules = GenerateRules(container, UniqueID);
            tile.m_DefaultSprite = templateTile.DefaultSprite;
            tile.m_DefaultGameObject = templateTile.DefaultGameObject;

            tile.customProperties.Clear();
            for (int i = 0; i < templateTile.customProperties.Count; i++)
            {
                tile.customProperties.Add(new CustomTileProperty(templateTile.customProperties[i]));
            }

            if (tile.m_DefaultSprite == null)
            {
                Debug.LogWarning($"Default sprite of tile \"{tile}\" is not set, which could result in the tile displaying as a blank space.");
            }

            return tile;
        }
        static BetterHexagonalRuleTile GenerateHexTile(BetterRuleTileContainer container, int UniqueID)
        {
            BetterHexagonalRuleTile tile = container._hexTileObjects.Find(t => t.UniqueID == UniqueID);
            if (tile == null) tile = ScriptableObject.CreateInstance<BetterHexagonalRuleTile>();

            var templateTile = container.Tiles.Find(t => t.UniqueID == UniqueID);

            tile.name = templateTile.Name;
            tile.UniqueID = UniqueID;
            //tile.variationParent = null;
            tile.otherTiles = new BetterHexagonalRuleTile[container._tiles.Max(t => t.UniqueID)];
            tile.m_DefaultColliderType = templateTile.ColliderType;

            tile.m_ExtendedTilingRules = GenerateRules(container, UniqueID);
            tile.m_DefaultSprite = templateTile.DefaultSprite;
            tile.m_DefaultGameObject = templateTile.DefaultGameObject;

            tile.customProperties.Clear();
            for (int i = 0; i < templateTile.customProperties.Count; i++)
            {
                tile.customProperties.Add(new CustomTileProperty(templateTile.customProperties[i]));
            }

            if (tile.m_DefaultSprite == null)
            {
                Debug.LogWarning($"Default sprite of tile \"{tile}\" is not set, which could result in the tile displaying as a blank space.");
            }

            return tile;
        }

        static void SetVariations(BetterRuleTileContainer container, List<BetterRuleTile> tiles)
        {
            //for every tile
            for (int i = 0; i < tiles.Count; i++)
            {
                //find tiles where the variated tile is this tile
                var variations = container._tiles.Where(t => !t.uniqueTile && t.variationOf == tiles[i].UniqueID).ToArray();
                //Debug.Log($"{tiles[i].name} variation count: {variations.Length}");

                //add those tiles
                foreach (var item in variations)
                {
                    var obj = tiles.Find(t => t.UniqueID == item.UniqueID);

                    //obj.variationParent = tiles[i];
                    AddParentRules(ref obj.m_ExtendedTilingRules, ref tiles[i].m_ExtendedTilingRules);
                    //tiles[i].variations.Add(obj); //not needed since the parent is only for adding the missing rules
                }

                //add variations set up manually by the user
                tiles[i].variations.Clear(); //reset
                var tileOf = container.Tiles.Find(t => t.UniqueID == tiles[i].UniqueID);
                foreach (var item in tileOf.variations)
                {
                    var brTile = tiles.Find(t => t.UniqueID == item);
                    if (brTile != null && !tiles[i].variations.Contains(brTile)) tiles[i].variations.Add(brTile);
                }
            }
        }
        static void SetVariations(BetterRuleTileContainer container, List<BetterHexagonalRuleTile> tiles)
        {
            //for every tile
            for (int i = 0; i < tiles.Count; i++)
            {
                //find tiles where the variated tile is this tile
                var variations = container._tiles.Where(t => !t.uniqueTile && t.variationOf == tiles[i].UniqueID).ToArray();
                //Debug.Log($"{tiles[i].name} variation count: {variations.Length}");

                //add those tiles
                foreach (var item in variations)
                {
                    var obj = tiles.Find(t => t.UniqueID == item.UniqueID);

                    //obj.variationParent = tiles[i];
                    AddParentRules(ref obj.m_ExtendedTilingRules, ref tiles[i].m_ExtendedTilingRules);
                    //tiles[i].variations.Add(obj); //not needed since the parent is only for adding the missing rules
                }

                //add variations set up manually by the user
                tiles[i].variations.Clear(); //reset
                var tileOf = container.Tiles.Find(t => t.UniqueID == tiles[i].UniqueID);
                foreach (var item in tileOf.variations)
                {
                    var brTile = tiles.Find(t => t.UniqueID == item);
                    if (brTile != null && !tiles[i].variations.Contains(brTile)) tiles[i].variations.Add(brTile);
                }
            }
        }
        #endregion

        /*static void ExportTilingRules<T>(T tile)
        {
            int count = tile.m_ExtendedTilingRules.Count();

            var tempTilingRules = new RuleTile.TilingRule[count];
            var tempExtras = new ExtraTilingRule[count];

            for (int i = 0; i < count; i++)
            {
                tempTilingRules[i] = tile.m_ExtendedTilingRules[i].ExportTilingRule();
                tempExtras[i] = tile.m_ExtendedTilingRules[i].ExportExtras();
            }

            tile.m_TilingRules = tempTilingRules.ToList();
            tile.m_ExtraTilingRules = tempExtras.ToList();

            tile.m_ExtendedTilingRules.Clear();
        }*/



        #region Generic: tiling rules
        static List<ExtendedTilingRule> GenerateRules(BetterRuleTileContainer container, int UniqueID)
        {
            //create new tiling rule list
            List<ExtendedTilingRule> tilingRules = new List<ExtendedTilingRule>();

            //find the tile object
            var tileOf = container.Tiles.Find(t => t.UniqueID == UniqueID);

            foreach (var item in container.Grid.FindAll(t => t.TileID == UniqueID))
            {
                //ignore tile if has no sprite
                if (item.Sprite == null) continue;
                //order neighborpositions by grid order
                item.NeighborPositions = item.NeighborPositions.OrderBy(t => t.y * -10000 + t.x).ToList();

                //create a new list for the neighbor position because it needs to be converted from editor to tilemap coords
                List<Vector3Int> NeighborPositions = new List<Vector3Int>();

                //check if the tile is part of a preset block
                var presetBlock = container._presetBlocks.Find(p => p.InBounds(item.Position));
                if (presetBlock != null)
                {
                    foreach (var neighbor in presetBlock.GetPositions(item.Position))
                    {
                        NeighborPositions.Add((Vector3Int)container.EditorToUnityCoord(new Vector2Int(neighbor.x, neighbor.y), item.Position));
                    }
                }
                //convert default neighbor positions from editor coordinates to tilemap coordinates
                else
                {
                    foreach (var neighbor in item.NeighborPositions)
                    {
                        NeighborPositions.Add((Vector3Int)container.EditorToUnityCoord(new Vector2Int(neighbor.x, neighbor.y), item.Position));
                    }
                }

                //create tiling
                int[] neighbors = new int[NeighborPositions.Count];

                //set neighbors
                for (int i = 0; i < neighbors.Length; i++)
                {
                    //neighbors[i] = GetNeighborRule(container, container.Grid.Find(t => t.Position == new Vector2Int(item.Position.x + NeighborPositions[i].x, item.Position.y - NeighborPositions[i].y)), UniqueID);
                    neighbors[i] = GetNeighborRule(container, container.Grid.Find(t => t.Position == item.Position + container.EditorToUnityCoord((Vector2Int)NeighborPositions[i], item.Position)), UniqueID);
                }

                //create tiling rule
                ExtendedTilingRule tilingRule = CreateTilingRule(container, item, tileOf, presetBlock);
                tilingRule.m_NeighborPositions = container.DisplaceRules(NeighborPositions, item.Position);
                tilingRule.m_Neighbors = neighbors.ToList();

                //add tiling rule to list
                tilingRules.Add(tilingRule);
            }

            //return list
            tilingRules = RemoveDuplicates(tilingRules);
            if (container.settings._collapseSimilarRules) tilingRules = SimplifyNeighborRules(tilingRules);
            tilingRules = SortRules(tilingRules);
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

            //settings which are not tied to the universal sprite settings or preset blocks
            //

            //return
            return tilingRule;
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
                        tilingRule.m_Neighbors[i] = Neighbor.Ignore;
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
        static List<ExtendedTilingRule> SortRules(List<ExtendedTilingRule> tilingRules) => tilingRules.OrderByDescending(t => GetNeighborPriority(t)).ToList();
        static int GetNeighborPriority(RuleTile.TilingRule tr)
        {
            int num = 0;
            for (int i = 0; i < tr.m_Neighbors.Count; i++)
            {
                if (tr.m_Neighbors[i] != Neighbor.Ignore) num += 100; //add 100 for every non space
                if (tr.m_Neighbors[i] > 0) num += 5; //add +5 for every connection tile
            }
            return num;
        }
        static int GetNeighborRule(BetterRuleTileContainer container, BetterRuleTileContainer.GridCell cell, int TileID)
        {
            if (cell == null) return Neighbor.Ignore;
            if (cell.TileID == TileID) return Neighbor.This;
            if (cell.TileID == -3) return Neighbor.NotThis;
            if (cell.TileID == -4) return Neighbor.Any;
            if (cell.TileID == -2) return Neighbor.Empty;

            if (container.Tiles.Exists(t => t.UniqueID == cell.TileID)) return cell.TileID;

            return Neighbor.Ignore;
        }

        static void AddParentRules(ref List<ExtendedTilingRule> tileRules, ref List<ExtendedTilingRule> parentRules)
        {
            //add extra rules for parent tile
            foreach (var item in parentRules) tileRules.Add(CopyTilingRule(item));
            //clear duplicate rules
            tileRules = RemoveDuplicates(tileRules);
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
        static void ExportTilingRules(ref List<ExtendedTilingRule> m_ExtendedTilingRules, out List<RuleTile.TilingRule> tempTilingRules, out List<ExtraTilingRule> tempExtras)
        {
            int count = m_ExtendedTilingRules.Count();

            tempTilingRules = new RuleTile.TilingRule[count].ToList();
            tempExtras = new ExtraTilingRule[count].ToList();

            for (int i = 0; i < count; i++)
            {
                tempTilingRules[i] = m_ExtendedTilingRules[i].ExportTilingRule();
                tempExtras[i] = m_ExtendedTilingRules[i].ExportExtras();
            }

            m_ExtendedTilingRules.Clear();
        }
        #endregion
    }
}
#endif