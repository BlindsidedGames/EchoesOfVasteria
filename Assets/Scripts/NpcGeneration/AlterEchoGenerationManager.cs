using System;
using System.Collections;
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
        public IReadOnlyList<AlterEchoGenerator> Generators => generators;
        public event Action OnGeneratorsRebuilt;
        private static Dictionary<string, Resource> lookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => lookup = null;

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
            AwayFor += HandleAwayForTime;
            ApplicationBackgrounded += HandleApplicationBackground;
            ApplicationForegrounded += HandleApplicationForeground;
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
            AwayFor -= HandleAwayForTime;
            ApplicationBackgrounded -= HandleApplicationBackground;
            ApplicationForegrounded -= HandleApplicationForeground;
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
            CoroutineUtils.RunNextFrame(this, () =>
            {
                BuildGenerators();
                ApplyOfflineProgress();
            });
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
        private void CaptureOfflineSnapshot(double timestamp)
        {
            if (oracle == null) return;
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null) continue;
                if (!oracle.saveData.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                {
                    rec = new GameData.DiscipleGenerationRecord
                    {
                        StoredResources = new Dictionary<string, double>(),
                        TotalCollected = new Dictionary<string, double>()
                    };
                    oracle.saveData.Disciples[gen.Resource.name] = rec;
                }
                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();
                rec.StoredResources[gen.Resource.name] = gen.GetStoredAmount(gen.Resource);
                rec.TotalCollected[gen.Resource.name] = gen.GetTotalCollected(gen.Resource);
                rec.Progress = gen.Progress;
                rec.LastGenerationTime = timestamp;
            }
        }

        private static double GetUtcTimestamp()
        {
            return DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        private void ApplyOfflineProgress()
        {
            if (oracle == null) return;
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            var now = GetUtcTimestamp();
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null) continue;
                if (!oracle.saveData.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                    continue;
                var seconds = now - rec.LastGenerationTime;
                if (seconds <= 0) continue;
                gen.ApplyOfflineProgress(seconds);
                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();
                rec.LastGenerationTime = now;
                rec.Progress = gen.Progress;
                rec.StoredResources[gen.Resource.name] = gen.GetStoredAmount(gen.Resource);
                rec.TotalCollected[gen.Resource.name] = gen.GetTotalCollected(gen.Resource);
            }
        }

        private void HandleApplicationBackground()
        {
            CaptureOfflineSnapshot(GetUtcTimestamp());
        }

        private void HandleApplicationForeground()
        {
            ApplyOfflineProgress();
        }

        private void HandleAwayForTime(float seconds)
        {
            ApplyOfflineProgress();
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
            TickGenerators(Time.unscaledDeltaTime);

            if (!ratesDirty) return;

            var now = Time.unscaledTime;
            if (now >= nextRatesRefreshTime)
            {
                ratesDirty = false;
                nextRatesRefreshTime = now + 0.25f; // refresh at most 4 Hz
                RefreshRates();
            }
        }

        internal void TickGenerators(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;

            foreach (var gen in generators)
            {
                if (gen != null)
                    gen.Tick(deltaSeconds);
            }
        }
    }
}
