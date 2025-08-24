using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.UI;
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
        private bool cardPoolsDirty = true;
        private float? _nextStatsEmitTime;
        private float? _nextSessionCardsEmitTime;
        [Header("Perf Throttling")] [SerializeField] [Min(0.05f)] private float cardPoolsRebuildMinInterval = 0.5f;
        [SerializeField] [Min(0.05f)] private float weightsNotifyInterval = 0.25f;
        private float nextCardPoolsRebuildAllowed;
        private float nextWeightsNotifyTime;

        public event Action OnStewChanged;
        public event Action OnWeightsChanged;
        public event Action<string, int> OnCardGained; // (cardId, amount)
        public event Action OnTasteSessionStarted;
        public event Action OnTasteSessionStopped;
        public event Action<int> OnSessionCardsChanged;
        public event Action<TastingStats> OnStatsChanged;

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
                oracle.saveData.CauldronStew = Math.Max(0, value);
                OnStewChanged?.Invoke();
            }
        }

        public bool IsTasting => tastingActive;

        public int EvaLevel
        {
            get => oracle != null ? Math.Max(1, oracle.saveData.CauldronEvaLevel) : 1;
            private set
            {
                if (oracle != null) oracle.saveData.CauldronEvaLevel = Math.Max(1, value);
            }
        }

        public double EvaXp
        {
            get => oracle != null ? oracle.saveData.CauldronEvaXp : 0;
            private set
            {
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

        public int GetResourceTier(string resourceName)
        {
            if (oracle == null || string.IsNullOrEmpty(resourceName)) return 0;
            var key = $"RES:{resourceName}";
            var dict = oracle.saveData.CauldronCardCounts;
            var count = dict.TryGetValue(key, out var c) ? c : 0;
            return GetTierFromThresholds(count, config != null ? config.resourceTierThresholds : null);
        }

        public int GetBuffTier(string buffName)
        {
            if (oracle == null || string.IsNullOrEmpty(buffName)) return 0;
            var key = $"BUFF:{buffName}";
            var dict = oracle.saveData.CauldronCardCounts;
            var count = dict.TryGetValue(key, out var c) ? c : 0;
            return GetTierFromThresholds(count, config != null ? config.buffTierThresholds : null);
        }

        public float GetResourceAlterEchoMultiplier(string resourceName)
        {
            // Multiplier applied to per-resource disciple generation rate
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
            if (config == null) return 0f;
            var tier = GetBuffTier(buffName);
            if (tier <= 0 || config.buffPowerBonusPerTier == null || config.buffPowerBonusPerTier.Length == 0)
                return 0f;
            var idx = Mathf.Clamp(tier - 1, 0, config.buffPowerBonusPerTier.Length - 1);
            return Mathf.Max(0f, config.buffPowerBonusPerTier[idx]);
        }

        public float GetBuffPowerMultiplier(string buffName)
        {
            var percent = GetBuffPowerPercent(buffName);
            return 1f + percent / 100f;
        }

        protected override void Awake()
        {
            base.Awake();
            resourceManager = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            // Resolve QuestManager once up-front to avoid per-tick object searches
            questManager = TimelessEchoes.Quests.QuestManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Quests.QuestManager>();
            if (config == null)
                Log("CauldronConfig missing", TELogCategory.General, this);
        }

        private void OnEnable()
        {
            if (UITicker.Instance != null)
                UITicker.Instance.Subscribe(TasteTick,
                    1f / Mathf.Max(1f, config != null ? config.rollsPerSecond : 10f));
            // Reset session stats on save load
            EventHandler.OnLoadData += ResetSessionStats;
            // Refresh weights when inventory or quests change
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChangedHandler;
            EventHandler.OnQuestHandin += OnQuestHandinHandler;
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(TasteTick);
            EventHandler.OnLoadData -= ResetSessionStats;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChangedHandler;
            EventHandler.OnQuestHandin -= OnQuestHandinHandler;
        }

        // -------- Mixing --------
        public bool CanMix(Resource a, Resource b)
        {
            if (a == null || b == null || a == b) return false;
            if (resourceManager == null) return false;
            return resourceManager.GetAmount(a) > 0 || resourceManager.GetAmount(b) > 0;
        }

        public double MixMax(Resource a, Resource b)
        {
            if (!CanMix(a, b)) return 0;
            var amountA = resourceManager.GetAmount(a);
            var amountB = resourceManager.GetAmount(b);
            if (amountA > 0) resourceManager.Spend(a, amountA);
            if (amountB > 0) resourceManager.Spend(b, amountB);
            var points = amountA * a.baseValue * a.valueMultiplier + amountB * b.baseValue * b.valueMultiplier;
            var stewGained = points / 100.0;
            Stew += stewGained;
            TrySave();
            return stewGained;
        }

        // -------- Tasting --------
        public void StartTasting()
        {
            if (tastingActive) return;
            tastingActive = true;
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
            var cost = Math.Max(0.0001f, config.stewPerRoll);
            if (Stew < cost)
            {
                StopTasting();
                return;
            }

            Stew -= cost;
            GainEvaXp(1); // 1 XP per roll (1 stew)
            sessionTastings++;
            if (oracle != null) oracle.saveData.CauldronTotals.TotalTastings++;
            ResolveTasteOutcome();
            // Throttle stats UI updates to reduce canvas rebuilds
            if (ShouldEmitStatsNow())
            {
                var statsHandler = OnStatsChanged;
                if (statsHandler != null)
                    statsHandler(GetStatsSnapshot());
            }
        }

        private void GainEvaXp(double amount)
        {
            EvaXp += amount;
            while (EvaXp >= GetXpToNextLevel(EvaLevel))
            {
                EvaXp -= GetXpToNextLevel(EvaLevel);
                EvaLevel++;
                OnWeightsChanged?.Invoke();
            }
        }

        private double GetXpToNextLevel(int level)
        {
            // Simple progression: 50 + 10*(level-1)
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
            VastX10
        }

        private void ResolveTasteOutcome()
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

            var subAEPresent = (wAE_Farm + wAE_Fish + wAE_Mine + wAE_Wood + wAE_Loot + wAE_Combat) > 0f;
            float total = wNothing + wBuff + wLow + wX2 + wX10
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
                else if ((r -= wBuff) <= 0) pick = RollType.Buff;
                else if ((r -= wLow) <= 0) pick = RollType.Lowest;
                else if ((r -= wX2) <= 0) pick = RollType.EvasX2;
                else pick = RollType.VastX10;
            }
            else
            {
                // If subcategories are not configured with non-zero weights, treat AE as absent
                if ((r -= wBuff) <= 0) pick = RollType.Buff;
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
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Farming);
                    break;
                case RollType.AEFishing:
                    countAEFishing++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEFishing++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Fishing);
                    break;
                case RollType.AEMining:
                    countAEMining++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEMining++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Mining);
                    break;
                case RollType.AEWoodcutting:
                    countAEWoodcutting++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AEWoodcutting++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Woodcutting);
                    break;
                case RollType.AELooting:
                    countAECombat++; // session total remains under legacy AE; per-sub has its own counter below
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AELooting++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Looting);
                    break;
                case RollType.AECombat:
                    countAECombat++;
                    countAlterEcho++;
                    if (oracle != null) { oracle.saveData.CauldronTotals.AECombat++; oracle.saveData.CauldronTotals.AlterEcho++; }
                    GrantRandomResourceCardFromGroup(AEResourceGroup.Combat);
                    break;
                case RollType.Buff:
                    countBuffs++;
                    if (oracle != null) oracle.saveData.CauldronTotals.Buffs++;
                    GrantRandomCards(1, onlyBuffs: true);
                    break;
                case RollType.Lowest:
                    countLowCards++;
                    if (oracle != null) oracle.saveData.CauldronTotals.LowCards++;
                    GrantLowestCard(1);
                    break;
                case RollType.EvasX2:
                    countEvasBlessing++;
                    if (oracle != null) oracle.saveData.CauldronTotals.EvasBlessing++;
                    GrantRandomCards(2);
                    break;
                case RollType.VastX10:
                    countVastSurge++;
                    if (oracle != null) oracle.saveData.CauldronTotals.VastSurge++;
                    GrantRandomCards(10);
                    break;
            }
        }

        private void GrantRandomCards(int count, bool onlyAlterEcho = false, bool onlyBuffs = false)
        {
            for (var i = 0; i < count; i++)
            {
                var id = PickRandomCardId(onlyAlterEcho, onlyBuffs);
                if (id == null) continue;
                AddCardCount(id, 1);
            }
        }

        private void GrantRandomResourceCardFromGroup(AEResourceGroup group)
        {
            var id = PickRandomResourceCardIdByGroup(group);
            if (id == null)
            {
                // Fallback to any resource
                id = PickRandomCardId(onlyAlterEcho: true, onlyBuffs: false);
            }
            if (id != null) AddCardCount(id, 1);
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

        private void GrantLowestCard(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var id = GetLowestCountCardId();
                if (id == null) return;
                AddCardCount(id, 1);
            }
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
            if (!dict.ContainsKey(id)) dict[id] = 0;
            dict[id] += delta;
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

            // Update disciple generation rates when card counts change
            try
            {
                TimelessEchoes.NpcGeneration.DiscipleGenerationManager.Instance?.MarkRatesDirty();
            }
            catch (Exception)
            {
                // ignore: manager may not be available in some scenes
            }
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
                        list.Add($"RES:{res.name}");
                }
            }
            if (!onlyAlterEcho)
            {
                foreach (var buff in AssetCache.GetAll<BuffRecipe>())
                {
                    if (buff == null) continue;
                    var required = buff.requiredQuest;
                    if (required == null || (qm != null && qm.IsQuestCompleted(required)))
                        list.Add($"BUFF:{buff.name}");
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

            var rm = resourceManager ?? ResourceManager.Instance;
            var qm = questManager ?? TimelessEchoes.Quests.QuestManager.Instance;

            foreach (var res in AssetCache.GetAll<Resource>())
            {
                if (res == null || res.DisableAlterEcho) continue;
                if (rm != null && rm.IsUnlocked(res))
                {
                    var id = $"RES:{res.name}";
                    poolAlterEchoCards.Add(id);
                    poolAllCards.Add(id);
                }
            }

            foreach (var buff in AssetCache.GetAll<BuffRecipe>())
            {
                if (buff == null) continue;
                var required = buff.requiredQuest;
                if (required == null || (qm != null && qm.IsQuestCompleted(required)))
                {
                    var id = $"BUFF:{buff.name}";
                    poolBuffCards.Add(id);
                    poolAllCards.Add(id);
                }
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
                    list.Add($"RES:{res.name}");
            }
            cachedGroupPools[group] = list;
            return list;
        }

        public AEResourceGroup GetResourceGroup(Resource res)
        {
            if (res == null) return AEResourceGroup.Combat;
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
                wX10 = config != null ? config.weightVastSurgeX10.Evaluate(evaLevel) : 0f
            };

            // Gate by eligibility
            RebuildCardPoolsIfDirty();
            bool anyAllCards = poolAllCards.Count > 0;
            if (!anyAllCards)
            {
                snap.wLow = 0f;
                snap.wX2 = 0f;
                snap.wX10 = 0f;
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
            DebouncedWeightsChanged();
        }

        private void OnQuestHandinHandler(string questId)
        {
            cardPoolsDirty = true;
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

        private bool ShouldEmitStatsNow()
        {
            var now = Time.unscaledTime;
            if (_nextStatsEmitTime == null || now >= _nextStatsEmitTime.Value)
            {
                _nextStatsEmitTime = now + 0.2f; // 5 Hz
                return true;
            }
            return false;
        }

        private bool ShouldEmitSessionCardsNow()
        {
            var now = Time.unscaledTime;
            if (_nextSessionCardsEmitTime == null || now >= _nextSessionCardsEmitTime.Value)
            {
                _nextSessionCardsEmitTime = now + 0.2f; // 5 Hz
                return true;
            }
            return false;
        }
    }
}