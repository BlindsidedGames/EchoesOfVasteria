using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Caps legacy resource and buff cards at their historical maxima, then moves every excess
    /// card into existing card capacity or canonical Infinity cards without consulting assets.
    /// </summary>
    internal sealed class Migration_CauldronOverflowRedistribution : ISaveMigration
    {
        private const int ResourceCardMaximum = 500;
        private const int BuffCardMaximum = 300;

        private static readonly string[] CanonicalInfinityIds =
        {
            "INF:AttackRate",
            "INF:CritChance",
            "INF:CritDamage",
            "INF:Damage",
            "INF:Defense",
            "INF:HealthRegen",
            "INF:MaxHealth",
            "INF:MoveSpeed"
        };

        public int? TargetSchema => null;
        public string TargetVersion => "1.2.17";
        public string Id => "CauldronOverflowRedistribution";

        public void Apply(GameData data)
        {
            if (data == null)
                return;

            data.CauldronCardCounts ??= new Dictionary<string, int>();
            var counts = data.CauldronCardCounts;
            var cappedCards = new List<CappedCard>();
            long overflow = 0;

            foreach (var id in counts.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList())
            {
                if (!TryGetMaximum(id, out var maximum) || counts[id] <= maximum)
                    continue;

                var excess = counts[id] - maximum;
                counts[id] = maximum;
                cappedCards.Add(new CappedCard(id, excess));
                overflow += excess;
            }

            if (overflow == 0)
                return;

            overflow = FillExistingCardCapacity(counts, overflow);
            overflow = DistributeToInfinityCards(counts, overflow);
            overflow = RestoreUndistributedCards(counts, cappedCards, overflow);

            if (overflow != 0)
                throw new InvalidOperationException("Cauldron overflow migration could not preserve every card.");
        }

        private static long FillExistingCardCapacity(Dictionary<string, int> counts, long overflow)
        {
            foreach (var id in counts.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList())
            {
                if (overflow == 0)
                    break;
                if (!TryGetMaximum(id, out var maximum))
                    continue;

                var current = counts[id];
                if (current < 0 || current >= maximum)
                    continue;

                var added = (int)Math.Min((long)maximum - current, overflow);
                counts[id] = current + added;
                overflow -= added;
            }

            return overflow;
        }

        private static long DistributeToInfinityCards(Dictionary<string, int> counts, long overflow)
        {
            var infinityIds = new SortedSet<string>(CanonicalInfinityIds, StringComparer.Ordinal);
            foreach (var id in counts.Keys)
            {
                if (id != null && id.StartsWith("INF:", StringComparison.Ordinal) && id.Length > 4)
                    infinityIds.Add(id);
            }

            var available = infinityIds
                .Where(id => !counts.TryGetValue(id, out var count) || count < int.MaxValue)
                .ToList();

            while (overflow > 0 && available.Count > 0)
            {
                var share = Math.Max(1L, overflow / available.Count);
                var madeProgress = false;

                for (var index = available.Count - 1; index >= 0 && overflow > 0; index--)
                {
                    var id = available[index];
                    var current = counts.TryGetValue(id, out var count) ? count : 0;
                    var room = (long)int.MaxValue - current;
                    if (room <= 0)
                    {
                        available.RemoveAt(index);
                        continue;
                    }

                    var added = Math.Min(Math.Min(room, share), overflow);
                    counts[id] = (int)(current + added);
                    overflow -= added;
                    madeProgress = true;

                    if (added == room)
                        available.RemoveAt(index);
                }

                if (!madeProgress)
                    break;
            }

            return overflow;
        }

        private static long RestoreUndistributedCards(
            Dictionary<string, int> counts,
            IReadOnlyList<CappedCard> cappedCards,
            long overflow)
        {
            foreach (var card in cappedCards)
            {
                if (overflow == 0)
                    break;

                var restored = (int)Math.Min(card.Excess, overflow);
                counts[card.Id] += restored;
                overflow -= restored;
            }

            return overflow;
        }

        private static bool TryGetMaximum(string id, out int maximum)
        {
            if (id != null && id.StartsWith("RES:", StringComparison.Ordinal))
            {
                maximum = ResourceCardMaximum;
                return true;
            }

            if (id != null && id.StartsWith("BUFF:", StringComparison.Ordinal))
            {
                maximum = BuffCardMaximum;
                return true;
            }

            maximum = 0;
            return false;
        }

        private readonly struct CappedCard
        {
            public CappedCard(string id, int excess)
            {
                Id = id;
                Excess = excess;
            }

            public string Id { get; }
            public int Excess { get; }
        }
    }
}
