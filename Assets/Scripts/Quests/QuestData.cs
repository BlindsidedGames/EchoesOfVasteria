using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Enemies;
using Sirenix.OdinInspector;
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
        public List<QuestData> requiredQuests = new();
        public List<Requirement> requirements = new();
        public GameObject unlockPrefab;
        public List<GameObject> unlockObjects = new();
        public int unlockBuffSlots;

        [Serializable]
        public class Requirement
        {
            public RequirementType type;
            [ShowIf("@type == RequirementType.Resource || type == RequirementType.Donation")]
            public Resource resource;
            [HideIf("type", RequirementType.Instant)]
            [HideIf("type", RequirementType.Meet)]
            public int amount = 1;
            [ShowIf("type", RequirementType.Kill)]
            public List<EnemyStats> enemies = new();
            [ShowIf("type", RequirementType.Kill)]
            public Sprite killIcon;
            [ShowIf("type", RequirementType.Meet)]
            public string meetNpcId;
        }

        public enum RequirementType
        {
            Resource,
            Kill,
            Donation,
            DistanceRun,
            DistanceTravel,
            Instant,
            Meet
        }
    }
}
