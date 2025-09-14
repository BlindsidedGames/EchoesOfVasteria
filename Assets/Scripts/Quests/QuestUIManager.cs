using System;
using System.Collections.Generic;
using TimelessEchoes.UI;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;

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
            // Force default-open state for Complete and Pinned at panel open
            SetCategoryExpanded(readyCategory, true);
            SetCategoryExpanded(pinnedCategory, true);
            // Ensure the list is freshly built and sorted whenever the quest UI opens
            var qm = QuestManager.Instance;
            qm?.RefreshNoticeboard();
            Canvas.ForceUpdateCanvases(); // ensure layout is valid
            if (questScroll != null)
                questScroll.verticalNormalizedPosition = 1f; // top

            // Ensure sections are expanded after load events
            EventHandler.OnLoadData += OnLoadDataHandler;

            // Re-assert expansion at end of frame to win any order-of-operations races
            StartCoroutine(ReopenCategoriesEndOfFrame());
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= OnLoadDataHandler;
        }

        private void OnLoadDataHandler()
        {
            // Re-open the desired categories when save data is loaded
            EnsureCategories();
            SetCategoryExpanded(readyCategory, true);
            SetCategoryExpanded(pinnedCategory, true);
        }

        private System.Collections.IEnumerator ReopenCategoriesEndOfFrame()
        {
            yield return null;
            EnsureCategories();
            SetCategoryExpanded(readyCategory, true);
            SetCategoryExpanded(pinnedCategory, true);
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
                readyCategory.startClosed = false;
                if (readyCategory.categoryName != null)
                    readyCategory.categoryName.text = "Complete";
                // Explicitly set open on creation to override prefab startClosed
                SetCategoryExpanded(readyCategory, true);
            }

            // Create Pinned category
            if (pinnedCategory == null)
            {
                pinnedCategory = Instantiate(questCategoryPrefab, questParent);
                pinnedCategory.startClosed = false;
                if (pinnedCategory.categoryName != null)
                    pinnedCategory.categoryName.text = "Pinned";
                // Explicitly set open on creation to override prefab startClosed
                SetCategoryExpanded(pinnedCategory, true);
            }

            // Create Active category
            if (activeCategory == null)
            {
                activeCategory = Instantiate(questCategoryPrefab, questParent);
                if (activeCategory.categoryName != null)
                    activeCategory.categoryName.text = "Active";
                // Explicitly set open on creation to override prefab startClosed
                SetCategoryExpanded(activeCategory, true);
            }

            // Create Quest History category (formerly "Completed")
            if (completedCategory == null)
            {
                completedCategory = Instantiate(questCategoryPrefab, questParent);
                if (completedCategory.categoryName != null)
                    completedCategory.categoryName.text = "Quest History";
                // Explicitly set closed on creation to override prefab startClosed
                SetCategoryExpanded(completedCategory, false);
            }

            // Desired default states: Complete + Pinned + Active expanded, Quest History closed
            SetCategoryExpanded(readyCategory, true);
            SetCategoryExpanded(pinnedCategory, true);
            SetCategoryExpanded(activeCategory, true);
            SetCategoryExpanded(completedCategory, false);
        }

        private static void SetCategoryExpanded(WikiUIToggle toggle, bool expanded)
        {
            if (toggle == null)
                return;
            // Prefer the explicit API to avoid fragile click-simulation
            toggle.SetExpanded(expanded);
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
                    completedCategory.categoryName.text = $"Quest History | {completedCount}";
                var show = completedCount > 0;
                completedCategory.gameObject.SetActive(show);
                if (show) SetCategoryExpanded(completedCategory, false);
            }
        }
    }
}
