using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Service for evaluating gear scores with caching for performance.
    /// Provides a clean API and reduces allocations through scratch dictionary reuse.
    /// </summary>
    public class ScoreEvaluationService : MonoBehaviour
    {
        public static ScoreEvaluationService Instance { get; private set; }

        // Cached theoretical max per slot (computed once, invalidated on asset reload)
        private Dictionary<string, float> _theoreticalMaxBySlot = new();
        private bool _cacheValid;

        // Per-item absolute score cache (cleared each frame)
        private readonly Dictionary<int, float> _absoluteScoreCache = new(32);
        private int _lastFrameCount = -1;

        // Static scratch dictionaries to avoid allocations in hot paths
        private static readonly Dictionary<HeroStatMapping, float> _scratchDelta = new();
        private static readonly Dictionary<HeroStatMapping, float> _scratchTotals = new();
        private static readonly List<float> _scratchContributions = new(16);

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
        /// Evaluation result struct to avoid multiple calls.
        /// </summary>
        public struct EvaluationResult
        {
            public float UpgradeScore;
            public float AbsoluteScore;
            public float QualityPercent;
            public bool IsUpgrade;
        }

        /// <summary>
        /// Single call to evaluate all metrics for a candidate item.
        /// Replaces 3 separate calls in CraftingService.
        /// </summary>
        public EvaluationResult Evaluate(CraftingService crafting, GearItem candidate, GearItem current, string slot)
        {
            // Clear per-item cache each frame
            ClearFrameCacheIfNeeded();

            var upgradeScore = ComputeUpgradeScoreInternal(crafting, candidate, current);
            var absoluteScore = GetOrComputeAbsoluteScore(crafting, candidate);
            var maxForSlot = GetTheoreticalMaxForSlot(slot);
            var qualityPercent = maxForSlot > 0f ? Mathf.Clamp01(absoluteScore / maxForSlot) * 100f : 0f;

            return new EvaluationResult
            {
                UpgradeScore = upgradeScore,
                AbsoluteScore = absoluteScore,
                QualityPercent = qualityPercent,
                IsUpgrade = upgradeScore > 0.0001f
            };
        }

        private void ClearFrameCacheIfNeeded()
        {
            var currentFrame = Time.frameCount;
            if (_lastFrameCount != currentFrame)
            {
                _absoluteScoreCache.Clear();
                _lastFrameCount = currentFrame;
            }
        }

        private float GetOrComputeAbsoluteScore(CraftingService crafting, GearItem item)
        {
            if (item == null) return 0f;

            var key = item.GetHashCode();
            if (_absoluteScoreCache.TryGetValue(key, out var cached))
                return cached;

            var score = ComputeAbsoluteScoreInternal(crafting, item);
            _absoluteScoreCache[key] = score;
            return score;
        }

        /// <summary>
        /// Gets the cached theoretical max for a slot.
        /// </summary>
        public float GetTheoreticalMaxForSlot(string slot)
        {
            if (!_cacheValid)
                RebuildCache();

            if (string.IsNullOrWhiteSpace(slot))
                return 0f;

            return _theoreticalMaxBySlot.TryGetValue(slot, out var max) ? max : ComputeTheoreticalMaxForSlotInternal(slot);
        }

        /// <summary>
        /// Invalidate the cache (call on asset database refresh or game start).
        /// </summary>
        public void InvalidateCache()
        {
            _cacheValid = false;
        }

        private void RebuildCache()
        {
            _theoreticalMaxBySlot.Clear();
            foreach (var slot in new[] { "Weapon", "Helmet", "Chest", "Boots" })
                _theoreticalMaxBySlot[slot] = ComputeTheoreticalMaxForSlotInternal(slot);
            _cacheValid = true;
        }

        /// <summary>
        /// Computes upgrade score using scratch dictionary to avoid allocations.
        /// </summary>
        private static float ComputeUpgradeScoreInternal(CraftingService crafting, GearItem candidate, GearItem current)
        {
            _scratchDelta.Clear();

            if (candidate?.affixes != null)
            {
                foreach (var a in candidate.affixes)
                {
                    if (a?.stat == null) continue;
                    var map = a.stat.heroMapping;
                    _scratchDelta.TryGetValue(map, out var val);
                    _scratchDelta[map] = val + a.value;
                }
            }

            if (current?.affixes != null)
            {
                foreach (var a in current.affixes)
                {
                    if (a?.stat == null) continue;
                    var map = a.stat.heroMapping;
                    _scratchDelta.TryGetValue(map, out var val);
                    _scratchDelta[map] = val - a.value;
                }
            }

            var score = 0f;
            foreach (var kv in _scratchDelta)
            {
                var def = crafting?.GetStatByMapping(kv.Key);
                var scale = def != null ? Mathf.Max(0f, def.ComparisonScale) : 1f;
                score += kv.Value * scale;
            }

            return score;
        }

        /// <summary>
        /// Computes absolute score using scratch dictionary to avoid allocations.
        /// </summary>
        private static float ComputeAbsoluteScoreInternal(CraftingService crafting, GearItem item)
        {
            if (item == null) return 0f;

            _scratchTotals.Clear();

            if (item.affixes != null)
            {
                foreach (var a in item.affixes)
                {
                    if (a?.stat == null) continue;
                    var map = a.stat.heroMapping;
                    _scratchTotals.TryGetValue(map, out var val);
                    _scratchTotals[map] = val + a.value;
                }
            }

            var score = 0f;
            foreach (var kv in _scratchTotals)
            {
                var def = crafting?.GetStatByMapping(kv.Key);
                var scale = def != null ? Mathf.Max(0f, def.ComparisonScale) : 1f;
                score += kv.Value * scale;
            }

            return score;
        }

        private static float ComputeTheoreticalMaxForSlotInternal(string slot)
        {
            var maxAffixes = 1;
            foreach (var rarity in AssetCache.GetAll<RaritySO>(string.Empty))
                if (rarity != null && rarity.affixCount > maxAffixes)
                    maxAffixes = rarity.affixCount;

            var stats = AssetCache.GetAll<StatDefSO>(string.Empty);
            if (stats == null || stats.Length == 0)
                return 0f;

            bool IsAllowed(StatDefSO stat)
            {
                if (stat == null) return false;
                if (stat.heroMapping == HeroStatMapping.MoveSpeed &&
                    !string.Equals(slot, "Boots", StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }

            _scratchContributions.Clear();
            foreach (var stat in stats)
            {
                if (!IsAllowed(stat)) continue;
                var scale = Mathf.Max(0f, stat.ComparisonScale);
                _scratchContributions.Add(stat.maxRoll * scale);
            }

            if (_scratchContributions.Count == 0)
                return 0f;

            _scratchContributions.Sort((a, b) => b.CompareTo(a));
            var count = Mathf.Clamp(maxAffixes, 1, _scratchContributions.Count);
            float sum = 0f;
            for (var i = 0; i < count; i++)
                sum += _scratchContributions[i];

            return sum;
        }
    }
}
