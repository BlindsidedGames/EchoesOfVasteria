using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Builds UI entries for all disciple generators and handles collecting resources.
    /// </summary>
    public class GeneratorUIManager : MonoBehaviour
    {
        public static GeneratorUIManager Instance { get; private set; }
        [SerializeField] private NpcGeneratorProgressUI progressUIPrefab;
        [SerializeField] private Transform progressUIParent;
        [SerializeField] private Button collectAllButton;

        private GenerationManager generationManager;
        private readonly Dictionary<DiscipleGenerator, List<NpcGeneratorProgressUI>> entries = new();

        private void Awake()
        {
            Instance = this;
            generationManager = GenerationManager.Instance;
            if (collectAllButton != null)
                collectAllButton.onClick.AddListener(CollectAll);
            BuildEntries();
        }

        private void OnDestroy()
        {
            if (collectAllButton != null)
                collectAllButton.onClick.RemoveListener(CollectAll);
            if (Instance == this)
                Instance = null;
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

                var list = new List<NpcGeneratorProgressUI>();
                foreach (var entry in gen.ResourceEntries)
                {
                    if (entry.resource == null) continue;
                    var ui = Instantiate(progressUIPrefab, progressUIParent);
                    ui.SetData(gen, entry.resource, entry.amount);
                    list.Add(ui);
                }
                entries[gen] = list;
            }
        }

        private void CollectAll()
        {
            if (generationManager == null) return;
            foreach (var gen in generationManager.Generators)
                gen?.CollectResources();
        }
    }
}
