using System.Collections.Generic;
using Blindsided.SaveData;
using TMPro;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Hero;
using TimelessEchoes.UI;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Simple conversation task for interacting with an NPC.
    /// Shows text lines sequentially and completes when finished.
    /// </summary>
    public class TalkToNpcTask : BaseTask
    {
        [SerializeField] private string npcId;
        [SerializeField] private Transform chatPoint;
        [SerializeField] private GameObject meetingPrefab;
        [SerializeField] private Sprite npcSprite;
        [TextArea]
        [SerializeField] private List<string> lines = new();
        // Stable presentation identities; existing raw lines remain the fallback.
        [SerializeField] private List<string> lineKeys = new();
        [SerializeField] private QuestSpeaker[] dialogueSpeakers = System.Array.Empty<QuestSpeaker>();

        private bool talked;
        private GameObject meetingInstance;

        public override Transform Target => chatPoint != null ? chatPoint : transform;
        public override bool BlocksMovement => true;

        private void Awake()
        {
        }

        public override void StartTask()
        {
            talked = false;
            meetingInstance = null;
            if (!string.IsNullOrEmpty(npcId))
                StaticReferences.ActiveNpcMeetings.Remove(npcId);
        }

        public override void OnArrival(HeroBase hero)
        {
            if (meetingInstance || talked) return;
            if (!string.IsNullOrEmpty(npcId))
                StaticReferences.ActiveNpcMeetings.Add(npcId);
            SpawnMeetingUI();
        }

        private void SpawnMeetingUI()
        {
            if (meetingPrefab == null || meetingInstance != null) return;
            var parent = GameManager.Instance != null ? GameManager.Instance.MeetingParent : null;
            meetingInstance = Object.Instantiate(meetingPrefab, parent, false);
            var toolkit = meetingInstance.GetComponent<TimelessEchoes.UI.Toolkit.ToolkitMeetingScreen>();
            if (toolkit != null)
            {
                toolkit.Init(npcSprite, lines, OnMeetingFinished, lineKeys, npcId: npcId, worldSubject: transform);
                toolkit.SetLineSpeakers(dialogueSpeakers);
                return;
            }
            var controller = meetingInstance.GetComponent<MeetingController>();
            controller?.Init(npcSprite, lines, OnMeetingFinished, lineKeys);
        }

        private void OnMeetingFinished()
        {
            if (!string.IsNullOrEmpty(npcId))
            {
                StaticReferences.ActiveNpcMeetings.Remove(npcId);
                StaticReferences.CompletedNpcTasks.Add(npcId);
                var qm = Object.FindAnyObjectByType<QuestManager>();
                qm?.OnNpcMet(npcId);
                TimelessEchoes.NPC.NpcObjectStateController.Instance?.UpdateObjectStates();
            }
            talked = true;
            if (npcId == "Ivan1") GameManager.Instance?.WaitForIvanReturn(transform);
            GrantCompletionXP();
            NotifyCompleted();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelMeeting();
        }

        private void OnDestroy() => CancelMeeting();
        private void CancelMeeting()
        {
            if (!string.IsNullOrEmpty(npcId)) StaticReferences.ActiveNpcMeetings.Remove(npcId);
            if (meetingInstance) { meetingInstance.SetActive(false); Destroy(meetingInstance); meetingInstance = null; }
        }

        public override bool IsComplete()
        {
            return talked;
        }
    }
}
