using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Blindsided.Utilities;
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
        [SerializeField] private TMP_Text availableResourcesText;

        private DiscipleGenerationManager generationManager;
        private readonly Dictionary<DiscipleGenerator, DiscipleGeneratorProgressUI> entries = new();

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
            OnLoadData += OnLoadDataHandler;
            BuildEntries();
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
            OnLoadData -= OnLoadDataHandler;
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

                var ui = Instantiate(progressUIPrefab, progressUIParent);
                ui.SetData(gen);
                entries[gen] = ui;
            }

            UpdateCollectAllButton();
        }

        private void CollectAll()
        {
            if (generationManager == null) return;
            foreach (var gen in generationManager.Generators)
                gen?.CollectResources();
            UpdateCollectAllButton();
        }

        private void OnQuestHandinHandler(string questId)
        {
            StartCoroutine(DeferredBuild());
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DeferredBuild());
        }

        private IEnumerator DeferredBuild()
        {
            yield return null;
            yield return null;
            BuildEntries();
        }

        private void Update()
        {
            UpdateCollectAllButton();
        }

        private void UpdateCollectAllButton()
        {
            if (collectAllButton == null && availableResourcesText == null)
                return;

            bool canCollect = false;
            double totalAvailable = 0;

            if (generationManager != null)
            {
                foreach (var gen in generationManager.Generators)
                {
                    if (gen == null || !gen.RequirementsMet)
                        continue;

                    foreach (var entry in gen.ResourceEntries)
                    {
                        if (entry.resource == null) continue;
                        var stored = gen.GetStoredAmount(entry.resource);
                        totalAvailable += stored;
                        if (!canCollect && stored > 0)
                            canCollect = true;
                    }
                }
            }

            if (collectAllButton != null)
                collectAllButton.interactable = canCollect;

            if (availableResourcesText != null)
                availableResourcesText.text = CalcUtils.FormatNumber(totalAvailable, true);
        }
    }
}