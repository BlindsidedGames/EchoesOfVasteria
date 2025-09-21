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
using static Blindsided.SaveData.StaticReferences;
using static TimelessEchoes.Upgrades.CauldronManager;


namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class AlterEchoGenerationManager : Singleton<AlterEchoGenerationManager>
    {
        [SerializeField] private AlterEchoGenerator generatorPrefab;

        private readonly List<AlterEchoGenerator> generators = new();

        private ResourceManager resourceManager;
        private GameplayStatTracker statTracker;
        private int lastUnlockedCount;
        private bool ratesDirty;
        private float nextRatesRefreshTime;
        [SerializeField] private float resumeApplyDebounceSeconds = 0.5f;
        private float lastResumeApplyRealtime;

        public IReadOnlyList<AlterEchoGenerator> Generators => generators;

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
            {
                if (!oracle.saveData.Resources.ContainsKey(key))
                {
                    toRemove.Add(key);
                    continue;
                }

                if (lookup.TryGetValue(key, out var res) && res != null && res.DisableAlterEcho)
                    toRemove.Add(key);
            }
            foreach (var k in toRemove)
                oracle.saveData.Disciples.Remove(k);

            lastUnlockedCount = 0;
            foreach (var pair in oracle.saveData.Resources)
            {
                if (!pair.Value.Earned) continue;
                lastUnlockedCount++;
                if (!lookup.TryGetValue(pair.Key, out var res) || res == null || res.DisableAlterEcho)
                    continue;

                var gen = Instantiate(generatorPrefab, transform);
                gen.name = res.name;
                var baseRate = pair.Value.BestPerMinute * DisciplePercent;
                var bonusMult = Singleton<CauldronManager>.Instance != null
                    ? Singleton<CauldronManager>.Instance.GetResourceAlterEchoMultiplier(res.name)
                    : 1f;
                var rate = baseRate * bonusMult;
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
                {
                    var baseRate = entry.BestPerMinute * DisciplePercent;
                    var bonusMult = Singleton<CauldronManager>.Instance != null
                        ? Singleton<CauldronManager>.Instance.GetResourceAlterEchoMultiplier(gen.Resource.name)
                        : 1f;
                    gen.UpdateRate(baseRate * bonusMult);
                }
            }
        }

        /// <summary>
        /// Apply offline progress for all generators using the saved LastGenerationTime.
        /// Intended to be called on app resume/focus gain. Respects OfflineTimeActive
        /// and does not force a disk save; it updates in-memory save data timestamps.
        /// </summary>
        public void ApplyOfflineOnResume()
        {
            if (oracle == null) return;
            if (!Blindsided.SaveData.StaticReferences.OfflineTimeActive) return;

            // Coalesce multiple rapid resume signals (focus + unpause etc.)
            var nowRt = Time.realtimeSinceStartup;
            var deltaRt = nowRt - lastResumeApplyRealtime;
            var minInterval = Mathf.Max(0.05f, resumeApplyDebounceSeconds);
            if (deltaRt > 0f && deltaRt < minInterval)
                return;
            lastResumeApplyRealtime = nowRt;

            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            var now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null) continue;
                if (!oracle.saveData.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                    continue;

                var seconds = now - rec.LastGenerationTime;
                if (seconds <= 0) continue;

                // Apply to live generator state (stored/progress).
                gen.ApplyOfflineProgress(seconds);

                // Update in-memory save to reflect new baseline without forcing a save.
                rec.LastGenerationTime = now;
                rec.Progress = gen.Progress;
                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();
                rec.StoredResources[gen.Resource.name] = gen.GetStoredAmount(gen.Resource);
                rec.TotalCollected[gen.Resource.name] = gen.GetTotalCollected(gen.Resource);
            }
        }

        /// <summary>
        /// Request alter-echo rate refresh; will be coalesced and processed with a short cooldown
        /// to avoid excessive cost when many cards are granted rapidly.
        /// </summary>
        public void MarkRatesDirty()
        {
            ratesDirty = true;
            // allow immediate refresh if cooldown has elapsed
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            foreach (var gen in generators)
                if (gen != null)
                    gen.Tick(dt);

            // Coalesce expensive rate recomputations
            if (ratesDirty)
            {
                var now = Time.unscaledTime;
                if (now >= nextRatesRefreshTime)
                {
                    ratesDirty = false;
                    nextRatesRefreshTime = now + 0.25f; // refresh at most 4 Hz
                    RefreshRates();
                }
            }
        }
    }
}


