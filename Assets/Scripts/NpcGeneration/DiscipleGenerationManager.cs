using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class DiscipleGenerationManager : MonoBehaviour
    {
        public static DiscipleGenerationManager Instance { get; private set; }
        [SerializeField] private DiscipleGenerator generatorPrefab;
        [SerializeField] private string disciplePath = "Disciples";

        private readonly List<DiscipleGenerator> generators = new();

        public IReadOnlyList<DiscipleGenerator> Generators => generators;

        private void Awake()
        {
            Instance = this;
            OnLoadData += OnLoadDataHandler;
            OnQuestHandin += OnQuestHandinHandler;
        }

        private void OnDestroy()
        {
            OnLoadData -= OnLoadDataHandler;
            OnQuestHandin -= OnQuestHandinHandler;
            if (Instance == this)
                Instance = null;
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DeferredBuild());
        }

        private void OnQuestHandinHandler(string questId)
        {
            StartCoroutine(DeferredBuild());
        }

        private IEnumerator DeferredBuild()
        {
            yield return null;
            BuildGenerators();
        }

        private void BuildGenerators()
        {
            foreach (var gen in generators)
                if (gen != null)
                    Destroy(gen.gameObject);
            generators.Clear();

            if (generatorPrefab == null)
                return;

            var loadedDisciples = Resources.LoadAll<Disciple>(disciplePath);
            foreach (var d in loadedDisciples)
            {
                if (d == null) continue;
                if (!QuestCompleted(d.requiredQuest))
                    continue;

                var gen = Instantiate(generatorPrefab, transform);
                gen.name = d.name;
                gen.SetData(d);
                generators.Add(gen);
            }
        }




        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var gen in generators)
            {
                if (gen != null)
                    gen.Tick(dt);
            }
        }
    }
}
