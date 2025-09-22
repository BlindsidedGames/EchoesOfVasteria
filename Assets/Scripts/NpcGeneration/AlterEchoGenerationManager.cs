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
        [SerializeField] private float resumeApplyDebounceSeconds = 0.5f;
        private float lastResumeApplyRealtime;
        private Coroutine applyOfflineRoutine;
        private const string OfflineLogPrefix = "[AlterEchoOffline]";

        private static void LogOffline(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{OfflineLogPrefix} {message}");
#else
            if (Debug.isDebugBuild)
            {
                Debug.Log($"{OfflineLogPrefix} {message}");
            }
#endif
        }

        private static void LogOfflineWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"{OfflineLogPrefix} {message}");
#else
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning($"{OfflineLogPrefix} {message}");
            }
#endif
        }

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

        private void CaptureOfflineSnapshot(double timestamp)
        {
            if (oracle == null)
            {
                LogOfflineWarning("CaptureOfflineSnapshot skipped because oracle is null.");
                return;
            }

            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            var trackedGenerators = 0;
            var skippedGenerators = 0;

            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null)
                {
                    skippedGenerators++;
                    continue;
                }

                if (!oracle.saveData.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                {
                    rec = new GameData.DiscipleGenerationRecord
                    {
                        StoredResources = new Dictionary<string, double>(),
                        TotalCollected = new Dictionary<string, double>()
                    };
                    oracle.saveData.Disciples[gen.Resource.name] = rec;
                    LogOffline($"CaptureOfflineSnapshot created new record for {gen.Resource.name}.");
                }

                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();
                var storedAmount = gen.GetStoredAmount(gen.Resource);
                var totalCollected = gen.GetTotalCollected(gen.Resource);
                rec.StoredResources[gen.Resource.name] = storedAmount;
                rec.TotalCollected[gen.Resource.name] = totalCollected;
                rec.Progress = gen.Progress;
                rec.LastGenerationTime = timestamp;
                trackedGenerators++;
            }

            LogOffline($"CaptureOfflineSnapshot complete at {timestamp:F0}: trackedGenerators={trackedGenerators}, skippedGenerators={skippedGenerators}.");
        }

        private void RecordFocusLoss(string reason)
        {
            if (oracle == null)
            {
                LogOfflineWarning($"RecordFocusLoss skipped ({reason}) because oracle is null.");
                return;
            }

            if (applyOfflineRoutine != null)
            {
                StopCoroutine(applyOfflineRoutine);
                applyOfflineRoutine = null;
            }

            var timestamp = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            LogOffline($"RecordFocusLoss capturing snapshot ({reason}) at {timestamp:F0} (generators={generators.Count}).");
            CaptureOfflineSnapshot(timestamp);
            lastResumeApplyRealtime = Time.realtimeSinceStartup;
        }

        private void QueueOfflineApply(string reason)
        {
            if (!isActiveAndEnabled)
            {
                LogOffline($"QueueOfflineApply skipped ({reason}) because manager is inactive.");
                return;
            }

            if (applyOfflineRoutine != null)
            {
                LogOffline($"QueueOfflineApply skipped ({reason}) because apply routine is already running.");
                return;
            }

            LogOffline($"QueueOfflineApply scheduled ({reason}).");
            applyOfflineRoutine = StartCoroutine(ApplyOfflineNextFrame(reason));
        }

        private IEnumerator ApplyOfflineNextFrame(string reason)
        {
            yield return null;
            try
            {
                LogOffline($"ApplyOfflineNextFrame executing ({reason}).");
                ApplyOfflineOnResume(reason);
            }
            catch (Exception ex)
            {
                LogOfflineWarning($"ApplyOfflineOnResume failed ({reason}): {ex.Message}");
            }
            applyOfflineRoutine = null;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
                QueueOfflineApply("focus gained");
            else
                RecordFocusLoss("focus lost");
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                RecordFocusLoss("application paused");
            else
                QueueOfflineApply("application resumed");
        }

        /// <summary>
        /// Apply offline progress for all generators using the saved LastGenerationTime.
        /// Intended to be called on app resume or focus gain. It does not force a disk save;
        /// it updates in-memory save data timestamps.
        /// </summary>
        public void ApplyOfflineOnResume(string reason = "manual")
        {
            if (oracle == null)
            {
                LogOfflineWarning($"ApplyOfflineOnResume skipped ({reason}) because oracle is null.");
                return;
            }

            var nowRt = Time.realtimeSinceStartup;
            var deltaRt = nowRt - lastResumeApplyRealtime;
            var minInterval = Mathf.Max(0.05f, resumeApplyDebounceSeconds);
            LogOffline($"ApplyOfflineOnResume start ({reason}): deltaRt={deltaRt:F3}s, minInterval={minInterval:F3}s, runInBackground={Application.runInBackground}.");

            if (Application.runInBackground && deltaRt > 0f)
            {
                lastResumeApplyRealtime = nowRt;
                LogOffline($"ApplyOfflineOnResume aborted ({reason}) because application runs in background (deltaRt={deltaRt:F3}s).");
                return;
            }

            if (deltaRt > minInterval)
            {
                lastResumeApplyRealtime = nowRt;
                LogOffline($"ApplyOfflineOnResume debounced ({reason}) because deltaRt={deltaRt:F3}s exceeded minInterval={minInterval:F3}s.");
                return;
            }

            lastResumeApplyRealtime = nowRt;

            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            var now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

            var appliedGenerators = 0;
            var missingRecords = 0;
            var totalSecondsApplied = 0d;

            foreach (var gen in generators)
            {
                if (gen == null || gen.Resource == null) continue;
                if (!oracle.saveData.Disciples.TryGetValue(gen.Resource.name, out var rec) || rec == null)
                {
                    missingRecords++;
                    LogOffline($"ApplyOfflineOnResume skipped generator {gen.Resource.name} ({reason}) due to missing record.");
                    continue;
                }

                var seconds = now - rec.LastGenerationTime;
                if (seconds <= 0) continue;

                gen.ApplyOfflineProgress(seconds);

                rec.StoredResources ??= new Dictionary<string, double>();
                rec.TotalCollected ??= new Dictionary<string, double>();

                rec.LastGenerationTime = now;
                rec.Progress = gen.Progress;

                var storedAmount = gen.GetStoredAmount(gen.Resource);
                var totalCollected = gen.GetTotalCollected(gen.Resource);

                rec.StoredResources[gen.Resource.name] = storedAmount;
                rec.TotalCollected[gen.Resource.name] = totalCollected;

                appliedGenerators++;
                totalSecondsApplied += seconds;

                LogOffline($"ApplyOfflineOnResume applied {seconds:F1}s to {gen.Resource.name} ({reason}); stored={storedAmount:F2}, progress={gen.Progress:F3}.");
            }

            LogOffline($"ApplyOfflineOnResume complete ({reason}): appliedGenerators={appliedGenerators}, totalSeconds={totalSecondsApplied:F1}, missingRecords={missingRecords}, totalGenerators={generators.Count}.");
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
