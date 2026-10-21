#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Tests
{
    public class TaskCategoryWeightTests
    {
        private readonly List<TaskData> tasks = new();

        private TaskData Task(float weight = 1)
        {
            var task = ScriptableObject.CreateInstance<TaskData>();
            task.taskID = -1000000 - tasks.Count;
            task.weight = weight;
            tasks.Add(task);
            return task;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var task in tasks)
                Object.DestroyImmediate(task);
            tasks.Clear();
        }

        [Test]
        public void ChestBoostRaisesLootingShareModestlyRegardlessOfRockVariety()
        {
            var chest = Task();
            var key = "TaskWeightToggle_" + chest.taskID;
            var hadKey = PlayerPrefs.HasKey(key);
            var previous = PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.SetInt(key, 0);
                var looting = new ProceduralTaskGenerator.WeightedTaskCategory { tasks = new() { chest } };
                var mining = new ProceduralTaskGenerator.WeightedTaskCategory { tasks = new() { Task(), Task(), Task(), Task() } };
                var baseline = looting.GetWeight(100) / (looting.GetWeight(100) + mining.GetWeight(100));
                Assert.That(baseline, Is.EqualTo(.5f).Within(.0001f), "Task variety alone must not bias categories.");
                PlayerPrefs.SetInt(key, 1);
                var boosted = looting.GetWeight(100) / (looting.GetWeight(100) + mining.GetWeight(100));
                Assert.That(boosted, Is.GreaterThan(baseline).And.LessThan(.6f), "Chest selection should nudge looting, not double the category.");

                chest.requiredSkillLevel = 999; // No associated skill: cannot spawn.
                Assert.That(looting.GetWeight(100), Is.EqualTo(1), "An unavailable chest cannot boost looting.");
                chest.requiredSkillLevel = 0;
                chest.enforceMinDistance = true;
                chest.minX = 200;
                Assert.That(looting.GetWeight(100), Is.EqualTo(1), "Future-distance tasks cannot boost the current stage.");
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previous);
                else PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
#endif
