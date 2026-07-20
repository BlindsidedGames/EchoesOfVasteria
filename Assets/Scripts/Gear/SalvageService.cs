using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.SaveData;
using Blindsided.Utilities;

namespace TimelessEchoes.Gear
{
    public class SalvageService : MonoBehaviour
    {
        public static SalvageService Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;


        // Scratch dictionary for batch salvage to avoid allocations
        private readonly Dictionary<Resource, float> _scratchExpected = new(8);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Batch salvage: compute expected yields for N items without rolling each individually.
        /// Uses expected value calculation based on drop weights and additional slot chances.
        /// </summary>
        public void BatchSalvage(CoreSO core, int count)
        {
            if (core == null || count <= 0) return;
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            rm.BeginBatch();
            try
            {
                BatchSalvageWithinBatch(core, count);
            }
            finally
            {
                rm.EndBatch();
            }
        }

        /// <summary>
        /// Batch salvage that assumes caller has already called BeginBatch.
        /// Use this when combining multiple operations into a single batch to reduce OnInventoryChanged events.
        /// </summary>
        public void BatchSalvageWithinBatch(CoreSO core, int count)
        {
            if (core == null || count <= 0) return;
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            var drops = core.salvageDrops;
            var extraChances = core.salvageAdditionalLootChances;
            if (drops == null || drops.Count == 0) return;

            // Calculate total weight for normalization
            float totalWeight = 0f;
            foreach (var drop in drops)
            {
                if (drop == null || drop.resource == null || drop.weight <= 0f) continue;
                totalWeight += drop.weight;
            }
            if (totalWeight <= 0f) return;

            // Calculate expected value per resource per salvage
            _scratchExpected.Clear();

            // Expected drops = 1 (guaranteed first slot) + sum of additional slot chances
            float expectedSlots = 1f;
            if (extraChances != null)
            {
                float cumulativeChance = 1f;
                foreach (var chance in extraChances)
                {
                    cumulativeChance *= Mathf.Clamp01(chance);
                    expectedSlots += cumulativeChance;
                }
            }

            // For each resource, calculate: (weight/totalWeight) * expectedSlots * avgAmount
            foreach (var drop in drops)
            {
                if (drop == null || drop.resource == null || drop.weight <= 0f) continue;
                float prob = drop.weight / totalWeight;
                // Average amount with bias towards lower values (same as RollAmount: t*t bias)
                // Expected value of biased roll = integral of floor(lerp(min, max+1, t*t)) dt from 0 to 1
                // Approximation: ~(min + max) / 3 + min * 2/3 = (min*2 + max) / 3
                int min = Mathf.Max(0, drop.dropRange.x);
                int max = Mathf.Max(min, drop.dropRange.y);
                float avgAmount = (min * 2f + max) / 3f;
                if (avgAmount < 0.5f) avgAmount = 1f; // Ensure at least some yield

                float expected = prob * expectedSlots * avgAmount;
                if (!_scratchExpected.ContainsKey(drop.resource))
                    _scratchExpected[drop.resource] = 0f;
                _scratchExpected[drop.resource] += expected;
            }

            // Award resources (no batch management - caller handles it)
            var o = Blindsided.Oracle.oracle;
            var forge = (o != null && o.saveData != null) ? o.saveData.Forge : null;

            foreach (var kv in _scratchExpected)
            {
                int amount = Mathf.RoundToInt(kv.Value * count);
                if (amount <= 0) continue;
                rm.Add(kv.Key, amount, trackStats: false);

                // Record stats
                if (forge != null)
                {
                    var rname = kv.Key.name;
                    if (!forge.ResourcesGainedFromSalvage.ContainsKey(rname))
                        forge.ResourcesGainedFromSalvage[rname] = 0;
                    forge.ResourcesGainedFromSalvage[rname] += amount;
                    if (!forge.SalvageYieldPerResource.ContainsKey(rname))
                        forge.SalvageYieldPerResource[rname] =
                            new Blindsided.SaveData.GameData.ForgeStats.ResourceAgg();
                    var agg = forge.SalvageYieldPerResource[rname];
                    agg.count += count;
                    agg.sum += amount;
                }
            }

            // Update salvage counters
            if (forge != null)
            {
                forge.TotalSalvaged += count;
                forge.SalvageItems += count;
                forge.BatchSalvageCount += count;
                forge.SalvagesByCore.Increment(core.name, count);
            }
        }

        public int Salvage(GearItem item, bool isAuto = false)
        {
            // Roll salvage drops using weights with optional extra slots from the core.
            if (item == null) return 0;
            var rm = ResourceManager.Instance;
            if (rm == null) return 0;

            var drops = item.core != null ? item.core.salvageDrops : null;
            var extraChances = item.core != null ? item.core.salvageAdditionalLootChances : null;
            if (drops == null || drops.Count == 0)
                return 0;

            var results = DropResolver.RollDrops(drops, extraChances, 0f, ignoreSkillLevel: true);
            int totalAwardedEntries = 0;
            rm.BeginBatch();
            try
            {
                foreach (var res in results)
                {
                    rm.Add(res.resource, res.count, trackStats: false);
                    totalAwardedEntries++;

                    // Record salvage yield per resource and total gained from salvage
                    var o = Blindsided.Oracle.oracle;
                    if (o != null && o.saveData != null && o.saveData.Forge != null && res.resource != null)
                    {
                        var forge = o.saveData.Forge;
                        var rname = res.resource.name;
                        if (!forge.ResourcesGainedFromSalvage.ContainsKey(rname))
                            forge.ResourcesGainedFromSalvage[rname] = 0;
                        forge.ResourcesGainedFromSalvage[rname] += res.count;
                        if (!forge.SalvageYieldPerResource.ContainsKey(rname))
                            forge.SalvageYieldPerResource[rname] =
                                new Blindsided.SaveData.GameData.ForgeStats.ResourceAgg();
                        var agg = forge.SalvageYieldPerResource[rname];
                        agg.count++;
                        agg.sum += res.count;
                    }
                }
            } 
            finally
            {
                rm.EndBatch();
            }

            // Update salvage counters (no disk save - autosave handles persistence)
            if (totalAwardedEntries > 0)
            {
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                {
                    var forge = o.saveData.Forge;
                    forge.TotalSalvaged++;
                    forge.SalvageItems++;
                    forge.SalvageEntries += totalAwardedEntries;
                    var rarityKey = item.rarity != null ? item.rarity.name : "(null)";
                    var coreKey = item.core != null ? item.core.name : "(null)";
                    forge.SalvagesByRarity.Increment(rarityKey);
                    forge.SalvagesByCore.Increment(coreKey);
                    if (!string.IsNullOrWhiteSpace(item.slot))
                        forge.SalvagesBySlot.Increment(item.slot);
                }
            }

            // Return item to pool for reuse
            GearObjectPool.ReleaseItem(item);

            return totalAwardedEntries;
        }
    }
}


