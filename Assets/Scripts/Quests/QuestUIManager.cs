using System;
using System.Collections.Generic;
using TimelessEchoes.UI;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.Oracle;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Manages quest UI entries.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class QuestUIManager : TimelessEchoes.Utilities.Singleton<QuestUIManager>
    {
        [SerializeField] private QuestEntryUI questEntryPrefab;
        [SerializeField] private GameObject dividerPrefab;
        [SerializeField] private Transform questParent;
        public WikiUIToggle questCategoryPrefab;
        [SerializeField] private ScrollRect questScroll;
        private readonly List<QuestEntryUI> entries = new();
        private readonly List<GameObject> extras = new();

        // Category headers and top divider (persistent; not cleared on refresh)
        private WikiUIToggle readyCategory;
        private WikiUIToggle pinnedCategory;
        private WikiUIToggle activeCategory;
        private WikiUIToggle completedCategory;
        private GameObject topDivider;

        protected override void Awake()
        {
            base.Awake();
            EnsureCategories();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public enum QuestCategory
        {
            Auto,
            Ready,
            Pinned,
            Active,
            Completed
        }

        public QuestEntryUI CreateEntry(QuestData quest, Action onTurnIn, bool showRequirements = true,
            bool completed = false, QuestCategory target = QuestCategory.Auto)
        {
            EnsureCategories();
            if (questEntryPrefab == null)
                return null;
            // Choose parent based on category
            Transform parent = questParent;
            if (target == QuestCategory.Completed || completed)
            {
                parent = completedCategory != null && completedCategory.questsParent != null
                    ? completedCategory.questsParent
                    : questParent;
            }
            else if (target == QuestCategory.Ready)
            {
                parent = readyCategory != null && readyCategory.questsParent != null
                    ? readyCategory.questsParent
                    : questParent;
            }
            else if (target == QuestCategory.Pinned)
            {
                parent = pinnedCategory != null && pinnedCategory.questsParent != null
                    ? pinnedCategory.questsParent
                    : questParent;
            }
            else if (target == QuestCategory.Active)
            {
                parent = activeCategory != null && activeCategory.questsParent != null
                    ? activeCategory.questsParent
                    : questParent;
            }
            else
            {
                var isPinned = oracle != null && oracle.saveData != null && quest != null &&
                               oracle.saveData.PinnedQuests != null &&
                               oracle.saveData.PinnedQuests.Contains(quest.questId);
                parent = isPinned && pinnedCategory != null && pinnedCategory.questsParent != null
                    ? pinnedCategory.questsParent
                    : (activeCategory != null && activeCategory.questsParent != null
                        ? activeCategory.questsParent
                        : questParent);
            }

            var ui = Instantiate(questEntryPrefab, parent);
            ui.Setup(quest, onTurnIn, showRequirements, completed);
            entries.Add(ui);
            return ui;
        }

        // Note: previous divider placement (between active/completed) is no longer used.

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

        private void OnEnable()
        {
            // Ensure categories are present and in the desired expanded/collapsed state
            EnsureCategories();
            // Ensure the list is freshly built and sorted whenever the quest UI opens
            var qm = QuestManager.Instance;
            qm?.RefreshNoticeboard();
            Canvas.ForceUpdateCanvases(); // ensure layout is valid
            if (questScroll != null)
                questScroll.verticalNormalizedPosition = 1f; // top
        }

        private void EnsureCategories()
        {
            if (questParent == null || questCategoryPrefab == null)
                return;

            // Create a single divider at the very top of questParent (before categories)
            if (topDivider == null && dividerPrefab != null)
            {
                topDivider = Instantiate(dividerPrefab, questParent);
            }

            // Create Complete category (appears first)
            if (readyCategory == null)
            {
                readyCategory = Instantiate(questCategoryPrefab, questParent);
                if (readyCategory.categoryName != null)
                    readyCategory.categoryName.text = "Complete";
            }

            // Create Pinned category
            if (pinnedCategory == null)
            {
                pinnedCategory = Instantiate(questCategoryPrefab, questParent);
                if (pinnedCategory.categoryName != null)
                    pinnedCategory.categoryName.text = "Pinned";
            }

            // Create Active category
            if (activeCategory == null)
            {
                activeCategory = Instantiate(questCategoryPrefab, questParent);
                if (activeCategory.categoryName != null)
                    activeCategory.categoryName.text = "Active";
            }

            // Create Completed category
            if (completedCategory == null)
            {
                completedCategory = Instantiate(questCategoryPrefab, questParent);
                if (completedCategory.categoryName != null)
                    completedCategory.categoryName.text = "Completed";
            }

            // Desired default states: Complete + Pinned + Active expanded, Completed closed
            SetCategoryExpanded(readyCategory, true);
            SetCategoryExpanded(pinnedCategory, true);
            SetCategoryExpanded(activeCategory, true);
            SetCategoryExpanded(completedCategory, false);
        }

        private static void SetCategoryExpanded(WikiUIToggle toggle, bool expanded)
        {
            if (toggle == null)
                return;
            // Use questsParent active state as a proxy for toggle expand/collapse
            var content = toggle.questsParent != null ? toggle.questsParent.gameObject : null;
            if (content == null)
                return;

            var isOpen = content.activeInHierarchy;
            if (expanded != isOpen)
            {
                var btn = toggle.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.Invoke();
            }
        }

        public void UpdateCategoryVisibility(int readyCount, int pinnedCount, int activeCount, int completedCount)
        {
            EnsureCategories();
            if (readyCategory != null)
            {
                if (readyCategory.categoryName != null)
                    readyCategory.categoryName.text = $"Complete | {readyCount}";
                var show = readyCount > 0;
                readyCategory.gameObject.SetActive(show);
                if (show) SetCategoryExpanded(readyCategory, true);
            }
            if (pinnedCategory != null)
            {
                if (pinnedCategory.categoryName != null)
                    pinnedCategory.categoryName.text = $"Pinned | {pinnedCount}";
                var show = pinnedCount > 0;
                pinnedCategory.gameObject.SetActive(show);
                if (show) SetCategoryExpanded(pinnedCategory, true);
            }
            if (activeCategory != null)
            {
                if (activeCategory.categoryName != null)
                    activeCategory.categoryName.text = $"Active | {activeCount}";
                var show = activeCount > 0;
                activeCategory.gameObject.SetActive(show);
                if (show) SetCategoryExpanded(activeCategory, true);
            }
            if (completedCategory != null)
            {
                if (completedCategory.categoryName != null)
                    completedCategory.categoryName.text = $"Completed | {completedCount}";
                var show = completedCount > 0;
                completedCategory.gameObject.SetActive(show);
                if (show) SetCategoryExpanded(completedCategory, false);
            }
        }
    }
}
