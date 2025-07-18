using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Disciple", menuName = "SO/Disciple")]
    public class Disciple : ScriptableObject
    {
        public List<DiscipleGenerator.ResourceEntry> resources = new();
        public QuestData requiredQuest;
        [Min(0f)] public float generationInterval = 5f;
    }
}
