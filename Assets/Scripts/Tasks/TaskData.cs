using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Quests;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    [ManageableData]
    [CreateAssetMenu(fileName = "TaskData", menuName = "SO/Task Data")]
    public class TaskData : ScriptableObject
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
        public float maxX = float.PositiveInfinity;
        [TitleGroup("General")]
        public QuestData requiredQuest;
        [TitleGroup("General")]
        public float taskDuration;
        [TitleGroup("General")]
        [Tooltip("Interval between repeated SFX plays while the task is active. Zero disables repeats.")]
        public float sfxInterval;
        [TitleGroup("General")]
        public List<ResourceDrop> resourceDrops = new();

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
    }
}
