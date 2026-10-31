using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Stats;

namespace Tests.EditMode
{
    public sealed class BackgroundRunEstimatorTests
    {
        private static BackgroundRunEstimator.Input Input(double seconds) => new()
        {
            Run = new RunSaveData { Map = "River" }, Seconds = seconds, BaseCap = 50,
            Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100, EndDistance = 100, Tasks = 20, Kills = 10 } },
            MaxHealth = 100, RestartSeconds = 0, Pressure = (_, _) => 0
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

        [Test]
        public void TrueCombatDeathRestartsWithoutGrantingAReapIncrease()
        {
            var input = Input(60); input.DistanceReaping = false; input.MaxHealth = 10;
            input.Pressure = (_, _) => 1;
            input.Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100, DamageTaken = 20, EndDistance = 100 } };
            var result = BackgroundRunEstimator.Forecast(input);
            Assert.That(result.Deaths, Is.EqualTo(1)); Assert.That(result.Reaps, Is.Zero);
            Assert.That(result.BaseCap, Is.EqualTo(50));
            Assert.That(result.Distance, Is.EqualTo(10).Within(.00001));
            Assert.That(result.HealthFraction, Is.EqualTo(.8).Within(.00001));
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
            input.Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100,
                EndDistance = 100, Tasks = 100, Kills = 50 } };
            var forecast = BackgroundRunEstimator.Forecast(input);
            var timeline = WatchForecastTimeline.Create(input, forecast, 2, 3);
            Assert.That(timeline.frames[0][3], Is.Zero, "Gain scope begins at forecast origin, not current-run total.");
            var atReap = timeline.frames.Last(f => f[0] == 2);
            Assert.That(atReap, Is.EqualTo(new[] { 2d, 0, 10.1, 7, 1, 1 }));
            var atRestart = timeline.frames.Last(f => f[0] == 7);
            Assert.That(atRestart, Is.EqualTo(new[] { 7d, 2, 10.1, 7, 1, 0 }));
            Assert.That(timeline.frames.Last()[3], Is.EqualTo(forecast.Tasks * 2 + forecast.Kills * 3));
            Assert.That(input.Run.Distance, Is.EqualTo(8));
            Assert.That(input.Run.Stats.Resources, Is.EqualTo(777));
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
