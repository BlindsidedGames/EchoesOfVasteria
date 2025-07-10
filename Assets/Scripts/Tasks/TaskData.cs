using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    [ManageableData]
    [CreateAssetMenu(fileName = "TaskData", menuName = "SO/Task Data")]
    public class TaskData : ScriptableObject
    {
        public string taskName;
        public int taskID;
        public Sprite taskIcon;
        public Skill associatedSkill;
        public float xpForCompletion;
        public string requiredQuestId;
        public float taskDuration;
        [Tooltip("Interval between repeated SFX plays while the task is active. Zero disables repeats.")]
        public float sfxInterval;
        public List<ResourceDrop> resourceDrops = new();

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
