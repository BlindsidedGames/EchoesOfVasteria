using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Resolves taste roll outcomes using weighted random selection.
    /// Extracted from CauldronManager to provide testable, isolated tasting logic.
    /// </summary>
    public class TasteRollResolver
    {
        /// <summary>
        /// Types of roll outcomes from tasting.
        /// </summary>
        public enum RollType
        {
            /// <summary>No card granted.</summary>
            Nothing,
            /// <summary>Legacy single Alter Echo (deprecated, kept for compatibility).</summary>
            AlterEcho,
            /// <summary>Alter Echo card from farming resources.</summary>
            AEFarming,
            /// <summary>Alter Echo card from fishing resources.</summary>
            AEFishing,
            /// <summary>Alter Echo card from mining resources.</summary>
            AEMining,
            /// <summary>Alter Echo card from woodcutting resources.</summary>
            AEWoodcutting,
            /// <summary>Alter Echo card from looting resources.</summary>
            AELooting,
            /// <summary>Alter Echo card from combat resources.</summary>
            AECombat,
            /// <summary>Buff card.</summary>
            Buff,
            /// <summary>Card with lowest current count.</summary>
            Lowest,
            /// <summary>Eva's Blessing: grants 2 cards.</summary>
            EvasX2,
            /// <summary>Vast Surge: grants 10 cards.</summary>
            VastX10,
            /// <summary>Infinity (Eternal Boons) card.</summary>
            Infinity
        }

        /// <summary>
        /// Result of a taste roll including the roll type and list of card IDs to grant.
        /// </summary>
        public struct RollResult
        {
            /// <summary>The type of roll that occurred.</summary>
            public RollType Type;
            /// <summary>List of card IDs to grant (may be empty for Nothing rolls).</summary>
            public List<string> CardIds;
            /// <summary>The base card count before multiplier scaling.</summary>
            public int BaseCount;
            /// <summary>The final card count after multiplier scaling.</summary>
            public int ScaledCount;
        }

        private readonly CardPoolManager _poolManager;
        private readonly CauldronConfig _config;

        // Reusable list to avoid allocations during roll resolution
        private readonly List<string> _grantedCards = new();

        /// <summary>
        /// Creates a new TasteRollResolver.
        /// </summary>
        /// <param name="poolManager">The card pool manager for picking cards.</param>
        /// <param name="config">The cauldron configuration with weight curves.</param>
        public TasteRollResolver(CardPoolManager poolManager, CauldronConfig config)
        {
            _poolManager = poolManager ?? throw new ArgumentNullException(nameof(poolManager));
            _config = config;
        }

        /// <summary>
        /// Resolve a taste roll at the given Eva level with card multiplier.
        /// Returns the roll type and list of card IDs to grant.
        /// </summary>
        /// <param name="evaLevel">The current Eva level for weight evaluation.</param>
        /// <param name="cardMultiplier">Multiplier applied to card counts (from upgrades).</param>
        /// <returns>The roll result containing type and cards to grant.</returns>
        public RollResult Resolve(int evaLevel, float cardMultiplier)
        {
            _grantedCards.Clear();

            var weights = ComputeEffectiveWeights(evaLevel);
            var rollType = SelectRollType(weights);
            var (baseCount, scaledCount) = GetCardCountForRoll(rollType, cardMultiplier);

            GrantCardsForRoll(rollType, scaledCount);

            return new RollResult
            {
                Type = rollType,
                CardIds = new List<string>(_grantedCards), // Copy to avoid mutation issues
                BaseCount = baseCount,
                ScaledCount = scaledCount
            };
        }

        /// <summary>
        /// Compute effective weights at a given Eva level, gated by pool eligibility.
        /// Weights are zeroed out for categories that have no eligible cards.
        /// </summary>
        /// <param name="evaLevel">The Eva level for weight curve evaluation.</param>
        /// <returns>A snapshot of effective weights for this level.</returns>
        public CauldronManager.EffectiveWeightsSnapshot ComputeEffectiveWeights(int evaLevel)
        {
            if (_config == null)
                return default;

            // Evaluate base weights from config curves
            var snap = new CauldronManager.EffectiveWeightsSnapshot
            {
                wNothing = _config.weightNothing.Evaluate(evaLevel),
                wAEFarming = _config.weightAEFarming.Evaluate(evaLevel),
                wAEFishing = _config.weightAEFishing.Evaluate(evaLevel),
                wAEMining = _config.weightAEMining.Evaluate(evaLevel),
                wAEWoodcutting = _config.weightAEWoodcutting.Evaluate(evaLevel),
                wAECombat = _config.weightAECombat.Evaluate(evaLevel),
                wAELooting = _config.weightAELooting.Evaluate(evaLevel),
                wBuff = _config.weightBuffCard.Evaluate(evaLevel),
                wLow = _config.weightLowestCountCard.Evaluate(evaLevel),
                wX2 = _config.weightEvasBlessingX2.Evaluate(evaLevel),
                wX10 = _config.weightVastSurgeX10.Evaluate(evaLevel),
                wInfinity = _config.weightInfinity.Evaluate(evaLevel)
            };

            // Gate by pool eligibility (uses cached flags from CardPoolManager)
            _poolManager.RebuildIfNeeded();

            bool anyAllCards = _poolManager.HasAnyCards;
            bool anyInfinity = _poolManager.HasInfinityCards;

            // Disable multi-card rolls if no cards available
            if (!anyAllCards && !anyInfinity)
            {
                snap.wLow = 0f;
                snap.wX2 = 0f;
                snap.wX10 = 0f;
            }

            // Lowest should always be disabled when no normal cards remain
            if (!anyAllCards) snap.wLow = 0f;

            // Infinity only when no other cards are available (all maxed) and at least one Infinity option exists
            bool infinityActive = !anyAllCards && anyInfinity;
            if (!infinityActive)
            {
                snap.wInfinity = 0f;
            }

            // Buff pool eligibility
            if (!_poolManager.HasBuffCards) snap.wBuff = 0f;

            // AE subcategory eligibility (uses cached flags - fast!)
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Farming)) snap.wAEFarming = 0f;
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Fishing)) snap.wAEFishing = 0f;
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Mining)) snap.wAEMining = 0f;
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Woodcutting)) snap.wAEWoodcutting = 0f;
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Looting)) snap.wAELooting = 0f;
            if (!_poolManager.HasCardsForGroup(CauldronManager.AEResourceGroup.Combat)) snap.wAECombat = 0f;

            return snap;
        }

        /// <summary>
        /// Select a roll type using weighted random selection.
        /// </summary>
        /// <param name="eff">The effective weights snapshot.</param>
        /// <returns>The selected roll type.</returns>
        private RollType SelectRollType(CauldronManager.EffectiveWeightsSnapshot eff)
        {
            // Calculate total weight
            float subAETotal = eff.wAEFarming + eff.wAEFishing + eff.wAEMining +
                               eff.wAEWoodcutting + eff.wAELooting + eff.wAECombat;
            bool subAEPresent = subAETotal > 0f;

            float total = eff.wNothing + eff.wBuff + eff.wLow + eff.wX2 + eff.wX10 + eff.wInfinity;
            if (subAEPresent) total += subAETotal;

            if (total <= 0f) return RollType.Nothing;

            // Weighted random selection
            float r = UnityEngine.Random.value * total;

            if ((r -= eff.wNothing) <= 0) return RollType.Nothing;

            if (subAEPresent)
            {
                if ((r -= eff.wAEFarming) <= 0) return RollType.AEFarming;
                if ((r -= eff.wAEFishing) <= 0) return RollType.AEFishing;
                if ((r -= eff.wAEMining) <= 0) return RollType.AEMining;
                if ((r -= eff.wAEWoodcutting) <= 0) return RollType.AEWoodcutting;
                if ((r -= eff.wAELooting) <= 0) return RollType.AELooting;
                if ((r -= eff.wAECombat) <= 0) return RollType.AECombat;
            }

            if ((r -= eff.wInfinity) <= 0) return RollType.Infinity;
            if ((r -= eff.wBuff) <= 0) return RollType.Buff;
            if ((r -= eff.wLow) <= 0) return RollType.Lowest;
            if ((r -= eff.wX2) <= 0) return RollType.EvasX2;

            return RollType.VastX10;
        }

        /// <summary>
        /// Get the base and scaled card counts for a roll type.
        /// </summary>
        /// <param name="type">The roll type.</param>
        /// <param name="multiplier">The card multiplier from upgrades.</param>
        /// <returns>A tuple of (base count, scaled count).</returns>
        private (int baseCount, int scaledCount) GetCardCountForRoll(RollType type, float multiplier)
        {
            int baseCount = type switch
            {
                RollType.Nothing => 0,
                RollType.EvasX2 => 2,
                RollType.VastX10 => 10,
                _ => 1
            };

            int scaled = ScaleCardCount(baseCount, multiplier);
            return (baseCount, scaled);
        }

        /// <summary>
        /// Scale a card count by a multiplier, ensuring at least the base count is returned.
        /// </summary>
        /// <param name="baseCount">The base card count.</param>
        /// <param name="multiplier">The multiplier to apply.</param>
        /// <returns>The scaled card count.</returns>
        private static int ScaleCardCount(int baseCount, float multiplier)
        {
            if (baseCount <= 0) return 0;
            if (multiplier <= 1f) return baseCount;
            return Math.Max(baseCount, Mathf.RoundToInt(baseCount * multiplier));
        }

        /// <summary>
        /// Grant cards for the selected roll type.
        /// </summary>
        /// <param name="type">The roll type.</param>
        /// <param name="count">The number of cards to grant.</param>
        private void GrantCardsForRoll(RollType type, int count)
        {
            if (count <= 0) return;

            switch (type)
            {
                case RollType.Nothing:
                    // No cards granted
                    break;

                case RollType.AEFarming:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Farming, count);
                    break;
                case RollType.AEFishing:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Fishing, count);
                    break;
                case RollType.AEMining:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Mining, count);
                    break;
                case RollType.AEWoodcutting:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Woodcutting, count);
                    break;
                case RollType.AELooting:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Looting, count);
                    break;
                case RollType.AECombat:
                    GrantFromGroup(CauldronManager.AEResourceGroup.Combat, count);
                    break;

                case RollType.Buff:
                    GrantRandomCards(count, onlyBuffs: true);
                    break;

                case RollType.Lowest:
                    GrantLowestCards(count);
                    break;

                case RollType.EvasX2:
                case RollType.VastX10:
                    // If all normal cards maxed, grant infinity instead
                    if (!_poolManager.HasAnyCards && _poolManager.HasInfinityCards)
                        GrantRandomInfinityCards(count);
                    else
                        GrantRandomCards(count);
                    break;

                case RollType.Infinity:
                    GrantRandomInfinityCards(count);
                    break;

                case RollType.AlterEcho:
                    // Legacy single AE - grant from any AE pool
                    GrantRandomCards(count, onlyAlterEcho: true);
                    break;
            }
        }

        /// <summary>
        /// Grant cards from a specific AE resource group.
        /// Falls back to any alter echo card if the specific group is empty.
        /// </summary>
        /// <param name="group">The AE resource group to pick from.</param>
        /// <param name="count">The number of cards to grant.</param>
        private void GrantFromGroup(CauldronManager.AEResourceGroup group, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = _poolManager.PickRandomCardFromGroup(group);
                if (id == null)
                {
                    // Fallback to any alter echo card
                    id = _poolManager.PickRandomCard(onlyAlterEcho: true);
                }
                if (id != null)
                    _grantedCards.Add(id);
            }
        }

        /// <summary>
        /// Grant random cards from the appropriate pool.
        /// </summary>
        /// <param name="count">The number of cards to grant.</param>
        /// <param name="onlyAlterEcho">If true, only pick from alter echo cards.</param>
        /// <param name="onlyBuffs">If true, only pick from buff cards.</param>
        private void GrantRandomCards(int count, bool onlyAlterEcho = false, bool onlyBuffs = false)
        {
            for (int i = 0; i < count; i++)
            {
                var id = _poolManager.PickRandomCard(onlyAlterEcho, onlyBuffs);
                if (id != null)
                    _grantedCards.Add(id);
            }
        }

        /// <summary>
        /// Grant random infinity cards.
        /// </summary>
        /// <param name="count">The number of cards to grant.</param>
        private void GrantRandomInfinityCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = _poolManager.PickRandomInfinityCard();
                if (id != null)
                    _grantedCards.Add(id);
            }
        }

        /// <summary>
        /// Grant cards with the lowest current count.
        /// </summary>
        /// <param name="count">The number of cards to grant.</param>
        private void GrantLowestCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = _poolManager.GetLowestCountCard();
                if (id != null)
                    _grantedCards.Add(id);
            }
        }
    }
}
