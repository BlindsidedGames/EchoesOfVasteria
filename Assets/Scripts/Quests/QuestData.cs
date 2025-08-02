using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using TimelessEchoes.Enemies;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.Localization;

namespace TimelessEchoes.Quests
{
    [ManageableData]
    [CreateAssetMenu(fileName = "QuestData", menuName = "SO/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questId;
        public LocalizedString questName;
        public LocalizedString description;
        public LocalizedString rewardDescription;
        public string npcId;
        /// <summary>
        ///     Automatically pin this quest when it becomes active.
        /// </summary>
        public bool autoPin;
        public List<QuestData> requiredQuests = new();
        public List<Requirement> requirements = new();
        public int unlockBuffSlots;
        public int unlockAutoBuffSlots;
        public float maxDistanceIncrease;

        [Serializable]
        public class Requirement
        {
            public RequirementType type;

            [ShowIf("@type == RequirementType.Resource")]
            public Resource resource;

            [HideIf("type", RequirementType.Instant)] [HideIf("type", RequirementType.Meet)]
            public int amount = 1;

            [ShowIf("type", RequirementType.Kill)] public List<EnemyData> enemies = new();
            [ShowIf("type", RequirementType.Kill)] public string killName;
            [ShowIf("type", RequirementType.Kill)] public Sprite killIcon;
            [ShowIf("type", RequirementType.Meet)] public string meetNpcId;
        }

        public enum RequirementType
        {
            Resource,
            Kill,
            DistanceRun,
            DistanceTravel,
            BuffCast,
            Instant,
            Meet
        }
    }
}