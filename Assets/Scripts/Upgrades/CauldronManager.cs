using System;
using System.Collections;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades.Cauldron;
using TimelessEchoes.Utilities;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Upgrades
{
	/// <summary>
	///     Manages Stew, Eva leveling, tasting rolls, and collection card counts.
	/// </summary>
	[DefaultExecutionOrder(-2)]
    public class CauldronManager : Singleton<CauldronManager>
    {
        [Header("Config")] [SerializeField] private CauldronConfig config;

        [Header("Options")] [SerializeField] private bool showAllCards;

        private ResourceManager resourceManager;
        private TimelessEchoes.Quests.QuestManager questManager;
        private bool tastingActive;
        private int sessionCardsGained;
        private int sessionTastings;
        private int countNothing;
        private int countAlterEcho; // legacy total (sum of subcategories)
        private int countBuffs;
        private int countLowCards;
        private int countEvasBlessing;
        private int countVastSurge;

        // Per-AE-subcategory session counters
        private int countAEFarming;
        private int countAEFishing;
        private int countAEMining;
        private int countAEWoodcutting;
        private int countAECombat;

        // Mapping of resources into AE subcategories, built lazily
        public enum AEResourceGroup { Farming, Fishing, Mining, Woodcutting, Looting, Combat }
        private readonly Dictionary<Resource, AEResourceGroup> resourceGroupMap = new();
        private readonly Dictionary<AEResourceGroup, List<string>> cachedGroupPools = new(); // group -> RES:<name>
        // Cached card pools to avoid rebuilding lists each tick
        private readonly List<string> poolAlterEchoCards = new(); // RES:<name>
        private readonly List<string> poolBuffCards = new(); // BUFF:<name>
        private readonly List<string> poolAllCards = new(); // union of above
        private readonly List<string> poolInfinityCards = new(); // INF:<Stat>
        private bool cardPoolsDirty = true;
        [Header("Perf Throttling")] [SerializeField] [Min(0.05f)] private float cardPoolsRebuildMinInterval = 0.5f;
        [SerializeField] [Min(0.05f)] private float weightsNotifyInterval = 0.25f;
        private float nextCardPoolsRebuildAllowed;
        private float nextWeightsNotifyTime;
        private Coroutine resourceSubscribeRoutine;
        private Coroutine tasteTickCoroutine;

        // Extracted services (Phase 1E.1)
        private CardTierCalculator _tierCalculator;
        private CardPoolManager _poolManager;
        private TasteRollResolver _rollResolver;
        private EvaProgressionService _evaProgression;
        private AEResourceGroupClassifier _groupClassifier;

        // Throttled actions (replacing manual throttle fields)
        private ThrottledAction _statsEmitThrottle;
        private ThrottledAction _sessionCardsEmitThrottle;
        private ThrottledAction _stewChangeThrottle;

        // Card batching for performance (Phase 2B.1)
        private readonly List<(string id, int delta)> _pendingCardGains = new();

        // Config hot-reload sync (once per second)
        private float _nextConfigSyncTime;

        // Fixed tick interval for tasting coroutine (decoupled from rollsPerSecond for UI responsiveness)
        // The time-accumulation in TasteTick handles any rollsPerSecond value automatically
        private const float UI_TICK_INTERVAL = 0.1f; // 10 Hz

        // Multi-roll per frame support
        private float _lastTasteTime;
        private float _accumulatedTasteTime;

        public event Action OnStewChanged;
        public event Action OnWeightsChanged;
        public event Action<string, int> OnCardGained; // (cardId, amount)
        public event Action OnTasteSessionStarted;
        public event Action OnTasteSessionStopped;
        public event Action<int> OnSessionCardsChanged;
        public event Action<TastingStats> OnStatsChanged;
        /// <summary>
        ///     Fired when mixing occurs. Amount is the total number of resource units consumed across both inputs.
        /// </summary>
        public event Action<int> OnResourcesMixed;

        public struct TastingStats
        {
            public int tastings;
            public int cardsGained;
            public int gainedNothing;
            public int alterEcho;
            public int buffs;
            public int lowCards;
            public int evasBlessing;
            public int vastSurge;
            // AE subcategories (persisted totals)
            public int aeFarming;
            public int aeFishing;
            public int aeMining;
            public int aeWoodcutting;
            public int aeLooting;
            public int aeCombat;
        }

        private TastingStats GetStatsSnapshot()
        {
            // Report persisted totals, not per-session
            return new TastingStats
            {
                tastings = oracle != null ? oracle.saveData.CauldronTotals.TotalTastings : 0,
                cardsGained = oracle != null ? oracle.saveData.CauldronTotals.TotalCards : 0,
                gainedNothing = oracle != null ? oracle.saveData.CauldronTotals.GainedNothing : 0,
                alterEcho = oracle != null ? oracle.saveData.CauldronTotals.AlterEcho : 0,
                buffs = oracle != null ? oracle.saveData.CauldronTotals.Buffs : 0,
                lowCards = oracle != null ? oracle.saveData.CauldronTotals.LowCards : 0,
                evasBlessing = oracle != null ? oracle.saveData.CauldronTotals.EvasBlessing : 0,
                vastSurge = oracle != null ? oracle.saveData.CauldronTotals.VastSurge : 0,
                aeFarming = oracle != null ? oracle.saveData.CauldronTotals.AEFarming : 0,
                aeFishing = oracle != null ? oracle.saveData.CauldronTotals.AEFishing : 0,
                aeMining = oracle != null ? oracle.saveData.CauldronTotals.AEMining : 0,
                aeWoodcutting = oracle != null ? oracle.saveData.CauldronTotals.AEWoodcutting : 0,
                aeLooting = oracle != null ? oracle.saveData.CauldronTotals.AELooting : 0,
                aeCombat = oracle != null ? oracle.saveData.CauldronTotals.AECombat : 0
            };
        }

        public double Stew
        {
            get => oracle != null ? oracle.saveData.CauldronStew : 0;
            private set
            {
                if (oracle == null) return;
                var oldValue = oracle.saveData.CauldronStew;
                oracle.saveData.CauldronStew = Math.Max(0, value);

                // Throttle stew change events to reduce UI update frequency
                // Always emit if changed significantly (> 10 stew) or if throttle allows
                var significantChange = Math.Abs(value - oldValue) > 10;
                if (significantChange || (_stewChangeThrottle?.TryExecute() ?? true))
                {
                    OnStewChanged?.Invoke();
                }
            }
        }

        public bool IsTasting => tastingActive;

        public int EvaLevel
        {
            get => _evaProgression?.Level ?? (oracle != null ? Math.Max(1, oracle.saveData.CauldronEvaLevel) : 1);
            private set
            {
                if (_evaProgression != null) _evaProgression.LoadFromSave(value, _evaProgression.Xp);
                if (oracle != null) oracle.saveData.CauldronEvaLevel = Math.Max(1, value);
            }
        }

        public double EvaXp
        {
            get => _evaProgression?.Xp ?? (oracle != null ? oracle.saveData.CauldronEvaXp : 0);
            private set
            {
                if (_evaProgression != null) _evaProgression.LoadFromSave(_evaProgression.Level, value);
                if (oracle != null) oracle.saveData.CauldronEvaXp = Math.Max(0, value);
            }
        }

        public bool ShowAllCards
        {
            get => oracle != null ? oracle.saveData.CauldronShowAllCards : showAllCards;
            set
            {
                if (oracle != null) oracle.saveData.CauldronShowAllCards = value;
                showAllCards = value;
            }
        }

        // -------- Tier Helpers & Bonuses --------
        private int GetTierFromThresholds(int count, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0)
                return 0;
            var tier = 0;
            for (var i = 0; i < thresholds.Length; i++)
                if (count >= thresholds[i]) tier = i + 1;
            return tier; // 0 when below first threshold
        }

        /// <summary>
        /// Returns normalized progress (0..1) toward the next tier for the given card id.
        /// Id should be formatted as "RES:<name>" or "BUFF:<name>" as used in save data.
        /// Returns 1 when already at max tier.
        /// </summary>
        public float GetTierFill01(string id)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.GetTierProgress(id);
            // Fallback to old logic if not initialized
            if (string.IsNullOrEmpty(id) || config == null || oracle == null)
                return 0f;

            var dict = oracle.saveData.CauldronCardCounts;
            var count = dict.TryGetValue(id, out var c) ? c : 0;

            int[] thresholds = null;
            if (id.StartsWith("RES:"))
                thresholds = config.resourceTierThresholds;
            else if (id.StartsWith("BUFF:"))
                thresholds = config.buffTierThresholds;

            if (thresholds == null || thresholds.Length == 0)
                return 0f;

            var tier = GetTierFromThresholds(count, thresholds); // 0..Length
            if (tier >= thresholds.Length)
                return 1f; // max tier => full

            // Compute progress between previous threshold (or 0) and next threshold
            var prev = tier <= 0 ? 0 : thresholds[Mathf.Clamp(tier - 1, 0, thresholds.Length - 1)];
            var next = thresholds[Mathf.Clamp(tier, 0, thresholds.Length - 1)];
            if (next <= prev)
                return 0f;
            return Mathf.InverseLerp(prev, next, count);
        }

        public int GetResourceTier(string resourceName)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.GetResourceTier(resourceName);
            // Fallback to old logic if not initialized
            if (oracle == null || string.IsNullOrEmpty(resourceName)) return 0;
            var key = $"RES:{resourceName}";
            var dict = oracle.saveData.CauldronCardCounts;
            var count = dict.TryGetValue(key, out var c) ? c : 0;
            return GetTierFromThresholds(count, config != null ? config.resourceTierThresholds : null);
        }

        public int GetBuffTier(string buffName)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.GetBuffTier(buffName);
            // Fallback to old logic if not initialized
            if (oracle == null || string.IsNullOrEmpty(buffName)) return 0;
            var key = $"BUFF:{buffName}";
            var dict = oracle.saveData.CauldronCardCounts;
            var count = dict.TryGetValue(key, out var c) ? c : 0;
            return GetTierFromThresholds(count, config != null ? config.buffTierThresholds : null);
        }

        public float GetResourceAlterEchoMultiplier(string resourceName)
        {
            // Multiplier applied to per-resource alter-echo generation rate
            if (config == null) return 1f;
            var tier = GetResourceTier(resourceName);
            if (tier <= 0 || config.resourcePowerBonusPerTier == null || config.resourcePowerBonusPerTier.Length == 0)
                return 1f;
            var idx = Mathf.Clamp(tier - 1, 0, config.resourcePowerBonusPerTier.Length - 1);
            var bonusPercent = config.resourcePowerBonusPerTier[idx];
            return 1f + Mathf.Max(0f, bonusPercent) / 100f;
        }

        public float GetBuffCooldownReductionPercent(string buffName)
        {
            if (config == null) return 0f;
            var tier = GetBuffTier(buffName);
            if (tier <= 0 || config.buffCooldownReductionPerTier == null || config.buffCooldownReductionPerTier.Length == 0)
                return 0f;
            var idx = Mathf.Clamp(tier - 1, 0, config.buffCooldownReductionPerTier.Length - 1);
            return Mathf.Max(0f, config.buffCooldownReductionPerTier[idx]);
        }

        public float GetBuffPowerPercent(string buffName)
        {
            var total = 0f;
            // Per-buff bonus from its own tier
            if (config != null)
            {
                var tier = GetBuffTier(buffName);
                if (tier > 0 && config.buffPowerBonusPerTier != null && config.buffPowerBonusPerTier.Length > 0)
                {
                    var idx = Mathf.Clamp(tier - 1, 0, config.buffPowerBonusPerTier.Length - 1);
                    total += Mathf.Max(0f, config.buffPowerBonusPerTier[idx]);
                }
            }

            // Global Buffs group bonus: +2.5% per Buffs group tier (Option A)
            var groupTier = GetBuffsGroupTier();
            if (groupTier > 0)
                total += 2.5f * groupTier;

            return total;
        }

        public float GetBuffPowerMultiplier(string buffName)
        {
            var percent = GetBuffPowerPercent(buffName);
            return 1f + percent / 100f;
        }

        // -------- Max Tier Helpers --------
        private bool IsResourceMaxed(string resourceName)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.IsResourceMaxed(resourceName);
            // Fallback to old logic if not initialized
            if (config == null) return false;
            var maxTier = config.resourceTierThresholds != null ? config.resourceTierThresholds.Length : 0;
            if (maxTier <= 0) return false;
            return GetResourceTier(resourceName) >= maxTier;
        }

        private bool IsBuffMaxed(string buffName)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.IsBuffMaxed(buffName);
            // Fallback to old logic if not initialized
            if (config == null) return false;
            var maxTier = config.buffTierThresholds != null ? config.buffTierThresholds.Length : 0;
            if (maxTier <= 0) return false;
            return GetBuffTier(buffName) >= maxTier;
        }

        private bool IsIdMaxed(string id)
        {
            // Delegate to service if available
            if (_tierCalculator != null) return _tierCalculator.IsMaxed(id);
            // Fallback to old logic if not initialized
            if (string.IsNullOrEmpty(id)) return false;
            if (id.StartsWith("RES:")) return IsResourceMaxed(id.Substring(4));
            if (id.StartsWith("BUFF:")) return IsBuffMaxed(id.Substring(5));
            return false; // INF has no max
        }

        /// <summary>
        /// Computes the Buffs collection group tier as the minimum tier among all eligible buffs (including maxed).
        /// Returns 0 when there are no eligible buffs.
        /// </summary>
        public int GetBuffsGroupTier()
        {
            var qm = questManager ?? TimelessEchoes.Quests.QuestManager.Instance;
            var any = false;
            var minTier = int.MaxValue;
            foreach (var buff in Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Buffs.BuffRecipe>())
            {
                if (buff == null) continue;
                var required = buff.requiredQuest;
                if (required != null && (qm == null || !qm.IsQuestCompleted(required)))
                    continue;
                any = true;
                var t = GetBuffTier(buff.name);
                if (t > 0 && t < minTier) minTier = t;
            }
            if (!any) return 0;
            return minTier == int.MaxValue ? 1 : Mathf.Max(1, minTier);
        }

        protected override void Awake()
        {
            base.Awake();
            resourceManager = ResourceManager.Instance;
            // Resolve QuestManager once up-front to avoid per-tick object searches
            questManager = TimelessEchoes.Quests.QuestManager.Instance;
            if (config == null)
                Log("CauldronConfig missing", TELogCategory.General, this);
            InitializeServices();
        }

        private void InitializeServices()
        {
            _tierCalculator = new CardTierCalculator(config, () => oracle?.saveData?.CauldronCardCounts);
            _groupClassifier = new AEResourceGroupClassifier();
            _poolManager = new CardPoolManager(
                _tierCalculator,
                _groupClassifier,
                () => resourceManager ?? ResourceManager.Instance,
                () => questManager ?? TimelessEchoes.Quests.QuestManager.Instance,
                cardPoolsRebuildMinInterval
            );
            _poolManager.Initialize(); // Cache asset lists and force initial rebuild (Phase 2B.2)
            _rollResolver = new TasteRollResolver(_poolManager, config);
            _evaProgression = new EvaProgressionService();
            _evaProgression.OnLevelUp += () => DebouncedWeightsChanged();

            // Initialize throttles
            _statsEmitThrottle = new ThrottledAction(0.2f);
            _sessionCardsEmitThrottle = new ThrottledAction(0.2f);
            _stewChangeThrottle = new ThrottledAction(config != null ? config.stewChangeThrottleInterval : 0.1f);

            // Sync Eva progression with current save data if available
            SyncEvaProgressionFromSave();
        }

        private void OnEnable()
        {
            // Start tasting coroutine at fixed 10 Hz; TasteTick's time-accumulation
            // handles any rollsPerSecond value automatically
            tasteTickCoroutine = StartCoroutine(TasteTickCoroutine());
            // Reset session stats on save load
            EventHandler.OnLoadData += ResetSessionStats;
            EventHandler.OnLoadData += HandleSaveDataLoaded;
            // Refresh weights when inventory or quests change
            resourceManager ??= ResourceManager.Instance;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChangedHandler;
            else
                resourceSubscribeRoutine = StartCoroutine(WaitAndSubscribeResourceManager());
            EventHandler.OnQuestHandin += OnQuestHandinHandler;
            TryAutoStartTasting();
        }

        private void OnDisable()
        {
            if (tasteTickCoroutine != null)
            {
                StopCoroutine(tasteTickCoroutine);
                tasteTickCoroutine = null;
            }
            EventHandler.OnLoadData -= ResetSessionStats;
            EventHandler.OnLoadData -= HandleSaveDataLoaded;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChangedHandler;
            if (resourceSubscribeRoutine != null)
            {
                StopCoroutine(resourceSubscribeRoutine);
                resourceSubscribeRoutine = null;
            }
            EventHandler.OnQuestHandin -= OnQuestHandinHandler;
        }

        // -------- Mixing --------
        public bool CanMix(Resource a, Resource b)
        {
            // Lazy resolve ResourceManager in case Awake order caused null
            if (resourceManager == null) resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;
            if (a == null || b == null || a == b) return false;
            return resourceManager.GetAmount(a) > 0 || resourceManager.GetAmount(b) > 0;
        }

        public double MixMax(Resource a, Resource b)
        {
            // Lazy resolve ResourceManager in case Awake order caused null
            if (resourceManager == null) resourceManager = ResourceManager.Instance;
            if (!CanMix(a, b)) return 0;
            var amountA = resourceManager.GetAmount(a);
            var amountB = resourceManager.GetAmount(b);
            if (amountA > 0) resourceManager.Spend(a, amountA);
            if (amountB > 0) resourceManager.Spend(b, amountB);
            var points = amountA * a.baseValue * a.valueMultiplier + amountB * b.baseValue * b.valueMultiplier;
            var stewGained = points / 100.0;
            Stew += stewGained;
            var totalUnits = (int)Mathf.RoundToInt((float)(amountA + amountB));
            if (totalUnits > 0)
            {
                var handler = OnResourcesMixed;
                if (handler != null)
                    handler(totalUnits);
            }
            TrySave();
            TryAutoStartTasting();
            return stewGained;
        }

        private double GetStewCostPerRoll()
        {
            var baseCost = config != null ? (double)config.stewPerRoll : 1d;
            var extra = GetCauldronExtraStewPerTaste();
            return Math.Max(0.0001d, baseCost + extra);
        }

        private double GetCauldronExtraStewPerTaste()
        {
            var controller = SkillController.Instance;
            if (controller == null)
                return 0d;

            var bonus = controller.Aggregator.GetCauldronTasteBonus();
            return Math.Max(0d, (double)bonus);
        }

        private float GetTasteCardMultiplier()
        {
            var controller = SkillController.Instance;
            if (controller == null)
                return 1f;

            var bonus = controller.Aggregator.GetCauldronTasteBonus();
            return bonus > 0f ? 1f + bonus : 1f;
        }

        private static int ScaleCardCount(int baseCount, float cardMultiplier)
        {
            if (baseCount <= 0)
                return 0;

            if (cardMultiplier <= 1f)
                return baseCount;

            var scaled = Mathf.RoundToInt(baseCount * cardMultiplier);
            return Math.Max(baseCount, scaled);
        }

        private bool HasStewForNextRoll()
        {
            return Stew >= GetStewCostPerRoll();
        }

        private void TryAutoStartTasting()
        {
            if (tastingActive) return;
            if (!HasStewForNextRoll()) return;
            StartTasting();
        }

        private void HandleSaveDataLoaded()
        {
            SyncEvaProgressionFromSave();
            TryAutoStartTasting();
        }

        private void SyncEvaProgressionFromSave()
        {
            if (_evaProgression != null && oracle?.saveData != null)
            {
                _evaProgression.LoadFromSave(oracle.saveData.CauldronEvaLevel, oracle.saveData.CauldronEvaXp);
            }
        }

        /// <summary>
        /// Syncs throttle intervals from config once per second, allowing runtime config tweaks.
        /// </summary>
        private void SyncConfigValuesIfNeeded()
        {
            var now = Time.unscaledTime;
            if (now < _nextConfigSyncTime) return;
            _nextConfigSyncTime = now + 1f;

            if (config == null) return;

            // Update stew change throttle interval from config
            _stewChangeThrottle?.SetInterval(config.stewChangeThrottleInterval);

            // Note: rollsPerSecond changes are handled automatically by TasteTick's time-accumulation.
            // The coroutine runs at fixed 10 Hz for consistent UI responsiveness.
        }

        // -------- Tasting --------
        public void StartTasting()
        {
            if (tastingActive) return;
            tastingActive = true;
            _lastTasteTime = Time.unscaledTime;
            _accumulatedTasteTime = 0f;
            ResetSessionStats();
            OnTasteSessionStarted?.Invoke();
            OnSessionCardsChanged?.Invoke(sessionCardsGained);
            var statsHandler = OnStatsChanged;
            if (statsHandler != null)
                statsHandler(GetStatsSnapshot());
        }

        public void StopTasting()
        {
            if (!tastingActive) return;
            tastingActive = false;
            OnTasteSessionStopped?.Invoke();
            TrySave();
        }

        private void TasteTick()
        {
            if (!tastingActive || config == null) return;
            SyncConfigValuesIfNeeded();

            // Calculate how many rolls should happen this frame based on elapsed time
            var now = Time.unscaledTime;
            var deltaTime = now - _lastTasteTime;
            _lastTasteTime = now;

            // Accumulate time and calculate rolls to process
            _accumulatedTasteTime += deltaTime;
            var rollInterval = 1f / Mathf.Max(1f, config.rollsPerSecond);
            var rollsToProcess = Mathf.FloorToInt(_accumulatedTasteTime / rollInterval);

            if (rollsToProcess <= 0) return;

            // Cap rolls per frame to prevent freezing (e.g., after alt-tab)
            const int maxRollsPerFrame = 500;
            rollsToProcess = Mathf.Min(rollsToProcess, maxRollsPerFrame);
            _accumulatedTasteTime -= rollsToProcess * rollInterval;

            var cost = GetStewCostPerRoll();
            var cardMultiplier = GetTasteCardMultiplier();

            for (int i = 0; i < rollsToProcess; i++)
            {
                if (Stew < cost)
                {
                    StopTasting();
                    return;
                }

                Stew -= cost;
                GainEvaXp(cost);
                sessionTastings++;
                if (oracle != null) oracle.saveData.CauldronTotals.TotalTastings++;
                ResolveTasteOutcome(cardMultiplier);
            }

            // Throttle stats UI updates to reduce canvas rebuilds
            if (ShouldEmitStatsNow())
            {
                var statsHandler = OnStatsChanged;
                if (statsHandler != null)
                    statsHandler(GetStatsSnapshot());
            }
        }

        private IEnumerator TasteTickCoroutine()
        {
            var wait = new WaitForSecondsRealtime(UI_TICK_INTERVAL);
            while (true)
            {
                TasteTick();
                yield return wait;
            }
        }

        private void GainEvaXp(double amount)
        {
            if (_evaProgression != null)
            {
                // Use the service (it will fire OnLevelUp which triggers DebouncedWeightsChanged)
                _evaProgression.GainXp(amount);
                // Sync to save data
                if (oracle != null)
                {
                    oracle.saveData.CauldronEvaLevel = _evaProgression.Level;
                    oracle.saveData.CauldronEvaXp = _evaProgression.Xp;
                }
            }
            else
            {
                // Fallback to old logic if service not initialized
                EvaXp += amount;
                while (EvaXp >= GetXpToNextLevel(EvaLevel))
                {
                    EvaXp -= GetXpToNextLevel(EvaLevel);
                    EvaLevel++;
                    OnWeightsChanged?.Invoke();
                }
            }
        }

        private double GetXpToNextLevel(int level)
        {
            // Delegate to service if available
            if (_evaProgression != null) return _evaProgression.GetXpToNextLevel(level);
            // Fallback to old logic if not initialized: 50 + 10*(level-1)
            return 50 + 10 * Mathf.Max(0, level - 1);
        }

        private enum RollType
        {
            Nothing,
            AlterEcho, // legacy single AE
            AEFarming,
            AEFishing,
            AEMining,
            AEWoodcutting,
            AELooting,
            AECombat,
            Buff,
            Lowest,
            EvasX2,
            VastX10,
            Infinity
        }

        private void ResolveTasteOutcome(float cardMultiplier)
        {
            var lvl = EvaLevel;
            var eff = ComputeEffectiveWeights(lvl);
            var wNothing = eff.wNothing;
            var wAE_Farm = eff.wAEFarming;
            var wAE_Fish = eff.wAEFishing;
            var wAE_Mine = eff.wAEMining;
            var wAE_Wood = eff.wAEWoodcutting;
            var wAE_Combat = eff.wAECombat;
            var wAE_Loot = eff.wAELooting;
            var wBuff = eff.wBuff;
            var wLow = eff.wLow;
            var wX2 = eff.wX2;
            var wX10 = eff.wX10;
            var wInfinity = eff.wInfinity;

            var subAEPresent = (wAE_Farm + wAE_Fish + wAE_Mine + wAE_Wood + wAE_Loot + wAE_Combat) > 0f;
            float total = wNothing + wBuff + wLow + wX2 + wX10 + wInfinity
                          + (subAEPresent ? (wAE_Farm + wAE_Fish + wAE_Mine + wAE_Wood + wAE_Loot + wAE_Combat) : 0f);
            // No legacy AE weight

            if (total <= 0f) return;

            var r = Random.value * total;
            RollType pick;
            if ((r -= wNothing) <= 0) pick = RollType.Nothing;
            else if (subAEPresent)
            {
                if ((r -= wAE_Farm) <= 0) pick = RollType.AEFarming;
                else if ((r -= wAE_Fish) <= 0) pick = RollType.AEFishing;
                else if ((r -= wAE_Mine) <= 0) pick = RollType.AEMining;
                else if ((r -= wAE_Wood) <= 0) pick = RollType.AEWoodcutting;
                else if ((r -= wAE_Loot) <= 0) pick = RollType.AELooting;
                else if ((r -= wAE_Combat) <= 0) pick = RollType.AECombat;
                else if ((r -= wInfinity) <= 0) pick = RollType.Infinity;
                else if ((r -= wBuff) <= 0) pick = RollType.Buff;
                else if ((r -= wLow) <= 0) pick = RollType.Lowest;
                else if ((r -= wX2) <= 0) pick = RollType.EvasX2;
                else pick = RollType.VastX10;
            }
            else
            {
                // If subcategories are not configured with non-zero weights, treat AE as absent
                if ((r -= wInfinity) <= 0) pick = RollType.Infinity;
                else if ((r -= wBuff) <= 0) pick = RollType.Buff;
                else if ((r -= wLow) <= 0) pick = RollType.Lowest;
                else if ((r -= wX2) <= 0) pick = RollType.EvasX2;
                else pick = RollType.VastX10;
            }

            switch (pick)
            {
                case RollType.Nothing:
                    countNothing++;
                    if (oracle != null) oracle.saveData.CauldronTotals.GainedNothing++;
                    break;
                // Legacy AlterEcho removed
                case RollType.AEFarming:
                    countAEFarming++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEFarming++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Farming, cardMultiplier);
                    break;
                case RollType.AEFishing:
                    countAEFishing++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEFishing++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Fishing, cardMultiplier);
                    break;
                case RollType.AEMining:
                    countAEMining++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEMining++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Mining, cardMultiplier);
                    break;
                case RollType.AEWoodcutting:
                    countAEWoodcutting++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEWoodcutting++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Woodcutting, cardMultiplier);
                    break;
                case RollType.AELooting:
                    countAECombat++; // session total remains under legacy AE; per-sub has its own counter below
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AELooting++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Looting, cardMultiplier);
                    break;
                case RollType.AECombat:
                    countAECombat++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AECombat++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Combat, cardMultiplier);
                    break;
                case RollType.Buff:
                    countBuffs++;
                    if (oracle != null) oracle.saveData.CauldronTotals.Buffs++;
                    GrantRandomCards(1, cardMultiplier, onlyBuffs: true);
                    break;
                case RollType.Lowest:
                    countLowCards++;
                    if (oracle != null) oracle.saveData.CauldronTotals.LowCards++;
                    GrantLowestCard(1, cardMultiplier);
                    break;
                case RollType.EvasX2:
                    countEvasBlessing++;
                    if (oracle != null) oracle.saveData.CauldronTotals.EvasBlessing++;
                    // Redirect to Infinity when all categories are maxed
                    if (poolAllCards.Count == 0 && poolInfinityCards.Count > 0) GrantRandomInfinityCards(2, cardMultiplier);
                    else GrantRandomCards(2, cardMultiplier);
                    break;
                case RollType.VastX10:
                    countVastSurge++;
                    if (oracle != null) oracle.saveData.CauldronTotals.VastSurge++;
                    if (poolAllCards.Count == 0 && poolInfinityCards.Count > 0) GrantRandomInfinityCards(10, cardMultiplier);
                    else GrantRandomCards(10, cardMultiplier);
                    break;
                case RollType.Infinity:
                    // Single Infinity gain
                    GrantRandomInfinityCards(1, cardMultiplier);
                    break;
            }
        }

        private void GrantRandomCards(int count, float cardMultiplier, bool onlyAlterEcho = false, bool onlyBuffs = false)
        {
            int draws = ScaleCardCount(count, cardMultiplier);
            if (draws <= 1)
            {
                // Single card - no batching needed
                var id = PickRandomCardId(onlyAlterEcho, onlyBuffs);
                if (id != null) AddCardCount(id, 1);
                return;
            }

            // Multiple cards - use batching for performance
            BeginCardBatch();
            for (var i = 0; i < draws; i++)
            {
                var id = PickRandomCardId(onlyAlterEcho, onlyBuffs);
                if (id != null) AddCardCountBatched(id, 1);
            }
            EndCardBatch();
        }

        private void GrantRandomInfinityCards(int count, float cardMultiplier)
        {
            int draws = ScaleCardCount(count, cardMultiplier);
            if (draws <= 1)
            {
                // Single card - no batching needed
                var id = PickRandomInfinityId();
                if (id != null) AddCardCount(id, 1);
                return;
            }

            // Multiple cards - use batching for performance
            BeginCardBatch();
            for (var i = 0; i < draws; i++)
            {
                var id = PickRandomInfinityId();
                if (id != null) AddCardCountBatched(id, 1);
            }
            EndCardBatch();
        }

        private void GrantRandomResourceCardFromGroup(AEResourceGroup group, float cardMultiplier)
        {
            int draws = ScaleCardCount(1, cardMultiplier);
            if (draws <= 1)
            {
                // Single card - no batching needed
                var id = PickRandomResourceCardIdByGroup(group);
                if (id == null) id = PickRandomCardId(onlyAlterEcho: true, onlyBuffs: false);
                if (id != null) AddCardCount(id, 1);
                return;
            }

            // Multiple cards - use batching for performance
            BeginCardBatch();
            for (var i = 0; i < draws; i++)
            {
                var id = PickRandomResourceCardIdByGroup(group);
                if (id == null) id = PickRandomCardId(onlyAlterEcho: true, onlyBuffs: false);
                if (id != null) AddCardCountBatched(id, 1);
            }
            EndCardBatch();
        }

        private string PickRandomCardId(bool onlyAlterEcho, bool onlyBuffs)
        {
            RebuildCardPoolsIfDirty();
            IList<string> pool;
            if (onlyBuffs) pool = poolBuffCards;
            else if (onlyAlterEcho) pool = poolAlterEchoCards;
            else pool = poolAllCards;
            if (pool == null || pool.Count == 0) return null;
            var idx = Random.Range(0, pool.Count);
            return pool[idx];
        }

        private string PickRandomInfinityId()
        {
            RebuildCardPoolsIfDirty();
            if (poolInfinityCards == null || poolInfinityCards.Count == 0) return null;
            var idx = Random.Range(0, poolInfinityCards.Count);
            return poolInfinityCards[idx];
        }

        private void GrantLowestCard(int count, float cardMultiplier)
        {
            int draws = ScaleCardCount(count, cardMultiplier);
            if (draws <= 1)
            {
                // Single card - no batching needed
                var id = GetLowestCountCardId();
                if (id != null) AddCardCount(id, 1);
                return;
            }

            // Multiple cards - use batching for performance
            BeginCardBatch();
            for (var i = 0; i < draws; i++)
            {
                var id = GetLowestCountCardId();
                if (id != null) AddCardCountBatched(id, 1);
            }
            EndCardBatch();
        }

        private string GetLowestCountCardId()
        {
            RebuildCardPoolsIfDirty();
            var all = poolAllCards;
            if (all == null || all.Count == 0) return null;
            var dict = oracle.saveData.CauldronCardCounts;
            var best = int.MaxValue;
            string chosen = null;
            foreach (var id in all)
            {
                var val = dict.TryGetValue(id, out var c) ? c : 0;
                if (val < best)
                {
                    best = val;
                    chosen = id;
                }
            }

            return chosen;
        }

        private void AddCardCount(string id, int delta)
        {
            var dict = oracle.saveData.CauldronCardCounts;

            // Detect pre-change tiers for buff updates
            int oldBuffTier = -1;
            int oldBuffsGroupTier = -1;
            bool isBuff = !string.IsNullOrEmpty(id) && id.StartsWith("BUFF:");
            bool isRes = !string.IsNullOrEmpty(id) && id.StartsWith("RES:");
            bool isInfinity = !string.IsNullOrEmpty(id) && id.StartsWith("INF:");
            string buffName = null;
            if (isBuff)
            {
                buffName = id.Substring(5);
                oldBuffTier = GetBuffTier(buffName);
                oldBuffsGroupTier = GetBuffsGroupTier();
            }

            // Clamp RES/BUFF at max tier; Infinity has no cap
            if (!isInfinity && IsIdMaxed(id))
                delta = 0;

            if (!dict.ContainsKey(id)) dict[id] = 0;
            dict[id] += delta;
            if (delta > 0)
            {
                sessionCardsGained += delta;
                OnCardGained?.Invoke(id, delta);
                if (ShouldEmitSessionCardsNow())
                    OnSessionCardsChanged?.Invoke(sessionCardsGained);
                if (oracle != null) oracle.saveData.CauldronTotals.TotalCards += delta;
                if (ShouldEmitStatsNow())
                {
                    var statsHandler = OnStatsChanged;
                    if (statsHandler != null)
                        statsHandler(GetStatsSnapshot());
                }
            }

            // Update alter-echo generation rates when card counts change
            try
            {
                TimelessEchoes.NpcGeneration.AlterEchoGenerationManager.Instance?.MarkRatesDirty();
            }
            catch (Exception)
            {
                // ignore: manager may not be available in some scenes
            }

            // If a buff tier or the Buffs group min tier changed, refresh active buff effects immediately
            if (isBuff)
            {
                int newBuffTier = GetBuffTier(buffName);
                int newBuffsGroupTier = GetBuffsGroupTier();
                if (newBuffTier != oldBuffTier || newBuffsGroupTier != oldBuffsGroupTier)
                {
                    try
                    {
                        TimelessEchoes.Buffs.BuffManager.Instance?.RecomputeActiveBuffEffects();
                    }
                    catch (Exception)
                    {
                        // ignore: buff manager may not be available in some scenes
                    }
                }
            }

            // If a RES or BUFF newly reached max, mark pools dirty and notify
            if (!isInfinity)
            {
                cardPoolsDirty = true;
                _poolManager?.MarkDirty();
                DebouncedWeightsChanged();
            }

            // Infinity affects hero stats directly; mark hero stats dirty
            try
            {
                TimelessEchoes.Hero.HeroStatSystem.MarkDirty(
                    TimelessEchoes.Hero.DirtyMask.Damage |
                    TimelessEchoes.Hero.DirtyMask.AttackRate |
                    TimelessEchoes.Hero.DirtyMask.CritChance |
                    TimelessEchoes.Hero.DirtyMask.Move |
                    TimelessEchoes.Hero.DirtyMask.Defense |
                    TimelessEchoes.Hero.DirtyMask.Regen |
                    TimelessEchoes.Hero.DirtyMask.MaxHealth,
                    TimelessEchoes.Hero.DirtyReason.BuffsChanged);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// Adds a card gain to the pending batch. Call FlushPendingCards() after batch is complete.
        /// </summary>
        private void AddCardCountBatched(string id, int delta)
        {
            _pendingCardGains.Add((id, delta));
        }

        /// <summary>
        /// Flushes all pending card gains in a single batch, triggering cascade updates only once.
        /// This dramatically reduces overhead when gaining multiple cards per taste (e.g., VastSurge x10).
        /// </summary>
        private void FlushPendingCards()
        {
            if (_pendingCardGains.Count == 0) return;

            bool anyBuffTierChanged = false;
            bool anyTierChanged = false;
            int totalCardsGained = 0;
            var dict = oracle.saveData.CauldronCardCounts;

            foreach (var (id, delta) in _pendingCardGains)
            {
                if (delta <= 0) continue;

                bool isInfinity = !string.IsNullOrEmpty(id) && id.StartsWith("INF:");

                // Skip if maxed (except infinity)
                if (!isInfinity && IsIdMaxed(id))
                    continue;

                // Track tier changes
                int oldTier = 0;
                if (!isInfinity && _tierCalculator != null)
                    oldTier = _tierCalculator.GetTier(id);

                // Update count
                if (!dict.ContainsKey(id)) dict[id] = 0;
                dict[id] += delta;
                totalCardsGained += delta;

                // Check for tier change
                if (!isInfinity && _tierCalculator != null)
                {
                    int newTier = _tierCalculator.GetTier(id);
                    if (newTier != oldTier)
                    {
                        anyTierChanged = true;
                        if (id.StartsWith("BUFF:"))
                            anyBuffTierChanged = true;
                    }
                }

                // Fire per-card event
                OnCardGained?.Invoke(id, delta);
            }

            if (totalCardsGained == 0)
            {
                _pendingCardGains.Clear();
                return;
            }

            // Update session stats
            sessionCardsGained += totalCardsGained;
            if (oracle != null) oracle.saveData.CauldronTotals.TotalCards += totalCardsGained;

            // Cascade updates ONCE (not per card)
            try { TimelessEchoes.NpcGeneration.AlterEchoGenerationManager.Instance?.MarkRatesDirty(); }
            catch { }

            if (anyBuffTierChanged)
            {
                try { TimelessEchoes.Buffs.BuffManager.Instance?.RecomputeActiveBuffEffects(); }
                catch { }
            }

            if (anyTierChanged)
            {
                cardPoolsDirty = true;
                _poolManager?.MarkDirty();
                DebouncedWeightsChanged();
            }

            try
            {
                TimelessEchoes.Hero.HeroStatSystem.MarkDirty(
                    TimelessEchoes.Hero.DirtyMask.Damage |
                    TimelessEchoes.Hero.DirtyMask.AttackRate |
                    TimelessEchoes.Hero.DirtyMask.CritChance |
                    TimelessEchoes.Hero.DirtyMask.Move |
                    TimelessEchoes.Hero.DirtyMask.Defense |
                    TimelessEchoes.Hero.DirtyMask.Regen |
                    TimelessEchoes.Hero.DirtyMask.MaxHealth,
                    TimelessEchoes.Hero.DirtyReason.BuffsChanged);
            }
            catch { }

            // Throttled UI updates
            if (ShouldEmitSessionCardsNow())
                OnSessionCardsChanged?.Invoke(sessionCardsGained);
            if (ShouldEmitStatsNow())
                OnStatsChanged?.Invoke(GetStatsSnapshot());

            _pendingCardGains.Clear();
        }

        /// <summary>
        /// Begins a batching session for card gains. Cards added via AddCardCountBatched will be held
        /// until EndCardBatch is called.
        /// </summary>
        private void BeginCardBatch()
        {
            _pendingCardGains.Clear();
        }

        /// <summary>
        /// Ends the batching session and flushes all pending cards.
        /// </summary>
        private void EndCardBatch()
        {
            FlushPendingCards();
        }

        private List<string> BuildAllCardIds(bool onlyAlterEcho, bool onlyBuffs)
        {
            // RES:<ResourceName> for alter-echo cards (non-disabled)
            // BUFF:<BuffName> for buff cards
            var list = new List<string>();
            // Use cached references; do not call Find* on hot paths
            var rm = resourceManager ?? ResourceManager.Instance;
            var qm = questManager ?? TimelessEchoes.Quests.QuestManager.Instance;
            if (!onlyBuffs)
            {
                foreach (var res in AssetCache.GetAll<Resource>())
                {
                    if (res == null || res.DisableAlterEcho) continue;
                    if (rm != null && rm.IsUnlocked(res))
                        list.Add(res.CardId);
                }
            }
            if (!onlyAlterEcho)
            {
                foreach (var buff in AssetCache.GetAll<BuffRecipe>())
                {
                    if (buff == null) continue;
                    var required = buff.requiredQuest;
                    if (required == null || (qm != null && qm.IsQuestCompleted(required)))
                        list.Add(buff.CardId);
                }
            }
            return list;
        }

        private void RebuildCardPoolsIfDirty()
        {
            if (!cardPoolsDirty) return;
            var now = Time.unscaledTime;
            if (now < nextCardPoolsRebuildAllowed) return; // defer rebuilds to avoid spamming
            nextCardPoolsRebuildAllowed = now + Mathf.Max(0.05f, cardPoolsRebuildMinInterval);
            cardPoolsDirty = false;

            poolAlterEchoCards.Clear();
            poolBuffCards.Clear();
            poolAllCards.Clear();
            poolInfinityCards.Clear();

            var rm = resourceManager ?? ResourceManager.Instance;
            var qm = questManager ?? TimelessEchoes.Quests.QuestManager.Instance;

            foreach (var res in AssetCache.GetAll<Resource>())
            {
                if (res == null || res.DisableAlterEcho) continue;
                if (rm != null && rm.IsUnlocked(res))
                {
                    var id = res.CardId;
                    // Skip maxed resources
                    if (!IsIdMaxed(id))
                    {
                        poolAlterEchoCards.Add(id);
                        poolAllCards.Add(id);
                    }
                }
            }

            foreach (var buff in AssetCache.GetAll<BuffRecipe>())
            {
                if (buff == null) continue;
                var required = buff.requiredQuest;
                if (required == null || (qm != null && qm.IsQuestCompleted(required)))
                {
                    var id = buff.CardId;
                    // Skip maxed buffs
                    if (!IsIdMaxed(id))
                    {
                        poolBuffCards.Add(id);
                        poolAllCards.Add(id);
                    }
                }
            }

            // Build Infinity ids
            foreach (var inf in Blindsided.Utilities.AssetCache.GetAll<InfinityCauldronStatSO>("Infinity"))
            {
                if (inf == null) continue;
                var id = $"INF:{inf.Stat}";
                poolInfinityCards.Add(id);
            }
        }

        private string PickRandomResourceCardIdByGroup(AEResourceGroup group)
        {
            var pool = BuildResourceIdsForGroup(group);
            if (pool == null || pool.Count == 0) return null;
            var idx = Random.Range(0, pool.Count);
            return pool[idx];
        }

        private List<string> BuildResourceIdsForGroup(AEResourceGroup group)
        {
            if (cachedGroupPools.TryGetValue(group, out var cached) && cached != null && cached.Count > 0)
                return cached;

            var list = new List<string>();
            var rm = resourceManager ?? ResourceManager.Instance;
            foreach (var res in AssetCache.GetAll<Resource>())
            {
                if (res == null || res.DisableAlterEcho) continue;
                if (rm != null && !rm.IsUnlocked(res)) continue;
                if (GetResourceGroup(res) == group)
                {
                    var id = res.CardId;
                    if (!IsIdMaxed(id))
                        list.Add(id);
                }
            }
            cachedGroupPools[group] = list;
            return list;
        }

        public AEResourceGroup GetResourceGroup(Resource res)
        {
            // Delegate to service if available
            if (_groupClassifier != null) return _groupClassifier.Classify(res);
            // Fallback to old logic if not initialized
            if (res == null) return AEResourceGroup.Combat;

            // Explicit override on Resource takes precedence
            if (res.cauldronCategory != Resource.CauldronCategory.Auto)
            {
                AEResourceGroup mapped = AEResourceGroup.Combat;
                switch (res.cauldronCategory)
                {
                    case Resource.CauldronCategory.Farming: mapped = AEResourceGroup.Farming; break;
                    case Resource.CauldronCategory.Fishing: mapped = AEResourceGroup.Fishing; break;
                    case Resource.CauldronCategory.Mining: mapped = AEResourceGroup.Mining; break;
                    case Resource.CauldronCategory.Woodcutting: mapped = AEResourceGroup.Woodcutting; break;
                    case Resource.CauldronCategory.Looting: mapped = AEResourceGroup.Looting; break;
                    case Resource.CauldronCategory.Combat: mapped = AEResourceGroup.Combat; break;
                }
                resourceGroupMap[res] = mapped;
                return mapped;
            }

            if (resourceGroupMap.TryGetValue(res, out var g)) return g;

            // Build counts per group using TaskData and EnemyData
            var counts = new Dictionary<AEResourceGroup, int>
            {
                { AEResourceGroup.Farming, 0 },
                { AEResourceGroup.Fishing, 0 },
                { AEResourceGroup.Mining, 0 },
                { AEResourceGroup.Woodcutting, 0 },
                { AEResourceGroup.Looting, 0 },
                { AEResourceGroup.Combat, 0 }
            };

            // Tasks
            foreach (var t in AssetCache.GetAll<TimelessEchoes.Tasks.TaskData>("Tasks"))
            {
                if (t == null) continue;
                var group = InferGroupFromTask(t);
                if (group == null) continue;
                foreach (var drop in t.resourceDrops)
                {
                    if (drop == null || drop.resource == null) continue;
                    if (drop.resource == res) counts[group.Value]++;
                }
            }

            // Enemies → Combat
            foreach (var e in AssetCache.GetAll<TimelessEchoes.Enemies.EnemyData>(""))
            {
                if (e == null) continue;
                foreach (var drop in e.resourceDrops)
                {
                    if (drop == null || drop.resource == null) continue;
                    if (drop.resource == res) counts[AEResourceGroup.Combat]++;
                }
            }

            // Chests/NPC looting tasks likely map to Looting skill/prefab naming through tasks

            // Decide best group
            var bestGroup = AEResourceGroup.Combat;
            var bestCount = -1;
            foreach (var kv in counts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    bestGroup = kv.Key;
                }
            }

            resourceGroupMap[res] = bestGroup;
            return bestGroup;
        }

        private AEResourceGroup? InferGroupFromTask(TimelessEchoes.Tasks.TaskData t)
        {
            var skill = t != null ? t.associatedSkill : null;
            var name = skill != null ? skill.name : null;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("Farm", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Farming;
                if (name.Contains("Fish", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Fishing;
                if (name.Contains("Min", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Mining;
                if (name.Contains("Wood", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Woodcutting;
                if (name.Contains("Loot", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Looting;
                if (name.Contains("Combat", StringComparison.OrdinalIgnoreCase)) return AEResourceGroup.Combat;
            }
            // Fallback: try prefab type name
            var prefab = t != null ? t.taskPrefab : null;
            if (prefab != null)
            {
                var typeName = prefab.GetType().Name;
                if (typeName.Contains("Farming")) return AEResourceGroup.Farming;
                if (typeName.Contains("Fishing")) return AEResourceGroup.Fishing;
                if (typeName.Contains("Mining")) return AEResourceGroup.Mining;
                if (typeName.Contains("Woodcutting")) return AEResourceGroup.Woodcutting;
                if (typeName.Contains("Chest") || typeName.Contains("Loot")) return AEResourceGroup.Looting;
            }
            return null;
        }

        private static void TrySave()
        {
            try
            {
                EventHandler.SaveData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cauldron SaveData failed: {ex}");
            }
        }

        private void ResetSessionStats()
        {
            sessionCardsGained = 0;
            sessionTastings = 0;
            countNothing = 0;
            countAlterEcho = 0;
            countBuffs = 0;
            countLowCards = 0;
            countEvasBlessing = 0;
            countVastSurge = 0;
            countAEFarming = 0;
            countAEFishing = 0;
            countAEMining = 0;
            countAEWoodcutting = 0;
            // No separate session counter for Looting yet; AlterEcho total covers aggregate
            countAECombat = 0;
        }

        // ---------- Effective weights (eligibility-aware) ----------
        public struct EffectiveWeightsSnapshot
        {
            public float wNothing;
            public float wAEFarming;
            public float wAEFishing;
            public float wAEMining;
            public float wAEWoodcutting;
            public float wAELooting;
            public float wAECombat;
            public float wBuff;
            public float wLow;
            public float wX2;
            public float wX10;
            public float wInfinity;
        }

        public EffectiveWeightsSnapshot GetEffectiveWeightsAtLevel(int evaLevel)
        {
            return ComputeEffectiveWeights(evaLevel);
        }

        private EffectiveWeightsSnapshot ComputeEffectiveWeights(int evaLevel)
        {
            var snap = new EffectiveWeightsSnapshot
            {
                wNothing = config != null ? config.weightNothing.Evaluate(evaLevel) : 0f,
                wAEFarming = config != null ? config.weightAEFarming.Evaluate(evaLevel) : 0f,
                wAEFishing = config != null ? config.weightAEFishing.Evaluate(evaLevel) : 0f,
                wAEMining = config != null ? config.weightAEMining.Evaluate(evaLevel) : 0f,
                wAEWoodcutting = config != null ? config.weightAEWoodcutting.Evaluate(evaLevel) : 0f,
                wAECombat = config != null ? config.weightAECombat.Evaluate(evaLevel) : 0f,
                wAELooting = config != null ? config.weightAELooting.Evaluate(evaLevel) : 0f,
                wBuff = config != null ? config.weightBuffCard.Evaluate(evaLevel) : 0f,
                wLow = config != null ? config.weightLowestCountCard.Evaluate(evaLevel) : 0f,
                wX2 = config != null ? config.weightEvasBlessingX2.Evaluate(evaLevel) : 0f,
                wX10 = config != null ? config.weightVastSurgeX10.Evaluate(evaLevel) : 0f,
                wInfinity = config != null ? config.weightInfinity.Evaluate(evaLevel) : 0f
            };

            // Gate by eligibility
            RebuildCardPoolsIfDirty();
            bool anyAllCards = poolAllCards.Count > 0;
            bool anyInfinity = poolInfinityCards.Count > 0;
            if (!anyAllCards && !anyInfinity)
            {
                snap.wLow = 0f;
                snap.wX2 = 0f;
                snap.wX10 = 0f;
            }
            // Lowest should always be disabled when no normal cards remain
            if (!anyAllCards) snap.wLow = 0f;

            // Infinity only when no other cards are available (all maxed) and at least one Infinity option exists
            bool infinityActive = !anyAllCards && anyInfinity;
            if (!infinityActive)
            {
                snap.wInfinity = 0f;
            }

            // Buff pool eligibility
            bool anyBuffs = poolBuffCards.Count > 0;
            if (!anyBuffs)
                snap.wBuff = 0f;

            // AE subcategory eligibility
            if (BuildResourceIdsForGroup(AEResourceGroup.Farming).Count == 0) snap.wAEFarming = 0f;
            if (BuildResourceIdsForGroup(AEResourceGroup.Fishing).Count == 0) snap.wAEFishing = 0f;
            if (BuildResourceIdsForGroup(AEResourceGroup.Mining).Count == 0) snap.wAEMining = 0f;
            if (BuildResourceIdsForGroup(AEResourceGroup.Woodcutting).Count == 0) snap.wAEWoodcutting = 0f;
            if (BuildResourceIdsForGroup(AEResourceGroup.Looting).Count == 0) snap.wAELooting = 0f;
            if (BuildResourceIdsForGroup(AEResourceGroup.Combat).Count == 0) snap.wAECombat = 0f;

            return snap;
        }

        private void OnInventoryChangedHandler()
        {
            cachedGroupPools.Clear();
            cardPoolsDirty = true;
            _poolManager?.MarkDirty();
            // NOTE: Do NOT clear _groupClassifier cache here - resource group classifications
            // are based on static TaskData/EnemyData drop tables, not inventory state.
            // Clearing it causes O(resources × tasks) recomputation with heavy string allocations.
            DebouncedWeightsChanged();
        }

        private void OnQuestHandinHandler(string questId)
        {
            cachedGroupPools.Clear();
            cardPoolsDirty = true;
            _poolManager?.MarkDirty();
            DebouncedWeightsChanged();
        }

        private void DebouncedWeightsChanged()
        {
            var now = Time.unscaledTime;
            if (now >= nextWeightsNotifyTime)
            {
                nextWeightsNotifyTime = now + Mathf.Max(0.05f, weightsNotifyInterval);
                OnWeightsChanged?.Invoke();
            }
        }

        private IEnumerator WaitAndSubscribeResourceManager()
        {
            // Wait until ResourceManager.Instance is available, then subscribe and invalidate caches
            while (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                {
                    yield return null;
                }
            }
            resourceManager.OnInventoryChanged += OnInventoryChangedHandler;
            // Invalidate caches immediately in case unlocks occurred before subscription
            OnInventoryChangedHandler();
            resourceSubscribeRoutine = null;
        }

        // ---------- Infinity (Eternal Boons) helpers ----------

        public bool IsInfinityActive()
        {
            RebuildCardPoolsIfDirty();
            return poolAllCards.Count == 0 && poolInfinityCards.Count > 0;
        }

        // Legacy helper kept for backward compatibility; no longer used for Infinity
        private static float SafeLog(float value, float logBase)
        {
            value = Mathf.Max(0f, value);
            logBase = Mathf.Max(1.0001f, logBase);
            return Mathf.Log(1f + value, logBase);
        }

        private int GetCountForId(string id)
        {
            if (oracle == null || oracle.saveData == null) return 0;
            var dict = oracle.saveData.CauldronCardCounts;
            return dict.TryGetValue(id, out var c) ? c : 0;
        }

        public float GetInfinityValueFor(TimelessEchoes.Gear.HeroStatMapping mapping, out bool isPercent)
        {
            isPercent = false;
            float total = 0f;
            // Sum contributions for all matching Infinity SOs
            foreach (var inf in Blindsided.Utilities.AssetCache.GetAll<InfinityCauldronStatSO>("Infinity"))
            {
                if (inf == null || inf.Stat != mapping) continue;
                isPercent = inf.IsPercent || isPercent;
                var key = $"INF:{inf.Stat}";
                var count = GetCountForId(key);
                if (count <= 0) continue;
                // New formula: value = (CardCount) ^ (Exponent)
                var v = Mathf.Pow(count, inf.Exponent);
                total += v;
            }
            return total;
        }

        private bool ShouldEmitStatsNow()
        {
            return _statsEmitThrottle?.TryExecute() ?? true;
        }

        private bool ShouldEmitSessionCardsNow()
        {
            return _sessionCardsEmitThrottle?.TryExecute() ?? true;
        }
    }
}

