using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;


namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class DiscipleGenerationManager : Singleton<DiscipleGenerationManager>
    {
        [SerializeField] private DiscipleGenerator generatorPrefab;

        private readonly List<DiscipleGenerator> generators = new();

        private ResourceManager resourceManager;
        private GameplayStatTracker statTracker;
        private int lastUnlockedCount;

        public IReadOnlyList<DiscipleGenerator> Generators => generators;

        public event Action OnGeneratorsRebuilt;

        private static Dictionary<string, Resource> lookup;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            resourceManager = ResourceManager.Instance;
            statTracker = GameplayStatTracker.Instance;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChanged;
            if (statTracker != null)
                statTracker.OnRunEnded += OnRunEnded;
            OnLoadData += OnLoadDataHandler;
            OnQuestHandin += OnQuestHandinHandler;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChanged;
            if (statTracker != null)
                statTracker.OnRunEnded -= OnRunEnded;
            OnLoadData -= OnLoadDataHandler;
            OnQuestHandin -= OnQuestHandinHandler;
        }

        private void OnRunEnded(bool died)
        {
            RefreshRates();
        }

        private void OnInventoryChanged()
        {
            if (oracle == null) return;
            oracle.saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            var count = 0;
            foreach (var entry in oracle.saveData.Resources.Values)
                if (entry.Earned)
                    count++;
            if (count != lastUnlockedCount)
            {
                lastUnlockedCount = count;
                CoroutineUtils.RunNextFrame(this, BuildGenerators);
            }
        }

        private void OnLoadDataHandler()
        {
            CoroutineUtils.RunNextFrame(this, BuildGenerators);
        }

        private void OnQuestHandinHandler(string questId)
        {
            CoroutineUtils.RunNextFrame(this, BuildGenerators);
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in AssetCache.GetAll<Resource>(string.Empty))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }

        private void BuildGenerators()
        {
            foreach (var gen in generators)
                if (gen != null)
                    Destroy(gen.gameObject);
            generators.Clear();

            if (generatorPrefab == null || oracle == null)
                return;

            EnsureLookup();
            oracle.saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            // purge legacy entries that no longer map to resources
            var toRemove = new List<string>();
            foreach (var key in oracle.saveData.Disciples.Keys)
                if (!oracle.saveData.Resources.ContainsKey(key))
                    toRemove.Add(key);
            foreach (var k in toRemove)
                oracle.saveData.Disciples.Remove(k);

            lastUnlockedCount = 0;
            foreach (var pair in oracle.saveData.Resources)
            {
                if (!pair.Value.Earned) continue;
                lastUnlockedCount++;
                if (!lookup.TryGetValue(pair.Key, out var res) || res == null)
                    continue;

                var gen = Instantiate(generatorPrefab, transform);
                gen.name = res.name;
                var rate = pair.Value.BestPerMinute * oracle.saveData.DisciplePercent;
                gen.Configure(res, rate);
                generators.Add(gen);
            }

            OnGeneratorsRebuilt?.Invoke();
        }

        public void RefreshRates()
        {
            if (oracle == null) return;
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null) continue;
                if (oracle.saveData.Resources.TryGetValue(gen.Resource.name, out var entry))
                    gen.UpdateRate(entry.BestPerMinute * oracle.saveData.DisciplePercent);
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            foreach (var gen in generators)
                if (gen != null)
                    gen.Tick(dt);
        }
    }
}