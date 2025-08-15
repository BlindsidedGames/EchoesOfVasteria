using System.Collections.Generic;
using Blindsided;
using TimelessEchoes.Quests;
using UnityEngine;
using static TimelessEchoes.Quests.QuestUtils;

namespace TimelessEchoes
{
    /// <summary>
    /// Enables or disables objects based on whether the player is in town or in a run.
    /// Entries may optionally require a quest to be completed before activation.
    /// </summary>
    public class LocationObjectStateController : MonoBehaviour
    {
        public static LocationObjectStateController Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            public GameObject gameObject;
            public QuestData requiredQuest;
        }

        [SerializeField]
        private List<Entry> enableInTown = new();
        [SerializeField]
        private List<Entry> enableInRun = new();

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
            bool inRun = GameManager.Instance != null && GameManager.Instance.CurrentMap != null;
            foreach (var entry in enableInTown)
            {
                if (entry?.gameObject == null) continue;
                bool questOk = entry.requiredQuest == null || QuestCompleted(entry.requiredQuest.questId);
                entry.gameObject.SetActive(!inRun && questOk);
            }
            foreach (var entry in enableInRun)
            {
                if (entry?.gameObject == null) continue;
                bool questOk = entry.requiredQuest == null || QuestCompleted(entry.requiredQuest.questId);
                entry.gameObject.SetActive(inRun && questOk);
            }
        }
    }
}

