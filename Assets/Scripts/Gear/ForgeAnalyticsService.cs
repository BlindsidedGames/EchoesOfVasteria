using System.Collections.Generic;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Centralized service for recording all forge-related analytics and telemetry.
    /// Extracted from CraftingService to reduce code duplication and enable batch processing.
    /// </summary>
    public class ForgeAnalyticsService : MonoBehaviour
    {
        public static ForgeAnalyticsService Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;


        // Batch mode state
        private bool _batchMode;
        private readonly List<CraftResult> _pendingCrafts = new(64);
        private int _pendingCraftCount;
        private int _pendingUpgradeCount;

        // Background processing toggle
        private bool _useBackgroundProcessing = true;

        /// <summary>
        /// Whether to use background thread processing for telemetry.
        /// Set to false to process synchronously on main thread.
        /// </summary>
        public bool UseBackgroundProcessing
        {
            get => _useBackgroundProcessing;
            set => _useBackgroundProcessing = value;
        }

        /// <summary>
        /// Whether batch mode is currently active.
        /// </summary>
        public bool IsBatchMode => _batchMode;

        /// <summary>
        /// Number of crafts pending in batch.
        /// </summary>
        public int PendingCraftCount => _pendingCraftCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #region Batch Mode

        /// <summary>
        /// Begins batch recording mode. All craft results are queued until EndBatch is called.
        /// Use during autocrafting to reduce per-craft overhead.
        /// </summary>
        public void BeginBatch()
        {
            _batchMode = true;
            _pendingCrafts.Clear();
            _pendingCraftCount = 0;
            _pendingUpgradeCount = 0;

            // Enable high-throughput mode on background processor
            BackgroundTelemetryProcessor.Instance?.SetHighThroughputMode(true);
        }

        /// <summary>
        /// Ends batch mode and flushes all pending results.
        /// </summary>
        /// <returns>Summary of the batch (total crafts, upgrades found)</returns>
        public (int totalCrafts, int upgradesFound) EndBatch()
        {
            _batchMode = false;
            var crafts = _pendingCraftCount;
            var upgrades = _pendingUpgradeCount;
            FlushBatch();

            // Disable high-throughput mode on background processor
            BackgroundTelemetryProcessor.Instance?.SetHighThroughputMode(false);

            return (crafts, upgrades);
        }

        /// <summary>
        /// Processes all pending craft results in batch.
        /// Called automatically by EndBatch.
        /// </summary>
        public void FlushBatch()
        {
            if (_pendingCrafts.Count == 0) return;

            // Route to background processor if available and enabled
            var bgProcessor = _useBackgroundProcessing ? BackgroundTelemetryProcessor.Instance : null;

            foreach (var result in _pendingCrafts)
            {
                if (bgProcessor != null)
                    bgProcessor.EnqueueResult(result);
                else
                    RecordCraftInternal(result);
            }

            _pendingCrafts.Clear();
            _pendingCraftCount = 0;
            _pendingUpgradeCount = 0;
        }

        #endregion

        /// <summary>
        /// Records a completed craft with all relevant data.
        /// If batch mode is active, queues the result for later processing.
        /// If background processing is enabled, routes to background thread.
        /// </summary>
        public void RecordCraft(CraftResult result)
        {
            if (_batchMode)
            {
                _pendingCrafts.Add(result);
                _pendingCraftCount++;
                if (result.IsUpgrade)
                    _pendingUpgradeCount++;
                return;
            }

            // Route to background processor if available and enabled
            if (_useBackgroundProcessing && BackgroundTelemetryProcessor.Instance != null)
            {
                BackgroundTelemetryProcessor.Instance.EnqueueResult(result);
                return;
            }

            RecordCraftInternal(result);
        }

        private void RecordCraftInternal(CraftResult result)
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;

            var forge = oracle.saveData.Forge;

            // Totals & counters
            forge.TotalCrafts++;
            forge.CraftsSinceLastUpgrade++;

            // Key generation
            var coreKey = result.Core != null ? result.Core.name : "(null)";
            var rarityKey = result.Rarity != null ? result.Rarity.name : "(null)";
            var slotKey = string.IsNullOrWhiteSpace(result.Slot) ? "(null)" : result.Slot;

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

            // Record stat rolls (skip in fast craft mode for performance)
            if (!StaticReferences.FastCraftMode)
                RecordStatRolls(forge, result.Item, rarityKey, slotKey);

            // Upgrade evaluation
            RecordUpgradeEvaluation(forge, result, slotKey, rarityKey, coreKey);
        }

        private void RecordStatRolls(GameData.ForgeStats forge, GearItem item, string rarityKey, string slotKey)
        {
            if (item?.affixes == null) return;

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

        private void RecordUpgradeEvaluation(
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

                forge.TotalUpgradeEvents++;
                forge.CumulativeCraftsBetweenUpgrades += forge.CraftsSinceLastUpgrade;
                if (forge.CraftsSinceLastUpgrade > forge.MaxCraftsBetweenUpgrades)
                    forge.MaxCraftsBetweenUpgrades = forge.CraftsSinceLastUpgrade;
                forge.AverageCraftsPerUpgrade = forge.TotalUpgradeEvents > 0
                    ? (float)(forge.CumulativeCraftsBetweenUpgrades / (double)forge.TotalUpgradeEvents)
                    : 0f;
                forge.CraftsSinceLastUpgrade = 0;
            }

            // Best piece scores (delta vs current)
            float pieceScore = Mathf.Max(0f, result.UpgradeScore);
            forge.BestPieceScoreBySlot.UpdateMax(slotKey, pieceScore);
            forge.BestPieceScoreByCore.UpdateMax(coreKey, pieceScore);
            forge.MinPieceScoreByCore.UpdateMin(coreKey, pieceScore);
            forge.MaxPieceScoreByCore.UpdateMax(coreKey, pieceScore);
            forge.BestPieceScoreByRarity.UpdateMax(rarityKey, pieceScore);

            // Best absolute piece scores (independent of equipped)
            RecordAbsoluteScores(forge, result.AbsoluteScore, slotKey, coreKey, rarityKey);
        }

        private void RecordAbsoluteScores(
            GameData.ForgeStats forge,
            float absScore,
            string slotKey,
            string coreKey,
            string rarityKey)
        {
            // Initialize dictionaries if needed
            forge.BestAbsolutePieceScoreBySlot ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByCore ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByRarity ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceSlotByCore ??= new Dictionary<string, string>();
            forge.BestAbsolutePieceSlotByRarity ??= new Dictionary<string, string>();

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

        /// <summary>
        /// Records resource spending for crafting operations.
        /// </summary>
        public void RecordResourceSpend(CoreSO core, Resource ingotResource, int ingotCost, Resource coreResource, int coreCost)
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;

            var forge = oracle.saveData.Forge;

            // Resource spend tracking
            if (ingotResource != null)
                forge.ResourcesSpent.IncrementDouble(ingotResource.name, ingotCost);

            if (coreResource != null)
                forge.ResourcesSpent.IncrementDouble(coreResource.name, coreCost);

            // Per-core tracking
            if (core != null)
            {
                var coreKey = core.name;
                forge.CoresSpentByCore.IncrementDouble(coreKey, coreCost);
                forge.IngotsSpentByCore.IncrementDouble(coreKey, ingotCost);
            }
        }

        /// <summary>
        /// Records Ivan XP telemetry after XP is granted.
        /// </summary>
        public void RecordIvanXp(CoreSO core, RaritySO rarity, int xpGranted, int levelBefore, int levelAfter)
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;

            var forge = oracle.saveData.Forge;

            forge.IvanLevelAtCraft = levelAfter;
            forge.IvanXpAtCraft = oracle.saveData.CraftingMasteryXP;
            forge.IvanXpGainedTotal += xpGranted;

            int levelUps = Mathf.Max(0, levelAfter - levelBefore);
            forge.IvanLevelUpsFromCrafts += levelUps;

            var coreKey = core != null ? core.name : "(null)";
            var rarityKey = rarity != null ? rarity.name : "(null)";

            forge.IvanXpByCore.IncrementDouble(coreKey, xpGranted);
            forge.IvanXpByRarity.IncrementDouble(rarityKey, xpGranted);
        }

        /// <summary>
        /// Records a conversion operation (ingot, crystal, chunk, or core).
        /// </summary>
        public void RecordConversion(ConversionType type, CoreSO core, double amount, Dictionary<Resource, double> costs)
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;

            var forge = oracle.saveData.Forge;
            var coreKey = core != null ? core.name : "(null)";

            // Record resource costs
            if (costs != null)
            {
                foreach (var kvp in costs)
                {
                    if (kvp.Key != null)
                        forge.ConversionSpentByResource.IncrementDouble(kvp.Key.name, kvp.Value);
                }
            }

            // Record type-specific metrics
            switch (type)
            {
                case ConversionType.Ingot:
                    forge.IngotConversions++;
                    forge.IngotsCraftedByResource.IncrementDouble(coreKey, amount);
                    break;

                case ConversionType.Crystal:
                    forge.CrystalCrafted += amount;
                    forge.CrystalsCraftedByResource.IncrementDouble(coreKey, amount);
                    break;

                case ConversionType.Chunk:
                    forge.ChunksCrafted += amount;
                    forge.ChunksCraftedByResource.IncrementDouble(coreKey, amount);
                    break;

                case ConversionType.Core:
                    forge.CoreConversions++;
                    forge.CoresCraftedByResource.IncrementDouble(coreKey, amount);
                    break;
            }
        }

        /// <summary>
        /// Records an autocraft session start.
        /// </summary>
        public void RecordAutocraftStart()
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;
            oracle.saveData.Forge.TotalAutocraftSessions++;
        }

        /// <summary>
        /// Records an autocraft session stop with reason.
        /// </summary>
        public void RecordAutocraftStop(string reason, GearItem lastItem)
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;

            var forge = oracle.saveData.Forge;
            forge.AutocraftStopReasons.Increment(reason ?? "Unknown");

            // Track best rarity found during autocraft by slot
            if (lastItem?.rarity != null && !string.IsNullOrWhiteSpace(lastItem.slot))
            {
                var slotKey = lastItem.slot;
                int rarityTier = lastItem.rarity.tierIndex;

                if (!forge.AutocraftBestRarityTierBySlot.TryGetValue(slotKey, out var current) || rarityTier > current)
                    forge.AutocraftBestRarityTierBySlot[slotKey] = rarityTier;
            }
        }

        /// <summary>
        /// Records a salvage operation.
        /// </summary>
        public void RecordSalvage(GearItem item, Dictionary<Resource, double> yields, bool isAuto)
        {
            if (oracle == null || oracle.saveData?.Forge == null || item == null) return;

            var forge = oracle.saveData.Forge;

            forge.TotalSalvaged++;
            forge.SalvageItems++;

            var rarityKey = item.rarity != null ? item.rarity.name : "(null)";
            var coreKey = item.core != null ? item.core.name : "(null)";
            var slotKey = string.IsNullOrWhiteSpace(item.slot) ? "(null)" : item.slot;

            forge.SalvagesByRarity.Increment(rarityKey);
            forge.SalvagesByCore.Increment(coreKey);
            forge.SalvagesBySlot.Increment(slotKey);

            // Record yields
            if (yields != null)
            {
                foreach (var kvp in yields)
                {
                    if (kvp.Key == null) continue;

                    forge.SalvageEntries++;
                    forge.ResourcesGainedFromSalvage.IncrementDouble(kvp.Key.name, kvp.Value);

                    // Yield aggregation
                    var yieldAgg = forge.SalvageYieldPerResource.GetOrCreate(
                        kvp.Key.name,
                        () => new GameData.ForgeStats.ResourceAgg());
                    yieldAgg.count++;
                    yieldAgg.sum += kvp.Value;
                }
            }
        }

        /// <summary>
        /// Records when a crafted item is equipped.
        /// </summary>
        public void RecordEquip(GearItem item)
        {
            if (oracle == null || oracle.saveData?.Forge == null || item == null) return;

            var forge = oracle.saveData.Forge;
            forge.TotalEquippedFromCraft++;

            var slotKey = string.IsNullOrWhiteSpace(item.slot) ? "(null)" : item.slot;
            forge.EquipsBySlot.Increment(slotKey);
        }

        /// <summary>
        /// Records a failed craft attempt (insufficient resources, etc.).
        /// </summary>
        public void RecordFailedCraftAttempt()
        {
            if (oracle == null || oracle.saveData?.Forge == null) return;
            oracle.saveData.Forge.TotalFailedCraftAttempts++;
        }
    }

    /// <summary>
    /// Data structure containing all information about a completed craft.
    /// </summary>
    public struct CraftResult
    {
        public CoreSO Core;
        public RaritySO Rarity;
        public string Slot;
        public GearItem Item;
        public GearItem EquippedComparison;
        public float UpgradeScore;
        public float AbsoluteScore;
        public bool IsUpgrade;
        public int IvanXpGranted;
    }

    /// <summary>
    /// Types of conversion operations in the forge.
    /// </summary>
    public enum ConversionType
    {
        Ingot,
        Crystal,
        Chunk,
        Core
    }
}
