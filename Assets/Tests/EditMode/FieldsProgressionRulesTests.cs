#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class FieldsProgressionRulesTests
    {
        [TestCase(1, 1d)] [TestCase(9, 1d)] [TestCase(10, .9d)]
        [TestCase(29, .9d)] [TestCase(30, .8d)] [TestCase(59, .8d)] [TestCase(60, .7d)] [TestCase(225, .7d)]
        public void SpeedMilestonesReplaceEarlierRank(int level, double expected)
        {
            Assert.AreEqual(expected, FarmContent.SpeedFactor(level), 1e-12);
        }

        [Test]
        public void HarvestXpCarriesSurplusAndConstructionEndpointDoesNotCapTwins()
        {
            var farm = new FarmState();
            FarmContent.AddXp(farm, 10);
            Assert.AreEqual(2, farm.TwinsLevel);
            Assert.AreEqual(5, farm.TwinsXp);
            FarmContent.AddXp(farm, 10);
            Assert.AreEqual(4, farm.TwinsLevel);
            Assert.AreEqual(3, farm.TwinsXp);
            farm.TwinsLevel = 225; farm.TwinsXp = 0;
            FarmContent.AddXp(farm, FarmContent.XpRequired(225));
            Assert.AreEqual(226, farm.TwinsLevel);
            Assert.AreEqual(0, farm.TwinsXp);
            Assert.AreEqual(3.25, FarmContent.YieldMultiplier(226), 1e-12);
        }

        [Test]
        public void WaterPreservesElapsedAndTickStopsAtCurrentBatchWithoutWallClockCatchup()
        {
            var farm = new FarmState { GardenCapacity = 1 };
            var bed = new FarmBedState { Unlocked = true, BatchId = "paid", DurationSeconds = 1800,
                ElapsedSeconds = 1000, LastGrowthUtcTicks = 1 };
            farm.Beds[FarmCommands.WestBedId] = bed;
            FarmCommands.TickFields(farm, 0);
            Assert.AreEqual(1000, bed.ElapsedSeconds);
            bed.Watered = true;
            Assert.IsTrue(bed.IsReady);
            Assert.AreEqual(1000, bed.ElapsedSeconds);
            FarmCommands.TickFields(farm, 3600);
            Assert.AreEqual(1000, bed.ElapsedSeconds);
            bed.Watered = false; bed.ElapsedSeconds = 0;
            FarmCommands.TickFields(farm, 3600);
            Assert.AreEqual(1800, bed.ElapsedSeconds);
            Assert.IsTrue(bed.IsReady);
        }
    }
}
#endif
