namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV11MapRunHistory : ISaveMigration
    {
        public int? TargetSchema => 11;
        public string TargetVersion => null;
        public string Id => "SchemaV11MapRunHistory";

        public void Apply(GameData data)
        {
            data.General ??= new GameData.GeneralStats();
            var stats = data.General;
            stats.RecentRuns ??= new System.Collections.Generic.List<GameData.RunRecord>();
            stats.PreviousRuns ??= new System.Collections.Generic.Dictionary<string, RunHistoryTotals>();
            // The old global window already discarded older records. Do not invent their totals.
            long known = stats.RecentRuns.FindAll(run => run != null).Count;
            foreach (var previous in stats.PreviousRuns.Values) known += previous?.Runs ?? 0;
            stats.EarlierRunDetailsUnavailable |= stats.NextRunNumber - 1L > known;
            MapRunHistory.Compact(stats.RecentRuns, stats.PreviousRuns);
        }
    }
}
