#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes;
namespace Tests.EditMode
{
    public sealed class AchievementMilestoneTests
    {
        [TestCase("Distance", "Reached100", 100)]
        [TestCase("Distance", "Reached1000", 500)]
        [TestCase("Distance", "Reached10000", 1000)]
        [TestCase("Kilometers", "Kilometers1", 1)]
        [TestCase("Kilometers", "Kilometers100", 100)]
        [TestCase("Kilometers", "Kilometers1000", 1000)]
        [TestCase("Tasks", "Tasks100", 100)]
        [TestCase("Tasks", "Tasks5k", 1000)]
        [TestCase("Tasks", "Tasks25k", 25000)]
        [TestCase("Buffs", "Buffs1", 10)]
        [TestCase("Buffs", "Buffs2", 100)]
        [TestCase("Buffs", "Buffs3", 1000)]
        [TestCase("Buffs", "Buffs4", 2500)]
        [TestCase("Buffs", "Buffs5", 9001)]
        [TestCase("Kills", "Enemies1", 50)]
        [TestCase("Kills", "Enemies2", 500)]
        [TestCase("Kills", "Enemies3", 2500)]
        [TestCase("Kills", "Enemies4", 5000)]
        [TestCase("Kills", "Enemies5", 10000)]
        [TestCase("Deaths", "Die10", 10)]
        [TestCase("Deaths", "Die250", 250)]
        [TestCase("Deaths", "Die1000", 1000)]
        [TestCase("Eva", "EvaLevel10", 10)]
        [TestCase("Eva", "EvaLevel50", 50)]
        [TestCase("Eva", "EvaLevel100", 100)]
        [TestCase("Eva", "EvaLevel200", 200)]
        [TestCase("Eva", "EvaLevel500", 500)]
        [TestCase("Ivan", "IvanLevel10", 10)]
        [TestCase("Ivan", "IvanLevel50", 50)]
        [TestCase("Ivan", "IvanLevel100", 100)]
        [TestCase("Ivan", "IvanLevel500", 500)]
        [TestCase("Ivan", "IvanLevel1000", 1000)]
        public void PublishedMilestoneUnlocksAtItsThreshold(string metric, string id, int threshold)
        {
            foreach (var offset in new[] { -1, 0, 1 })
            {
                var value = threshold + offset;
                var progress = new AchievementProgress(
                    highestDistance: metric == "Distance" ? value : 0,
                    kilometers: metric == "Kilometers" ? value : 0,
                    tasks: metric == "Tasks" ? value : 0, buffs: metric == "Buffs" ? value : 0,
                    deaths: metric == "Deaths" ? value : 0, kills: metric == "Kills" ? value : 0,
                    evaLevel: metric == "Eva" ? value : 0, ivanLevel: metric == "Ivan" ? value : 0);
                var earned = new HashSet<string>();
                AchievementMilestones.ReportEarned(progress, id => earned.Add(id));
                Assert.AreEqual(offset >= 0, earned.Contains(id), $"{id} at {value}");
            }
        }
    }
}
#endif
