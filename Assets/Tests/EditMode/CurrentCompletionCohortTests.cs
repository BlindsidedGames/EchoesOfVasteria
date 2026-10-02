#if UNITY_INCLUDE_TESTS
using Blindsided.SaveData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class CurrentCompletionCohortTests
    {
        [Test]
        public void CurrentCohort_CountsAliasOnceAndExcludesPreservedRetiredHistory()
        {
            var data = new GameData();
            foreach (var id in new[] { "Mildred1", "BuffSlot2", "Unknown retired quest" })
                data.Quests[id] = new GameData.QuestRecord { Completed = true };
            data.Quests["Fence2"] = new GameData.QuestRecord { DistanceTravelProgress = 12d };
            data.Resources["Radish"] = new GameData.ResourceEntry { Earned = true };
            data.Resources["Onion"] = new GameData.ResourceEntry { Earned = true, Amount = 79d };
            data.Resources["Unknown legacy resource"] = new GameData.ResourceEntry { Earned = true };

            var percentage = StaticReferences.CalculateCompletionPercentage(data,
                new[] { "BuffSlot2", "Fence2", "New expansion objective", "Current fourth quest" },
                new[] { "Radish", "Corn" });
            Assert.AreEqual(100f / 3f, percentage, 0.0001f, "Two completed members out of six authored entries.");
            Assert.AreEqual(79d, data.Resources["Onion"].Amount, "Completion projection cannot erase old inventory.");
            Assert.IsTrue(data.Quests["Mildred1"].Completed);
            Assert.IsTrue(data.Quests["Unknown retired quest"].Completed);
        }

        [Test]
        public void DuplicateAndInvalidAuthoredIds_DoNotInflateEitherSideOfCohort()
        {
            var data = new GameData();
            data.Quests["BuffSlot2"] = new GameData.QuestRecord { Completed = true };
            data.Resources["Radish"] = new GameData.ResourceEntry { Earned = true };
            Assert.AreEqual(100f, StaticReferences.CalculateCompletionPercentage(data,
                new[] { "BuffSlot2", "BuffSlot2", null, "" }, new[] { "Radish", "Radish", " " }));
        }

        [Test]
        public void EmptyOrMissingCurrentCohort_DoesNotGrantCompletionFromOldRecords()
        {
            var data = new GameData();
            data.Quests["Old complete quest"] = new GameData.QuestRecord { Completed = true };
            Assert.AreEqual(0f, StaticReferences.CalculateCompletionPercentage(data, null, null));
            Assert.AreEqual(0f, StaticReferences.CalculateCompletionPercentage(null, new[] { "Current quest" }, null));
        }
    }
}
#endif
