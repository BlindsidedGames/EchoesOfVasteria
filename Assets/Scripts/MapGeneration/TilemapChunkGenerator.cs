using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;
using VinTools.BetterRuleTiles;
using TimelessEchoes.Tasks;
using Random = System.Random;

namespace TimelessEchoes.MapGeneration
{
    public class TilemapChunkGenerator : MonoBehaviour
    {
        [SerializeField]
        private MapGenerationConfig config;

        [Header("Tilemaps")] [TabGroup("References")] [SerializeField]
        private Tilemap terrainMap;


        [Header("Tiles")] [TabGroup("References")] [SerializeField]
        [FormerlySerializedAs("waterTile")]
        [HideInInspector]
        private BetterRuleTile waterBetterRuleTile;

        [TabGroup("References")] [SerializeField]
        [FormerlySerializedAs("sandRuleTile")]
        [HideInInspector]
        private BetterRuleTile sandBetterRuleTile;

        [TabGroup("References")] [SerializeField]
        [FormerlySerializedAs("grassRuleTile")]
        [HideInInspector]
        private BetterRuleTile grassBetterRuleTile;


        [Header("Generation Settings")] [TabGroup("Settings")] [SerializeField] [Min(2)]
        [HideInInspector]
        private int minAreaWidth = 2;

        [TabGroup("Settings")] [SerializeField] [Min(0)]
        [HideInInspector]
        private int edgeWaviness = 1;


        [Header("Depth Ranges (Min, Max)")] [TabGroup("Settings")] [SerializeField]
        [HideInInspector]
        private Vector2Int sandDepthRange = new(2, 6);

        [TabGroup("Settings")] [SerializeField]
        [HideInInspector]
        private Vector2Int grassDepthRange = new(2, 6);

        [Header("Random Seed")] [TabGroup("Settings")] [SerializeField]
        [HideInInspector]
        private int seed;

        [TabGroup("Settings")] [SerializeField]
        [HideInInspector]
        private bool randomizeSeed = true;

        private Random rng;
        private int prevSandDepth = -1;
        private int prevGrassDepth = -1;
        public Tilemap TerrainMap => terrainMap;
        public BetterRuleTile WaterBetterRuleTile => waterBetterRuleTile;
        public BetterRuleTile SandBetterRuleTile => sandBetterRuleTile;
        public BetterRuleTile GrassBetterRuleTile => grassBetterRuleTile;

        private void Awake()
        {
            ApplyConfig();
            var taskGen = GetComponent<Tasks.ProceduralTaskGenerator>();
            AssignTilemaps(taskGen);
            rng = randomizeSeed ? new Random() : new Random(seed);
            prevSandDepth = -1;
            prevGrassDepth = -1;
        }

        private void ApplyConfig()
        {
            if (config == null) return;

            if (terrainMap == null)
                terrainMap = config.tilemapChunkSettings.terrainMap;
            if (waterBetterRuleTile == null)
                waterBetterRuleTile = config.tilemapChunkSettings.waterBetterRuleTile;
            if (sandBetterRuleTile == null)
                sandBetterRuleTile = config.tilemapChunkSettings.sandBetterRuleTile;
            if (grassBetterRuleTile == null)
                grassBetterRuleTile = config.tilemapChunkSettings.grassBetterRuleTile;
            minAreaWidth = config.tilemapChunkSettings.minAreaWidth;
            edgeWaviness = config.tilemapChunkSettings.edgeWaviness;
            sandDepthRange = config.tilemapChunkSettings.sandDepthRange;
            grassDepthRange = config.tilemapChunkSettings.grassDepthRange;
            seed = config.tilemapChunkSettings.seed;
            randomizeSeed = config.tilemapChunkSettings.randomizeSeed;
        }

        public void AssignTilemaps(Tasks.ProceduralTaskGenerator generator)
        {
            if (generator == null)
                return;

            generator.SetTilemapReferences(terrainMap, waterBetterRuleTile, sandBetterRuleTile, grassBetterRuleTile);
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
                    terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), waterBetterRuleTile);

                for (var y = waterDepth; y < waterDepth + sandDepth; y++)
                    terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), sandBetterRuleTile);

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                    if (y < segmentSize.y)
                        terrainMap.SetTile(new Vector3Int(offset.x + x, offset.y + y, 0), grassBetterRuleTile);

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
                    if (terrainMap.GetTile(pos) == grassBetterRuleTile) continue;

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
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;
                    var isTileBelowSideEdge = y - 1 < leftGrassBottom || y - 1 < rightGrassBottom;
                    var isTileBelowEdge = isTileBelowSideEdge;

                    if (isCurrentTileSideEdge || isTileBelowEdge || isTopEdge) continue;

                }
            }

            if (segmentSize.x > 0)
            {
                var lastIndex = segmentSize.x - 1;
                prevSandDepth = sandDepths[lastIndex];
                prevGrassDepth = grassDepths[lastIndex];
            }
        }

        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new Random() : new Random(seed);

            return rng.Next(minInclusive, maxExclusive);
        }

        private void ClearMaps()
        {
            terrainMap.ClearAllTiles();
        }

        public void ClearSegment(Vector2Int offset, Vector2Int segmentSize)
        {
            for (var x = 0; x < segmentSize.x; x++)
            for (var y = 0; y < segmentSize.y; y++)
            {
                var pos = new Vector3Int(offset.x + x, offset.y + y, 0);
                terrainMap.SetTile(pos, null);
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