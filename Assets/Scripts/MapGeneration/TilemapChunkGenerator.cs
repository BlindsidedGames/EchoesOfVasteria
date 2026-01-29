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
        private Transform currentDecorParent;
        public Tilemap TerrainMap => terrainMap;
        private BetterRuleTile BottomTile => bottomTerrain != null ? bottomTerrain.tile : null;
        private BetterRuleTile MiddleTile => middleTerrain != null ? middleTerrain.tile : null;
        private BetterRuleTile TopTile => topTerrain != null ? topTerrain.tile : null;
        public Tilemap DecorMap => decorMap;

        // Reusable buffer for SetTilesBlock per-column writes
        private TileBase[] columnBuffer;
        // Dedicated null buffer used only for clears to avoid residual data
        private TileBase[] clearColumnBuffer;
        // Per-segment lookup for decor prefabs by cell for O(1) clears
        private readonly Dictionary<Vector3Int, Transform> decorLookup = new();

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

            currentDecorParent = decorParent;
            decorLookup.Clear();
            GenerateInternal(offset, segmentSize, decorParent);
        }

        public System.Collections.IEnumerator GenerateSegmentAsync(Vector2Int offset, Vector2Int segmentSize, Transform decorParent = null, int columnsPerYield = 2)
        {
            rng = randomizeSeed ? new Random() : new Random(seed);
            currentDecorParent = decorParent;
            decorLookup.Clear();

            var sandDepths = new int[segmentSize.x];
            var grassDepths = new int[segmentSize.x];

            var currentSandDepth = prevSandDepth >= 0 ? prevSandDepth : RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
            var currentGrassDepth = prevGrassDepth >= 0 ? prevGrassDepth : RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

            for (var x = 0; x < segmentSize.x;)
            {
                for (var segX = 0; segX < minAreaWidth && x < segmentSize.x; segX++, x++)
                {
                    sandDepths[x] = currentSandDepth;
                    grassDepths[x] = currentGrassDepth;
                }

                var sandDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                var grassDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);

                currentSandDepth = Mathf.Clamp(currentSandDepth + sandDelta, sandDepthRange.x, sandDepthRange.y);
                currentGrassDepth = Mathf.Clamp(currentGrassDepth + grassDelta, grassDepthRange.x, grassDepthRange.y);

                if (currentSandDepth + currentGrassDepth > segmentSize.y)
                    currentGrassDepth = Mathf.Clamp(segmentSize.y - currentSandDepth, grassDepthRange.x, grassDepthRange.y);
            }

            if (columnBuffer == null || columnBuffer.Length != segmentSize.y)
                columnBuffer = new TileBase[segmentSize.y];

            var processed = 0;
            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);

                for (var y = 0; y < segmentSize.y; y++)
                {
                    TileBase t = null;
                    if (y < waterDepth) t = BottomTile;
                    else if (y < waterDepth + sandDepth) t = MiddleTile;
                    else if (y < waterDepth + sandDepth + grassDepth) t = TopTile;
                    columnBuffer[y] = t;
                }

                var bounds = new BoundsInt(offset.x + x, offset.y, 0, 1, segmentSize.y, 1);
                terrainMap.SetTilesBlock(bounds, columnBuffer);

                processed++;
                if (processed % Mathf.Max(1, columnsPerYield) == 0)
                    yield return null;
            }

            // Second pass: place decor after all tiles are written so adjacency checks are valid
            processed = 0;
            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);
                PlaceDecorForColumn(offset.x + x, offset.y, waterDepth, sandDepth, grassDepth, decorParent);
                processed++;
                if (processed % Mathf.Max(1, columnsPerYield) == 0)
                    yield return null;
            }

            if (segmentSize.x > 0)
            {
                var lastIndex = segmentSize.x - 1;
                prevSandDepth = sandDepths[lastIndex];
                prevGrassDepth = grassDepths[lastIndex];
            }
        }

        public void ClearDecorAtPosition(Vector3 pos)
        {
            if (decorMap != null)
            {
                var cell = decorMap.WorldToCell(pos);
                decorMap.SetTile(cell, null);
                decorMap.SetTransformMatrix(cell, Matrix4x4.identity);
                if (decorLookup.TryGetValue(cell, out var tr) && tr != null)
                {
                    Blindsided.Utilities.Pooling.PoolManager.Release(tr.gameObject);
                    decorLookup.Remove(cell);
                }
            }

            // If no tilemap, try parent scope lookup via approximate cell from terrain
            else if (terrainMap != null)
            {
                var cell = terrainMap.WorldToCell(pos);
                if (decorLookup.TryGetValue(cell, out var tr) && tr != null)
                {
                    Blindsided.Utilities.Pooling.PoolManager.Release(tr.gameObject);
                    decorLookup.Remove(cell);
                }
            }
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

            // Batch tile writes per column to reduce SetTile overhead
            if (columnBuffer == null || columnBuffer.Length != segmentSize.y)
                columnBuffer = new TileBase[segmentSize.y];

            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);

                // Fill column buffer once per column
                for (var y = 0; y < segmentSize.y; y++)
                {
                    TileBase t = null;
                    if (y < waterDepth) t = BottomTile;
                    else if (y < waterDepth + sandDepth) t = MiddleTile;
                    else if (y < waterDepth + sandDepth + grassDepth) t = TopTile;
                    columnBuffer[y] = t;
                }

                var bounds = new BoundsInt(offset.x + x, offset.y, 0, 1, segmentSize.y, 1);
                terrainMap.SetTilesBlock(bounds, columnBuffer);

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

                if (settings == null)
                    continue;

                // Early density gate to avoid per-cell work when not placing
                if (settings.decor == null)
                    continue;
                var density = settings.decor.density;
                if (RandomRangeFloat(0f, 1f) > density)
                    continue;

                // Single-pass weighted reservoir sampling (non-alloc)
                // Each entry accepted with probability w/totalSoFar, giving uniform weighted distribution
                DecorEntry picked = null;
                var totalWeight = 0f;

                if (settings.decor.decor != null)
                {
                    foreach (var d in settings.decor.decor)
                    {
                        if (d == null) continue;
                        if (d.GetWeight(worldPos) <= 0f) continue;

                        var cfg = d.config;
                        if (cfg == null) continue;

                        var allowed = !cfg.borderOnly
                            ? !IsBufferedEdge(cell, baseTile, cfg.topBuffer, cfg.bottomBuffer, cfg.sideBuffer)
                            : IsBorderCell(cell, baseTile, cfg);
                        if (!allowed) continue;

                        var w = cfg.weight;
                        if (w <= 0f) continue;

                        totalWeight += w;
                        if (RandomRangeFloat(0f, totalWeight) < w)
                            picked = d;
                    }
                }

                if (picked == null)
                    continue;

                // Place prefab and/or tile for the picked entry
                if (picked.prefab != null)
                {
                    var center = terrainMap.GetCellCenterWorld(cell);
                    var instance = Blindsided.Utilities.Pooling.PoolManager.Get(picked.prefab);
                    instance.transform.SetParent(decorParent, false);
                    instance.transform.position = center;
                    instance.transform.rotation = Quaternion.identity;
                    // Track for direct clears later in this segment
                    decorLookup[cell] = instance.transform;
                    if (picked.randomFlipX && RandomRangeFloat(0f, 1f) < 0.5f)
                    {
                        var renderer = instance.GetComponentInChildren<SpriteRenderer>();
                        if (renderer != null)
                            renderer.flipX = true;
                    }
                }

                if (decorMap != null && picked.tile != null)
                {
                    decorMap.SetTile(cell, picked.tile);
                    if (picked.randomFlipX)
                    {
                        decorMap.SetTileFlags(cell, TileFlags.None);
                        var matrix = RandomRangeFloat(0f, 1f) < 0.5f
                            ? Matrix4x4.Scale(new Vector3(-1, 1, 1))
                            : Matrix4x4.identity;
                        decorMap.SetTransformMatrix(cell, matrix);
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
            var bottomRaw = config.bottomBorderOffset;
            var leftRaw = config.leftBorderOffset;
            var rightRaw = config.rightBorderOffset;

            // Negative offsets forbid cells that touch the corresponding edge
            if (topRaw < 0 && upDist == 0) return false;
            if (bottomRaw < 0 && downDist == 0) return false;
            if (leftRaw < 0 && leftDist == 0) return false;
            if (rightRaw < 0 && rightDist == 0) return false;

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
                if (dx == 0 || dy == 0) continue;
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
            if (!inCore || inInnerCore) return false;

            var upDist = CountSame(cell + Vector3Int.up, Vector3Int.up, tile);
            var downDist = CountSame(cell + Vector3Int.down, Vector3Int.down, tile);
            var leftDist = CountSame(cell + Vector3Int.left, Vector3Int.left, tile);
            var rightDist = CountSame(cell + Vector3Int.right, Vector3Int.right, tile);

            if (config.topBorderOffset < 0 && upDist == 0) return false;
            if (config.bottomBorderOffset < 0 && downDist == 0) return false;
            if (config.leftBorderOffset < 0 && leftDist == 0) return false;
            if (config.rightBorderOffset < 0 && rightDist == 0) return false;

            var touchesTop = config.topBorderOffset >= 0 &&
                             upDist == Mathf.Max(0, config.topBorderOffset);
            var touchesBottom = config.bottomBorderOffset >= 0 &&
                                downDist == Mathf.Max(0, config.bottomBorderOffset);
            var touchesLeft = config.leftBorderOffset >= 0 &&
                              leftDist == Mathf.Max(0, config.leftBorderOffset);
            var touchesRight = config.rightBorderOffset >= 0 &&
                               rightDist == Mathf.Max(0, config.rightBorderOffset);

            var sideCount = 0;
            if (touchesTop) sideCount++;
            if (touchesBottom) sideCount++;
            if (touchesLeft) sideCount++;
            if (touchesRight) sideCount++;

            return sideCount == 1;
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
            // Batch-clear tiles using SetTilesBlock for the terrain map
            if (terrainMap != null)
            {
                if (clearColumnBuffer == null || clearColumnBuffer.Length != segmentSize.y)
                    clearColumnBuffer = new TileBase[segmentSize.y]; // default nulls
                for (var x = 0; x < segmentSize.x; x++)
                {
                    var bounds = new BoundsInt(offset.x + x, offset.y, 0, 1, segmentSize.y, 1);
                    terrainMap.SetTilesBlock(bounds, clearColumnBuffer);
                }
            }

            // For decor map, also reset transform matrices to identity
            if (decorMap != null)
            {
                if (clearColumnBuffer == null || clearColumnBuffer.Length != segmentSize.y)
                    clearColumnBuffer = new TileBase[segmentSize.y];
                for (var x = 0; x < segmentSize.x; x++)
                {
                    var bounds = new BoundsInt(offset.x + x, offset.y, 0, 1, segmentSize.y, 1);
                    decorMap.SetTilesBlock(bounds, clearColumnBuffer);
                    for (var y = 0; y < segmentSize.y; y++)
                    {
                        var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                        decorMap.SetTransformMatrix(pos, Matrix4x4.identity);
                    }
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
