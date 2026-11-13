using System;
using System.Linq;

namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV12CauldronCounters : IPostVersionSaveMigration
    {
        public int? TargetSchema => 12;
        public string TargetVersion => null;
        public string Id => "SchemaV12CauldronCounters";

        public void Apply(GameData data)
        {
            data.CauldronTotals ??= new GameData.CauldronTotalsRecord();
            var retainedCards = 0L;
            if (data.CauldronCardCounts != null)
            {
                foreach (var id in data.CauldronCardCounts.Keys.ToArray())
                {
                    var count = data.CauldronCardCounts[id];
                    if (count < 0 && id.StartsWith("INF:", StringComparison.Ordinal))
                        data.CauldronCardCounts[id] = count = int.MaxValue;
                    retainedCards += Math.Max(0, count);
                }
            }
            data.CauldronTotals.TotalCards = RecoverCounter(data.CauldronTotals.TotalCards, retainedCards);
            data.CauldronTotals.TotalTastings = RecoverCounter(data.CauldronTotals.TotalTastings, 0);
        }

        private static long RecoverCounter(long total, long minimum)
        {
            // A nonnegative legacy total may be absent or incomplete, so only restore
            // the retained cards we can prove. A negative total proves a signed wrap.
            if (total >= 0) return Math.Max(total, minimum);
            const long wrap = 1L << 32;
            total += wrap;
            // Each wrap loses 2^32. Retained cards give a lower bound, not proof of
            // the exact lifetime total after multiple wraps. Preserve the remainder.
            if (total < minimum)
                total += ((minimum - total + wrap - 1) / wrap) * wrap;
            return total;
        }
    }
}
