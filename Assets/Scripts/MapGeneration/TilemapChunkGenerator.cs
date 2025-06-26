using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    ///     Procedurally generates a tilemap chunk with water, sand and grass sections.
    ///     Thickness of sand and grass varies along the width of the chunk.
    ///     The cutoff points for both sand and grass can be configured.
    /// </summary>
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
        private TileBase[] waterDecorativeTiles;

        [TabGroup("References")] [SerializeField]
        private TileBase[] sandDecorativeTiles;

        [TabGroup("References")] [SerializeField]
        private TileBase[] grassDecorativeTiles;

        [TabGroup("Settings")] [SerializeField] [Range(0f, 1f)]
        private float decorativeSpawnChance = 0.05f;

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

            var sandDepths = new int[size.x];
            var grassDepths = new int[size.x];

            var currentSandDepth = RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
            var currentGrassDepth = RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

            var grassCutoffStart = Mathf.Max(0, size.x - grassCutoffWidth);
            var sandCutoffStart = Mathf.Max(0, size.x - RandomRange(sandCutoffRange.x, sandCutoffRange.y + 1));

            for (var x = 0; x < size.x;)
            {
                for (var segX = 0; segX < minAreaWidth && x < size.x; segX++, x++)
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

                if (currentSandDepth + currentGrassDepth > size.y)
                    currentGrassDepth = Mathf.Clamp(size.y - currentSandDepth, grassDepthRange.x, grassDepthRange.y);
            }

            for (var x = 0; x < size.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, size.y - sandDepth - grassDepth);

                for (var y = 0; y < waterDepth; y++) waterMap.SetTile(new Vector3Int(x, y, 0), waterTile);

                for (var y = waterDepth; y < size.y; y++) sandMap.SetTile(new Vector3Int(x, y, 0), sandRuleTile);

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                    if (y < size.y)
                        grassMap.SetTile(new Vector3Int(x, y, 0), grassRuleTile);

                for (var y = 0; y < waterDepth; y++)
                {
                    var isEdge = y == 0 || y == waterDepth - 1;
                    if (!isEdge && waterDecorativeTiles != null && waterDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < decorativeSpawnChance)
                        decorationMap.SetTile(new Vector3Int(x, y, 0),
                            waterDecorativeTiles[RandomRange(0, waterDecorativeTiles.Length)]);
                }

                for (var y = waterDepth; y < waterDepth + sandDepth; y++)
                {
                    var isGroundLevel = y == waterDepth;

                    var leftWaterLvl = x > 0 ? size.y - sandDepths[x - 1] - grassDepths[x - 1] : waterDepth;
                    var rightWaterLvl = x < size.x - 1 ? size.y - sandDepths[x + 1] - grassDepths[x + 1] : waterDepth;

                    var isSideEdge = y < leftWaterLvl || y < rightWaterLvl;

                    if (!isGroundLevel && !isSideEdge && sandDecorativeTiles != null &&
                        sandDecorativeTiles.Length > 0 && rng.NextDouble() < decorativeSpawnChance)
                        decorationMap.SetTile(new Vector3Int(x, y, 0),
                            sandDecorativeTiles[RandomRange(0, sandDecorativeTiles.Length)]);
                }

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                {
                    var isGrassGroundLevel = y == waterDepth + sandDepth;
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;

                    if (!isGrassGroundLevel && !isTopEdge && grassDecorativeTiles != null &&
                        grassDecorativeTiles.Length > 0 && rng.NextDouble() < decorativeSpawnChance)
                        decorationMap.SetTile(new Vector3Int(x, y, 0),
                            grassDecorativeTiles[RandomRange(0, grassDecorativeTiles.Length)]);
                }
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
            waterMap.ClearAllTiles();
            sandMap.ClearAllTiles();
            grassMap.ClearAllTiles();
            decorationMap.ClearAllTiles();
        }

        /// <summary>
        ///     Remove all tiles from the chunk's tilemaps.
        /// </summary>
        public void Clear()
        {
            ClearMaps();
        }
    }
}