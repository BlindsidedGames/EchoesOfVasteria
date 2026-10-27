using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using UnityEngine;
using static TimelessEchoes.Quests.QuestUtils;

namespace TimelessEchoes.Quests
{
    /// <summary>
    /// Enables or disables objects based on whether specific quests have been completed.
    /// </summary>
    public class QuestObjectStateController : MonoBehaviour
    {
        public static QuestObjectStateController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;


        [System.Serializable]
        public class Entry
        {
            public QuestData quest;
            public List<GameObject> disableUntilComplete = new();
            public List<GameObject> enableUntilComplete = new();
            public List<GameObject> enableWhileInProgress = new();
        }

        [SerializeField]
        private List<Entry> entries = new();
        private bool presentationHeld;

        public void HoldPresentation() => presentationHeld = true;
        public void ReleasePresentation() { presentationHeld = false; UpdateObjectStates(); }

        private Dictionary<GameObject, bool> DesiredStates()
        {
            var desired = new Dictionary<GameObject, bool>();
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var completed = entry.quest && QuestCompleted(entry.quest.questId);
                var active = entry.quest && QuestInProgress(entry.quest.questId);
                foreach (var obj in entry.disableUntilComplete) if (obj) desired[obj] = completed;
                foreach (var obj in entry.enableUntilComplete) if (obj) desired[obj] = !completed;
                foreach (var obj in entry.enableWhileInProgress) if (obj) desired[obj] = active;
            }
            return desired;
        }

        public List<TownVisualChange> PendingPresentationChanges(Transform town)
        {
            var changes = new List<TownVisualChange>();
            foreach (var pair in DesiredStates())
            {
                var obj = pair.Key;
                if (obj.activeSelf == pair.Value || !obj.transform.IsChildOf(town) ||
                    (obj.transform.parent && !obj.transform.parent.gameObject.activeInHierarchy)) continue;
                var bounds = TownQuestReveal.SpriteBounds(obj);
                if (!bounds.HasValue) continue;
                var enabled = pair.Value;
                changes.Add(new TownVisualChange(bounds.Value, () => { if (obj) obj.SetActive(enabled); }));
            }
            return changes;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            EventHandler.OnLoadData += UpdateObjectStates;
            EventHandler.OnQuestHandin += OnQuestHandin;
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= UpdateObjectStates;
            EventHandler.OnQuestHandin -= OnQuestHandin;
        }

        private void Start()
        {
            UpdateObjectStates();
        }

        private void OnQuestHandin(string questId)
        {
            UpdateObjectStates();
        }

        /// <summary>
        /// Iterates over all entries and updates the active state of their objects.
        /// </summary>
        public void UpdateObjectStates()
        {
            if (presentationHeld) return;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                bool completed = entry.quest != null && QuestCompleted(entry.quest.questId);
                bool inProgress = entry.quest != null && QuestInProgress(entry.quest.questId);
                foreach (var obj in entry.disableUntilComplete)
                    if (obj != null)
                        obj.SetActive(completed);
                foreach (var obj in entry.enableUntilComplete)
                    if (obj != null)
                        obj.SetActive(!completed);
                foreach (var obj in entry.enableWhileInProgress)
                    if (obj != null)
                        obj.SetActive(inProgress);
            }
        }

        private static bool QuestInProgress(string questId)
        {
            var manager = Object.FindAnyObjectByType<QuestManager>();
            return manager != null && manager.IsQuestInProgress(questId);
        }
    }
}
