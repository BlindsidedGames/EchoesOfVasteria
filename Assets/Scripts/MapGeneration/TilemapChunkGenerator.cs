using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;
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
        private BetterRuleTile waterTile;

        [TabGroup("References")] [SerializeField]
        private BetterRuleTile sandRuleTile;

        [TabGroup("References")] [SerializeField]
        private BetterRuleTile grassRuleTile;

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

        [Header("Generation Settings")] [TabGroup("Settings")] [SerializeField] [Min(2)]
        private int minAreaWidth = 2;

        [TabGroup("Settings")] [SerializeField] [Min(0)]
        private int edgeWaviness = 1;


        [Header("Depth Ranges (Min, Max)")] [TabGroup("Settings")] [SerializeField]
        private Vector2Int sandDepthRange = new(2, 6);

        [TabGroup("Settings")] [SerializeField]
        private Vector2Int grassDepthRange = new(2, 6);

        [Header("Random Seed")] [TabGroup("Settings")] [SerializeField]
        private int seed;

        [TabGroup("Settings")] [SerializeField]
        private bool randomizeSeed = true;

        private Random rng;
        private int prevSandDepth = -1;
        private int prevGrassDepth = -1;
        public Tilemap WaterMap => waterMap;
        public Tilemap SandMap => sandMap;
        public Tilemap GrassMap => grassMap;
        public Tilemap DecorationMap => decorationMap;

        private void Awake()
        {
            rng = randomizeSeed ? new Random() : new Random(seed);
            prevSandDepth = -1;
            prevGrassDepth = -1;
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
                    var leftWaterBottom = x > 0
                        ? segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1]
                        : waterDepth;
                    var rightWaterBottom = x < segmentSize.x - 1
                        ? segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1]
                        : waterDepth;

                    var isCurrentTileSideEdge = y < leftWaterBottom || y < rightWaterBottom;
                    var isWaterGroundLevel = y == 0;
                    var isTopEdge = y == waterDepth - 1;
                    var isTileBelowGroundLevel = y - 1 == 0;
                    var isTileBelowSideEdge = y - 1 < leftWaterBottom || y - 1 < rightWaterBottom;
                    var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowSideEdge;

                    if (isCurrentTileSideEdge || isTileBelowEdge || isWaterGroundLevel || isTopEdge) continue;

                    if (waterDecorativeTiles != null && waterDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < waterDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(offset.x + x, offset.y + y, 0), waterDecorativeTiles);
                }

                for (var y = waterDepth + 1; y < waterDepth + sandDepth; y++)
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
                    var isSandGroundLevel = y == waterDepth;
                    var isTopEdge = y == waterDepth + sandDepth - 1;
                    var isTileBelowGroundLevel = y - 1 == waterDepth;
                    var isTileBelowWaterEdge = y - 1 < leftWaterBottom || y - 1 < rightWaterBottom;
                    var isTileBelowGrassEdge = y - 1 >= leftGrassBottom || y - 1 >= rightGrassBottom;
                    var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowWaterEdge || isTileBelowGrassEdge;

                    if (isCurrentTileWaterEdge || isCurrentTileGrassEdge || isTileBelowEdge || isSandGroundLevel || isTopEdge)
                        continue;

                    var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                    if (grassMap.GetTile(pos) != null) continue;

                    if (sandDecorativeTiles != null && sandDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < sandDecorationDensity)
                        PlaceDecorativeTile(pos, sandDecorativeTiles);
                }

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                {
                    var leftGrassBottom = x > 0
                        ? (segmentSize.y - sandDepths[x - 1] - grassDepths[x - 1]) + sandDepths[x - 1]
                        : waterDepth + sandDepth;
                    var rightGrassBottom = x < segmentSize.x - 1
                        ? (segmentSize.y - sandDepths[x + 1] - grassDepths[x + 1]) + sandDepths[x + 1]
                        : waterDepth + sandDepth;

                    var isCurrentTileSideEdge = y < leftGrassBottom || y < rightGrassBottom;
                    var isGrassGroundLevel = y == waterDepth + sandDepth;
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;
                    var isTileBelowGroundLevel = y - 1 == waterDepth + sandDepth;
                    var isTileBelowSideEdge = y - 1 < leftGrassBottom || y - 1 < rightGrassBottom;
                    var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowSideEdge;

                    if (isCurrentTileSideEdge || isTileBelowEdge || isGrassGroundLevel || isTopEdge) continue;

                    if (grassDecorativeTiles != null && grassDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < grassDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(offset.x + x, offset.y + y, 0), grassDecorativeTiles);
                }
            }

            if (segmentSize.x > 0)
            {
                var lastIndex = segmentSize.x - 1;
                prevSandDepth = sandDepths[lastIndex];
                prevGrassDepth = grassDepths[lastIndex];
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
            prevSandDepth = -1;
            prevGrassDepth = -1;
        }
    }
}