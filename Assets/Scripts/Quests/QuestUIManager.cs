using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Manages quest UI entries.
    /// </summary>
    public class QuestUIManager : MonoBehaviour
    {
        [SerializeField] private QuestEntryUI questEntryPrefab;
        [SerializeField] private Transform questParent;

        private readonly List<QuestEntryUI> entries = new();

        public QuestEntryUI CreateEntry(QuestData quest, Action onTurnIn)
        {
            if (questEntryPrefab == null || questParent == null)
                return null;
            var ui = Instantiate(questEntryPrefab, questParent);
            ui.Setup(quest, onTurnIn);
            entries.Add(ui);
            return ui;
        }

        public void Clear()
        {
            foreach (var entry in entries)
                if (entry != null)
                    Destroy(entry.gameObject);
            entries.Clear();
        }

        public void RemoveEntry(QuestEntryUI entry)
        {
            if (entry == null) return;
            entries.Remove(entry);
            Destroy(entry.gameObject);
        }
    }
}
