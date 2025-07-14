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
        public DecorSettings decorSettings = new();

        [Serializable]
        public class TilemapChunkSettings
        {
            [HideInInspector] public Tilemap terrainMap;
            [FormerlySerializedAs("waterTile")] public BetterRuleTile waterBetterRuleTile;
            [FormerlySerializedAs("sandRuleTile")] public BetterRuleTile sandBetterRuleTile;

            [FormerlySerializedAs("grassRuleTile")]
            public BetterRuleTile grassBetterRuleTile;

            [Min(2)] public int minAreaWidth = 2;
            [Min(0)] public int edgeWaviness = 1;

            public Vector2Int sandDepthRange = new(2, 6);
            public Vector2Int grassDepthRange = new(2, 6);

            public int seed;
            public bool randomizeSeed = true;
        }

        [Serializable]
        public class ProceduralTaskSettings
        {
            public float minX;
            public float height = 18f;
            [FormerlySerializedAs("density")] public float taskDensity = 0.1f;
            public float waterTaskDensity = 0.1f;
            public float sandTaskDensity = 0.1f;
            public float grassTaskDensity = 0.1f;

            public float enemyDensity = 0.1f;

            public LayerMask blockingMask;
            [MinValue(0f)] public float otherTaskEdgeOffset = 1f;

            public List<ProceduralTaskGenerator.WeightedSpawn> enemies = new();
            public List<ProceduralTaskGenerator.WeightedSpawn> tasks = new();
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
                [MinValue(0)] public int topBuffer = 0;
                public bool spawnOnWater;
                public bool spawnOnSand;
                public bool spawnOnGrass = true;
                public bool spawnOnlyOnce = true;
            }
        }

        [Serializable]
        public class SegmentedMapSettings
        {
            public Vector2Int segmentSize = new(64, 18);
        }

        [Serializable]
        public class DecorSettings
        {
            [HideInInspector] [TabGroup("Decor", "References")]
            public Tilemap decorMap;

            [Range(0f, 1f)] public float density = 1f;

            [Title("Decor")]
            [Button("Update All Names")]
            [PropertyOrder(-1)]
            private void UpdateAllNames()
            {
                if (decor == null) return;
                foreach (var entry in decor) entry.UpdateName();
                TELogger.Log("Decor entry names updated for search.", TELogCategory.Map);
            }

            [Searchable] [ListDrawerSettings(ListElementLabelName = "Name", ShowFoldout = false)]
            public List<DecorEntry> decor = new();
        }
    }
}