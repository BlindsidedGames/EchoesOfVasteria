using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using TimelessEchoes.Stats;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;
using static Blindsided.SaveData.StaticReferences;
using Resources = UnityEngine.Resources;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Upgrades
{
    [DefaultExecutionOrder(-2)]
    public class ResourceManager : Singleton<ResourceManager>
    {
        private static Dictionary<string, Resource> lookup;
        private int batchDepth;
        private bool pendingInventoryChanged;
        private readonly Dictionary<Resource, int> tiers = new();

        /// <summary>
        ///     Invoked whenever the stored resource amounts or unlocked state changes.
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        ///     Invoked whenever resources are added via <see cref="Add" />.
        ///     The boolean parameter indicates whether the resources were a
        ///     bonus (e.g. from retreating).
        /// </summary>
        public event Action<Resource, double, bool> OnResourceAdded;
        public event Action<Resource, int> OnResourceTierUpgraded;

        [Title("Tier Settings")]
        [SerializeField]
        private List<int> tierUpgradeDenominators = new() { 0, 1000, 5000, 10000, 0, 0, 0, 0 }; // index = current tier (1-based)

        [SerializeField]
        private List<float> tierBonusPercents = new() { 0f, 10f, 20f, 30f, 0f, 0f, 0f, 0f }; // index = tier-1

        [Title("Debug Controls")] [SerializeField]
        private Resource debugResource;

        [SerializeField] private double debugAmount = 1;
        private readonly Dictionary<Resource, double> amounts = new();
        private readonly HashSet<Resource> unlocked = new();

        private void InvokeInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        public void BeginBatch()
        {
            batchDepth++;
        }

        public void EndBatch()
        {
            batchDepth = Mathf.Max(0, batchDepth - 1);
            if (batchDepth == 0 && pendingInventoryChanged)
            {
                pendingInventoryChanged = false;
                InvokeInventoryChanged();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
            OnResetData += ResetState;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            OnResetData -= ResetState;
        }

        [Button]
        private void AddDebugResource()
        {
            Add(debugResource, debugAmount, false, true, false);
            SaveState();
        }

        [Button]
        private void UnlockDebugResource()
        {
            if (debugResource != null)
            {
                unlocked.Add(debugResource);
                SaveState();
            }
        }

        public double GetAmount(Resource resource)
        {
            return amounts.TryGetValue(resource, out var value) ? value : 0;
        }

        public int GetTier(Resource resource)
        {
            if (resource == null) return 1;
            return tiers.TryGetValue(resource, out var t) && t > 0 ? t : 1;
        }

        public float GetTierBonusPercent(int tier)
        {
            if (tier <= 0) tier = 1;
            var idx = Mathf.Clamp(tier - 1, 0, tierBonusPercents.Count - 1);
            return tierBonusPercents.Count > 0 ? tierBonusPercents[idx] : 0f;
        }

        private int GetUpgradeDenominatorForTier(int tier)
        {
            var idx = Mathf.Clamp(tier, 0, tierUpgradeDenominators.Count - 1);
            return tierUpgradeDenominators.Count > 0 ? tierUpgradeDenominators[idx] : 0;
        }

        private void TryRollTierUpgrade(Resource resource)
        {
            if (resource == null) return;
            var currentTier = GetTier(resource);
            // Prevent upgrade if already at or beyond configured tiers
            if (currentTier >= Mathf.Max(tierBonusPercents.Count, tierUpgradeDenominators.Count)) return;
            var denom = GetUpgradeDenominatorForTier(currentTier);
            if (denom <= 0) return;
            // 1/denom chance
            if (UnityEngine.Random.Range(0, denom) == 0)
            {
                var newTier = currentTier + 1;
                tiers[resource] = newTier;
                OnResourceTierUpgraded?.Invoke(resource, newTier);
            }
        }

        public void Add(Resource resource, double amount, bool bonus = false, bool trackStats = true, bool eligibleForTierRoll = true)
        {
            if (resource == null || amount <= 0) return;
            // Apply current tier bonus (do not include any upgrade that may roll this call)
            var tier = GetTier(resource);
            var bonusPercent = resource.DisableAlterEcho ? 0f : GetTierBonusPercent(tier);
            var multiplier = 1.0 + (bonusPercent / 100.0);
            var adjustedAmount = amount * multiplier;
            var newlyUnlocked = unlocked.Add(resource);
            if (amounts.ContainsKey(resource))
                amounts[resource] += adjustedAmount;
            else
                amounts[resource] = adjustedAmount;
            // Accumulate exact double values to support very large totals without precision loss
            resource.totalReceived += adjustedAmount;
            if (trackStats)
            {
                var tracker = GameplayStatTracker.Instance;
                if (tracker == null)
                    Log("GameplayStatTracker missing", TELogCategory.Resource, this);
                else
                    tracker.AddResources(adjustedAmount, bonus);
                OnResourceAdded?.Invoke(resource, adjustedAmount, bonus);
            }
            if (batchDepth > 0)
                pendingInventoryChanged = true;
            else
                InvokeInventoryChanged();
            if (newlyUnlocked)
                UpdateCompletionPercentage();

            if (eligibleForTierRoll)
                TryRollTierUpgrade(resource);
        }

        public bool Spend(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return true;
            var current = GetAmount(resource);
            if (current < amount) return false;
            amounts[resource] = current - amount;
            // Accumulate exact double values to support very large totals without precision loss
            resource.totalSpent += amount;
            if (batchDepth > 0)
                pendingInventoryChanged = true;
            else
                InvokeInventoryChanged();
            return true;
        }

        public bool IsUnlocked(Resource resource)
        {
            return resource != null && unlocked.Contains(resource);
        }

        private void ResetState()
        {
            amounts.Clear();
            unlocked.Clear();
            tiers.Clear();
            if (batchDepth > 0)
                pendingInventoryChanged = true;
            else
                InvokeInventoryChanged();
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var existing = oracle.saveData.Resources ?? new Dictionary<string, GameData.ResourceEntry>();
            var dict = new Dictionary<string, GameData.ResourceEntry>();
            foreach (var pair in amounts)
            {
                if (pair.Key == null) continue;
                existing.TryGetValue(pair.Key.name, out var oldEntry);
                dict[pair.Key.name] = new GameData.ResourceEntry
                {
                    Earned = unlocked.Contains(pair.Key),
                    Amount = pair.Value,
                    BestPerMinute = oldEntry?.BestPerMinute ?? 0,
                    Tier = GetTier(pair.Key)
                };
            }

            foreach (var res in unlocked)
            {
                if (res == null) continue;
                if (!dict.ContainsKey(res.name))
                {
                    existing.TryGetValue(res.name, out var oldEntry);
                    dict[res.name] = new GameData.ResourceEntry
                    {
                        Earned = true,
                        Amount = 0,
                        BestPerMinute = oldEntry?.BestPerMinute ?? 0,
                        Tier = GetTier(res)
                    };
                }
            }

            oracle.saveData.Resources = dict;

            var stats = new Dictionary<string, GameData.ResourceRecord>();
            foreach (var res in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
            {
                if (res == null) continue;
                stats[res.name] = new GameData.ResourceRecord
                {
                    TotalReceived = res.totalReceived,
                    TotalSpent = res.totalSpent
                };
            }

            oracle.saveData.ResourceStats = stats;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            oracle.saveData.ResourceStats ??= new Dictionary<string, GameData.ResourceRecord>();
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            EnsureLookup();
            // purge legacy disciple keys that do not map to resources or are disabled
            var toRemove = new List<string>();
            foreach (var key in oracle.saveData.Disciples.Keys)
            {
                if (!oracle.saveData.Resources.ContainsKey(key))
                {
                    toRemove.Add(key);
                    continue;
                }

                if (lookup.TryGetValue(key, out var res) && res != null && res.DisableAlterEcho)
                    toRemove.Add(key);
            }
            foreach (var k in toRemove)
                oracle.saveData.Disciples.Remove(k);
            amounts.Clear();
            unlocked.Clear();
            foreach (var pair in oracle.saveData.Resources)
                if (lookup.TryGetValue(pair.Key, out var res) && res != null)
                {
                    amounts[res] = pair.Value.Amount;
                    if (pair.Value.Earned) unlocked.Add(res);
                    var t = pair.Value.Tier > 0 ? pair.Value.Tier : 1;
                    tiers[res] = t;
                }

            foreach (var res in lookup.Values)
                if (oracle.saveData.ResourceStats.TryGetValue(res.name, out var s))
                {
                    res.totalReceived = s.TotalReceived;
                    res.totalSpent = s.TotalSpent;
                }
                else
                {
                    res.totalReceived = 0;
                    res.totalSpent = 0;
                }

            InvokeInventoryChanged();
            UpdateCompletionPercentage();
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }
    }
}
