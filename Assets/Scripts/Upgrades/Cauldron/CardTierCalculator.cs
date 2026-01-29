using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Calculates card tiers based on count thresholds.
    /// Extracted from CauldronManager to provide reusable tier calculation logic.
    /// </summary>
    public class CardTierCalculator
    {
        public const string ResourcePrefix = "RES:";
        public const string BuffPrefix = "BUFF:";
        public const string InfinityPrefix = "INF:";

        private readonly CauldronConfig _config;
        private readonly Func<Dictionary<string, int>> _cardCountsGetter;

        /// <summary>
        /// Creates a new CardTierCalculator.
        /// </summary>
        /// <param name="config">The cauldron configuration containing tier thresholds.</param>
        /// <param name="cardCountsGetter">A function that returns the current card counts dictionary.</param>
        public CardTierCalculator(CauldronConfig config, Func<Dictionary<string, int>> cardCountsGetter)
        {
            _config = config;
            _cardCountsGetter = cardCountsGetter;
        }

        /// <summary>
        /// Gets the current count for a card by its full ID (e.g., "RES:Wheat" or "BUFF:Haste").
        /// </summary>
        public int GetCount(string cardId)
        {
            var dict = _cardCountsGetter?.Invoke();
            if (dict == null) return 0;
            return dict.TryGetValue(cardId, out var c) ? c : 0;
        }

        /// <summary>
        /// Gets the tier for a card by its full ID.
        /// </summary>
        public int GetTier(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _config == null)
                return 0;

            var count = GetCount(cardId);

            int[] thresholds = null;
            if (cardId.StartsWith(ResourcePrefix))
                thresholds = _config.resourceTierThresholds;
            else if (cardId.StartsWith(BuffPrefix))
                thresholds = _config.buffTierThresholds;
            // INF: cards have no tier system

            return GetTierFromThresholds(count, thresholds);
        }

        /// <summary>
        /// Gets the tier for a resource by name (without prefix).
        /// </summary>
        public int GetResourceTier(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName) || _config == null)
                return 0;

            var key = $"{ResourcePrefix}{resourceName}";
            var count = GetCount(key);
            return GetTierFromThresholds(count, _config.resourceTierThresholds);
        }

        /// <summary>
        /// Gets the tier for a buff by name (without prefix).
        /// </summary>
        public int GetBuffTier(string buffName)
        {
            if (string.IsNullOrEmpty(buffName) || _config == null)
                return 0;

            var key = $"{BuffPrefix}{buffName}";
            var count = GetCount(key);
            return GetTierFromThresholds(count, _config.buffTierThresholds);
        }

        /// <summary>
        /// Returns normalized progress (0..1) toward the next tier for the given card id.
        /// Id should be formatted as "RES:name" or "BUFF:name".
        /// Returns 1 when already at max tier.
        /// </summary>
        public float GetTierProgress(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _config == null)
                return 0f;

            var count = GetCount(cardId);

            int[] thresholds = null;
            if (cardId.StartsWith(ResourcePrefix))
                thresholds = _config.resourceTierThresholds;
            else if (cardId.StartsWith(BuffPrefix))
                thresholds = _config.buffTierThresholds;

            if (thresholds == null || thresholds.Length == 0)
                return 0f;

            var tier = GetTierFromThresholds(count, thresholds);
            if (tier >= thresholds.Length)
                return 1f; // max tier => full

            // Compute progress between previous threshold (or 0) and next threshold
            var prev = tier <= 0 ? 0 : thresholds[Mathf.Clamp(tier - 1, 0, thresholds.Length - 1)];
            var next = thresholds[Mathf.Clamp(tier, 0, thresholds.Length - 1)];
            if (next <= prev)
                return 0f;
            return Mathf.InverseLerp(prev, next, count);
        }

        /// <summary>
        /// Checks if a card has reached the maximum tier.
        /// Optimized to avoid Substring allocations by checking prefix char and using full cardId.
        /// </summary>
        public bool IsMaxed(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _config == null)
                return false;

            // Check first char to determine type without allocating via StartsWith
            var firstChar = cardId[0];
            if (firstChar == 'R') // RES:
                return IsMaxedByFullId(cardId, _config.resourceTierThresholds);
            if (firstChar == 'B') // BUFF:
                return IsMaxedByFullId(cardId, _config.buffTierThresholds);

            // INF: cards have no max
            return false;
        }

        /// <summary>
        /// Checks if a card is maxed using its full ID directly (avoids Substring allocation).
        /// </summary>
        private bool IsMaxedByFullId(string fullCardId, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0)
                return false;
            var count = GetCount(fullCardId);
            var tier = GetTierFromThresholds(count, thresholds);
            return tier >= thresholds.Length;
        }

        /// <summary>
        /// Checks if a resource has reached the maximum tier.
        /// </summary>
        public bool IsResourceMaxed(string resourceName)
        {
            if (_config == null) return false;
            var maxTier = _config.resourceTierThresholds != null ? _config.resourceTierThresholds.Length : 0;
            if (maxTier <= 0) return false;
            return GetResourceTier(resourceName) >= maxTier;
        }

        /// <summary>
        /// Checks if a buff has reached the maximum tier.
        /// </summary>
        public bool IsBuffMaxed(string buffName)
        {
            if (_config == null) return false;
            var maxTier = _config.buffTierThresholds != null ? _config.buffTierThresholds.Length : 0;
            if (maxTier <= 0) return false;
            return GetBuffTier(buffName) >= maxTier;
        }

        /// <summary>
        /// Gets the maximum possible tier for resources.
        /// </summary>
        public int GetMaxResourceTier()
        {
            if (_config == null || _config.resourceTierThresholds == null)
                return 0;
            return _config.resourceTierThresholds.Length;
        }

        /// <summary>
        /// Gets the maximum possible tier for buffs.
        /// </summary>
        public int GetMaxBuffTier()
        {
            if (_config == null || _config.buffTierThresholds == null)
                return 0;
            return _config.buffTierThresholds.Length;
        }

        /// <summary>
        /// Calculates tier from count using the provided thresholds array.
        /// Returns 0 when below first threshold, or tier index + 1 for each threshold met.
        /// </summary>
        private int GetTierFromThresholds(int count, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0)
                return 0;

            var tier = 0;
            for (var i = 0; i < thresholds.Length; i++)
            {
                if (count >= thresholds[i])
                    tier = i + 1;
            }
            return tier;
        }
    }
}
