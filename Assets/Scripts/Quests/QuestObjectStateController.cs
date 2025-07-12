using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Quests
{
    /// <summary>
    /// Enables or disables objects based on whether specific quests have been completed.
    /// </summary>
    public class QuestObjectStateController : MonoBehaviour
    {
        public static QuestObjectStateController Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            public QuestData quest;
            public List<GameObject> disableUntilComplete = new();
            public List<GameObject> enableUntilComplete = new();
        }

        [SerializeField]
        private List<Entry> entries = new();

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
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                bool completed = entry.quest != null && QuestCompleted(entry.quest.questId);
                foreach (var obj in entry.disableUntilComplete)
                    if (obj != null)
                        obj.SetActive(completed);
                foreach (var obj in entry.enableUntilComplete)
                    if (obj != null)
                        obj.SetActive(!completed);
            }
        }

        private static bool QuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return false;
            if (Oracle.oracle == null)
                return false;
            Oracle.oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return Oracle.oracle.saveData.Quests.TryGetValue(questId, out var rec) && rec.Completed;
        }
    }
}
