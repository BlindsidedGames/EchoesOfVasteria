using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.EventHandler;
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

        // Pity tracking: crafts since last guaranteed at each tier (Rare/Epic/Legendary/Mythic)
        private readonly Dictionary<int, int> pityCounters = new();
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

			// Persist pity across saves
			OnSaveData += SaveState;
			OnLoadData += LoadState;

            // Fallback to auto-load assets if not assigned in inspector
            if (rarities == null || rarities.Count == 0)
                rarities = AssetCache.GetAll<RaritySO>("").Where(r => r != null).OrderBy(r => r.tierIndex).ToList();
            if (stats == null || stats.Count == 0)
                stats = AssetCache.GetAll<StatDefSO>("").Where(s => s != null).ToList();

            // Initialize pity from loaded save if available
			if (oracle != null && oracle.saveData != null)
			{
				pityCounters[0] = Mathf.Max(0, oracle.saveData.PityCraftsSinceLast);
			}

            // Emit initial XP state once listeners subscribe later (UI can also poll)
        }

		private void OnDestroy()
		{
			OnSaveData -= SaveState;
			OnLoadData -= LoadState;
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

            // Spend both costs
            if (!rm.Spend(core.requiredIngot, core.ingotCost))
                return null;
            if (!rm.Spend(coreResource, coreCost))
                return null;

            var rarity = RollRarity(core);
            var slot = !string.IsNullOrWhiteSpace(selectedSlot) ? selectedSlot : RollSlot(core, slotWhitelist);
			var item = new GearItem { rarity = rarity, slot = slot, core = core };
            RollAffixes(item);
            RegisterRecentSlot(slot);
            // Award Ivan XP based on core tier and rolled rarity
            GrantIvanExperience(core, rarity);
            return item;
        }

		private RaritySO RollRarity(CoreSO core)
        {
			// Compute weights with pity clamp and optional level scaling
			var weights = new List<(RaritySO rarity, float w)>();
			int level = GetIvanLevel();
			foreach (var r in rarities)
			{
				float baseW = (r != null ? core.GetRarityWeight(r) : 0f) * (r != null ? r.globalWeightMultiplier : 1f);
				float bonus = (r != null && config != null && config.enableLevelScaling) ? core.GetRarityWeightPerLevel(r) * level : 0f;
				var w = Mathf.Max(0f, baseW + bonus);
				weights.Add((r, w));
			}

            // Pity: raise min rarity based on counters (unless globally disabled)
            var minTier = UpgradeFeatureToggle.DisableCraftingPity ? 0 : GetPityMinTier();
            for (int i = 0; i < weights.Count; i++)
            {
                var r = weights[i].rarity;
                if (r != null && r.tierIndex < minTier)
                    weights[i] = (r, 0f);
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
				var rarity = item.rarity;
				var band = def.GetBandForRarity(rarity);

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
				float floorQ = rarity != null ? Mathf.Clamp01(rarity.floorPercent / 100f) : 0f;
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

        private int GetPityMinTier()
        {
            if (config == null) return 0;
            // Simple interpretation: based on crafts since last trigger, clamp minimum rarity tier
            // Tiers: Rare=2, Epic=3, Legendary=4, Mythic=5 (0=Common,1=Uncommon)
            int crafts = 0;
            pityCounters.TryGetValue(0, out crafts);

            if (crafts >= config.pityMythicWithin) return 5;
            if (crafts >= config.pityLegendaryWithin) return 4;
            if (crafts >= config.pityEpicWithin) return 3;
            if (crafts >= config.pityRareWithin) return 2;
            return 0;
        }

        // Call this from outside after each craft resolves to update pity counters
		public void RegisterCraftOutcome(RaritySO rarity)
        {
            int crafts = 0;
            pityCounters.TryGetValue(0, out crafts);
            crafts += 1;
            pityCounters[0] = crafts;

			// Reset counter if we hit Rare or better
			if (rarity != null && rarity.tierIndex >= 2)
				pityCounters[0] = 0;
        }

		private void SaveState()
		{
			if (oracle == null) return;
			int crafts;
			pityCounters.TryGetValue(0, out crafts);
			oracle.saveData.PityCraftsSinceLast = Mathf.Max(0, crafts);
		}

		private void LoadState()
		{
			if (oracle == null) return;
			pityCounters[0] = Mathf.Max(0, oracle.saveData.PityCraftsSinceLast);
		}
    }
}


