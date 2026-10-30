using System;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Farming/Story")]
    public sealed class FarmStoryDefinition : ScriptableObject
    {
        [Serializable] public sealed class Line
        {
            public QuestSpeaker speaker;
            public string key;
            [TextArea] public string text;
        }
        [Serializable] public sealed class Conversation
        {
            public string id;
            public Line[] lines = Array.Empty<Line>();
        }
        public GameObject meetingPrefab;
        public Conversation[] conversations = Array.Empty<Conversation>();
        public Conversation Find(string id) => Array.Find(conversations, c => c.id == id);
    }
}
