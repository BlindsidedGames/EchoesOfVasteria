using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    ///     Procedurally generates a tilemap chunk with water, sand and grass sections.
    ///     Thickness of sand and grass varies along the width of the chunk.
    /// </summary>
    public class TilemapChunkGenerator : MonoBehaviour
    {
        [Header("Tilemaps")]
        [SerializeField] private Tilemap waterMap;
        [SerializeField] private Tilemap sandMap;
        [SerializeField] private Tilemap grassMap;

        [Header("Tiles")]
        [SerializeField] private TileBase waterTile;
        [SerializeField] private TileBase sandRuleTile;
        [SerializeField] private TileBase grassRuleTile;

        [Header("Dimensions")]
        [SerializeField] private Vector2Int size = new Vector2Int(900, 18);

        [Header("Generation Settings")]
        [SerializeField, Min(2)] private int minAreaWidth = 2;
        [SerializeField, Min(0)] private int edgeWaviness = 1;
        [SerializeField, Min(0)] private int islandWidth = 50;

        [Header("Depth Ranges (Min, Max)")]
        [SerializeField] private Vector2Int sandDepthRange = new Vector2Int(2, 6);
        [SerializeField] private Vector2Int grassDepthRange = new Vector2Int(2, 6);

        [Header("Random Seed")]
        [SerializeField] private int seed = 0;
        [SerializeField] private bool randomizeSeed = true;

        private System.Random rng;

        private void Awake()
        {
            rng = randomizeSeed ? new System.Random() : new System.Random(seed);
        }

        [ContextMenu("Generate Chunk")]
        [Button]
        public void Generate()
        {
            ClearMaps();

            int currentSandDepth = RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
            int currentGrassDepth = RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

            int islandStart = Mathf.Max(0, size.x - islandWidth);
            for (int x = 0; x < islandStart; )
            {
                for (int segX = 0; segX < minAreaWidth && x < islandStart; segX++, x++)
                {
                    PlaceColumn(x, currentSandDepth, currentGrassDepth);
                }

                int sandDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                int grassDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);

                currentSandDepth = Mathf.Clamp(currentSandDepth + sandDelta, sandDepthRange.x, sandDepthRange.y);
                currentGrassDepth = Mathf.Clamp(currentGrassDepth + grassDelta, grassDepthRange.x, grassDepthRange.y);

                if (currentSandDepth + currentGrassDepth > size.y)
                {
                    currentGrassDepth = Mathf.Clamp(size.y - currentSandDepth, grassDepthRange.x, grassDepthRange.y);
                }
            }

            GenerateIsland(islandStart, size.x - islandStart);
        }

        private void PlaceColumn(int x, int sandDepth, int grassDepth)
        {
            sandDepth = Mathf.Clamp(sandDepth, sandDepthRange.x, sandDepthRange.y);
            grassDepth = Mathf.Clamp(grassDepth, grassDepthRange.x, grassDepthRange.y);

            int waterDepth = size.y - sandDepth - grassDepth;
            if (waterDepth < 0)
                waterDepth = 0;

            for (int y = 0; y < waterDepth; y++)
                waterMap.SetTile(new Vector3Int(x, y, 0), waterTile);

            // Fill the sand map all the way to the top of the chunk
            for (int y = waterDepth; y < size.y; y++)
                sandMap.SetTile(new Vector3Int(x, y, 0), sandRuleTile);

            for (int y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth && y < size.y; y++)
                grassMap.SetTile(new Vector3Int(x, y, 0), grassRuleTile);
        }

        private void GenerateIsland(int startX, int width)
        {
            for (int x = startX; x < startX + width && x < size.x; x++)
            {
                float halfWidth = width / 2f;
                float dx = (x - startX) - halfWidth;
                float t = Mathf.Abs(dx) / (halfWidth > 0 ? halfWidth : 1f);
                int sandHeight = Mathf.RoundToInt((1f - t * t) * (size.y - 1));
                sandHeight = Mathf.Clamp(sandHeight, 0, size.y);

                int waterDepth = size.y - sandHeight;
                for (int y = 0; y < waterDepth; y++)
                    waterMap.SetTile(new Vector3Int(x, y, 0), waterTile);
                for (int y = waterDepth; y < size.y; y++)
                    sandMap.SetTile(new Vector3Int(x, y, 0), sandRuleTile);
            }
        }

        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new System.Random() : new System.Random(seed);

            return rng.Next(minInclusive, maxExclusive);
        }

        private void ClearMaps()
        {
            waterMap.ClearAllTiles();
            sandMap.ClearAllTiles();
            grassMap.ClearAllTiles();
        }
    }
}
