using System.Collections;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/NPC Introduction")]
    public sealed class ToolkitNpcIntroduction : ScriptableObject
    {
        public string npcId;
        public GameObject meetingPrefab;
        public Sprite portrait;
        public Sprite[] portraitFrames;
        [Min(1)] public float portraitFramesPerSecond = 12f;
        public QuestData previouslyCompletedIntroduction;
        [TextArea] public string[] lines;
        public string[] lineKeys;

        public bool HasBeenCompleted => StaticReferences.CompletedNpcTasks.Contains(npcId) ||
            (previouslyCompletedIntroduction && QuestUtils.QuestCompleted(previouslyCompletedIntroduction));

        public IEnumerator ShowAndWait(Transform owner)
        {
            if (!owner || Oracle.oracle == null || !Oracle.oracle.HasLoadedCurrentSlotData ||
                string.IsNullOrEmpty(npcId) || !meetingPrefab ||
                HasBeenCompleted ||
                StaticReferences.ActiveNpcMeetings.Contains(npcId))
                yield break;

            var meeting = Instantiate(meetingPrefab, owner, false).GetComponent<ToolkitMeetingScreen>();
            var finished = false;
            StaticReferences.ActiveNpcMeetings.Add(npcId);
            try
            {
                meeting.Init(portrait, lines, () =>
                {
                    StaticReferences.CompletedNpcTasks.Add(npcId);
                    QuestManager.Instance?.OnNpcMet(npcId);
                    NPC.NpcObjectStateController.Instance?.UpdateObjectStates();
                    finished = true;
                }, lineKeys, npcId: npcId);
                if (portraitFrames != null && portraitFrames.Length > 0)
                    meeting.SetPortraitAnimation(portraitFrames, portraitFramesPerSecond);
                meeting.BeginDialogue();
                while (owner && meeting && !finished) yield return null;
            }
            finally
            {
                StaticReferences.ActiveNpcMeetings.Remove(npcId);
                if (meeting) Destroy(meeting.gameObject);
            }
        }
    }
}
