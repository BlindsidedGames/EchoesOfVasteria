using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Manages quest UI entries.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class QuestUIManager : MonoBehaviour
    {
        public static QuestUIManager Instance { get; private set; }
        [SerializeField] private QuestEntryUI questEntryPrefab;
        [SerializeField] private GameObject dividerPrefab;
        [SerializeField] private Transform questParent;
        [SerializeField] private ScrollRect questScroll;
        private readonly List<QuestEntryUI> entries = new();
        private readonly List<GameObject> extras = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public QuestEntryUI CreateEntry(QuestData quest, Action onTurnIn, bool showRequirements = true, bool completed = false)
        {
            if (questEntryPrefab == null || questParent == null)
                return null;
            var ui = Instantiate(questEntryPrefab, questParent);
            ui.Setup(quest, onTurnIn, showRequirements, completed);
            entries.Add(ui);
            return ui;
        }

        public GameObject CreateDivider()
        {
            if (dividerPrefab == null || questParent == null)
                return null;
            var obj = Instantiate(dividerPrefab, questParent);
            extras.Add(obj);
            return obj;
        }

        public void Clear()
        {
            foreach (var entry in entries)
                if (entry != null)
                    Destroy(entry.gameObject);
            foreach (var extra in extras)
                if (extra != null)
                    Destroy(extra);
            entries.Clear();
            extras.Clear();
        }

        public void RemoveEntry(QuestEntryUI entry)
        {
            if (entry == null) return;
            entries.Remove(entry);
            Destroy(entry.gameObject);
        }

        private void OnEnable()
        {
            // Ensure the list is freshly built and sorted whenever the quest UI opens
            var qm = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
            qm?.RefreshNoticeboard();
            Canvas.ForceUpdateCanvases(); // ensure layout is valid
            if (questScroll != null)
                questScroll.verticalNormalizedPosition = 1f; // top
        }

    }
}
