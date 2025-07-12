using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Builds UI entries for all disciple generators and handles collecting resources.
    /// </summary>
    public class DiscipleGeneratorUIManager : MonoBehaviour
    {
        public static DiscipleGeneratorUIManager Instance { get; private set; }
        [SerializeField] private DiscipleGeneratorProgressUI progressUIPrefab;
        [SerializeField] private Transform progressUIParent;
        [SerializeField] private Button collectAllButton;

        private DiscipleGenerationManager generationManager;
        private readonly Dictionary<DiscipleGenerator, List<DiscipleGeneratorProgressUI>> entries = new();

        private void Awake()
        {
            Instance = this;
            generationManager = DiscipleGenerationManager.Instance;
            if (collectAllButton != null)
                collectAllButton.onClick.AddListener(CollectAll);
            BuildEntries();
        }

        private void Start()
        {
            StartCoroutine(DeferredBuild());
        }

        private void OnEnable()
        {
            OnQuestHandin += OnQuestHandinHandler;
            BuildEntries();
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
        }

        private void OnDestroy()
        {
            if (collectAllButton != null)
                collectAllButton.onClick.RemoveListener(CollectAll);
            if (Instance == this)
                Instance = null;
            OnQuestHandin -= OnQuestHandinHandler;
        }

        private void BuildEntries()
        {
            if (progressUIPrefab == null || progressUIParent == null || generationManager == null)
                return;

            foreach (Transform child in progressUIParent)
                Destroy(child.gameObject);
            entries.Clear();

            foreach (var gen in generationManager.Generators)
            {
                if (gen == null || !gen.RequirementsMet)
                    continue;

                var list = new List<DiscipleGeneratorProgressUI>();
                foreach (var entry in gen.ResourceEntries)
                {
                    if (entry.resource == null) continue;
                    var ui = Instantiate(progressUIPrefab, progressUIParent);
                    ui.SetData(gen, entry.resource, entry.amount);
                    list.Add(ui);
                }

                entries[gen] = list;
            }

            UpdateCollectAllButton();
        }

        private void CollectAll()
        {
            if (generationManager == null) return;
            foreach (var gen in generationManager.Generators)
                gen?.CollectResources();
        }

        private void OnQuestHandinHandler(string questId)
        {
            StartCoroutine(DeferredBuild());
        }

        private IEnumerator DeferredBuild()
        {
            yield return null;
            BuildEntries();
        }

        private void Update()
        {
            UpdateCollectAllButton();
        }

        private void UpdateCollectAllButton()
        {
            if (collectAllButton == null)
                return;

            bool canCollect = false;
            if (generationManager != null)
            {
                foreach (var gen in generationManager.Generators)
                {
                    if (gen == null || !gen.RequirementsMet)
                        continue;
                    foreach (var entry in gen.ResourceEntries)
                    {
                        if (entry.resource == null) continue;
                        if (gen.GetStoredAmount(entry.resource) > 0)
                        {
                            canCollect = true;
                            break;
                        }
                    }
                    if (canCollect) break;
                }
            }

            collectAllButton.interactable = canCollect;
        }
    }
}