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
        private double sessionCardsGained;

        public event Action OnStewChanged;
        public event Action OnWeightsChanged;
        public event Action<string, int> OnCardGained; // (cardId, amount)
        public event Action OnTasteSessionStarted;
        public event Action OnTasteSessionStopped;

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
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(TasteTick);
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
            sessionCardsGained = 0;
            OnTasteSessionStarted?.Invoke();
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
            ResolveTasteOutcome();
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
                    break;
                case RollType.AlterEcho:
                    GrantRandomCards(1, true);
                    break;
                case RollType.Buff:
                    GrantRandomCards(1, onlyBuffs: true);
                    break;
                case RollType.Lowest:
                    GrantLowestCard(1);
                    break;
                case RollType.EvasX2:
                    GrantRandomCards(2);
                    break;
                case RollType.VastX10:
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
    }
}