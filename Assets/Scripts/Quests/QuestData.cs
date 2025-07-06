using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Enemies;
using UnityEngine;

namespace TimelessEchoes.Quests
{
    [ManageableData]
    [CreateAssetMenu(fileName = "QuestData", menuName = "SO/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questId;
        public string questName;
        [TextArea] public string description;
        [TextArea] public string rewardDescription;
        public string npcId;
        public List<Requirement> requirements = new();
        public QuestData nextQuest;
        public GameObject unlockPrefab;
        public List<GameObject> unlockObjects = new();

        [Serializable]
        public class Requirement
        {
            public RequirementType type;
            public Resource resource;
            public int amount = 1;
            public List<EnemyStats> enemies = new();
        }

        public enum RequirementType
        {
            Resource,
            Kill
        }
    }
}
