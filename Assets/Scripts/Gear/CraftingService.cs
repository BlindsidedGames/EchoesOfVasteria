using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;

namespace TimelessEchoes.Gear
{
    public class CraftingService : MonoBehaviour
    {
        public static CraftingService Instance { get; private set; }

        [SerializeField] private CraftingConfigSO config;
        [SerializeField] private List<RaritySO> rarities = new();
        [SerializeField] private List<StatDefSO> stats = new();
        [SerializeField] private EquipmentController equipment;

        public CraftingConfigSO Config => config;

        public event Action<int, float, float> OnIvanXpChanged; // level, current, needed
        public event Action<int> OnIvanLevelUp;

        private readonly Queue<string> recentSlots = new();

        // Pooled scratch lists to reduce allocations in hot paths
        private readonly List<(RaritySO rarity, float w)> _scratchWeights = new(16);
        private readonly List<string> _scratchSlotPool = new(8);
        private readonly List<string> _scratchRecentSlots = new(8);
        private readonly List<StatDefSO> _scratchAvailableStats = new(16);
        private float[] _scratchSlotWeights = new float[8];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (equipment == null)
                equipment = EquipmentController.Instance;

            // Fallback to auto-load assets if not assigned in inspector
            if (rarities == null || rarities.Count == 0)
                rarities = AssetCache.GetAll<RaritySO>("").Where(r => r != null).OrderBy(r => r.tierIndex).ToList();
            if (stats == null || stats.Count == 0)
                stats = AssetCache.GetAll<StatDefSO>("").Where(s => s != null).ToList();
            // Emit initial XP state once listeners subscribe later (UI can also poll)
        }

        public StatDefSO GetStatByMapping(HeroStatMapping mapping)
        {
            foreach (var s in stats)
            {
                if (s != null && s.heroMapping == mapping)
                    return s;
            }
            return null;
        }

        public GearItem Craft(CoreSO core, string selectedSlot = null, List<string> slotWhitelist = null, TimelessEchoes.Upgrades.Resource coreResource = null, int coreCost = 1)
        {
            if (core == null || core.requiredIngot == null) return null;

            var rm = ResourceManager.Instance;
            if (rm == null) return null;

            // Require a core resource and at least one core to craft
            if (coreResource == null || coreCost <= 0)
                return null;

            // Pre-check affordability to avoid partial spend
            var haveIngots = rm.GetAmount(core.requiredIngot) >= core.ingotCost;
            var haveCores = rm.GetAmount(coreResource) >= coreCost;
            if (!haveIngots || !haveCores)
                return null;

            // Spend both costs in a single batch to coalesce inventory change notifications
            rm.BeginBatch();
            try
            {
                if (!rm.Spend(core.requiredIngot, core.ingotCost))
                    return null;
                if (!rm.Spend(coreResource, coreCost))
                    return null;
            }
            finally
            {
                rm.EndBatch();
            }

            // Record resource spending via analytics service
            ForgeAnalyticsService.Instance?.RecordResourceSpend(core, core.requiredIngot, core.ingotCost, coreResource, coreCost);

            var rarity = RollRarity(core);
            var slot = !string.IsNullOrWhiteSpace(selectedSlot) ? selectedSlot : RollSlot(core, slotWhitelist);
			var item = GearObjectPool.GetItem();
			item.rarity = rarity;
			item.slot = slot;
			item.core = core;
            RollAffixes(item);
            RegisterRecentSlot(slot);
            // Award Ivan XP based on core tier and rolled rarity
            GrantIvanExperience(core, rarity);

            // Record craft telemetry via analytics service
            if (ForgeAnalyticsService.Instance != null)
            {
                var eq = equipment != null ? equipment.GetEquipped(slot) : null;
                float upgradeScore = TimelessEchoes.Gear.UI.UpgradeEvaluator.ComputeUpgradeScore(this, item, eq);
                float absScore = TimelessEchoes.Gear.UI.UpgradeEvaluator.ComputeAbsoluteScore(this, item);
                bool isUpgrade = eq == null || TimelessEchoes.Gear.UI.UpgradeEvaluator.IsPotentialUpgrade(this, item, eq);

                var craftResult = new CraftResult
                {
                    Core = core,
                    Rarity = rarity,
                    Slot = slot,
                    Item = item,
                    EquippedComparison = eq,
                    UpgradeScore = upgradeScore,
                    AbsoluteScore = absScore,
                    IsUpgrade = isUpgrade
                };

                ForgeAnalyticsService.Instance.RecordCraft(craftResult);
            }

            return item;
        }

        /// <summary>
        /// Batch crafting result structure.
        /// </summary>
        public struct BatchCraftResult
        {
            public int TotalCrafted;
            public int UpgradesFound;
            public GearItem BestUpgrade;
            public float BestUpgradeScore;
            public GearItem LastCrafted;
        }

        /// <summary>
        /// Crafts multiple items in batch for autocrafting efficiency.
        /// Uses ForgeAnalyticsService batch mode to defer telemetry.
        /// </summary>
        /// <param name="count">Number of items to craft</param>
        /// <param name="core">Core to craft with</param>
        /// <param name="selectedSlot">Optional fixed slot</param>
        /// <param name="slotWhitelist">Optional slot whitelist</param>
        /// <param name="coreResource">Core resource to spend</param>
        /// <param name="coreCost">Cost per craft</param>
        /// <param name="stopOnUpgrade">Stop when an upgrade is found</param>
        /// <returns>Batch result with statistics</returns>
        public BatchCraftResult CraftBatch(
            int count,
            CoreSO core,
            string selectedSlot = null,
            List<string> slotWhitelist = null,
            TimelessEchoes.Upgrades.Resource coreResource = null,
            int coreCost = 1,
            bool stopOnUpgrade = false)
        {
            var result = new BatchCraftResult();

            if (core == null || count <= 0 || coreResource == null || coreCost <= 0)
                return result;

            var rm = ResourceManager.Instance;
            if (rm == null) return result;

            // Begin analytics batch mode
            ForgeAnalyticsService.Instance?.BeginBatch();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    // Check resources before each craft
                    var haveIngots = rm.GetAmount(core.requiredIngot) >= core.ingotCost;
                    var haveCores = rm.GetAmount(coreResource) >= coreCost;
                    if (!haveIngots || !haveCores)
                        break;

                    var item = Craft(core, selectedSlot, slotWhitelist, coreResource, coreCost);
                    if (item == null)
                        break;

                    result.TotalCrafted++;
                    result.LastCrafted = item;

                    // Check if upgrade
                    var eq = equipment?.GetEquipped(item.slot);
                    var upgradeScore = TimelessEchoes.Gear.UI.UpgradeEvaluator.ComputeUpgradeScore(this, item, eq);
                    var isUpgrade = eq == null || upgradeScore > 0.0001f;

                    if (isUpgrade)
                    {
                        result.UpgradesFound++;
                        if (upgradeScore > result.BestUpgradeScore)
                        {
                            result.BestUpgradeScore = upgradeScore;
                            result.BestUpgrade = item;
                        }

                        if (stopOnUpgrade)
                            break;
                    }
                }
            }
            finally
            {
                // End batch mode (flushes all pending telemetry)
                ForgeAnalyticsService.Instance?.EndBatch();
            }

            return result;
        }

        private RaritySO RollRarity(CoreSO core)
        {
            // Use pooled scratch list to avoid allocations
            _scratchWeights.Clear();
            int level = GetIvanLevel();

            foreach (var r in rarities)
            {
                float baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
                float bonus = (r != null && config != null && config.enableLevelScaling) ? core.GetRarityWeightPerLevel(r) * level : 0f;
                var raw = Mathf.Max(0f, baseW + bonus);
                var range = r != null ? core.GetRarityWeightRange(r) : new Vector2(0f, float.PositiveInfinity);
                var w = Mathf.Clamp(raw, range.x, range.y);
                _scratchWeights.Add((r, w));
            }

            float total = 0f;
            foreach (var t in _scratchWeights)
                total += t.w;

            if (total <= 0f)
                return rarities.OrderBy(r => r.tierIndex).FirstOrDefault();

            var roll = UnityEngine.Random.value * total;
            foreach (var (rarity, w) in _scratchWeights)
            {
                if (w <= 0f) continue;
                if (roll <= w) return rarity;
                roll -= w;
            }

            // Fallback to last valid weight
            for (int i = _scratchWeights.Count - 1; i >= 0; i--)
            {
                if (_scratchWeights[i].w > 0f)
                    return _scratchWeights[i].rarity;
            }
            return rarities.OrderBy(r => r.tierIndex).FirstOrDefault();
        }

		private int GetIvanLevel()
		{
			if (oracle == null || oracle.saveData == null) return 0;
			return Mathf.Max(0, oracle.saveData.CraftingMasteryLevel);
		}

		private void GrantIvanExperience(CoreSO core, RaritySO rolled)
		{
			if (oracle == null || oracle.saveData == null || config == null || core == null || rolled == null) return;
			int coreTier = Mathf.Clamp(core.tierIndex, 0, int.MaxValue);
			int baseXp = 0;
			if (config.baselineXpPerCoreTier != null && config.baselineXpPerCoreTier.Length > 0)
			{
				int idx = Mathf.Min(coreTier, config.baselineXpPerCoreTier.Length - 1);
				baseXp = Mathf.Max(0, config.baselineXpPerCoreTier[idx]);
			}
			int grant = baseXp;
			if (config.useRolledRarityXp)
			{
				// Use rolled rarity tier to index into baseline table
				int ridx = Mathf.Max(0, rolled.tierIndex);
				int bidx = Mathf.Min(ridx, (config.baselineXpPerCoreTier?.Length ?? 1) - 1);
				grant = Mathf.Max(0, (config.baselineXpPerCoreTier != null && config.baselineXpPerCoreTier.Length > 0)
					? config.baselineXpPerCoreTier[bidx] : baseXp);
			}
			else
			{
				int stepsAbove = Mathf.Max(0, rolled.tierIndex - coreTier);
				grant += Mathf.Max(0, stepsAbove) * Mathf.Max(0, config.xpPerRarityStepAbove);
			}

			if (grant <= 0) return;
			// Apply XP and handle level-ups with exponential curve
			int beforeLevel = oracle.saveData.CraftingMasteryLevel;
			oracle.saveData.CraftingMasteryXP += grant;
			int safety = 0;
			while (safety++ < 100)
			{
				float currentLevel = Mathf.Max(1, oracle.saveData.CraftingMasteryLevel);
				float need = config.xpForFirstLevel * Mathf.Pow(currentLevel, config.xpLevelMultiplier);
				if (oracle.saveData.CraftingMasteryXP >= need)
				{
					oracle.saveData.CraftingMasteryXP -= need;
					oracle.saveData.CraftingMasteryLevel++;
					OnIvanLevelUp?.Invoke(oracle.saveData.CraftingMasteryLevel);
				}
				else break;
			}
			float needed = config.xpForFirstLevel * Mathf.Pow(Mathf.Max(1, oracle.saveData.CraftingMasteryLevel), config.xpLevelMultiplier);
			OnIvanXpChanged?.Invoke(oracle.saveData.CraftingMasteryLevel, oracle.saveData.CraftingMasteryXP, needed);

			// Record Ivan XP telemetry via analytics service
			ForgeAnalyticsService.Instance?.RecordIvanXp(core, rolled, grant, beforeLevel, oracle.saveData.CraftingMasteryLevel);
		}

        public (int level, float currentXp, float neededXp) GetIvanXpState()
        {
            if (oracle == null || oracle.saveData == null || config == null)
                return (0, 0f, 1f);
            int level = Mathf.Max(1, oracle.saveData.CraftingMasteryLevel);
            float needed = config.xpForFirstLevel * Mathf.Pow(level, config.xpLevelMultiplier);
            return (level, oracle.saveData.CraftingMasteryXP, needed);
        }

        private string RollSlot(CoreSO core, List<string> slotWhitelist)
        {
            // Use pooled scratch list
            _scratchSlotPool.Clear();
            if (equipment != null)
            {
                foreach (var slot in equipment.Slots)
                    _scratchSlotPool.Add(slot);
            }
            else
            {
                _scratchSlotPool.AddRange(new[] { "Weapon", "Helmet", "Chest", "Boots" });
            }

            // Filter by whitelist if provided
            if (slotWhitelist != null && slotWhitelist.Count > 0)
            {
                for (int i = _scratchSlotPool.Count - 1; i >= 0; i--)
                {
                    if (!slotWhitelist.Contains(_scratchSlotPool[i]))
                        _scratchSlotPool.RemoveAt(i);
                }
            }

            if (_scratchSlotPool.Count == 0)
                _scratchSlotPool.Add("Weapon");

            // Calculate weights (reuse array if large enough, resize only when needed)
            if (_scratchSlotWeights.Length < _scratchSlotPool.Count)
                _scratchSlotWeights = new float[_scratchSlotPool.Count * 2];
            var weights = _scratchSlotWeights;
            bool useCore = core.slotNames != null && core.slotNames.Count == core.slotWeights.Count && core.slotNames.Count > 0;
            for (int i = 0; i < _scratchSlotPool.Count; i++)
            {
                float w = 1f;
                if (useCore)
                {
                    var idx = core.slotNames.FindIndex(n => string.Equals(n, _scratchSlotPool[i], StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) w = Mathf.Max(0f, core.slotWeights[idx]);
                }
                weights[i] = w;
            }

            // Apply recent slot penalty
            if (config != null && config.enableSmartSlotProtection && recentSlots.Count > 0)
            {
                _scratchRecentSlots.Clear();
                foreach (var s in recentSlots)
                    _scratchRecentSlots.Add(s);

                for (int i = 0; i < _scratchSlotPool.Count; i++)
                {
                    if (_scratchRecentSlots.Contains(_scratchSlotPool[i]))
                        weights[i] *= Mathf.Clamp01(1f - config.recentSlotPenalty);
                }
            }

            float total = 0f;
            foreach (var w in weights)
                total += w;

            if (total <= 0f)
                return _scratchSlotPool[UnityEngine.Random.Range(0, _scratchSlotPool.Count)];

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < _scratchSlotPool.Count; i++)
            {
                var w = weights[i];
                if (w <= 0f) continue;
                if (roll <= w) return _scratchSlotPool[i];
                roll -= w;
            }
            return _scratchSlotPool[_scratchSlotPool.Count - 1];
        }

        private void RollAffixes(GearItem item)
        {
            if (item == null || item.rarity == null) return;
			int count = Mathf.Max(1, item.rarity.affixCount);

			// Use pooled scratch list
			_scratchAvailableStats.Clear();
			foreach (var s in stats)
			{
				if (s == null) continue;
				// Restrict Move Speed to Boots only
				if (s.heroMapping == HeroStatMapping.MoveSpeed &&
					!string.Equals(item.slot, "Boots", StringComparison.OrdinalIgnoreCase))
					continue;
				_scratchAvailableStats.Add(s);
			}

			// Determine guaranteed first-line stat by slot (avoid string allocations)
			StatDefSO guaranteed = null;
			var slotStr = item.slot;
			if (string.Equals(slotStr, "Boots", StringComparison.OrdinalIgnoreCase))
				guaranteed = GetStatByMapping(HeroStatMapping.MoveSpeed);
			else if (string.Equals(slotStr, "Chest", StringComparison.OrdinalIgnoreCase))
				guaranteed = GetStatByMapping(HeroStatMapping.Defense);
			else if (string.Equals(slotStr, "Helmet", StringComparison.OrdinalIgnoreCase))
				guaranteed = GetStatByMapping(HeroStatMapping.MaxHealth);
			else if (string.Equals(slotStr, "Weapon", StringComparison.OrdinalIgnoreCase))
				guaranteed = GetStatByMapping(HeroStatMapping.Damage);

			// Helper to roll a value with rarity-aware bands, optional jackpot, and rarity floor
			float RollValue(StatDefSO def)
			{
				if (def == null) return 0f;
				var rarity2 = item.rarity;
				var band = def.GetBandForRarity(rarity2);

				float t;
				// Sample inside the rarity band, shaped by within-tier curve (jackpot disabled)
				float u = UnityEngine.Random.value;
				float shaped = (band != null && band.withinTierCurve != null) ? band.withinTierCurve.Evaluate(u) : u;
				float minQ = band != null ? band.GetClampedMin() : 0f;
				float maxQ = band != null ? band.GetClampedMax() : 1f;
				if (maxQ < minQ) { var tmp = minQ; minQ = maxQ; maxQ = tmp; }
				t = Mathf.Lerp(minQ, maxQ, Mathf.Clamp01(shaped));

				float v = def.RemapRoll(t);
				// Apply rarity floor as a lower bound in value space
				float floorQ = rarity2 != null ? Mathf.Clamp01(rarity2.floorPercent / 100f) : 0f;
				float floorValue = Mathf.Lerp(def.minRoll, def.maxRoll, floorQ);
				return Mathf.Max(v, floorValue);
			}

			// Add guaranteed first affix if available
			if (guaranteed != null)
			{
				var affix = GearObjectPool.GetAffix();
				affix.stat = guaranteed;
				affix.value = RollValue(guaranteed);
				item.affixes.Add(affix);
				// Remove guaranteed stat manually to avoid lambda allocation
				for (int i = _scratchAvailableStats.Count - 1; i >= 0; i--)
				{
					if (_scratchAvailableStats[i] == guaranteed)
					{
						_scratchAvailableStats.RemoveAt(i);
						break;
					}
				}
			}

			// Fill remaining affixes without duplicates
			for (int i = item.affixes.Count; i < count && _scratchAvailableStats.Count > 0; i++)
			{
				int idx = UnityEngine.Random.Range(0, _scratchAvailableStats.Count);
				var def = _scratchAvailableStats[idx];
				_scratchAvailableStats.RemoveAt(idx);
				var affix = GearObjectPool.GetAffix();
				affix.stat = def;
				affix.value = RollValue(def);
				item.affixes.Add(affix);
			}
		}

        private void RegisterRecentSlot(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || config == null || !config.enableSmartSlotProtection) return;
            recentSlots.Enqueue(slot);
            while (recentSlots.Count > Mathf.Max(1, config.recentWindow))
                recentSlots.Dequeue();
        }
    }
}


