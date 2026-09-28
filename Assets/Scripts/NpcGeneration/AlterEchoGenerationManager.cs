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
        private int rebuildRequestVersion;
        private GameData queuedRebuildData;
        private bool queuedOfflineProgress;
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
        }
        private void OnRunEnded(bool died)
        {
            RefreshRates();
        }
        private void OnInventoryChanged()
        {
            var data = oracle?.saveData;
            if (data == null) return;
            data.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            var count = 0;
            foreach (var entry in data.Resources.Values)
                if (entry?.Earned == true)
                    count++;
            if (count != lastUnlockedCount)
            {
                lastUnlockedCount = count;
                QueueGeneratorRebuild(data, applyOfflineProgress: false);
            }
        }
        private void OnLoadDataHandler()
        {
            var data = oracle?.saveData;
            if (data != null)
                QueueGeneratorRebuild(data, applyOfflineProgress: true);
        }
        private void OnQuestHandinHandler(string questId)
        {
            var data = oracle?.saveData;
            if (data != null)
                QueueGeneratorRebuild(data, applyOfflineProgress: false);
        }

        private void QueueGeneratorRebuild(GameData expectedData, bool applyOfflineProgress)
        {
            if (!ReferenceEquals(queuedRebuildData, expectedData))
            {
                queuedRebuildData = expectedData;
                queuedOfflineProgress = applyOfflineProgress;
            }
            else
            {
                queuedOfflineProgress |= applyOfflineProgress;
            }

            var requestVersion = ++rebuildRequestVersion;
            CoroutineUtils.RunNextFrame(this, () =>
            {
                if (requestVersion != rebuildRequestVersion)
                    return;
                if (!ReferenceEquals(oracle?.saveData, expectedData))
                {
                    queuedRebuildData = null;
                    queuedOfflineProgress = false;
                    return;
                }

                var shouldApplyOfflineProgress = queuedOfflineProgress;
                queuedRebuildData = null;
                queuedOfflineProgress = false;
                BuildGenerators(expectedData);
                if (shouldApplyOfflineProgress)
                    ApplyOfflineProgress(expectedData);
            });
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in AssetCache.GetAll<Resource>(string.Empty))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }
        private void BuildGenerators(GameData data)
        {
            if (generatorPrefab == null || data == null || !ReferenceEquals(oracle?.saveData, data))
                return;

            foreach (var gen in generators)
                if (gen != null)
                {
                    gen.PrepareForRebuild();
                    Destroy(gen.gameObject);
                }
            generators.Clear();
            EnsureLookup();
            data.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            data.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            // Records for missing, renamed, or temporarily disabled resources stay in the save.
            // They cost almost nothing and may become usable again when content is restored.
            lastUnlockedCount = 0;
            foreach (var pair in data.Resources)
            {
                if (pair.Value?.Earned != true) continue;
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
            var data = oracle?.saveData;
            if (data?.Resources == null) return;
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null || !gen.IsOwnedBy(data)) continue;
                if (data.Resources.TryGetValue(gen.Resource.name, out var entry) && entry != null)
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
            var data = oracle?.saveData;
            if (data == null) return;
            data.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null || !gen.IsOwnedBy(data)) continue;
                if (!data.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                {
                    rec = new GameData.DiscipleGenerationRecord
                    {
                        StoredResources = new Dictionary<string, double>(),
                        TotalCollected = new Dictionary<string, double>()
                    };
                    data.Disciples[gen.Resource.name] = rec;
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

        private void ApplyOfflineProgress(GameData expectedData = null)
        {
            var data = oracle?.saveData;
            if (data == null || (expectedData != null && !ReferenceEquals(data, expectedData)))
                return;
            data.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            var now = GetUtcTimestamp();
            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null || !gen.IsOwnedBy(data)) continue;
                if (!data.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                    continue;
                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();
                var lastGenerationTime = rec.LastGenerationTime;
                if (!(lastGenerationTime > 0d) || double.IsNaN(lastGenerationTime) ||
                    double.IsInfinity(lastGenerationTime) || lastGenerationTime > now)
                {
                    // Zero is the legacy/default "never captured" value. Treat invalid or future
                    // timestamps as a new baseline instead of granting decades of progress.
                    rec.LastGenerationTime = now;
                    rec.Progress = gen.Progress;
                    rec.StoredResources[gen.Resource.name] = gen.GetStoredAmount(gen.Resource);
                    rec.TotalCollected[gen.Resource.name] = gen.GetTotalCollected(gen.Resource);
                    continue;
                }

                var seconds = now - lastGenerationTime;
                if (seconds > 0d && !double.IsNaN(seconds) && !double.IsInfinity(seconds))
                    gen.ApplyOfflineProgress(seconds);
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
