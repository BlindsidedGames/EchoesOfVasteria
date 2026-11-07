using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Farming
{
    /// <summary>Read-only presentation forecast using the same crop progression as settlement.</summary>
    public sealed class FarmBackgroundProjection
    {
        private readonly List<(double seconds, double resources)> points = new();
        public double HorizonSeconds { get; private set; }
        public double Total => points.Count == 0 ? 0 : points[^1].resources;

        public static FarmBackgroundProjection Create(GameData owner, FarmContent content,
            BackgroundRunEstimator.Result run, double horizonSeconds,
            IReadOnlyDictionary<Resource, double> seedsPerTask = null)
        {
            if (owner?.Farm?.Beds == null || !content || !FarmProjects.AutoReplant(owner.Farm) ||
                !owner.Farm.Beds.Any(pair => FarmCommands.AccessibleBed(owner.Farm, pair.Key) && pair.Value?.Repeat == true) ||
                !double.IsFinite(horizonSeconds) || horizonSeconds <= 0) return null;

            var data = owner.CopyEconomicMutationInputs();
            data.Farm = owner.Farm.DeepClone();
            var inventory = ResourceManager.Instance;
            if (inventory && ReferenceEquals(owner, Blindsided.Oracle.oracle?.saveData))
                foreach (var resource in content.recipes.SelectMany(recipe =>
                    new[] { recipe?.paidInput, recipe?.output, recipe?.secondaryOutput }).Where(resource => resource).Distinct())
                {
                    if (!data.Resources.TryGetValue(resource.name, out var entry) || entry == null)
                        data.Resources[resource.name] = entry = new GameData.ResourceEntry();
                    entry.Amount = inventory.GetAmount(resource, owner);
                    entry.Tier = inventory.GetTier(resource);
                    entry.Earned = inventory.IsUnlocked(resource, owner);
                }
            var projection = new FarmBackgroundProjection
            {
                HorizonSeconds = Math.Min(horizonSeconds, BackgroundRunEstimator.MaxAwaySeconds)
            };
            projection.points.Add((0, 0));
            var started = DateTime.UtcNow;
            double elapsed = 0, gathered = 0;
            var seedFractions = new Dictionary<Resource, double>();
            var paidInputs = content.recipes.Where(recipe => recipe?.paidInput != null)
                .Select(recipe => recipe.paidInput).ToHashSet();

            void Apply(FarmCommandResult proposal)
            {
                if (!FarmTransaction.TryPrepareProgress(data, proposal, out var candidate, out var error))
                    throw new InvalidOperationException(error);
                data.Farm = candidate.Farm;
                data.Resources = candidate.Resources;
                data.ResourceStats = candidate.ResourceStats;
                data.AppliedMigrationIds = candidate.AppliedMigrationIds;
                data.SchemaVersion = candidate.SchemaVersion;
                foreach (var amount in proposal.ResourceDeltas.Values)
                    if (amount > 0) gathered += amount;
            }

            void Record()
            {
                if (projection.points[^1].seconds == elapsed)
                    projection.points[^1] = (elapsed, gathered);
                else projection.points.Add((elapsed, gathered));
            }

            void Advance(double seconds)
            {
                var remaining = Math.Min(Math.Max(0, seconds), projection.HorizonSeconds - elapsed);
                while (remaining > 0)
                {
                    var slice = Math.Min(2, remaining);
                    FarmCommands.AdvanceFields(data, slice, started.AddSeconds(elapsed), content, Apply);
                    elapsed += slice;
                    remaining -= slice;
                    Record();
                }
            }

            void SupplySeeds(int tasks)
            {
                if (seedsPerTask == null || tasks <= 0 || elapsed >= projection.HorizonSeconds) return;
                foreach (var pair in seedsPerTask)
                {
                    if (!pair.Key || !paidInputs.Contains(pair.Key) || !double.IsFinite(pair.Value) || pair.Value <= 0) continue;
                    seedFractions.TryGetValue(pair.Key, out var remainder);
                    var expected = remainder + pair.Value * tasks;
                    var count = Math.Floor(expected);
                    seedFractions[pair.Key] = expected - count;
                    if (count < 1) continue;
                    if (!data.Resources.TryGetValue(pair.Key.name, out var entry) || entry == null)
                        data.Resources[pair.Key.name] = entry = new GameData.ResourceEntry { Tier = 1 };
                    entry.Amount += count;
                    entry.Earned = true;
                }
                FarmCommands.AdvanceFields(data, 0, started.AddSeconds(elapsed), content, Apply);
                Record();
            }

            if (run != null)
                foreach (var step in run.Steps)
                {
                    if (elapsed >= projection.HorizonSeconds) break;
                    Advance(step.Seconds);
                    SupplySeeds(step.Tasks);
                    Advance(step.RestartSeconds);
                }
            Advance(projection.HorizonSeconds - elapsed);
            return projection.Total > 0 ? projection : null;
        }

        public double At(double seconds)
        {
            if (points.Count == 0 || !double.IsFinite(seconds) || seconds <= 0) return 0;
            var low = 0;
            var high = points.Count - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (points[middle].seconds <= seconds) low = middle;
                else high = middle - 1;
            }
            return points[low].resources;
        }
    }
}
