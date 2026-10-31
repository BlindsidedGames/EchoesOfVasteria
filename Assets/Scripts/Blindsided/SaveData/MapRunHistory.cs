using System;
using System.Collections.Generic;
using System.Linq;

namespace Blindsided.SaveData
{
    /// <summary>Totals of evicted runs only; lifetime gameplay statistics are maintained separately.</summary>
    public sealed class RunHistoryTotals
    {
        public long Runs;
        public double Duration, Distance, Resources, BonusResources, DamageDealt, DamageTaken;
        public long Tasks, Kills, Deaths, Reaps, Abandoned;

        public void Add(GameData.RunRecord run)
        {
            Runs++;
            Duration += run.Duration; Distance += run.Distance;
            Resources += run.ResourcesCollected; BonusResources += run.BonusResourcesCollected;
            DamageDealt += run.DamageDealtAsDouble; DamageTaken += run.DamageTakenAsDouble;
            Tasks += run.TasksCompleted; Kills += run.EnemiesKilled;
            // Reaping also sets Died in the existing run record. Outcomes must be exclusive.
            if (run.Abandoned) Abandoned++;
            else if (run.Reaped) Reaps++;
            else if (run.Died) Deaths++;
        }

        public void Add(RunHistoryTotals totals)
        {
            Runs += totals.Runs; Duration += totals.Duration; Distance += totals.Distance;
            Resources += totals.Resources; BonusResources += totals.BonusResources;
            DamageDealt += totals.DamageDealt; DamageTaken += totals.DamageTaken;
            Tasks += totals.Tasks; Kills += totals.Kills;
            Deaths += totals.Deaths; Reaps += totals.Reaps; Abandoned += totals.Abandoned;
        }
    }

    /// <summary>One retention rule shared by migration and live run recording.</summary>
    public static class MapRunHistory
    {
        public const int RecentLimit = 10;

        public static void Compact(List<GameData.RunRecord> recent, Dictionary<string, RunHistoryTotals> previous)
        {
            recent.RemoveAll(run => run == null);
            // Keep chronological order, including interleaved and currently unavailable maps.
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var evicted = new HashSet<GameData.RunRecord>();
            for (var i = recent.Count - 1; i >= 0; i--)
            {
                var key = recent[i].MapType ?? string.Empty;
                counts.TryGetValue(key, out var count);
                counts[key] = count + 1;
                if (count >= RecentLimit) evicted.Add(recent[i]);
            }
            foreach (var run in recent.Where(evicted.Contains))
            {
                var key = run.MapType ?? string.Empty;
                if (!previous.TryGetValue(key, out var totals) || totals == null)
                    previous[key] = totals = new RunHistoryTotals();
                totals.Add(run);
            }
            recent.RemoveAll(evicted.Contains);
        }

        public static RunHistoryTotals Previous(IReadOnlyDictionary<string, RunHistoryTotals> history, string map = null)
        {
            var totals = new RunHistoryTotals();
            if (history == null) return totals;
            foreach (var pair in history)
                if ((map == null || pair.Key == map) && pair.Value != null) totals.Add(pair.Value);
            return totals;
        }
    }
}
