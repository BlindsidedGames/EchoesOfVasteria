#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;

namespace Tests.EditMode
{
    public sealed class FarmBackgroundProjectionTests
    {
        private static readonly DateTime Now = new(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        private FarmContent content;

        [SetUp]
        public void SetUp() => content = FarmContent.Load();

        private GameData PlantedGarden()
        {
            var data = new GameData();
            data.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
            data.Farm.GardenCapacity = 1;
            data.Farm.Projects = FarmProject.MillBody | FarmProject.MillSails | FarmProject.Well;
            data.Farm.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked = true };
            data.Resources["Radish Seed Pack"] = new GameData.ResourceEntry { Amount = 1, Tier = 1, Earned = true };
            data = Commit(data, FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, FarmCommands.RadishRecipeId,
                FarmJournal.NextOperation(data.Farm, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, FarmCommands.RadishRecipeId)), Now, content));
            return Commit(data, FarmCommands.SetRepeat(data.Farm, FarmCommands.WestBedId, true,
                FarmJournal.NextOperation(data.Farm, "repeat:" + FarmCommands.WestBedId + ":1"), Now));
        }

        private static GameData Commit(GameData data, FarmCommandResult proposal)
        {
            Assert.True(proposal.Accepted, proposal.Reason);
            Assert.True(FarmTransaction.TryPrepare(data, proposal, out var candidate, out var error), error);
            return candidate;
        }

        [Test]
        public void ForecastKeepsSourceUntouchedAndShowsFarmGainsAfterAdventureStops()
        {
            var data = PlantedGarden();
            var forecast = new BackgroundRunEstimator.Result { BaseCap = 100, Distance = 22, Seconds = 10 };
            forecast.Steps.Add(new BackgroundRunEstimator.Step { Seconds = 10, Distance = 22, Cap = 100 });
            var projection = FarmBackgroundProjection.Create(data, content, forecast, 7200);
            Assert.NotNull(projection);
            Assert.AreEqual(100, projection.Total, 1e-9);
            Assert.AreEqual(0, projection.At(59));
            Assert.AreEqual(100, projection.At(60), 1e-9);
            Assert.AreEqual(3600, projection.HorizonSeconds);
            Assert.AreEqual(0, data.Farm.TotalHarvests);
            Assert.AreEqual(1, data.Farm.TwinsLevel);
            Assert.AreEqual(0, data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.False(data.Resources.ContainsKey("Radish"));

            var input = new BackgroundRunEstimator.Input
            {
                Run = new RunSaveData { Map = "Farmlands", Distance = 12 },
                DistanceReaping = true
            };
            var timeline = WatchForecastTimeline.Create(input, forecast, 0, 0, projection);
            Assert.AreEqual(3600, timeline.horizonSeconds);
            Assert.AreEqual(22, timeline.frames[^1][1]);
            Assert.AreEqual(100, timeline.frames[^1][3], 1e-9);
            Assert.AreEqual(2, timeline.frames[^1][5]);
            Assert.AreEqual(0, timeline.frames[^1][6]);
            Assert.AreEqual(0, timeline.frames[^1][7]);
        }

        [Test]
        public void ForecastPlantsExpectedSeedsOnlyAfterTheirTaskCompletes()
        {
            var data = PlantedGarden();
            FarmCommands.TickFields(data.Farm, 60);
            data = Commit(data, FarmCommands.HarvestFields(data,
                FarmJournal.NextOperation(data.Farm, "harvest-auto"), Now, content, true));
            var forecast = new BackgroundRunEstimator.Result { Seconds = 60 };
            forecast.Steps.Add(new BackgroundRunEstimator.Step { Seconds = 60, Tasks = 1 });
            var seeds = new Dictionary<Resource, double> { [content.Recipe(FarmCommands.RadishRecipeId).paidInput] = 1 };
            var projection = FarmBackgroundProjection.Create(data, content, forecast, 120, seeds);
            Assert.NotNull(projection);
            Assert.AreEqual(0, projection.At(119));
            Assert.AreEqual(101, projection.At(120), 1e-9);
            Assert.AreEqual(100, data.Resources["Radish"].Amount, 1e-9);
            Assert.AreEqual(0, data.Resources["Radish Seed Pack"].Amount);
            Assert.AreEqual(1, data.Farm.TotalHarvests);
            Assert.False(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.True(data.Farm.Beds[FarmCommands.WestBedId].Repeat);
        }
    }
}
#endif
