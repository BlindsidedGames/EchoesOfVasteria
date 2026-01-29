using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Enemies;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Classifies resources into Alter Echo subcategories.
    /// Extracted from CauldronManager to provide reusable resource classification logic.
    /// </summary>
    public class AEResourceGroupClassifier
    {
        private readonly Dictionary<Resource, CauldronManager.AEResourceGroup> _cache = new();

        // Static cache for TaskData→AEResourceGroup mappings. These are based on static
        // ScriptableObject data and never change at runtime, so we cache them permanently
        // to avoid repeated string allocations from skill.name access.
        private static readonly Dictionary<TaskData, CauldronManager.AEResourceGroup?> _taskGroupCache = new();

        /// <summary>
        /// Classifies a resource into an AE subcategory.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="res">The resource to classify.</param>
        /// <returns>The AE resource group for the resource.</returns>
        public CauldronManager.AEResourceGroup Classify(Resource res)
        {
            if (res == null) return CauldronManager.AEResourceGroup.Combat;
            if (_cache.TryGetValue(res, out var cached)) return cached;

            var group = ClassifyInternal(res);
            _cache[res] = group;
            return group;
        }

        private CauldronManager.AEResourceGroup ClassifyInternal(Resource res)
        {
            // 1. Check explicit cauldronCategory override on Resource
            if (res.cauldronCategory != Resource.CauldronCategory.Auto)
            {
                CauldronManager.AEResourceGroup mapped = CauldronManager.AEResourceGroup.Combat;
                switch (res.cauldronCategory)
                {
                    case Resource.CauldronCategory.Farming:
                        mapped = CauldronManager.AEResourceGroup.Farming;
                        break;
                    case Resource.CauldronCategory.Fishing:
                        mapped = CauldronManager.AEResourceGroup.Fishing;
                        break;
                    case Resource.CauldronCategory.Mining:
                        mapped = CauldronManager.AEResourceGroup.Mining;
                        break;
                    case Resource.CauldronCategory.Woodcutting:
                        mapped = CauldronManager.AEResourceGroup.Woodcutting;
                        break;
                    case Resource.CauldronCategory.Looting:
                        mapped = CauldronManager.AEResourceGroup.Looting;
                        break;
                    case Resource.CauldronCategory.Combat:
                        mapped = CauldronManager.AEResourceGroup.Combat;
                        break;
                }
                return mapped;
            }

            // 2. Build counts per group using TaskData and EnemyData
            var counts = new Dictionary<CauldronManager.AEResourceGroup, int>
            {
                { CauldronManager.AEResourceGroup.Farming, 0 },
                { CauldronManager.AEResourceGroup.Fishing, 0 },
                { CauldronManager.AEResourceGroup.Mining, 0 },
                { CauldronManager.AEResourceGroup.Woodcutting, 0 },
                { CauldronManager.AEResourceGroup.Looting, 0 },
                { CauldronManager.AEResourceGroup.Combat, 0 }
            };

            // Tasks
            foreach (var t in AssetCache.GetAll<TaskData>("Tasks"))
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

            // Enemies -> Combat
            foreach (var e in AssetCache.GetAll<EnemyData>(""))
            {
                if (e == null) continue;
                foreach (var drop in e.resourceDrops)
                {
                    if (drop == null || drop.resource == null) continue;
                    if (drop.resource == res) counts[CauldronManager.AEResourceGroup.Combat]++;
                }
            }

            // 3. Return group with highest count
            var bestGroup = CauldronManager.AEResourceGroup.Combat;
            var bestCount = -1;
            foreach (var kv in counts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    bestGroup = kv.Key;
                }
            }

            return bestGroup;
        }

        /// <summary>
        /// Infers the AE resource group from a task based on its associated skill or prefab type.
        /// Results are cached in a static dictionary to avoid repeated string allocations.
        /// </summary>
        /// <param name="t">The task data to analyze.</param>
        /// <returns>The inferred group, or null if no clear classification.</returns>
        private CauldronManager.AEResourceGroup? InferGroupFromTask(TaskData t)
        {
            if (t == null) return null;

            // Check static cache first to avoid string allocations from skill.name
            if (_taskGroupCache.TryGetValue(t, out var cached))
                return cached;

            var result = InferGroupFromTaskInternal(t);
            _taskGroupCache[t] = result;
            return result;
        }

        private static CauldronManager.AEResourceGroup? InferGroupFromTaskInternal(TaskData t)
        {
            var skill = t.associatedSkill;
            var name = skill != null ? skill.name : null;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("Farm", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Farming;
                if (name.Contains("Fish", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Fishing;
                if (name.Contains("Min", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Mining;
                if (name.Contains("Wood", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Woodcutting;
                if (name.Contains("Loot", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Looting;
                if (name.Contains("Combat", StringComparison.OrdinalIgnoreCase))
                    return CauldronManager.AEResourceGroup.Combat;
            }

            // Fallback: try prefab type name
            var prefab = t.taskPrefab;
            if (prefab != null)
            {
                var typeName = prefab.GetType().Name;
                if (typeName.Contains("Farming"))
                    return CauldronManager.AEResourceGroup.Farming;
                if (typeName.Contains("Fishing"))
                    return CauldronManager.AEResourceGroup.Fishing;
                if (typeName.Contains("Mining"))
                    return CauldronManager.AEResourceGroup.Mining;
                if (typeName.Contains("Woodcutting"))
                    return CauldronManager.AEResourceGroup.Woodcutting;
                if (typeName.Contains("Chest") || typeName.Contains("Loot"))
                    return CauldronManager.AEResourceGroup.Looting;
            }

            return null;
        }

        /// <summary>
        /// Clears the classification cache.
        /// Call this if resource data changes at runtime.
        /// </summary>
        public void ClearCache() => _cache.Clear();

        /// <summary>
        /// Pre-computes classification for all provided resources.
        /// Useful for warming up the cache at load time.
        /// </summary>
        /// <param name="resources">The resources to classify.</param>
        public void PrecomputeAll(IEnumerable<Resource> resources)
        {
            foreach (var res in resources)
            {
                if (res != null) Classify(res);
            }
        }

        /// <summary>
        /// Gets the current cache size.
        /// </summary>
        public int CacheCount => _cache.Count;

        /// <summary>
        /// Checks if a resource is already cached.
        /// </summary>
        public bool IsCached(Resource res) => res != null && _cache.ContainsKey(res);
    }
}
