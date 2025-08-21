using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
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
        private bool tastingActive;
        private int sessionCardsGained;
        private int sessionTastings;
        private int countNothing;
        private int countAlterEcho;
        private int countBuffs;
        private int countLowCards;
        private int countEvasBlessing;
        private int countVastSurge;

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
                vastSurge = oracle != null ? oracle.saveData.CauldronTotals.VastSurge : 0
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
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(TasteTick);
            EventHandler.OnLoadData -= ResetSessionStats;
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
            OnStatsChanged?.Invoke(GetStatsSnapshot());
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
            OnStatsChanged?.Invoke(GetStatsSnapshot());
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
            AlterEcho,
            Buff,
            Lowest,
            EvasX2,
            VastX10
        }

        private void ResolveTasteOutcome()
        {
            var lvl = EvaLevel;
            var wNothing = config.weightNothing.Evaluate(lvl);
            var wAE = config.weightAlterEchoCard.Evaluate(lvl);
            var wBuff = config.weightBuffCard.Evaluate(lvl);
            var wLow = config.weightLowestCountCard.Evaluate(lvl);
            var wX2 = config.weightEvasBlessingX2.Evaluate(lvl);
            var wX10 = config.weightVastSurgeX10.Evaluate(lvl);
            var total = wNothing + wAE + wBuff + wLow + wX2 + wX10;
            if (total <= 0f) return;

            var r = Random.value * total;
            RollType pick;
            if ((r -= wNothing) <= 0) pick = RollType.Nothing;
            else if ((r -= wAE) <= 0) pick = RollType.AlterEcho;
            else if ((r -= wBuff) <= 0) pick = RollType.Buff;
            else if ((r -= wLow) <= 0) pick = RollType.Lowest;
            else if ((r -= wX2) <= 0) pick = RollType.EvasX2;
            else pick = RollType.VastX10;

            switch (pick)
            {
                case RollType.Nothing:
                    countNothing++;
                    if (oracle != null) oracle.saveData.CauldronTotals.GainedNothing++;
                    break;
                case RollType.AlterEcho:
                    countAlterEcho++;
                    if (oracle != null) oracle.saveData.CauldronTotals.AlterEcho++;
                    GrantRandomCards(1, true);
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

        private string PickRandomCardId(bool onlyAlterEcho, bool onlyBuffs)
        {
            var pool = BuildAllCardIds(onlyAlterEcho, onlyBuffs);
            if (pool.Count == 0) return null;
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
            var all = BuildAllCardIds(false, false);
            if (all.Count == 0) return null;
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
            OnSessionCardsChanged?.Invoke(sessionCardsGained);
            if (oracle != null) oracle.saveData.CauldronTotals.TotalCards += delta;
            OnStatsChanged?.Invoke(GetStatsSnapshot());

            // Update disciple generation rates when card counts change
            try
            {
                TimelessEchoes.NpcGeneration.DiscipleGenerationManager.Instance?.RefreshRates();
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
            if (!onlyBuffs)
                foreach (var res in AssetCache.GetAll<Resource>())
                    if (res != null && !res.DisableAlterEcho)
                        list.Add($"RES:{res.name}");
            if (!onlyAlterEcho)
                foreach (var buff in AssetCache.GetAll<BuffRecipe>())
                    if (buff != null)
                        list.Add($"BUFF:{buff.name}");
            return list;
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
        }
    }
}