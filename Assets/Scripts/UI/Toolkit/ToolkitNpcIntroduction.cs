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

        [System.Serializable]
        public sealed class WorldArrival
        {
            public Sprite[] entranceFrames, departureFrames, standingFrames;
            public bool standingFacesRight = true;
            [Min(1)] public float framesPerSecond = 12;
            [Min(.1f)] public float approachSeconds = .7f;
            [Min(0)] public float startingInset = 4;
            public Vector2 standingOffset = new(2, 0);
        }
        public WorldArrival worldArrival;
        public bool HasWorldArrival => worldArrival?.entranceFrames is { Length: > 0 };
        public float StartingInset => HasWorldArrival && !HasBeenCompleted ? worldArrival.startingInset : 0;

        public bool HasBeenCompleted => StaticReferences.CompletedNpcTasks.Contains(npcId) ||
            (previouslyCompletedIntroduction && QuestUtils.QuestCompleted(previouslyCompletedIntroduction));

        public IEnumerator ShowAndWait(Transform owner, Transform worldSubject = null)
        {
            if (!owner || Oracle.oracle == null || !Oracle.oracle.HasLoadedCurrentSlotData ||
                string.IsNullOrEmpty(npcId) || !meetingPrefab ||
                HasBeenCompleted ||
                StaticReferences.ActiveNpcMeetings.Contains(npcId))
                yield break;

            ToolkitMeetingScreen meeting = null;
            WorldIntroductionActor actor = null;
            System.IDisposable hold = null;
            var finished = false;
            StaticReferences.ActiveNpcMeetings.Add(npcId);
            try
            {
                var hero = Hero.HeroController.Instance;
                if (HasWorldArrival && hero && GameManager.Instance && GameManager.Instance.CurrentMap)
                {
                    hold = hero.BeginNpcConversation();
                    actor = WorldIntroductionActor.Create(this, owner, hero);
                }
                meeting = Instantiate(meetingPrefab, owner, false).GetComponent<ToolkitMeetingScreen>();
                if (actor) meeting.SetEntrancePending(true);
                meeting.Init(portrait, lines, () =>
                {
                    StaticReferences.CompletedNpcTasks.Add(npcId);
                    QuestManager.Instance?.OnNpcMet(npcId);
                    NPC.NpcObjectStateController.Instance?.UpdateObjectStates();
                    finished = true;
                }, lineKeys, npcId: npcId, worldSubject: actor ? actor.transform : worldSubject ? worldSubject : WorldSpeakerAnchor.Find(npcId));
                if (portraitFrames != null && portraitFrames.Length > 0)
                    meeting.SetPortraitAnimation(portraitFrames, portraitFramesPerSecond, actor ? actor.AnimationStartedAt : (float?)null);
                meeting.BeginDialogue();
                if (actor)
                {
                    yield return actor.WaitForArrival();
                    if (!owner || !meeting || !actor) yield break;
                    meeting.SetEntrancePending(false);
                }
                while (owner && meeting && !finished) yield return null;
                if (finished && actor) yield return actor.Depart();
            }
            finally
            {
                StaticReferences.ActiveNpcMeetings.Remove(npcId);
                if (meeting) Destroy(meeting.gameObject);
                if (actor) Destroy(actor.gameObject);
                hold?.Dispose();
            }
        }
    }
}
