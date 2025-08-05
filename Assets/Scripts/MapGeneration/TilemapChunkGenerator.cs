using System.Collections.Generic;
using Sirenix.OdinInspector;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;
// Make sure this is included
using Random = System.Random;

namespace TimelessEchoes.MapGeneration
{
    public class TilemapChunkGenerator : MonoBehaviour
    {

        [Header("Tilemaps")] [TabGroup("References")] [SerializeField]
        private Tilemap terrainMap;

        [TabGroup("References")] [SerializeField]
        private Tilemap decorMap;

        private enum TerrainArea
        {
            Bottom,
            Middle,
            Top
        }

        [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private TerrainSettings bottomTerrain;
        [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private TerrainSettings middleTerrain;
        [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private TerrainSettings topTerrain;


        [Header("Tiles")]


        [Header("Generation Settings")] [TabGroup("Settings")] [SerializeField] [Min(2)] [HideInInspector]
        private int minAreaWidth = 2;

        [TabGroup("Settings")] [SerializeField] [Min(0)] [HideInInspector]
        private int edgeWaviness = 1;


        [Header("Depth Ranges (Min, Max)")] [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private Vector2Int sandDepthRange = new(2, 6);

        [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private Vector2Int grassDepthRange = new(2, 6);

        [Header("Random Seed")] [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private int seed;

        [TabGroup("Settings")] [SerializeField] [HideInInspector]
        private bool randomizeSeed = true;

        private Random rng;
        private int prevSandDepth = -1;
        private int prevGrassDepth = -1;
        public Tilemap TerrainMap => terrainMap;
        private BetterRuleTile BottomTile => bottomTerrain != null ? bottomTerrain.tile : null;
        private BetterRuleTile MiddleTile => middleTerrain != null ? middleTerrain.tile : null;
        private BetterRuleTile TopTile => topTerrain != null ? topTerrain.tile : null;
        public Tilemap DecorMap => decorMap;

        private void Awake()
        {
            ApplyConfig(GameManager.CurrentGenerationConfig);
            var taskGen = GetComponent<ProceduralTaskGenerator>();
            AssignTilemaps(taskGen);
            rng = randomizeSeed ? new Random() : new Random(seed);
            prevSandDepth = -1;
            prevGrassDepth = -1;
        }

        private void ApplyConfig(MapGenerationConfig cfg)
        {
            if (cfg == null) return;

            if (terrainMap == null)
                terrainMap = cfg.tilemapChunkSettings.terrainMap;
            if (decorMap == null)
                decorMap = cfg.tilemapChunkSettings.decorMap;
            bottomTerrain = cfg.tilemapChunkSettings.bottomTerrain;
            middleTerrain = cfg.tilemapChunkSettings.middleTerrain;
            topTerrain = cfg.tilemapChunkSettings.topTerrain;
            minAreaWidth = cfg.tilemapChunkSettings.minAreaWidth;
            edgeWaviness = cfg.tilemapChunkSettings.edgeWaviness;
            sandDepthRange = cfg.tilemapChunkSettings.middleDepthRange;
            grassDepthRange = cfg.tilemapChunkSettings.topDepthRange;
            seed = cfg.tilemapChunkSettings.seed;
            randomizeSeed = cfg.tilemapChunkSettings.randomizeSeed;
        }

        public void AssignTilemaps(ProceduralTaskGenerator generator)
        {
            if (generator == null)
                return;

            generator.SetTilemapReferences(terrainMap, bottomTerrain, middleTerrain, topTerrain);
        }

        public void GenerateSegment(Vector2Int offset, Vector2Int segmentSize, Transform decorParent = null)
        {
            rng = randomizeSeed ? new Random() : new Random(seed);

            GenerateInternal(offset, segmentSize, decorParent);
        }

        private void GenerateInternal(Vector2Int offset, Vector2Int segmentSize, Transform decorParent)
        {
            var sandDepths = new int[segmentSize.x];
            var grassDepths = new int[segmentSize.x];

            var currentSandDepth = prevSandDepth >= 0
                ? prevSandDepth
                : RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
            var currentGrassDepth = prevGrassDepth >= 0
                ? prevGrassDepth
                : RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

            for (var x = 0; x < segmentSize.x;)
            {
                for (var segX = 0; segX < minAreaWidth && x < segmentSize.x; segX++, x++)
                {
                    var sandDepth = currentSandDepth;
                    var grassDepth = currentGrassDepth;


                    sandDepths[x] = sandDepth;
                    grassDepths[x] = grassDepth;
                }

                var sandDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                var grassDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);

                currentSandDepth = Mathf.Clamp(currentSandDepth + sandDelta, sandDepthRange.x, sandDepthRange.y);
                currentGrassDepth = Mathf.Clamp(currentGrassDepth + grassDelta, grassDepthRange.x, grassDepthRange.y);

                if (currentSandDepth + currentGrassDepth > segmentSize.y)
                    currentGrassDepth = Mathf.Clamp(segmentSize.y - currentSandDepth, grassDepthRange.x,
                        grassDepthRange.y);
            }

            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);

                for (var y = 0; y < waterDepth; y++)
                    terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), BottomTile);

                for (var y = waterDepth; y < waterDepth + sandDepth; y++)
                    terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), MiddleTile);

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                    if (y < segmentSize.y)
                        terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), TopTile);

                for (var y = 0; y < waterDepth; y++)
                {
                    var leftWaterBottom = x > 0
                        ? segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1]
                        : waterDepth;
                    var rightWaterBottom = x < segmentSize.x - 1
                        ? segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1]
                        : waterDepth;

                    var isCurrentTileSideEdge = y < leftWaterBottom || y < rightWaterBottom;
                    var isTopEdge = y == waterDepth - 1;
                    var isTileBelowSideEdge = y - 1 < leftWaterBottom || y - 1 < rightWaterBottom;
                    var isTileBelowEdge = isTileBelowSideEdge;

                    if (isCurrentTileSideEdge || isTileBelowEdge || isTopEdge) continue;
                }

                for (var y = waterDepth; y < waterDepth + sandDepth; y++)
                {
                    var leftWaterBottom = x > 0
                        ? segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1]
                        : waterDepth;
                    var rightWaterBottom = x < segmentSize.x - 1
                        ? segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1]
                        : waterDepth;

                    var leftGrassBottom = x > 0
                        ? segmentSize.y - grassDepths[x - 1]
                        : waterDepth + sandDepth;
                    var rightGrassBottom = x < segmentSize.x - 1
                        ? segmentSize.y - grassDepths[x + 1]
                        : waterDepth + sandDepth;

                    var isCurrentTileWaterEdge = y < leftWaterBottom || y < rightWaterBottom;
                    var isCurrentTileGrassEdge = y >= leftGrassBottom || y >= rightGrassBottom;
                    var isTopEdge = y == waterDepth + sandDepth - 1;
                    var isTileBelowWaterEdge = y - 1 < leftWaterBottom || y - 1 < rightWaterBottom;
                    var isTileBelowGrassEdge = y - 1 >= leftGrassBottom || y - 1 >= rightGrassBottom;
                    var isTileBelowEdge = isTileBelowWaterEdge || isTileBelowGrassEdge;

                    if (isCurrentTileWaterEdge || isCurrentTileGrassEdge || isTileBelowEdge || isTopEdge)
                        continue;

                    var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                    if (terrainMap.GetTile(pos) == TopTile) continue;
                }

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                {
                    var leftGrassBottom = x > 0
                        ? segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1] + sandDepths[x - 1]
                        : waterDepth + sandDepth;
                    var rightGrassBottom = x < segmentSize.x - 1
                        ? segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1] + sandDepths[x + 1]
                        : waterDepth + sandDepth;

                    var isCurrentTileSideEdge = y < leftGrassBottom || y < rightGrassBottom;
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;
                    var isTileBelowSideEdge = y - 1 < leftGrassBottom || y - 1 < rightGrassBottom;
                    var isTileBelowEdge = isTileBelowSideEdge;

                    if (isCurrentTileSideEdge || isTileBelowEdge || isTopEdge) continue;
                }
            }

            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);
                PlaceDecorForColumn(offset.x + x, offset.y, waterDepth, sandDepth, grassDepth, decorParent);
            }

            if (segmentSize.x > 0)
            {
                var lastIndex = segmentSize.x - 1;
                prevSandDepth = sandDepths[lastIndex];
                prevGrassDepth = grassDepths[lastIndex];
            }
        }

        private void PlaceDecorForColumn(int worldX, int offsetY, int waterDepth, int sandDepth, int grassDepth, Transform decorParent)
        {
            if (decorMap == null && decorParent == null)
                return;

            for (var y = 0; y < waterDepth + sandDepth + grassDepth; y++)
            {
                TileBase baseTile;
                TerrainSettings settings;
                if (y < waterDepth)
                {
                    baseTile = BottomTile;
                    settings = bottomTerrain;
                }
                else if (y < waterDepth + sandDepth)
                {
                    baseTile = MiddleTile;
                    settings = middleTerrain;
                }
                else
                {
                    baseTile = TopTile;
                    settings = topTerrain;
                }

                var cell = new Vector3Int(worldX, offsetY + y, 0);
                var worldPos = terrainMap.CellToWorld(cell).x;

                var choices = new List<DecorEntry>();
                if (settings != null)
                {
                    foreach (var d in settings.decor.decor)
                    {
                        if (d.GetWeight(worldPos) <= 0f)
                            continue;

                        var cfg = d.config;
                        var allowed = !cfg.borderOnly
                            ? !IsBufferedEdge(cell, baseTile, cfg.topBuffer, cfg.bottomBuffer, cfg.sideBuffer)
                            : IsBorderCell(cell, baseTile, cfg);

                        if (allowed)
                            choices.Add(d);
                    }
                }
                var density = settings != null ? settings.decor.density : 0f;
                if (choices.Count == 0 || RandomRangeFloat(0f, 1f) > density)
                    continue;

                var total = 0f;
                foreach (var d in choices) total += d.config.weight;
                var r = RandomRangeFloat(0f, total);
                foreach (var d in choices)
                {
                    r -= d.config.weight;
                    if (r <= 0f)
                    {
                        if (d.prefab != null)
                        {
                            var center = terrainMap.GetCellCenterWorld(cell);
                            var instance = Instantiate(d.prefab, center, Quaternion.identity, decorParent);
                            if (d.randomFlipX && RandomRangeFloat(0f, 1f) < 0.5f)
                            {
                                var renderer = instance.GetComponentInChildren<SpriteRenderer>();
                                if (renderer != null)
                                    renderer.flipX = true;
                            }
                        }

                        if (decorMap != null && d.tile != null)
                        {
                            decorMap.SetTile(cell, d.tile);
                            if (d.randomFlipX)
                            {
                                decorMap.SetTileFlags(cell, TileFlags.None);
                                var matrix = RandomRangeFloat(0f, 1f) < 0.5f
                                    ? Matrix4x4.Scale(new Vector3(-1, 1, 1))
                                    : Matrix4x4.identity;
                                decorMap.SetTransformMatrix(cell, matrix);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private int CountSame(Vector3Int start, Vector3Int dir, TileBase tile)
        {
            var count = 0;
            var pos = start;
            while (terrainMap.GetTile(pos) == tile)
            {
                count++;
                pos += dir;
            }

            return count;
        }

        private bool IsInCore(Vector3Int cell, TileBase tile, DecorConfig config, int extraOffset)
        {
            if (terrainMap.GetTile(cell) != tile) return false;

            var upDist = CountSame(cell + Vector3Int.up, Vector3Int.up, tile);
            var downDist = CountSame(cell + Vector3Int.down, Vector3Int.down, tile);
            var leftDist = CountSame(cell + Vector3Int.left, Vector3Int.left, tile);
            var rightDist = CountSame(cell + Vector3Int.right, Vector3Int.right, tile);

            var topRaw = config.topBorderOffset;
            if (topRaw < 0 && upDist > 0) return false;
            var bottomRaw = config.bottomBorderOffset;
            if (bottomRaw < 0 && downDist > 0) return false;
            var leftRaw = config.leftBorderOffset;
            if (leftRaw < 0 && leftDist > 0) return false;
            var rightRaw = config.rightBorderOffset;
            if (rightRaw < 0 && rightDist > 0) return false;

            if (topRaw < 0 && terrainMap.GetTile(cell + Vector3Int.up) != tile) return false;
            if (bottomRaw < 0 && terrainMap.GetTile(cell + Vector3Int.down) != tile) return false;
            if (leftRaw < 0 && terrainMap.GetTile(cell + Vector3Int.left) != tile) return false;
            if (rightRaw < 0 && terrainMap.GetTile(cell + Vector3Int.right) != tile) return false;

            var topOffset = Mathf.Max(0, topRaw) + extraOffset;
            var bottomOffset = Mathf.Max(0, bottomRaw) + extraOffset;
            var leftOffset = Mathf.Max(0, leftRaw) + extraOffset;
            var rightOffset = Mathf.Max(0, rightRaw) + extraOffset;

            if (upDist < topOffset) return false;
            if (downDist < bottomOffset) return false;
            if (leftDist < leftOffset) return false;
            if (rightDist < rightOffset) return false;

            for (var dx = -leftOffset; dx <= rightOffset; dx++)
            for (var dy = -bottomOffset; dy <= topOffset; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var checkPos = cell + new Vector3Int(dx, dy, 0);
                if (terrainMap.GetTile(checkPos) != tile)
                    return false;
            }

            return true;
        }

        private bool IsBorderCell(Vector3Int cell, TileBase tile, DecorConfig config)
        {
            var inCore = IsInCore(cell, tile, config, 0);
            var inInnerCore = IsInCore(cell, tile, config, 1);
            return inCore && !inInnerCore;
        }

        // Updated to use HasFlag for the [Flags] enum

        private bool IsBufferedEdge(Vector3Int cell, TileBase tile, int topBuffer, int bottomBuffer, int sideBuffer)
        {
            for (var dx = -sideBuffer; dx <= sideBuffer; dx++)
            for (var dy = -bottomBuffer; dy <= topBuffer; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (terrainMap.GetTile(cell + new Vector3Int(dx, dy, 0)) != tile)
                    return true;
            }

            return false;
        }

        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new Random() : new Random(seed);

            return rng.Next(minInclusive, maxExclusive);
        }

        private float RandomRangeFloat(float minInclusive, float maxInclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new Random() : new Random(seed);

            return (float)(rng.NextDouble() * (maxInclusive - minInclusive) + minInclusive);
        }

        private void ClearMaps()
        {
            terrainMap.ClearAllTiles();
            decorMap?.ClearAllTiles();
        }

        public void ClearSegment(Vector2Int offset, Vector2Int segmentSize)
        {
            for (var x = 0; x < segmentSize.x; x++)
            for (var y = 0; y < segmentSize.y; y++)
            {
                var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                terrainMap.SetTile(pos, null);
                if (decorMap != null)
                {
                    decorMap.SetTile(pos, null);
                    decorMap.SetTransformMatrix(pos, Matrix4x4.identity);
                }
            }
        }

        public void Clear()
        {
            ClearMaps();
            prevSandDepth = -1;
            prevGrassDepth = -1;
        }
    }
}