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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (equipment == null)
                equipment = EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();

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

            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
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

            // Record resource spends per resource and per core
            if (oracle != null && oracle.saveData != null)
            {
                var forgeLocal = oracle.saveData.Forge;
                if (forgeLocal != null)
                {
                    if (core.requiredIngot != null)
                    {
                        var keyIngot = core.requiredIngot.name;
                        if (!forgeLocal.ResourcesSpent.ContainsKey(keyIngot)) forgeLocal.ResourcesSpent[keyIngot] = 0;
                        forgeLocal.ResourcesSpent[keyIngot] += core.ingotCost;
                    }
                    if (coreResource != null)
                    {
                        var keyCore = coreResource.name;
                        if (!forgeLocal.ResourcesSpent.ContainsKey(keyCore)) forgeLocal.ResourcesSpent[keyCore] = 0;
                        forgeLocal.ResourcesSpent[keyCore] += coreCost;
                    }

                    var coreKeySpend = core.name;
                    if (!forgeLocal.CoresSpentByCore.ContainsKey(coreKeySpend)) forgeLocal.CoresSpentByCore[coreKeySpend] = 0;
                    forgeLocal.CoresSpentByCore[coreKeySpend] += coreCost;
                    if (!forgeLocal.IngotsSpentByCore.ContainsKey(coreKeySpend)) forgeLocal.IngotsSpentByCore[coreKeySpend] = 0;
                    forgeLocal.IngotsSpentByCore[coreKeySpend] += core.ingotCost;
                }
            }

            var rarity = RollRarity(core);
            var slot = !string.IsNullOrWhiteSpace(selectedSlot) ? selectedSlot : RollSlot(core, slotWhitelist);
			var item = new GearItem { rarity = rarity, slot = slot, core = core };
            RollAffixes(item);
            RegisterRecentSlot(slot);
            // Award Ivan XP based on core tier and rolled rarity
            GrantIvanExperience(core, rarity);

            // Update forge craft stats
            if (oracle != null && oracle.saveData != null && oracle.saveData.Forge != null)
            {
                var forge = oracle.saveData.Forge;

                // Totals & counters
                forge.TotalCrafts++;
                forge.CraftsSinceLastUpgrade++;

                // Distributions
                var coreKey = core != null ? core.name : "(null)";
                var rarityKey = rarity != null ? rarity.name : "(null)";
                var slotKey = string.IsNullOrWhiteSpace(slot) ? "(null)" : slot;
                if (!forge.CraftsByCore.ContainsKey(coreKey)) forge.CraftsByCore[coreKey] = 0;
                forge.CraftsByCore[coreKey]++;
                if (!forge.CraftsByRarity.ContainsKey(rarityKey)) forge.CraftsByRarity[rarityKey] = 0;
                forge.CraftsByRarity[rarityKey]++;
                if (!forge.CraftsBySlot.ContainsKey(slotKey)) forge.CraftsBySlot[slotKey] = 0;
                forge.CraftsBySlot[slotKey]++;
                if (!forge.CraftsBySlotTotals.ContainsKey(slotKey)) forge.CraftsBySlotTotals[slotKey] = 0;
                forge.CraftsBySlotTotals[slotKey]++;

                if (!forge.RarityCountsByCore.ContainsKey(coreKey)) forge.RarityCountsByCore[coreKey] = new System.Collections.Generic.Dictionary<string, int>();
                if (!forge.RarityCountsByCore[coreKey].ContainsKey(rarityKey)) forge.RarityCountsByCore[coreKey][rarityKey] = 0;
                forge.RarityCountsByCore[coreKey][rarityKey]++;

                if (!forge.SlotCountsByCore.ContainsKey(coreKey)) forge.SlotCountsByCore[coreKey] = new System.Collections.Generic.Dictionary<string, int>();
                if (!forge.SlotCountsByCore[coreKey].ContainsKey(slotKey)) forge.SlotCountsByCore[coreKey][slotKey] = 0;
                forge.SlotCountsByCore[coreKey][slotKey]++;

                int affixCount = item.affixes != null ? item.affixes.Count : 0;
                if (!forge.AffixCountDistribution.ContainsKey(affixCount)) forge.AffixCountDistribution[affixCount] = 0;
                forge.AffixCountDistribution[affixCount]++;

                // Stat roll aggregates (global, by rarity, by slot) and cumulative totals
                if (item.affixes != null)
                {
                    foreach (var a in item.affixes)
                    {
                        if (a == null || a.stat == null) continue;
                        var def = a.stat;
                        var statId = string.IsNullOrWhiteSpace(def.id) ? def.name : def.id;

                        if (!forge.StatRolls.ContainsKey(statId)) forge.StatRolls[statId] = new GameData.ForgeStats.StatAgg();
                        var agg = forge.StatRolls[statId];
                        agg.count++;
                        agg.sum += a.value;
                        if (a.value < agg.min) agg.min = a.value;
                        if (a.value > agg.max) agg.max = a.value;

                        if (!forge.StatRollsByRarity.ContainsKey(rarityKey)) forge.StatRollsByRarity[rarityKey] = new System.Collections.Generic.Dictionary<string, GameData.ForgeStats.StatAgg>();
                        if (!forge.StatRollsByRarity[rarityKey].ContainsKey(statId)) forge.StatRollsByRarity[rarityKey][statId] = new GameData.ForgeStats.StatAgg();
                        var rAgg = forge.StatRollsByRarity[rarityKey][statId];
                        rAgg.count++;
                        rAgg.sum += a.value;
                        if (a.value < rAgg.min) rAgg.min = a.value;
                        if (a.value > rAgg.max) rAgg.max = a.value;

                        if (!forge.StatRollsBySlot.ContainsKey(slotKey)) forge.StatRollsBySlot[slotKey] = new System.Collections.Generic.Dictionary<string, GameData.ForgeStats.StatAgg>();
                        if (!forge.StatRollsBySlot[slotKey].ContainsKey(statId)) forge.StatRollsBySlot[slotKey][statId] = new GameData.ForgeStats.StatAgg();
                        var sAgg = forge.StatRollsBySlot[slotKey][statId];
                        sAgg.count++;
                        sAgg.sum += a.value;
                        if (a.value < sAgg.min) sAgg.min = a.value;
                        if (a.value > sAgg.max) sAgg.max = a.value;

                        // High roll threshold: top-percent-of-range against stat def's min/max
                        float thrQ = Mathf.Clamp01(forge.HighRollTopPercentThreshold);
                        float thrVal = Mathf.Lerp(def.minRoll, def.maxRoll, thrQ);
                        if (a.value >= thrVal)
                        {
                            if (!forge.HighRollsByStat.ContainsKey(statId)) forge.HighRollsByStat[statId] = 0;
                            forge.HighRollsByStat[statId]++;
                        }

                        if (!forge.CumulativeStatTotalsByStat.ContainsKey(statId)) forge.CumulativeStatTotalsByStat[statId] = 0;
                        forge.CumulativeStatTotalsByStat[statId] += a.value;

                        // Highest single roll per stat
                        if (!forge.HighestRollByStat.ContainsKey(statId) || a.value > forge.HighestRollByStat[statId])
                            forge.HighestRollByStat[statId] = a.value;
                    }
                }

                // Upgrade evaluation
                var eq = equipment != null ? equipment.GetEquipped(slot) : null;
                float delta = TimelessEchoes.Gear.UI.UpgradeEvaluator.ComputeUpgradeScore(this, item, eq);
                if (!forge.UpgradeScoreDeltaBySlot.ContainsKey(slotKey)) forge.UpgradeScoreDeltaBySlot[slotKey] = new GameData.ForgeStats.FloatAgg();
                forge.UpgradeScoreDeltaBySlot[slotKey].count++;
                forge.UpgradeScoreDeltaBySlot[slotKey].sum += delta;

                bool isUpgrade = eq == null || TimelessEchoes.Gear.UI.UpgradeEvaluator.IsPotentialUpgrade(this, item, eq);
                if (isUpgrade)
                {
                    if (!forge.UpgradesBySlot.ContainsKey(slotKey)) forge.UpgradesBySlot[slotKey] = 0;
                    forge.UpgradesBySlot[slotKey]++;
                    if (!forge.UpgradesByRarity.ContainsKey(rarityKey)) forge.UpgradesByRarity[rarityKey] = 0;
                    forge.UpgradesByRarity[rarityKey]++;

                    forge.TotalUpgradeEvents++;
                    forge.CumulativeCraftsBetweenUpgrades += forge.CraftsSinceLastUpgrade;
                    if (forge.CraftsSinceLastUpgrade > forge.MaxCraftsBetweenUpgrades)
                        forge.MaxCraftsBetweenUpgrades = forge.CraftsSinceLastUpgrade;
                    forge.AverageCraftsPerUpgrade = forge.TotalUpgradeEvents > 0
                        ? (float)(forge.CumulativeCraftsBetweenUpgrades / (double)forge.TotalUpgradeEvents)
                        : 0f;
                    forge.CraftsSinceLastUpgrade = 0;
                }

                // Track best single-piece score by slot and core
                float pieceScore = Mathf.Max(0f, delta);
                if (!forge.BestPieceScoreBySlot.ContainsKey(slotKey) || pieceScore > forge.BestPieceScoreBySlot[slotKey])
                    forge.BestPieceScoreBySlot[slotKey] = pieceScore;
                if (!forge.BestPieceScoreByCore.ContainsKey(coreKey) || pieceScore > forge.BestPieceScoreByCore[coreKey])
                    forge.BestPieceScoreByCore[coreKey] = pieceScore;
                if (!forge.MinPieceScoreByCore.ContainsKey(coreKey) || pieceScore < forge.MinPieceScoreByCore[coreKey])
                    forge.MinPieceScoreByCore[coreKey] = pieceScore;
                if (!forge.MaxPieceScoreByCore.ContainsKey(coreKey) || pieceScore > forge.MaxPieceScoreByCore[coreKey])
                    forge.MaxPieceScoreByCore[coreKey] = pieceScore;
                // Best by rarity
                if (!forge.BestPieceScoreByRarity.ContainsKey(rarityKey) || pieceScore > forge.BestPieceScoreByRarity[rarityKey])
                    forge.BestPieceScoreByRarity[rarityKey] = pieceScore;
            }

            return item;
        }

        private RaritySO RollRarity(CoreSO core)
        {
                        // Compute rarity weights with optional level scaling
                        var weights = new List<(RaritySO rarity, float w)>();
                        int level = GetIvanLevel();
                        foreach (var r in rarities)
                        {
                                float baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
                                float bonus = (r != null && config != null && config.enableLevelScaling) ? core.GetRarityWeightPerLevel(r) * level : 0f;
                                var w = Mathf.Max(0f, baseW + bonus);
                                weights.Add((r, w));
                        }

            var total = weights.Sum(t => t.w);
            if (total <= 0f)
                return rarities.OrderBy(r => r.tierIndex).FirstOrDefault();

            var roll = UnityEngine.Random.value * total;
            foreach (var (rarity, w) in weights)
            {
                if (w <= 0f) continue;
                if (roll <= w) return rarity;
                roll -= w;
            }

			var chosen = weights.LastOrDefault(t => t.w > 0f).rarity ?? rarities.OrderBy(r => r.tierIndex).FirstOrDefault();
			return chosen;
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

			// Update Forge XP aggregates and snapshot
			if (oracle.saveData.Forge != null)
			{
				var forgeLocal = oracle.saveData.Forge;
				forgeLocal.IvanLevelAtCraft = oracle.saveData.CraftingMasteryLevel;
				forgeLocal.IvanXpAtCraft = oracle.saveData.CraftingMasteryXP;
				forgeLocal.IvanXpGainedTotal += grant;
				int levelUps = Mathf.Max(0, oracle.saveData.CraftingMasteryLevel - beforeLevel);
				forgeLocal.IvanLevelUpsFromCrafts += levelUps;
				var coreKey = core != null ? core.name : "(null)";
				var rarityKey = rolled != null ? rolled.name : "(null)";
				if (!forgeLocal.IvanXpByCore.ContainsKey(coreKey)) forgeLocal.IvanXpByCore[coreKey] = 0;
				forgeLocal.IvanXpByCore[coreKey] += grant;
				if (!forgeLocal.IvanXpByRarity.ContainsKey(rarityKey)) forgeLocal.IvanXpByRarity[rarityKey] = 0;
				forgeLocal.IvanXpByRarity[rarityKey] += grant;
			}
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
            var pool = equipment != null ? equipment.Slots.ToList() : new List<string> { "Weapon", "Helmet", "Chest", "Boots" };
            if (slotWhitelist != null && slotWhitelist.Count > 0)
                pool = pool.Where(slotWhitelist.Contains).ToList();
            if (pool.Count == 0) pool.Add("Weapon");

            // Optional per-core slot weights
            float[] weights = new float[pool.Count];
            bool useCore = core.slotNames != null && core.slotNames.Count == core.slotWeights.Count && core.slotNames.Count > 0;
            for (int i = 0; i < pool.Count; i++)
            {
                float w = 1f;
                if (useCore)
                {
                    var idx = core.slotNames.FindIndex(n => string.Equals(n, pool[i], StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) w = Mathf.Max(0f, core.slotWeights[idx]);
                }
                weights[i] = w;
            }

            if (config != null && config.enableSmartSlotProtection && recentSlots.Count > 0)
            {
                var recent = recentSlots.ToList();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (recent.Contains(pool[i]))
                        weights[i] *= Mathf.Clamp01(1f - config.recentSlotPenalty);
                }
            }

            float total = weights.Sum();
            if (total <= 0f) return pool[UnityEngine.Random.Range(0, pool.Count)];
            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                var w = weights[i];
                if (w <= 0f) continue;
                if (roll <= w) return pool[i];
                roll -= w;
            }
            return pool.Last();
        }

        private void RollAffixes(GearItem item)
        {
            if (item == null || item.rarity == null) return;
			int count = Mathf.Max(1, item.rarity.affixCount);
			var available = new List<StatDefSO>(stats.Where(s => s != null));
			// Restrict Move Speed to Boots only
			if (!string.Equals(item.slot, "Boots", StringComparison.OrdinalIgnoreCase))
			{
				available = available.Where(s => s.heroMapping != HeroStatMapping.MoveSpeed).ToList();
			}

			// Determine guaranteed first-line stat by slot
			StatDefSO guaranteed = null;
			switch ((item.slot ?? string.Empty).Trim().ToLowerInvariant())
			{
				case "boots":
					guaranteed = GetStatByMapping(HeroStatMapping.MoveSpeed);
					break;
				case "chest":
					guaranteed = GetStatByMapping(HeroStatMapping.Defense);
					break;
				case "helmet":
					guaranteed = GetStatByMapping(HeroStatMapping.MaxHealth);
					break;
				case "weapon":
					guaranteed = GetStatByMapping(HeroStatMapping.Damage);
					break;
			}

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
				item.affixes.Add(new GearAffix { stat = guaranteed, value = RollValue(guaranteed) });
				available.RemoveAll(s => s == guaranteed);
			}

			// Fill remaining affixes without duplicates
			for (int i = item.affixes.Count; i < count && available.Count > 0; i++)
			{
				int idx = UnityEngine.Random.Range(0, available.Count);
				var def = available[idx];
				available.RemoveAt(idx);
				item.affixes.Add(new GearAffix { stat = def, value = RollValue(def) });
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


