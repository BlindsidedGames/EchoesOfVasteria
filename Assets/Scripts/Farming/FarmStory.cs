using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using TimelessEchoes.Quests;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    /// <summary>Cosmetic conversations for committed Fields progress. Never grants project rewards.</summary>
    public sealed class FarmStory : MonoBehaviour
    {
        public static FarmStory Instance { get; private set; }
        private FarmStoryDefinition definition;
        private readonly Queue<string> pending = new();
        public bool DialogueCompleted { get; private set; }
        public bool IsSpeaking { get; private set; }
        private bool Ready => FarmService.Instance && FarmService.Instance.Ready && Oracle.oracle?.saveData != null;
        private void Awake() { Instance = this; definition = Resources.Load<FarmStoryDefinition>("Farming/FarmStory"); }
        private void OnEnable() => Blindsided.EventHandler.OnLoadData += Loaded;
        private void OnDisable() => Blindsided.EventHandler.OnLoadData -= Loaded;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Loaded() => pending.Clear();
        public bool HasCompletion(string id) => definition && definition.Find(id) != null;
        public void QueueCompletion(string id) { if (HasCompletion(id) && !pending.Contains(id)) pending.Enqueue(id); }
        private bool CanSpeak => Ready && GameManager.Instance && GameManager.Instance.IsInTown && !GameManager.Instance.RunLoadingActive &&
            !Characters.CharacterCreation.Required && !WorldPresentation.IsBusy && !TownQuestReveal.IsPlaying &&
            !ToolkitMeetingScreen.HasVisibleMeeting && Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Count == 0 &&
            FindAnyObjectByType<ToolkitRunScreen>()?.HasOpenDetails != true &&
            TownWindowManager.Instance && !TownWindowManager.Instance.HasOpenWindow;

        private IEnumerator Start()
        {
            while (true)
            {
                if (!CanSpeak || !definition) { yield return null; continue; }
                var data = Oracle.oracle.saveData;
                if (data.CompletedNpcTasks.Contains("Farmers1") && !FarmContent.Completed(data, FarmContent.IntroductionId) &&
                    QuestManager.Instance && QuestManager.Instance.TryTurnInQuest(FarmContent.IntroductionId))
                {
                    // The existing introduction transaction owns the one-off seed gift.
                    yield return ShowCompletion("town-introduction");
                }
                else if (pending.Count > 0)
                {
                    var id = pending.Dequeue();
                    yield return ShowCompletion(id);
                    if (DialogueCompleted && id == "Farm.Garden.Build01.v1")
                        TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Farm);
                }
                yield return null;
            }
        }

        public IEnumerator ShowCompletion(string id, WorldPresentation.RevealSession reveal = null)
        {
            DialogueCompleted = false;
            var conversation = definition ? definition.Find(id) : null;
            if (conversation == null || !definition.meetingPrefab || !Ready) yield break;
            var capturedOwner = Oracle.oracle.saveData;
            var subject = WorldSpeakerAnchor.Find("Farmers1");
            ToolkitMeetingScreen screen = null;
            IsSpeaking = true;
            try
            {
                screen = Instantiate(definition.meetingPrefab).GetComponent<ToolkitMeetingScreen>();
                if (reveal != null) screen.AdoptReveal(reveal);
                screen.Init(conversation.lines[0].speaker?.portrait, conversation.lines.Select(l => l.text).ToArray(),
                    () => DialogueCompleted = true, conversation.lines.Select(l => l.key).ToArray(), npcId: "Farmers1", worldSubject: subject);
                screen.SetLineSpeakers(conversation.lines.Select(l => l.speaker).ToArray());
                if (id == "Farm.Garden.Build01.v1") screen.SetFinalActionLabel("farm.display-name", "Fields");
                screen.BeginDialogue();
                while (screen && Ready && ReferenceEquals(capturedOwner, Oracle.oracle.saveData)) yield return null;
                while (WorldPresentation.IsBusy && Ready && ReferenceEquals(capturedOwner, Oracle.oracle.saveData)) yield return null;
            }
            finally { if (screen) Destroy(screen.gameObject); IsSpeaking = false; }
        }
    }
}
