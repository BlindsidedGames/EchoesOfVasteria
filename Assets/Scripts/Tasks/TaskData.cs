using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using TimelessEchoes.MapGeneration;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    [ManageableData]
    [CreateAssetMenu(fileName = "TaskData", menuName = "SO/Task Data")]
    public class TaskData : ScriptableObject, IWeighted
    {
        [TitleGroup("General")]
        public string taskName;
        [TitleGroup("General")]
        public int taskID;
        [TitleGroup("General")]
        [PreviewField(60, ObjectFieldAlignment.Left)]
        public Sprite taskIcon;
        [TitleGroup("General")]
        public Skill associatedSkill;
        [TitleGroup("General")]
        public float xpForCompletion;
        [TitleGroup("Spawn Range")]
        [LabelWidth(70)]
        [MinValue(0f)]
        public float minX;
        [TitleGroup("Spawn Range")]
        [LabelText("Enforce Min Distance")]
        public bool enforceMinDistance;
        [TitleGroup("Spawn Range")]
        public float maxX = float.PositiveInfinity;
        [TitleGroup("Spawn Range")]
        [LabelText("Enforce Max Distance")]
        public bool enforceMaxDistance;
        [TitleGroup("Spawn Range")]
        [Tooltip("Skill level required for this task (0 = no requirement)")]
        public int requiredSkillLevel;
        [TitleGroup("General")]
        public float taskDuration;
        [TitleGroup("General")]
        [Tooltip("Interval between repeated SFX plays while the task is active. Zero disables repeats.")]
        public float sfxInterval;

        [TitleGroup("General")]
        [Required]
        public BaseTask taskPrefab;

        [TitleGroup("General")]
        [MinValue(0)]
        public float weight = 1f;

        // Terrains this task may spawn on.
        [TitleGroup("General")]
        public List<TerrainSettings> spawnTerrains = new();
        [System.Serializable]
        public class TerrainWeight
        {
            public TerrainSettings terrain;
            [MinValue(0)] public float multiplier = 1;
        }
        [TitleGroup("General")]
        [Tooltip("Optional relative weights by available terrain. Empty retains existing spawn weights.")]
        public List<TerrainWeight> terrainWeights = new();

        public float GetTerrainMultiplier(params TerrainSettings[] available)
        {
            if (terrainWeights == null || terrainWeights.Count == 0) return 1;
            float result = 0;
            foreach (var terrain in available)
            {
                if (terrain == null || (spawnTerrains.Count > 0 && !spawnTerrains.Contains(terrain))) continue;
                var multiplier = 1f;
                foreach (var entry in terrainWeights)
                    if (entry != null && entry.terrain == terrain) { multiplier = Mathf.Max(0, entry.multiplier); break; }
                result = Mathf.Max(result, multiplier);
            }
            return result;
        }
        [TitleGroup("General")]
        public List<ResourceDrop> resourceDrops = new();
        [System.Serializable]
        public class BonusDrop
        {
            public Resource resource;
            [MinValue(0f), MaxValue(1f)] public float chance;
            public Vector2Int range = new(1, 1);
        }
        [TitleGroup("General")]
        [Tooltip("Independent fixed-quantity propagation drops. No gathering/tier/buff multiplication.")]
        public List<BonusDrop> bonusDrops = new();

        [TitleGroup("General")]
        [Tooltip("Chance (0-1) for each additional drop slot; evaluated sequentially after the first guaranteed slot.")]
        [MinValue(0f), MaxValue(1f)]
        public List<float> additionalLootChances = new();

        [TitleGroup("General")]
        [Tooltip("Restart task progress when returning after an interrupt.")]
        public bool resetProgressOnInterrupt;

        [System.Serializable]
        public class Persistent
        {
            public int totalTimesCompleted;
            public float timeSpent;
            public float experienceGained;
        }

        [HideInInspector]
        public Persistent persistent = new();

        public float GetWeight(float worldX)
        {
            return TaskWeightService.GetEffectiveWeight(this, worldX);
        }

        public float GetEffectiveMinX()
        {
            return enforceMinDistance ? minX : 10f;
        }

        public float GetEffectiveMaxX()
        {
            return enforceMaxDistance ? maxX : float.PositiveInfinity;
        }
    }
}
