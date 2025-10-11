using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Blindsided.Utilities;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;

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
        [TitleGroup("General")]
        [Tooltip("Skill progression that unlocks this task. Falls back to Associated Skill when unset.")]
        [SerializeField] private Skill unlockSkill;
        [TitleGroup("General")]
        [MinValue(1)]
        [Tooltip("Required level in the unlock skill before any milestone can grant this task.")]
        [SerializeField] private int unlockSkillLevel = 1;
        [TitleGroup("Spawn Range")]
        [LabelWidth(70)]
        [MinValue(0f)]
        public float minX;
        [TitleGroup("Spawn Range")]
        public float maxX = float.PositiveInfinity;
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
        [SerializeField] private float weight = 1f;

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

        public Skill UnlockSkill => unlockSkill != null ? unlockSkill : associatedSkill;
        public int UnlockSkillLevel => Mathf.Max(1, unlockSkillLevel);
        public float BaseWeight => Mathf.Max(0f, weight);

        public float GetWeight(float worldX)
        {
            var controller = SkillController.Instance;
            var aggregator = controller?.Aggregator;
            if (aggregator == null)
                return 0f;

            var unlockWeight = aggregator.GetTaskWeight(this);
            if (unlockWeight <= 0f)
                return 0f;

            if (worldX < minX)
                return 0f;
            if (worldX > maxX)
                return unlockWeight * 0.1f;
            return unlockWeight;
        }
    }
}
