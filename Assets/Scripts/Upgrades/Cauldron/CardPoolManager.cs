using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Manages card pools for tasting - caches eligible cards and handles pool rebuilding.
    /// Extracted from CauldronManager to centralize pool management logic.
    /// </summary>
    /// <remarks>
    /// Card pools are lazily rebuilt when marked dirty. A throttling mechanism prevents
    /// excessive rebuilds during high-frequency updates. The class maintains separate pools
    /// for different card types (resources, buffs, infinity) and per-group resource pools.
    /// </remarks>
    public class CardPoolManager
    {
        /// <summary>
        /// Fired after card pools have been rebuilt.
        /// </summary>
        public event Action OnPoolsRebuilt;

        // Card pools
        private readonly List<string> _alterEchoPool = new();
        private readonly List<string> _buffPool = new();
        private readonly List<string> _allPool = new();
        private readonly List<string> _infinityPool = new();
        private readonly Dictionary<CauldronManager.AEResourceGroup, List<string>> _groupPools = new();

        // Dependencies
        private readonly CardTierCalculator _tierCalculator;
        private readonly AEResourceGroupClassifier _groupClassifier;
        private readonly Func<ResourceManager> _resourceManagerGetter;
        private readonly Func<QuestManager> _questManagerGetter;

        // Throttling
        private readonly ThrottledAction _rebuildThrottle;
        private bool _dirty = true;

        // Cached asset lists (for performance - Phase 2B.2)
        private Resource[] _cachedResources;
        private BuffRecipe[] _cachedBuffs;
        private InfinityCauldronStatSO[] _cachedInfinity;

        // Cached string IDs to avoid string allocations during rebuild (Phase 2B.3)
        private readonly Dictionary<Resource, string> _resourceIdCache = new();
        private readonly Dictionary<BuffRecipe, string> _buffIdCache = new();
        private readonly Dictionary<InfinityCauldronStatSO, string> _infinityIdCache = new();

        // Cached group eligibility flags (for performance - Phase 2A.1)
        private readonly bool[] _groupHasCards = new bool[6];

        // Cached lowest card ID (for performance - Phase 2C.2)
        private string _cachedLowestCardId;
        private int _cachedLowestCount = int.MaxValue;

        /// <summary>
        /// Creates a new CardPoolManager.
        /// </summary>
        /// <param name="tierCalculator">Calculator for card tier and maxed status checks.</param>
        /// <param name="groupClassifier">Classifier for determining resource groups.</param>
        /// <param name="resourceManagerGetter">Function to get the ResourceManager instance.</param>
        /// <param name="questManagerGetter">Function to get the QuestManager instance.</param>
        /// <param name="rebuildMinInterval">Minimum interval between pool rebuilds in seconds.</param>
        public CardPoolManager(
            CardTierCalculator tierCalculator,
            AEResourceGroupClassifier groupClassifier,
            Func<ResourceManager> resourceManagerGetter,
            Func<QuestManager> questManagerGetter,
            float rebuildMinInterval = 0.5f)
        {
            _tierCalculator = tierCalculator;
            _groupClassifier = groupClassifier;
            _resourceManagerGetter = resourceManagerGetter;
            _questManagerGetter = questManagerGetter;
            _rebuildThrottle = new ThrottledAction(rebuildMinInterval);
        }

        #region Public Pool Accessors (read-only)

        /// <summary>
        /// Gets the pool of eligible Alter Echo (resource) card IDs.
        /// </summary>
        public IReadOnlyList<string> AlterEchoCards
        {
            get
            {
                RebuildIfNeeded();
                return _alterEchoPool;
            }
        }

        /// <summary>
        /// Gets the pool of eligible buff card IDs.
        /// </summary>
        public IReadOnlyList<string> BuffCards
        {
            get
            {
                RebuildIfNeeded();
                return _buffPool;
            }
        }

        /// <summary>
        /// Gets the combined pool of all eligible card IDs (resources + buffs).
        /// </summary>
        public IReadOnlyList<string> AllCards
        {
            get
            {
                RebuildIfNeeded();
                return _allPool;
            }
        }

        /// <summary>
        /// Gets the pool of infinity stat card IDs.
        /// </summary>
        public IReadOnlyList<string> InfinityCards
        {
            get
            {
                RebuildIfNeeded();
                return _infinityPool;
            }
        }

        /// <summary>
        /// Gets the pool of eligible card IDs for a specific AE resource group.
        /// </summary>
        /// <param name="group">The AE resource group to get cards for.</param>
        /// <returns>A read-only list of card IDs for the group, or empty if none.</returns>
        public IReadOnlyList<string> GetGroupPool(CauldronManager.AEResourceGroup group)
        {
            RebuildIfNeeded();
            return _groupPools.TryGetValue(group, out var list) ? list : Array.Empty<string>();
        }

        #endregion

        #region Quick Eligibility Checks

        /// <summary>
        /// Quickly checks if a group has any eligible cards (uses cached flags after rebuild).
        /// </summary>
        /// <param name="group">The AE resource group to check.</param>
        /// <returns>True if the group has at least one eligible card.</returns>
        public bool HasCardsForGroup(CauldronManager.AEResourceGroup group)
        {
            RebuildIfNeeded();
            return _groupHasCards[(int)group];
        }

        /// <summary>
        /// Returns true if there are any eligible cards in the combined pool.
        /// </summary>
        public bool HasAnyCards
        {
            get
            {
                RebuildIfNeeded();
                return _allPool.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there are any infinity stat cards available.
        /// </summary>
        public bool HasInfinityCards
        {
            get
            {
                RebuildIfNeeded();
                return _infinityPool.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there are any eligible buff cards.
        /// </summary>
        public bool HasBuffCards
        {
            get
            {
                RebuildIfNeeded();
                return _buffPool.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there are any eligible Alter Echo (resource) cards.
        /// </summary>
        public bool HasAlterEchoCards
        {
            get
            {
                RebuildIfNeeded();
                return _alterEchoPool.Count > 0;
            }
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Marks the pools as dirty, causing them to be rebuilt on next access.
        /// </summary>
        public void MarkDirty() => _dirty = true;

        /// <summary>
        /// Rebuilds pools if they are dirty and the throttle allows it.
        /// </summary>
        public void RebuildIfNeeded()
        {
            if (!_dirty) return;
            if (!_rebuildThrottle.TryExecute()) return;

            _dirty = false;
            RebuildPools();
            OnPoolsRebuilt?.Invoke();
        }

        /// <summary>
        /// Forces an immediate pool rebuild, bypassing the throttle.
        /// </summary>
        public void ForceRebuild()
        {
            _rebuildThrottle.Reset();
            _dirty = true;
            RebuildIfNeeded();
        }

        /// <summary>
        /// Initializes the pool manager by caching asset lists and forcing an initial rebuild.
        /// Call this during game startup for optimal performance.
        /// </summary>
        public void Initialize()
        {
            CacheAssetLists();

            // Pre-compute resource groups for all resources (Phase 2C.1)
            if (_cachedResources != null && _groupClassifier != null)
            {
                _groupClassifier.PrecomputeAll(_cachedResources);
            }

            ForceRebuild();
        }

        /// <summary>
        /// Cache asset lists at startup for performance (Phase 2B.2).
        /// Call this after game initialization to avoid repeated asset lookups.
        /// </summary>
        public void CacheAssetLists()
        {
            _cachedResources = AssetCache.GetAll<Resource>();
            _cachedBuffs = AssetCache.GetAll<BuffRecipe>();
            _cachedInfinity = AssetCache.GetAll<InfinityCauldronStatSO>("Infinity");
        }

        /// <summary>
        /// Clears the cached asset lists, forcing fresh lookups on next rebuild.
        /// </summary>
        public void ClearAssetCache()
        {
            _cachedResources = null;
            _cachedBuffs = null;
            _cachedInfinity = null;
            // Also clear ID caches when asset cache is cleared
            _resourceIdCache.Clear();
            _buffIdCache.Clear();
            _infinityIdCache.Clear();
        }

        /// <summary>
        /// Gets a cached resource ID, creating it if not already cached.
        /// </summary>
        private string GetCachedResourceId(Resource res)
        {
            if (!_resourceIdCache.TryGetValue(res, out var id))
            {
                id = CardIdentifierFactory.ForResource(res.name);
                _resourceIdCache[res] = id;
            }
            return id;
        }

        /// <summary>
        /// Gets a cached buff ID, creating it if not already cached.
        /// </summary>
        private string GetCachedBuffId(BuffRecipe buff)
        {
            if (!_buffIdCache.TryGetValue(buff, out var id))
            {
                id = CardIdentifierFactory.ForBuff(buff.name);
                _buffIdCache[buff] = id;
            }
            return id;
        }

        /// <summary>
        /// Gets a cached infinity stat ID, creating it if not already cached.
        /// </summary>
        private string GetCachedInfinityId(InfinityCauldronStatSO inf)
        {
            if (!_infinityIdCache.TryGetValue(inf, out var id))
            {
                id = CardIdentifierFactory.ForInfinity(inf.Stat);
                _infinityIdCache[inf] = id;
            }
            return id;
        }

        private void RebuildPools()
        {
            // Clear existing pools
            _alterEchoPool.Clear();
            _buffPool.Clear();
            _allPool.Clear();
            _infinityPool.Clear();
            foreach (var kvp in _groupPools)
                kvp.Value.Clear();

            // Reset cached lowest card (Phase 2C.2)
            _cachedLowestCardId = null;
            _cachedLowestCount = int.MaxValue;

            var rm = _resourceManagerGetter?.Invoke();
            var qm = _questManagerGetter?.Invoke();

            // Use cached arrays if available, otherwise fetch fresh
            var resources = _cachedResources ?? AssetCache.GetAll<Resource>();
            var buffs = _cachedBuffs ?? AssetCache.GetAll<BuffRecipe>();
            var infinity = _cachedInfinity ?? AssetCache.GetAll<InfinityCauldronStatSO>("Infinity");

            // Build resource pools (using cached IDs to avoid string allocations)
            foreach (var res in resources)
            {
                if (res == null || res.DisableAlterEcho) continue;
                if (rm != null && !rm.IsUnlocked(res)) continue;

                var id = GetCachedResourceId(res);
                if (_tierCalculator.IsMaxed(id)) continue;

                _alterEchoPool.Add(id);
                _allPool.Add(id);

                // Track lowest count card during rebuild (Phase 2C.2)
                var count = _tierCalculator.GetCount(id);
                if (count < _cachedLowestCount)
                {
                    _cachedLowestCount = count;
                    _cachedLowestCardId = id;
                }

                // Also add to group pool
                var group = _groupClassifier.Classify(res);
                if (!_groupPools.TryGetValue(group, out var groupList))
                {
                    groupList = new List<string>();
                    _groupPools[group] = groupList;
                }
                groupList.Add(id);
            }

            // Build buff pools (using cached IDs to avoid string allocations)
            foreach (var buff in buffs)
            {
                if (buff == null) continue;
                var required = buff.requiredQuest;
                if (required != null && (qm == null || !qm.IsQuestCompleted(required)))
                    continue;

                var id = GetCachedBuffId(buff);
                if (_tierCalculator.IsMaxed(id)) continue;

                _buffPool.Add(id);
                _allPool.Add(id);

                // Track lowest count card during rebuild (Phase 2C.2)
                var count = _tierCalculator.GetCount(id);
                if (count < _cachedLowestCount)
                {
                    _cachedLowestCount = count;
                    _cachedLowestCardId = id;
                }
            }

            // Build infinity pools (infinity cards have no max tier, using cached IDs)
            foreach (var inf in infinity)
            {
                if (inf == null) continue;
                var id = GetCachedInfinityId(inf);
                _infinityPool.Add(id);
            }

            // Cache group eligibility flags (Phase 2A.1)
            for (int i = 0; i < 6; i++)
            {
                var group = (CauldronManager.AEResourceGroup)i;
                _groupHasCards[i] = _groupPools.TryGetValue(group, out var list) && list.Count > 0;
            }
        }

        #endregion

        #region Card Picking Methods

        /// <summary>
        /// Picks a random card from the appropriate pool.
        /// </summary>
        /// <param name="onlyAlterEcho">If true, only pick from resource cards.</param>
        /// <param name="onlyBuffs">If true, only pick from buff cards. Takes precedence over onlyAlterEcho.</param>
        /// <returns>A random card ID, or null if the pool is empty.</returns>
        public string PickRandomCard(bool onlyAlterEcho = false, bool onlyBuffs = false)
        {
            RebuildIfNeeded();
            IReadOnlyList<string> pool;
            if (onlyBuffs) pool = _buffPool;
            else if (onlyAlterEcho) pool = _alterEchoPool;
            else pool = _allPool;

            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// Picks a random infinity stat card.
        /// </summary>
        /// <returns>A random infinity card ID, or null if none available.</returns>
        public string PickRandomInfinityCard()
        {
            RebuildIfNeeded();
            if (_infinityPool.Count == 0) return null;
            return _infinityPool[UnityEngine.Random.Range(0, _infinityPool.Count)];
        }

        /// <summary>
        /// Picks a random card from a specific AE resource group.
        /// </summary>
        /// <param name="group">The resource group to pick from.</param>
        /// <returns>A random card ID from the group, or null if empty.</returns>
        public string PickRandomCardFromGroup(CauldronManager.AEResourceGroup group)
        {
            var pool = GetGroupPool(group);
            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// Gets the card ID with the lowest current count from the combined pool.
        /// Useful for "catch-up" mechanics that boost underrepresented cards.
        /// Uses cached value computed during pool rebuild for O(1) lookup (Phase 2C.2).
        /// </summary>
        /// <returns>The card ID with lowest count, or null if pool is empty.</returns>
        public string GetLowestCountCard()
        {
            RebuildIfNeeded();
            return _cachedLowestCardId;
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Gets the current pool counts for diagnostics.
        /// </summary>
        /// <returns>A tuple containing (alterEcho, buff, all, infinity) pool counts.</returns>
        public (int alterEcho, int buff, int all, int infinity) GetPoolCounts()
        {
            RebuildIfNeeded();
            return (_alterEchoPool.Count, _buffPool.Count, _allPool.Count, _infinityPool.Count);
        }

        /// <summary>
        /// Gets whether the pools are currently marked dirty.
        /// </summary>
        public bool IsDirty => _dirty;

        #endregion
    }
}
