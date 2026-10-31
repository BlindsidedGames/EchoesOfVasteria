using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Stats;
using TimelessEchoes.Hero;
using TimelessEchoes.Enemies;
using UnityEngine;
using Object = UnityEngine.Object;
using TimelessEchoes.Buffs;
using TimelessEchoes.MapGeneration;

namespace Tests.EditMode
{
    public sealed class BackgroundRunEstimatorTests
    {
        private static BackgroundRunEstimator.Input Input(double seconds) => new()
        {
            Run = new RunSaveData { Map = "River" }, Seconds = seconds, BaseCap = 50,
            Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100, EndDistance = 100, Tasks = 20, Kills = 10 } },
            MaxHealth = 100, RestartSeconds = 0, Combat = (_, _, _, _) => new(0, 0)
        };

        [Test]
        public void ReapingContinuesIntoTheNextRunAndRaisesItsCap()
        {
            var input = Input(20); input.Run.Distance = 40;
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Reaps, Is.EqualTo(1));
            Assert.That(result.BaseCap, Is.EqualTo(50.5).Within(.00001));
            Assert.That(result.Distance, Is.EqualTo(10).Within(.00001));
            Assert.That(result.Tasks, Is.EqualTo(4));
            Assert.That(result.Kills, Is.EqualTo(2));
            Assert.That(input.Run.Distance, Is.EqualTo(40), "Forecast must not publish progression.");
        }

        [Test]
        public void SuccessiveReapsUseTheRaisedCapEachTime()
        {
            var input = Input(30); input.BaseCap = 10;
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Reaps, Is.EqualTo(2));
            Assert.That(result.BaseCap, Is.EqualTo(10.201).Within(.00001));
            Assert.That(result.Distance, Is.EqualTo(9.9).Within(.00001));
        }

        [Test]
        public void BuffedDistanceCapRaisesTheBaseByOnePercentOfTheEffectiveCap()
        {
            var input = Input(1); input.Run.Distance = 99; input.CapModifiers = (_, _) => (2, 0);
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Reaps, Is.EqualTo(1));
            Assert.That(result.BaseCap, Is.EqualTo(51).Within(.00001));
        }

        [Test]
        public void CountdownConsumesAwayTimeWithoutCreatingMoreWork()
        {
            var input = Input(15); input.Run.Distance = 40; input.RestartSeconds = 20;
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Reaps, Is.EqualTo(1));
            Assert.That(result.Distance, Is.EqualTo(50));
            Assert.That(result.MovingSeconds, Is.EqualTo(10));
            Assert.That(result.Seconds, Is.EqualTo(15));
            Assert.That(result.Steps.Last().RestartSeconds, Is.EqualTo(5));
        }

        [Test]
        public void EncounterStopsProgressUntilThePlayerReturns()
        {
            var input = Input(100); input.GateDistance = 15;
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Distance, Is.EqualTo(15));
            Assert.That(result.Seconds, Is.EqualTo(15));
            Assert.That(result.Reaps, Is.Zero);
            Assert.That(result.StopReason, Is.EqualTo("NPC encounter"));
        }

        [Test]
        public void MapWithoutItsOwnEvidenceGetsNoInventedProgress()
        {
            var input = Input(300); input.Samples = Array.Empty<BackgroundRunSample>();
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.Status, Is.EqualTo("insufficientHistory"));
        }

        [Test]
        public void NewestRunsInfluencePaceMoreThanOlderRuns()
        {
            var input = Input(10); input.BaseCap = 1000;
            input.Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100 },
                new BackgroundRunSample { Seconds = 100, Distance = 200 } };
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Distance, Is.EqualTo(50d / 3).Within(.00001));
        }

        [TestCase(0, 10)]
        [TestCase(20, 10)]
        [TestCase(2000, 10)]
        [TestCase(0, 0)]
        [TestCase(0, 5)]
        public void CombatDeathUsesCurrentThreatRatherThanRecordedDamage(double recordedDamage, int recordedKills)
        {
            var input = Input(60); input.DistanceReaping = false; input.MaxHealth = 10;
            input.Combat = (_, _, _, _) => new(2, 0);
            input.EnemyDensity = .1;
            input.Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100, Kills = recordedKills, DamageTaken = recordedDamage, EndDistance = 100 } };
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Deaths, Is.EqualTo(1)); Assert.That(result.Reaps, Is.Zero);
            Assert.That(result.BaseCap, Is.EqualTo(50));
            Assert.That(result.Distance, Is.EqualTo(10).Within(.00001));
            Assert.That(result.HealthFraction, Is.EqualTo(.8).Within(.00001));
        }

        [Test]
        public void ShortHistoryDoesNotCapASurvivableRun()
        {
            var input = Input(3600); input.Run.Map = "Mines";
            input.Run.Distance = 2300; input.BaseCap = 38000;
            input.Samples = new[] { new BackgroundRunSample { Seconds = 240,
                Distance = 2400, EndDistance = 2400, Tasks = 48, Kills = 24 } };

            var result = BackgroundRunEstimator.Forecast(input);

            Assert.That(result.Deaths, Is.Zero, "Passing the recorded reach is not evidence of a combat death.");
            Assert.That(result.Reaps, Is.EqualTo(1));
            Assert.That(result.Steps.First(s => s.EndsRun).Distance, Is.EqualTo(38000).Within(.00001));
            Assert.That(result.BaseCap, Is.EqualTo(38380).Within(.00001));
            Assert.That(result.Distance, Is.EqualTo(300).Within(.00001));
            Assert.That(input.Run.Distance, Is.EqualTo(2300));
        }

        [TestCase(2, 0, 0, 0, 50)]
        [TestCase(4, 0, 0, 0, 25)]
        [TestCase(2, 25, 0, 0, 25)]
        [TestCase(2, 0, 100, 0, 25)]
        [TestCase(2, 0, 0, 1, 18.75)]
        public void EncounterDamageUsesAttackRateDefenseCritsAndSharedEchoHealth(
            float attacksPerSecond, float defense, float critChance, double echoes, double expected)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                enemy.maxHealth = 100; enemy.damage = 10; enemy.attackSpeed = 1;
                var hero = new HeroStatsSnapshot { damage = 10, attacksPerSecond = attacksPerSecond,
                    defense = defense, critChancePercent = critChance, critDamagePercent = 100 };
                Assert.That(TimelessEchoes.Combat.ExpectedEncounter(hero, enemy, 1, echoes).damage,
                    Is.EqualTo(expected).Within(.0001));
            }
            finally { Object.DestroyImmediate(enemy); }
        }

        [TestCase(1, 200)]
        [TestCase(2, 450)]
        public void IncreasingMobLevelsRaisesDamageWithoutAnyRecordedDamage(int level, double expected)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                enemy.maxHealth = 100; enemy.healthPerLevel = 100;
                enemy.damage = 10; enemy.damagePerLevel = 10; enemy.attackSpeed = 1;
                var hero = new HeroStatsSnapshot { damage = 10, attacksPerSecond = 2 };
                Assert.That(TimelessEchoes.Combat.ExpectedEncounter(hero, enemy, level, 0).damage,
                    Is.EqualTo(expected).Within(.0001));
            }
            finally { Object.DestroyImmediate(enemy); }
        }

        [Test]
        public void CurrentStrengthDeterminesDeathOrReapWithTheSameUndamagedHistory()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                enemy.maxHealth = 100; enemy.healthPerLevel = 10;
                enemy.damage = 1; enemy.damagePerLevel = .5f; enemy.attackSpeed = 1; enemy.distancePerLevel = 10;
                var hero = new HeroStatsSnapshot { damage = 10, attacksPerSecond = 2 };
                var input = Input(600); input.BaseCap = 500;
                input.Combat = (distance, _, _, _) => new(TimelessEchoes.Combat.ExpectedEncounter(
                    hero, enemy, enemy.GetLevel((float)distance), 0).damage, 0);
                var weak = BackgroundRunEstimator.Forecast(input);
                Assert.That(weak.Deaths, Is.GreaterThan(0));
                Assert.That(weak.Reaps, Is.Zero);
                Assert.That(weak.Steps.First(s => s.EndsRun).Distance, Is.InRange(1, 499));
                hero.damage = 10000;
                var strong = BackgroundRunEstimator.Forecast(input);
                Assert.That(strong.Deaths, Is.Zero);
                Assert.That(strong.Reaps, Is.EqualTo(1));
                Assert.That(strong.Steps.First(s => s.EndsRun).Distance, Is.EqualTo(500));
            }
            finally { Object.DestroyImmediate(enemy); }
        }

        [Test]
        public void UnbuffedForecastStatsDoNotReplaceOrClearTheLiveStatCache()
        {
            var before = HeroStatSystem.GetSnapshot();
            HeroStatSystem.GetSnapshotWithoutBuffs();
            Assert.That(HeroStatSystem.GetSnapshot().version, Is.EqualTo(before.version));
            HeroStatSystem.MarkDirty();
            HeroStatSystem.GetSnapshotWithoutBuffs();
            Assert.That(HeroStatSystem.IsDirty, Is.True);
            HeroStatSystem.GetSnapshot();
        }

        [Test]
        public void SavedCombatEchoProtectionExpiresAndDoesNotCarryIntoTheNextRun()
        {
            var recipe = Resources.Load<BuffRecipe>("Buffs/Echo Combat");
            Assert.That(recipe && recipe.GetEchoCount() > 0, Is.True);
            var map = ScriptableObject.CreateInstance<MapGenerationConfig>();
            var terrain = ScriptableObject.CreateInstance<TerrainSettings>();
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            var prefab = new GameObject("Forecast enemy");
            try
            {
                terrain.traversal = TerrainSettings.Traversal.Walkable;
                map.tilemapChunkSettings.middleTerrain = terrain;
                enemy.prefab = prefab; enemy.spawnTerrains.Add(terrain);
                enemy.maxHealth = 100; enemy.damage = 10; enemy.attackSpeed = 1;
                map.taskGeneratorSettings.enemies.Add(enemy);
                var run = new RunSaveData { Map = "Fixture", Buffs = new BuffManager.BuffRunState() };
                var normal = new BackgroundRunContent(map, false, 1, 1, run).CombatRates(100, 0, 0, true);
                run.Buffs.Active.Add(new BuffManager.BuffState { Recipe = recipe.name, Remaining = 4 });
                var withEcho = new BackgroundRunContent(map, false, 1, 1, run);
                Assert.That(withEcho.CombatRates(100, 0, 0, true).DamagePerEncounter, Is.LessThan(normal.DamagePerEncounter));
                Assert.That(withEcho.CombatRates(100, 0, 4, true).DamagePerEncounter, Is.EqualTo(normal.DamagePerEncounter));
                Assert.That(withEcho.CombatRates(100, 0, 0, false).DamagePerEncounter, Is.EqualTo(normal.DamagePerEncounter));
                Assert.That(run.Buffs.Active[0].Remaining, Is.EqualTo(4), "Forecasts must not consume the saved ability.");
            }
            finally { Object.DestroyImmediate(prefab); Object.DestroyImmediate(enemy); Object.DestroyImmediate(terrain); Object.DestroyImmediate(map); }
        }

        [TestCase(100, 0, 6)]
        [TestCase(10, 15, 0)]
        public void BombschroomOnlyGrantsKillsWhenDefeatedBeforeTheFuse(float damage, double expected, int kills)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            var prefab = new GameObject("Forecast Bombschroom");
            try
            {
                prefab.AddComponent<BombschroomFuse>(); enemy.prefab = prefab;
                enemy.maxHealth = 100; enemy.damage = 10; enemy.attackSpeed = 0;
                var hero = new HeroStatsSnapshot { damage = damage, attacksPerSecond = 2 };
                var encounter = TimelessEchoes.Combat.ExpectedEncounter(hero, enemy, 1, 0);
                Assert.That(encounter.damage, Is.EqualTo(expected).Within(.0001));
                var input = Input(60); input.BaseCap = 1000;
                input.Combat = (_, _, _, _) => new(encounter.damage, 0, encounter.defeated ? 1 : 0);
                Assert.That(BackgroundRunEstimator.Forecast(input).Kills, Is.EqualTo(kills));
            }
            finally { Object.DestroyImmediate(prefab); Object.DestroyImmediate(enemy); }
        }

        [TestCase(0, .38)]
        [TestCase(.2, .5)]
        [TestCase(.3, .56)]
        public void CurrentRegenerationBalancesExpectedCombatDamage(double regen, double expectedHealth)
        {
            var input = Input(60); input.DistanceReaping = false; input.Run.HealthFraction = .5f;
            input.Combat = (_, _, _, _) => new(2, regen);
            Assert.That(BackgroundRunEstimator.Forecast(input).HealthFraction, Is.EqualTo(expectedHealth).Within(.00001));
        }

        [TestCase(120, 120)]
        [TestCase(28800, 3600)]
        public void BackgroundAllowanceCreditsAtMostOneHour(double away, double credited)
        {
            var input = Input(away); input.BaseCap = 1000000;
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Seconds, Is.EqualTo(credited).Within(.00001));
            Assert.That(result.Distance, Is.EqualTo(credited).Within(.00001));
            Assert.That(result.Kills, Is.EqualTo((int)(credited / 10)));
        }

        [Test]
        public void EstimatedRunsCannotReplaceTheTenRealCalibrationSamples()
        {
            var data = new GameData();
            for (var i = 1; i <= 12; i++) BackgroundRunSample.Remember(data, new GameData.RunRecord
                { MapType = "River", Duration = 100, Distance = i, Reaped = true });
            for (var i = 0; i < 50; i++) BackgroundRunSample.Remember(data, new GameData.RunRecord
                { MapType = "River", Duration = 100, Distance = 10000, Estimated = true });
            Assert.That(data.BackgroundSamples["River"].Count, Is.EqualTo(10));
            Assert.That(data.BackgroundSamples["River"].First().Distance, Is.EqualTo(3));
            Assert.That(data.BackgroundSamples["River"].Last().Distance, Is.EqualTo(12));
        }
        [Test]
        public void WatchTimelinePreservesReapResetPauseAndWindowScopedResources()
        {
            var input = Input(30); input.Run.Distance = 8; input.BaseCap = 10;
            input.StartingDistance = 2; input.RestartSeconds = 5;
            input.Run.Stats.Resources = 777;
            input.Run.Stats.Tasks = 123; input.Run.Stats.Kills = 45;
            input.Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100,
                EndDistance = 100, Tasks = 100, Kills = 50 } };
            var forecast = BackgroundRunEstimator.Forecast(input);
            var timeline = WatchForecastTimeline.Create(input, forecast, 2, 3);
            Assert.That(timeline.frames[0][3], Is.Zero, "Gain scope begins at forecast origin, not current-run total.");
            Assert.That(timeline.frames[0][6], Is.Zero);
            Assert.That(timeline.frames[0][7], Is.Zero);
            var atReap = timeline.frames.Last(f => f[0] == 2);
            Assert.That(atReap, Is.EqualTo(new[] { 2d, 0, 10.1, 7, 1, 1, 2, 1 }));
            var atRestart = timeline.frames.Last(f => f[0] == 7);
            Assert.That(atRestart, Is.EqualTo(new[] { 7d, 2, 10.1, 7, 1, 0, 2, 1 }));
            Assert.That(timeline.frames.Last()[3], Is.EqualTo(forecast.Tasks * 2 + forecast.Kills * 3));
            Assert.That(input.Run.Distance, Is.EqualTo(8));
            Assert.That(input.Run.Stats.Resources, Is.EqualTo(777));
            Assert.That(input.Run.Stats.Tasks, Is.EqualTo(123));
            Assert.That(input.Run.Stats.Kills, Is.EqualTo(45));
            var replaced = WatchForecastTimeline.Create(input, forecast, 2, 3);
            Assert.That(replaced.frames[0][3], Is.Zero, "A replacement forecast must not add the old projection.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WatchTimelineRecordsEffectiveCapAndStopsWithoutInventingRestart(bool firstReap)
        {
            var input = Input(1); input.Run.Distance = 99;
            input.CapModifiers = (_, _) => (2, 0); input.StopOnFirstReap = firstReap;
            input.RestartSeconds = 5;
            var forecast = BackgroundRunEstimator.Forecast(input);
            var timeline = WatchForecastTimeline.Create(input, forecast, 2, 3);
            Assert.That(timeline.frames[0][2], Is.EqualTo(100));
            Assert.That(timeline.horizonSeconds, Is.EqualTo(1));
            var end = timeline.frames.Last();
            Assert.That(end[1], Is.Zero); Assert.That(end[2], Is.EqualTo(51));
            Assert.That(end[4], Is.EqualTo(1));
            Assert.That(end[5], Is.EqualTo(firstReap ? 2 : 1));
        }

        [Test]
        public void WatchTimelineBoundsCapChangesAndSerializesHollowWithoutInfinity()
        {
            var input = Input(900); input.BaseCap = 10000;
            input.CapModifiers = (seconds, _) => (1, seconds);
            var forecast = BackgroundRunEstimator.Forecast(input);
            var bounded = WatchForecastTimeline.Create(input, forecast, 2, 3);
            Assert.That(bounded.frames.Count, Is.LessThanOrEqualTo(WatchForecastTimeline.MaxFrames));
            Assert.IsTrue(bounded.truncated);
            Assert.That(bounded.horizonSeconds, Is.LessThan(forecast.Seconds));
            Assert.That(Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(bounded)), Is.LessThan(24000));
            input.DistanceReaping = false;
            var hollow = WatchForecastTimeline.Create(input, BackgroundRunEstimator.Forecast(input), 2, 3);
            Assert.IsFalse(hollow.distanceReaping);
            Assert.That(hollow.frames.All(f => f[2] == 0 && f.All(double.IsFinite)), Is.True);
            Assert.That(hollow.frames.Last()[4], Is.Zero);
        }

    }
}
