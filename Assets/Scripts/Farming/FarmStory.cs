using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    /// <summary>Town quest conversations. QuestManager owns acceptance, payments and rewards.</summary>
    public sealed class FarmStory : MonoBehaviour
    {
        public static FarmStory Instance { get; private set; }
        private FarmStoryDefinition definition;
        public bool DialogueCompleted { get; private set; }
        public bool IsSpeaking { get; private set; }
        private bool Ready => FarmService.Instance && FarmService.Instance.Ready && Oracle.oracle?.saveData != null;
        private void Awake() { Instance = this; definition = Resources.Load<FarmStoryDefinition>("Farming/FarmStory"); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private static FarmStoryDefinition.Conversation FindCompletion(FarmStoryDefinition story, string questId)
        {
            if (!story || !story.meetingPrefab || string.IsNullOrEmpty(questId)) return null;
            var id = questId == FarmContent.IntroductionId ? "town-introduction" : questId;
            return story.Find(id) is { lines: { Length: > 0 } } completion ? completion : null;
        }

        internal static void QueueCompletion(GameData owner, string questId)
        {
            if (questId == "Unlock Corn" && FarmContent.Completed(owner, "Unlock Cauldron")) return;
            var story = Instance ? Instance.definition : Resources.Load<FarmStoryDefinition>("Farming/FarmStory");
            if (FindCompletion(story, questId) == null) return;
            owner.Farm ??= new FarmState();
            var pending = owner.Farm.PendingQuestDialogues ??= new List<string>();
            if (!pending.Contains(questId)) pending.Add(questId);
        }

        private string NextCompletion(GameData data) =>
            data.Farm?.PendingQuestDialogues?.FirstOrDefault(id => FindCompletion(definition, id) != null);

        private static bool SummaryClosed => FindAnyObjectByType<ToolkitRunScreen>()?.HasOpenDetails != true;
        internal bool CanPresentCompletion(string questId) =>
            Ready && !IsSpeaking && !string.IsNullOrEmpty(questId) && SummaryClosed &&
            NextCompletion(Oracle.oracle.saveData) == questId;

        private bool CanSpeak => Ready && GameManager.Instance && GameManager.Instance.IsInTown && !GameManager.Instance.RunLoadingActive &&
            !GameManager.HardcoreFallen &&
            !Characters.CharacterCreation.Required && !WorldPresentation.IsBusy && !TownQuestReveal.IsPlaying &&
            !ToolkitMeetingScreen.HasVisibleMeeting && Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Count == 0 &&
            SummaryClosed &&
            TownWindowManager.Instance && !TownWindowManager.Instance.HasOpenWindow;

        private IEnumerator Start()
        {
            while (true)
            {
                if (!CanSpeak || !definition) { yield return null; continue; }
                var data = Oracle.oracle.saveData;
                if (NextCompletion(data) is string id)
                {
                    yield return ShowCompletion(id);
                    if (DialogueCompleted && Ready && ReferenceEquals(data, Oracle.oracle.saveData) && id == "Farm.Garden.Build01.v1")
                        TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Farm);
                }
                else if (data.CompletedNpcTasks.Contains("Farmers1") && !FarmContent.Completed(data, FarmContent.IntroductionId) &&
                    QuestManager.Instance && QuestManager.Instance.TryTurnInQuest(FarmContent.IntroductionId))
                {
                    // The hand-in stages the introduction alongside the one-off seed gift.
                    yield return null;
                    continue;
                }
                else
                {
                    var manager = QuestManager.Instance;
                    var opening = definition.conversations.FirstOrDefault(c => c.openingQuest && manager &&
                        manager.NeedsAcceptance(c.openingQuest) &&
                        !c.openingQuest.requirements.Any(r => QuestResourceAccess.Blockers(r, data).Count > 0));
                    // Acceptance has no grant for these conversations. The existing
                    // quest owner persists it before presentation, so reload/skip
                    // cannot repeat an opening or pay a hand-in accidentally.
                    if (opening != null && definition.meetingPrefab && opening.lines.Length > 0 &&
                        manager.TryTurnInQuest(opening.openingQuest.questId))
                        yield return ShowConversation(opening);
                }
                yield return null;
            }
        }

        public IEnumerator ShowCompletion(string questId, WorldPresentation.RevealSession reveal = null)
        {
            DialogueCompleted = false;
            if (!CanPresentCompletion(questId)) yield break;
            yield return ShowConversation(FindCompletion(definition, questId), questId, reveal);
        }

        private IEnumerator ShowConversation(FarmStoryDefinition.Conversation conversation,
            string questId = null, WorldPresentation.RevealSession reveal = null)
        {
            DialogueCompleted = false;
            if (!Ready) yield break;
            var capturedOwner = Oracle.oracle.saveData;
            var orchardBuilt = capturedOwner.Farm?.OrchardCapacity > 0;
            var npcId = string.IsNullOrEmpty(conversation.npcId) ? "Farmers1" : conversation.npcId;
            var subject = WorldSpeakerAnchor.Find(npcId);
            ToolkitMeetingScreen screen = null;
            IsSpeaking = true;
            try
            {
                screen = Instantiate(definition.meetingPrefab).GetComponent<ToolkitMeetingScreen>();
                if (reveal != null) screen.AdoptReveal(reveal);
                screen.Init(conversation.lines[0].speaker?.portrait, conversation.lines.Select(l => l.Text(orchardBuilt)).ToArray(),
                    Finished, conversation.lines.Select(l => l.Key(orchardBuilt)).ToArray(), npcId: npcId, worldSubject: subject);
                screen.SetLineSpeakers(conversation.lines.Select(l => l.speaker).ToArray());
                if (questId == "Farm.Garden.Build01.v1") screen.SetFinalActionLabel("farm.display-name", "Fields");
                screen.BeginDialogue();
                while (screen && Ready && ReferenceEquals(capturedOwner, Oracle.oracle.saveData)) yield return null;
                // Adoption normally consumes the reveal. Release it here if the
                // meeting was interrupted before it could take the camera.
                reveal?.Dispose();
                while (WorldPresentation.IsBusy && Ready && ReferenceEquals(capturedOwner, Oracle.oracle.saveData)) yield return null;
            }
            finally { if (screen) Destroy(screen.gameObject); IsSpeaking = false; }

            void Finished()
            {
                if (!Ready || !ReferenceEquals(capturedOwner, Oracle.oracle.saveData)) return;
                DialogueCompleted = true;
                if (questId == null || capturedOwner.Farm?.PendingQuestDialogues is not { } pending || !pending.Remove(questId)) return;
                if (pending.Count == 0) capturedOwner.Farm.PendingQuestDialogues = null;
                Oracle.oracle.RequestCheckpointSave();
            }
        }
    }
}
