using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes.Conversations
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Conversations")]
    public sealed class ConversationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id, npcId, titleKey, title;
            public TalkToNpcTask task;
            public ToolkitNpcIntroduction introduction;
            public FarmStoryDefinition story;
            public string storyId;
            public QuestData quest;
            public FarmStoryDefinition.Conversation Story => story ? story.Find(storyId) : null;
            public string Title => quest
                ? ToolkitLocalization.Text(Story?.openingQuest ? "conversations.request" : "conversations.complete",
                    Story?.openingQuest ? "{0} — Request" : "{0} — Completed", quest.questName.GetLocalizedString())
                : ToolkitLocalization.Text(titleKey, title);
            public GameObject Prefab => task ? task.MeetingPrefab : introduction ? introduction.meetingPrefab : story.meetingPrefab;

            public bool WasCompleted(GameData data)
            {
                if (task) return data.CompletedNpcTasks?.Contains(task.NpcId) == true;
                if (introduction) return data.CompletedNpcTasks?.Contains(introduction.npcId) == true ||
                    introduction.previouslyCompletedIntroduction && QuestUtils.QuestCompleted(introduction.previouslyCompletedIntroduction, data);
                var conversation = Story;
                if (conversation?.openingQuest)
                    return data.Quests.TryGetValue(conversation.openingQuest.questId, out var opened) && (opened.Accepted || opened.Completed);
                var questId = storyId == "town-introduction" ? FarmContent.IntroductionId : storyId;
                return data.Quests.TryGetValue(questId, out var completed) && completed.Completed &&
                    data.Farm?.PendingQuestDialogues?.Contains(questId) != true;
            }

            public ToolkitMeetingScreen Replay(bool orchardBuilt, Transform subject, Action finished)
            {
                var screen = Instantiate(Prefab).GetComponent<ToolkitMeetingScreen>();
                screen.SetReplay();
                if (task)
                {
                    screen.Init(task.Portrait, task.Lines, finished, task.LineKeys, npcId: npcId, worldSubject: subject);
                    screen.SetLineSpeakers(task.Speakers);
                }
                else if (introduction)
                {
                    screen.Init(introduction.portrait, introduction.lines, finished, introduction.lineKeys, npcId: npcId, worldSubject: subject);
                    screen.SetPortraitAnimation(introduction.portraitFrames, introduction.portraitFramesPerSecond);
                }
                else
                {
                    var lines = Story.lines;
                    screen.Init(lines[0].speaker?.portrait, lines.Select(l => l.Text(orchardBuilt)).ToArray(), finished,
                        lines.Select(l => l.Key(orchardBuilt)).ToArray(), npcId: npcId, worldSubject: subject);
                    screen.SetLineSpeakers(lines.Select(l => l.speaker).ToArray());
                }
                screen.BeginDialogue();
                return screen;
            }
        }

        public Entry[] entries = Array.Empty<Entry>();
        public static ConversationCatalog Load() => Resources.Load<ConversationCatalog>("Conversations/Catalog");

        public static void EnsureHistory(GameData owner, string currentConversation = null)
        {
            if (owner == null || owner.HeardConversations != null) return;
            owner.HeardConversations = new Dictionary<string, bool>();
            var catalog = Load();
            if (!catalog) return;
            foreach (var entry in catalog.entries)
                if (entry.id != currentConversation && entry.WasCompleted(owner))
                    owner.HeardConversations[entry.id] = owner.Farm?.OrchardCapacity > 0;
        }
    }
}
