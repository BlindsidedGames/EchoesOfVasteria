using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Disciple", menuName = "SO/Disciple")]
    public class Disciple : ScriptableObject
    {
        [Serializable]
        public class ResourceEntry
        {
            public Resource resource;
            public double amount = 1;
        }

        public List<ResourceEntry> resources = new();
        public QuestData requiredQuest;
        [Min(0f)] public float generationInterval = 5f;
    }
}