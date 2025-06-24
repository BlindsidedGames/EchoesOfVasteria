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

            for (int x = 0; x < size.x; x++)
            {
                int sandDepth = RandomRange(sandDepthRange.x, sandDepthRange.y + 1);
                int grassDepth = RandomRange(grassDepthRange.x, grassDepthRange.y + 1);

                // Ensure total does not exceed height
                int maxDepth = Math.Min(sandDepth + grassDepth, size.y);
                sandDepth = Math.Min(sandDepth, size.y);
                grassDepth = Math.Min(grassDepth, size.y - sandDepth);

                int waterDepth = size.y - sandDepth - grassDepth;
                if (waterDepth < 0)
                    waterDepth = 0;

                // Water tiles
                for (int y = 0; y < waterDepth; y++)
                    waterMap.SetTile(new Vector3Int(x, y, 0), waterTile);

                // Sand tiles
                for (int y = waterDepth; y < waterDepth + sandDepth && y < size.y; y++)
                    sandMap.SetTile(new Vector3Int(x, y, 0), sandRuleTile);

                // Grass tiles
                for (int y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth && y < size.y; y++)
                    grassMap.SetTile(new Vector3Int(x, y, 0), grassRuleTile);
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
