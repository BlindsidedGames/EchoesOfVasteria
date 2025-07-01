using System;
using System.Collections.Generic;
using Pathfinding;
using Sirenix.OdinInspector;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;
using VinTools.BetterRuleTiles;

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
            [HideInInspector]
            public Tilemap terrainMap;
            [FormerlySerializedAs("waterTile")]
            public BetterRuleTile waterBetterRuleTile;
            [FormerlySerializedAs("sandRuleTile")]
            public BetterRuleTile sandBetterRuleTile;
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
            public float density = 0.1f;

            public LayerMask blockingMask;
            [MinValue(0f)] public float otherTaskEdgeOffset = 1f;

            public List<ProceduralTaskGenerator.WeightedSpawn> enemies = new();
            public List<ProceduralTaskGenerator.WeightedSpawn> tasks = new();

            [MinValue(0f)] public float minTaskDistance = 1.5f;

        }

        [Serializable]
        public class SegmentedMapSettings
        {
            public Vector2Int segmentSize = new(64, 18);
            public Transform segmentParent;
            public AstarPath pathfinder;
        }
    }
}
