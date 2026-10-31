#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using Sirenix.Serialization;

namespace Tests.EditMode
{
    public sealed class MapRunHistoryTests
    {
        private static GameData.RunRecord Run(int number, string map) => new()
        {
            RunNumber = number, MapType = map, Duration = 12.5f, Distance = number,
            ResourcesCollected = 15.25, BonusResourcesCollected = 1.5,
            TasksCompleted = 3, EnemiesKilled = 2, DamageDealtDouble = 101.5, DamageTakenDouble = 7.25,
            Died = number % 3 != 0, Reaped = number % 3 == 1, Abandoned = number % 3 == 0
        };

        [Test]
        public void FiftyWoodsRunsDoNotEvictTheTenRiverRuns()
        {
            var recent = Enumerable.Range(1, 10).Select(n => Run(n, "River")).ToList();
            var previous = new Dictionary<string, RunHistoryTotals>();
            foreach (var number in Enumerable.Range(11, 50))
            {
                recent.Add(Run(number, "Woods"));
                MapRunHistory.Compact(recent, previous);
            }
            Assert.AreEqual(10, recent.Count(r => r.MapType == "River"));
            CollectionAssert.AreEqual(Enumerable.Range(51, 10), recent.Where(r => r.MapType == "Woods").Select(r => r.RunNumber));
            Assert.AreEqual(40, previous["Woods"].Runs);
            Assert.False(previous.ContainsKey("River"));
            Assert.AreEqual(Enumerable.Range(11, 40).Sum(), previous["Woods"].Distance);
        }

        [Test]
        public void InterleavedMapsKeepAllTotalsAcrossRolloverAndSerialization()
        {
            var data = new GameData();
            var all = Enumerable.Range(1, 600).Select(n => Run(n, n % 2 == 0 ? "River" : "Unavailable future map")).ToList();
            foreach (var run in all)
            {
                data.General.RecentRuns.Add(run);
                MapRunHistory.Compact(data.General.RecentRuns, data.General.PreviousRuns);
            }
            data = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(data), DataFormat.Binary);
            MapRunHistory.Compact(data.General.RecentRuns, data.General.PreviousRuns);
            var sum = MapRunHistory.Previous(data.General.PreviousRuns);
            foreach (var run in data.General.RecentRuns) sum.Add(run);
            Assert.AreEqual(20, data.General.RecentRuns.Count);
            Assert.AreEqual(600, sum.Runs);
            Assert.AreEqual(all.Sum(r => r.Duration), sum.Duration);
            Assert.AreEqual(all.Sum(r => (double)r.Distance), sum.Distance);
            Assert.AreEqual(all.Sum(r => r.ResourcesCollected), sum.Resources);
            Assert.AreEqual(all.Sum(r => r.BonusResourcesCollected), sum.BonusResources);
            Assert.AreEqual(all.Sum(r => r.DamageDealtAsDouble), sum.DamageDealt);
            Assert.AreEqual(all.Sum(r => r.DamageTakenAsDouble), sum.DamageTaken);
            Assert.AreEqual(all.Sum(r => r.TasksCompleted), sum.Tasks);
            Assert.AreEqual(all.Sum(r => r.EnemiesKilled), sum.Kills);
            Assert.AreEqual(200, sum.Deaths); Assert.AreEqual(200, sum.Reaps); Assert.AreEqual(200, sum.Abandoned);
            Assert.AreEqual(300, MapRunHistory.Previous(data.General.PreviousRuns, "River").Runs + 10);
        }

        [Test]
        public void MigrationDoesNotInventLostRunsOrArchiveTwiceOnReload()
        {
            var source = new GameData { SchemaVersion = 10, LastGameVersion = "9999.0.0" };
            source.General.NextRunNumber = 1001;
            source.General.TotalKills = 9876;
            source.MapStats["River"] = new GameData.MapStatistics { Kills = 9876 };
            source.General.RecentRuns = Enumerable.Range(951, 50).Select(n => Run(n, "River")).ToList();
            var first = SaveMigrationRunner.TryMigrate(source, "9999.0.0");
            Assert.True(first.Succeeded, first.Error);
            Assert.True(first.Data.General.EarlierRunDetailsUnavailable);
            Assert.AreEqual(40, first.Data.General.PreviousRuns["River"].Runs);
            Assert.AreEqual(50, source.General.RecentRuns.Count);
            Assert.IsEmpty(source.General.PreviousRuns);
            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(first.Data), DataFormat.Binary);
            var second = SaveMigrationRunner.TryMigrate(loaded, "9999.0.0");
            Assert.True(second.Succeeded, second.Error);
            Assert.AreEqual(40, second.Data.General.PreviousRuns["River"].Runs);
            Assert.AreEqual(9876, second.Data.General.TotalKills);
            Assert.AreEqual(9876, second.Data.MapStats["River"].Kills);
        }

        [Test]
        public void LegacyDamageAndUnknownMapRemainInArchive()
        {
            var recent = Enumerable.Range(1, 11).Select(n => Run(n, null)).ToList();
            recent.Insert(1, null);
#pragma warning disable 618
            recent[0].DamageDealtDouble = 0; recent[0].DamageDealt = 123.5f;
#pragma warning restore 618
            var previous = new Dictionary<string, RunHistoryTotals>();
            MapRunHistory.Compact(recent, previous);
            Assert.AreEqual(10, recent.Count);
            Assert.AreEqual(123.5, previous[""].DamageDealt);
            MapRunHistory.Compact(recent, previous);
            Assert.AreEqual(1, previous[""].Runs);
        }
    }
}
#endif
