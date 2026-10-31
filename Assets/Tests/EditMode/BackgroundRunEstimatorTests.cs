using System;
using System.Linq;
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
    }
}
