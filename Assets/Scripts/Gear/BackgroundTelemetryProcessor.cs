using System.Collections.Concurrent;
using System.Threading;
using Blindsided.SaveData;
using Blindsided.Utilities;
using UnityEngine;
using static Blindsided.Oracle;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Processes forge telemetry on a background thread to avoid main thread blocking.
    /// Thread-safe design using ConcurrentQueue for craft results.
    /// </summary>
    public class BackgroundTelemetryProcessor : MonoBehaviour
    {
        public static BackgroundTelemetryProcessor Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;


        private readonly ConcurrentQueue<CraftResult> _pendingResults = new();
        private Thread _processorThread;
        private volatile bool _running;
        private readonly AutoResetEvent _workAvailable = new(false);
        private volatile bool _highThroughputMode;

        /// <summary>
        /// Number of pending results waiting to be processed.
        /// </summary>
        public int PendingCount => _pendingResults.Count;

        /// <summary>
        /// Whether high throughput mode is active (reduces processing overhead).
        /// </summary>
        public bool IsHighThroughputMode => _highThroughputMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            StartProcessor();
        }

        private void OnDisable()
        {
            StopProcessor();
        }

        private void OnDestroy()
        {
            StopProcessor();
        }

        /// <summary>
        /// Starts the background processing thread.
        /// </summary>
        public void StartProcessor()
        {
            if (_running) return;

            _running = true;
            _processorThread = new Thread(ProcessLoop)
            {
                Name = "ForgeTelemetry",
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            };
            _processorThread.Start();
        }

        /// <summary>
        /// Stops the background processing thread.
        /// </summary>
        public void StopProcessor()
        {
            if (!_running) return;

            _running = false;
            _workAvailable.Set(); // Wake thread to exit

            if (_processorThread != null && _processorThread.IsAlive)
            {
                _processorThread.Join(1000);
                if (_processorThread.IsAlive)
                {
                    Debug.LogWarning("BackgroundTelemetryProcessor: Thread did not exit cleanly.");
                }
            }
            _processorThread = null;
        }

        /// <summary>
        /// Enqueues a craft result for background processing.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        public void EnqueueResult(CraftResult result)
        {
            _pendingResults.Enqueue(result);
            _workAvailable.Set();
        }

        /// <summary>
        /// Sets high throughput mode for autocrafting.
        /// In this mode, less critical telemetry may be skipped.
        /// </summary>
        public void SetHighThroughputMode(bool enabled)
        {
            _highThroughputMode = enabled;
        }

        /// <summary>
        /// Flushes all pending results synchronously (call before save).
        /// </summary>
        public void FlushSync()
        {
            var forge = oracle?.saveData?.Forge;
            if (forge == null) return;

            while (_pendingResults.TryDequeue(out var result))
            {
                ProcessResultThreadSafe(result, forge);
            }
        }

        private void ProcessLoop()
        {
            while (_running)
            {
                // Wait for work or timeout (100ms poll for clean shutdown)
                _workAvailable.WaitOne(100);

                var forge = oracle?.saveData?.Forge;
                if (forge == null) continue;

                // Process all pending results
                while (_pendingResults.TryDequeue(out var result))
                {
                    if (!_running) break;
                    ProcessResultThreadSafe(result, forge);
                }
            }
        }

        private void ProcessResultThreadSafe(CraftResult result, GameData.ForgeStats forge)
        {
            // Use Interlocked for simple counters
            Interlocked.Increment(ref forge.TotalCrafts);
            Interlocked.Increment(ref forge.CraftsSinceLastUpgrade);

            // Extract keys (string operations are thread-safe)
            var coreKey = result.Core != null ? result.Core.name : "(null)";
            var rarityKey = result.Rarity != null ? result.Rarity.name : "(null)";
            var slotKey = string.IsNullOrWhiteSpace(result.Slot) ? "(null)" : result.Slot;

            // Lock for dictionary operations
            lock (forge)
            {
                // Basic distributions
                forge.CraftsByCore.Increment(coreKey);
                forge.CraftsByRarity.Increment(rarityKey);
                forge.CraftsBySlot.Increment(slotKey);
                forge.CraftsBySlotTotals.Increment(slotKey);

                // Nested distributions
                forge.RarityCountsByCore.IncrementNested(coreKey, rarityKey);
                forge.SlotCountsByCore.IncrementNested(coreKey, slotKey);

                // Affix count distribution
                int affixCount = result.Item?.affixes?.Count ?? 0;
                if (!forge.AffixCountDistribution.ContainsKey(affixCount))
                    forge.AffixCountDistribution[affixCount] = 0;
                forge.AffixCountDistribution[affixCount]++;

                // Skip detailed stat tracking in high throughput mode or fast craft mode
                bool skipDetailedStats = _highThroughputMode || StaticReferences.FastCraftMode;
                if (!skipDetailedStats && result.Item?.affixes != null)
                {
                    ProcessStatRolls(forge, result.Item, rarityKey, slotKey);
                }

                // Upgrade tracking
                ProcessUpgradeTracking(forge, result, slotKey, rarityKey, coreKey);
            }
        }

        private void ProcessStatRolls(GameData.ForgeStats forge, GearItem item, string rarityKey, string slotKey)
        {
            foreach (var affix in item.affixes)
            {
                if (affix?.stat == null) continue;

                var def = affix.stat;
                var statId = string.IsNullOrWhiteSpace(def.id) ? def.name : def.id;

                // Global stat aggregates
                forge.StatRolls.UpdateStatAgg(statId, affix.value);

                // By rarity
                forge.StatRollsByRarity.UpdateNestedStatAgg(rarityKey, statId, affix.value);

                // By slot
                forge.StatRollsBySlot.UpdateNestedStatAgg(slotKey, statId, affix.value);

                // High roll tracking
                float thrQ = Mathf.Clamp01(forge.HighRollTopPercentThreshold);
                float thrVal = Mathf.Lerp(def.minRoll, def.maxRoll, thrQ);
                if (affix.value >= thrVal)
                    forge.HighRollsByStat.Increment(statId);

                // Cumulative totals
                forge.CumulativeStatTotalsByStat.IncrementDouble(statId, affix.value);

                // Highest single roll per stat
                forge.HighestRollByStat.UpdateMax(statId, affix.value);
            }
        }

        private void ProcessUpgradeTracking(
            GameData.ForgeStats forge,
            CraftResult result,
            string slotKey,
            string rarityKey,
            string coreKey)
        {
            // Upgrade score delta
            forge.UpgradeScoreDeltaBySlot.UpdateFloatAgg(slotKey, result.UpgradeScore);

            if (result.IsUpgrade)
            {
                forge.UpgradesBySlot.Increment(slotKey);
                forge.UpgradesByRarity.Increment(rarityKey);

                Interlocked.Increment(ref forge.TotalUpgradeEvents);

                // These need to be inside the lock since they read/modify multiple related fields
                forge.CumulativeCraftsBetweenUpgrades += forge.CraftsSinceLastUpgrade;
                if (forge.CraftsSinceLastUpgrade > forge.MaxCraftsBetweenUpgrades)
                    forge.MaxCraftsBetweenUpgrades = forge.CraftsSinceLastUpgrade;
                forge.AverageCraftsPerUpgrade = forge.TotalUpgradeEvents > 0
                    ? (float)(forge.CumulativeCraftsBetweenUpgrades / (double)forge.TotalUpgradeEvents)
                    : 0f;

                Interlocked.Exchange(ref forge.CraftsSinceLastUpgrade, 0);
            }

            // Best piece scores
            float pieceScore = Mathf.Max(0f, result.UpgradeScore);
            forge.BestPieceScoreBySlot.UpdateMax(slotKey, pieceScore);
            forge.BestPieceScoreByCore.UpdateMax(coreKey, pieceScore);
            forge.MinPieceScoreByCore.UpdateMin(coreKey, pieceScore);
            forge.MaxPieceScoreByCore.UpdateMax(coreKey, pieceScore);
            forge.BestPieceScoreByRarity.UpdateMax(rarityKey, pieceScore);

            // Absolute scores
            ProcessAbsoluteScores(forge, result.AbsoluteScore, slotKey, coreKey, rarityKey);
        }

        private void ProcessAbsoluteScores(
            GameData.ForgeStats forge,
            float absScore,
            string slotKey,
            string coreKey,
            string rarityKey)
        {
            // Initialize dictionaries if needed
            forge.BestAbsolutePieceScoreBySlot ??= new System.Collections.Generic.Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByCore ??= new System.Collections.Generic.Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByRarity ??= new System.Collections.Generic.Dictionary<string, float>();
            forge.BestAbsolutePieceSlotByCore ??= new System.Collections.Generic.Dictionary<string, string>();
            forge.BestAbsolutePieceSlotByRarity ??= new System.Collections.Generic.Dictionary<string, string>();

            // By slot
            forge.BestAbsolutePieceScoreBySlot.UpdateMax(slotKey, absScore);

            // By core (with slot tracking)
            if (!forge.BestAbsolutePieceScoreByCore.TryGetValue(coreKey, out var currentCoreScore) || absScore > currentCoreScore)
            {
                forge.BestAbsolutePieceScoreByCore[coreKey] = absScore;
                forge.BestAbsolutePieceSlotByCore[coreKey] = slotKey;
            }

            // By rarity (with slot tracking)
            if (!forge.BestAbsolutePieceScoreByRarity.TryGetValue(rarityKey, out var currentRarityScore) || absScore > currentRarityScore)
            {
                forge.BestAbsolutePieceScoreByRarity[rarityKey] = absScore;
                forge.BestAbsolutePieceSlotByRarity[rarityKey] = slotKey;
            }
        }
    }
}
