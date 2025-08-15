using System.Collections;
using System.Collections.Generic;
using Blindsided.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Utilities;
using static Blindsided.EventHandler;
using static Blindsided.SaveData.StaticReferences;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Builds UI entries for all disciple generators and handles collecting resources.
    /// </summary>
    public class DiscipleGeneratorUIManager : Singleton<DiscipleGeneratorUIManager>
    {
        [SerializeField] private DiscipleGeneratorProgressUI progressUIPrefab;
        [SerializeField] private Transform progressUIParent;
        [SerializeField] private Button collectAllButton;
        [SerializeField] private TMP_Text availableResourcesText;
        [SerializeField] private TMP_Text disciplePercentText;

        private DiscipleGenerationManager generationManager;
        private readonly Dictionary<DiscipleGenerator, DiscipleGeneratorProgressUI> entries = new();

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            generationManager = DiscipleGenerationManager.Instance;
            if (collectAllButton != null)
                collectAllButton.onClick.AddListener(CollectAll);
            if (generationManager != null)
                generationManager.OnGeneratorsRebuilt += OnGeneratorsRebuilt;
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (collectAllButton != null)
                collectAllButton.onClick.RemoveListener(CollectAll);
            if (generationManager != null)
                generationManager.OnGeneratorsRebuilt -= OnGeneratorsRebuilt;
            OnQuestHandin -= OnQuestHandinHandler;
        }

        private void BuildEntries()
        {
            if (progressUIPrefab == null || progressUIParent == null || generationManager == null)
                return;

            UIUtils.ClearChildren(progressUIParent);
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

        private void OnGeneratorsRebuilt()
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
            if (collectAllButton == null && availableResourcesText == null && disciplePercentText == null)
                return;

            var canCollect = false;
            double totalAvailable = 0;

            if (generationManager != null)
                foreach (var gen in generationManager.Generators)
                {
                    if (gen == null || !gen.RequirementsMet)
                        continue;

                    var res = gen.Resource;
                    if (res == null) continue;
                    var stored = gen.GetStoredAmount(res);
                    totalAvailable += stored;
                    if (!canCollect && stored > 0)
                        canCollect = true;
                }

            if (collectAllButton != null)
                collectAllButton.interactable = canCollect;

            if (availableResourcesText != null)
                availableResourcesText.text = $"\u2514 {CalcUtils.FormatNumber(totalAvailable, true)}";

            if (disciplePercentText != null)
                disciplePercentText.text = $"Echo Power {DisciplePercent * 100f:0.#}%";
        }
    }
}