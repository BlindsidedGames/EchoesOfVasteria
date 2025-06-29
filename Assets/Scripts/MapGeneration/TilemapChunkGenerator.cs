using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class DecorativeTileEntry
    {
        [HorizontalGroup("Entry", 175, LabelWidth = 45)]
        public TileBase Tile;

        [HorizontalGroup("Entry")] [MinValue(1)]
        public int Weight = 1;

        [HorizontalGroup("Entry", Width = 160)] [LabelWidth(125)]
        public bool AllowRotation;
    }

    public class TilemapChunkGenerator : MonoBehaviour
    {
        [Header("Tilemaps")] [TabGroup("References")] [SerializeField]
        private Tilemap waterMap;

        [TabGroup("References")] [SerializeField]
        private Tilemap sandMap;

        [TabGroup("References")] [SerializeField]
        private Tilemap grassMap;

        [TabGroup("References")] [SerializeField]
        private Tilemap decorationMap;

        [Header("Tiles")] [TabGroup("References")] [SerializeField]
        private TileBase waterTile;

        [TabGroup("References")] [SerializeField]
        private TileBase sandRuleTile;

        [TabGroup("References")] [SerializeField]
        private TileBase grassRuleTile;

        [Header("Decorative Tiles")] [TabGroup("References")] [SerializeField]
        private DecorativeTileEntry[] waterDecorativeTiles;

        [TabGroup("References")] [SerializeField]
        private DecorativeTileEntry[] sandDecorativeTiles;

        [TabGroup("References")] [SerializeField]
        private DecorativeTileEntry[] grassDecorativeTiles;

        [Header("Decoration Density")] [TabGroup("Settings")] [SerializeField] [Range(0f, 1f)]
        private float waterDecorationDensity = 0.05f;

        [TabGroup("Settings")] [SerializeField] [Range(0f, 1f)]
        private float sandDecorationDensity = 0.05f;

        [TabGroup("Settings")] [SerializeField] [Range(0f, 1f)]
        private float grassDecorationDensity = 0.05f;

        [Header("Dimensions")] [TabGroup("Settings")] [SerializeField]
        private Vector2Int size = new(900, 18);

        [Header("Generation Settings")] [TabGroup("Settings")] [SerializeField] [Min(2)]
        private int minAreaWidth = 2;

        [TabGroup("Settings")] [SerializeField] [Min(0)]
        private int edgeWaviness = 1;

        [Header("Cutoff Settings")] [TabGroup("Settings")] [SerializeField] [Min(0)]
        private int grassCutoffWidth = 50;

        [TabGroup("Settings")] [SerializeField]
        private Vector2Int sandCutoffRange = new(3, 6);

        [Header("Depth Ranges (Min, Max)")] [TabGroup("Settings")] [SerializeField]
        private Vector2Int sandDepthRange = new(2, 6);

        [TabGroup("Settings")] [SerializeField]
        private Vector2Int grassDepthRange = new(2, 6);

        [Header("Random Seed")] [TabGroup("Settings")] [SerializeField]
        private int seed;

        [TabGroup("Settings")] [SerializeField]
        private bool randomizeSeed = true;

        private Random rng;
        public Tilemap WaterMap => waterMap;
        public Tilemap SandMap => sandMap;
        public Tilemap GrassMap => grassMap;
        public Tilemap DecorationMap => decorationMap;

        private void Awake()
        {
            rng = randomizeSeed ? new Random() : new Random(seed);
        }

        [ContextMenu("Generate Chunk")]
        [Button]
        public void Generate()
        {
            ClearMaps();
            rng = randomizeSeed ? new Random() : new Random(seed);

            GenerateInternal(Vector2Int.zero, size);
        }

        public void GenerateSegment(Vector2Int offset, Vector2Int segmentSize)
        {
            rng = randomizeSeed ? new Random() : new Random(seed);

            GenerateInternal(offset, segmentSize);
        }

        private void GenerateInternal(Vector2Int offset, Vector2Int segmentSize)
        {
            var sandDepths = new int[segmentSize.x];
            var grassDepths = new int[segmentSize.x];

            var currentSandDepth = RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
            var currentGrassDepth = RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

            var grassCutoffStart = Mathf.Max(0, segmentSize.x - grassCutoffWidth);
            var sandCutoffStart = Mathf.Max(0, segmentSize.x - RandomRange(sandCutoffRange.x, sandCutoffRange.y + 1));

            for (var x = 0; x < segmentSize.x;)
            {
                for (var segX = 0; segX < minAreaWidth && x < segmentSize.x; segX++, x++)
                {
                    var sandDepth = currentSandDepth;
                    var grassDepth = currentGrassDepth;

                    if (x >= sandCutoffStart)
                    {
                        sandDepth = 0;
                        grassDepth = 0;
                    }
                    else if (x >= grassCutoffStart)
                    {
                        grassDepth = 0;
                    }

                    sandDepths[x] = sandDepth;
                    grassDepths[x] = grassDepth;
                }

                var sandDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                var grassDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);

                currentSandDepth = Mathf.Clamp(currentSandDepth + sandDelta, sandDepthRange.x, sandDepthRange.y);
                currentGrassDepth = Mathf.Clamp(currentGrassDepth + grassDelta, grassDepthRange.x, grassDepthRange.y);

                if (currentSandDepth + currentGrassDepth > segmentSize.y)
                    currentGrassDepth = Mathf.Clamp(segmentSize.y - currentSandDepth, grassDepthRange.x, grassDepthRange.y);
            }

            for (var x = 0; x < segmentSize.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, segmentSize.y - sandDepth - grassDepth);

                for (var y = 0; y < waterDepth; y++)
                    waterMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), waterTile);

                for (var y = waterDepth; y < segmentSize.y; y++)
                    sandMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), sandRuleTile);

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                    if (y < segmentSize.y)
                        grassMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), grassRuleTile);

                for (var y = 0; y < waterDepth; y++)
                {
                    var isEdge = y == 0 || y == waterDepth - 1;
                    if (!isEdge && waterDecorativeTiles != null && waterDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < waterDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(offset.x + x, offset.y + y, 0), waterDecorativeTiles);
                }

                for (var y = waterDepth + 1; y < waterDepth + sandDepth; y++)
                {
                    var leftWaterLvl = x > 0 ? segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1] : waterDepth;
                    var rightWaterLvl = x < segmentSize.x - 1 ? segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1] : waterDepth;

                    // Check if the current tile itself is a side edge
                    var isCurrentTileSideEdge = y < leftWaterLvl || y < rightWaterLvl;

                    // Check if the tile directly below the current one is an edge.
                    // An edge tile is one that is either on the ground level or is a side edge.
                    var isTileBelowGroundLevel = y - 1 == waterDepth;
                    var isTileBelowSideEdge = y - 1 < leftWaterLvl || y - 1 < rightWaterLvl;
                    var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowSideEdge;

                    // If the current tile is a side edge, OR if it's sitting on top of an edge tile, skip it.
                    if (isCurrentTileSideEdge || isTileBelowEdge) continue;

                    // Standard check to prevent decor from being placed under grass
                    var isTopmostSandLayer = y == waterDepth + sandDepth - 1;
                    var isGrassAbove = grassDepth > 0;
                    var canSpawn = !isTopmostSandLayer || !isGrassAbove;

                    if (canSpawn && sandDecorativeTiles != null &&
                        sandDecorativeTiles.Length > 0 && rng.NextDouble() < sandDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(offset.x + x, offset.y + y, 0), sandDecorativeTiles);
                }

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                {
                    var isGrassGroundLevel = y == waterDepth + sandDepth;
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;

                    if (!isGrassGroundLevel && !isTopEdge && grassDecorativeTiles != null &&
                        grassDecorativeTiles.Length > 0 && rng.NextDouble() < grassDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(offset.x + x, offset.y + y, 0), grassDecorativeTiles);
                }
            }
        }

        private void PlaceDecorativeTile(Vector3Int position, DecorativeTileEntry[] decorations)
        {
            var entry = GetWeightedRandomEntry(decorations);
            if (entry == null || entry.Tile == null) return;

            decorationMap.SetTile(position, entry.Tile);

            if (entry.AllowRotation)
            {
                var rotationAngle = rng.Next(0, 4) * 90f;
                var pivot = new Vector3(0.5f, 0.5f, 0);
                var matrix = Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, rotationAngle), Vector3.one) *
                             Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                decorationMap.SetTransformMatrix(position, matrix);
            }
        }

        private DecorativeTileEntry GetWeightedRandomEntry(DecorativeTileEntry[] decorations)
        {
            if (decorations == null || decorations.Length == 0) return null;

            var totalWeight = decorations.Sum(entry => entry.Weight);
            if (totalWeight <= 0) return null;

            var randomValue = rng.Next(0, totalWeight);

            foreach (var entry in decorations)
            {
                if (randomValue < entry.Weight) return entry;
                randomValue -= entry.Weight;
            }

            return null;
        }

        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new Random() : new Random(seed);

            return rng.Next(minInclusive, maxExclusive);
        }

        private void ClearMaps()
        {
            waterMap.ClearAllTiles();
            sandMap.ClearAllTiles();
            grassMap.ClearAllTiles();
            decorationMap.ClearAllTiles();
        }

        public void ClearSegment(Vector2Int offset, Vector2Int segmentSize)
        {
            for (var x = 0; x < segmentSize.x; x++)
            for (var y = 0; y < segmentSize.y; y++)
            {
                var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                waterMap.SetTile(pos, null);
                sandMap.SetTile(pos, null);
                grassMap.SetTile(pos, null);
                decorationMap.SetTile(pos, null);
            }
        }

        public void Clear()
        {
            ClearMaps();
        }
    }
}