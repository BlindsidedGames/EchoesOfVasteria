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
        [TitleGroup("General")]
        public List<ResourceDrop> resourceDrops = new();

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
