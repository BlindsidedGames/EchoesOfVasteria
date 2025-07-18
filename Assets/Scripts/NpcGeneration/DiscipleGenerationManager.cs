using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class DiscipleGenerationManager : MonoBehaviour
    {
        public static DiscipleGenerationManager Instance { get; private set; }
        [SerializeField] private List<Disciple> disciples = new();
        [SerializeField] private DiscipleGenerator generatorPrefab;

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

            foreach (var d in disciples)
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

        private static bool QuestCompleted(QuestData quest)
        {
            if (quest == null)
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(quest.questId, out var rec) && rec.Completed;
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
