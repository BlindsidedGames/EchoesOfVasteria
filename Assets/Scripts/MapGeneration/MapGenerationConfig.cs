using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.MapGeneration
{
    [CreateAssetMenu(menuName = "SO/Map Generation Config")]
    public class MapGenerationConfig : ScriptableObject
    {
        public TilemapChunkSettings tilemapChunkSettings = new();
        public ProceduralTaskSettings taskGeneratorSettings = new();
        public SegmentedMapSettings segmentedMapSettings = new();

        [Serializable]
        public class TilemapChunkSettings
        {
            [HideInInspector] public Tilemap terrainMap;
            [HideInInspector] public Tilemap decorMap;

            public TerrainSettings bottomTerrain;
            public TerrainSettings middleTerrain;
            public TerrainSettings topTerrain;

            [Min(2)] public int minAreaWidth = 2;
            [Min(0)] public int edgeWaviness = 1;

            public Vector2Int middleDepthRange = new(2, 6);
            public Vector2Int topDepthRange = new(2, 6);

            public int seed;
            public bool randomizeSeed = true;
        }

        [Serializable]
        public class ProceduralTaskSettings
        {
            public float minX;
            public float height = 18f;
            [FormerlySerializedAs("density")] public float taskDensity = 0.1f;

            public float enemyDensity = 0.1f;

            public LayerMask blockingMask;
            [MinValue(0f)] public float otherTaskEdgeOffset = 1f;

            public List<ProceduralTaskGenerator.WeightedSpawn> enemies = new();

            [Header("Woodcutting")] public ProceduralTaskGenerator.WeightedTaskCategory woodcutting = new();
            [Header("Mining")] public ProceduralTaskGenerator.WeightedTaskCategory mining = new();
            [Header("Farming")] public ProceduralTaskGenerator.WeightedTaskCategory farming = new();
            [Header("Fishing")] public ProceduralTaskGenerator.WeightedTaskCategory fishing = new();
            [Header("Looting")] public ProceduralTaskGenerator.WeightedTaskCategory looting = new();

            public List<NpcSpawnEntry> npcTasks = new();

            [MinValue(0f)] public float minTaskDistance = 1.5f;

            [Serializable]
            [InlineProperty]
            [HideLabel]
            public class NpcSpawnEntry
            {
                [Required] public GameObject prefab;
                public string id;
                public float localX;
                [MinValue(0)] public int topBuffer;
                // Terrains where this NPC is allowed to spawn.
                public List<TerrainSettings> spawnTerrains = new();
                public bool spawnOnlyOnce = true;
            }
        }

        [Serializable]
        public class SegmentedMapSettings
        {
            public Vector2Int segmentSize = new(64, 18);
        }

    }
}