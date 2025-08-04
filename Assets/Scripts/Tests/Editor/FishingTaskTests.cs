using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Tasks;

namespace TimelessEchoes.Tests
{
    public class FishingTaskTests
    {
        private GameObject obj;
        private FishingTask task;

        [SetUp]
        public void SetUp()
        {
            obj = new GameObject();
            task = obj.AddComponent<FishingTask>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void TargetReturnsAssignedPoint()
        {
            var point = new GameObject().transform;
            typeof(FishingTask).GetField("fishingPoint", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(task, point);

            Assert.AreEqual(point, task.Target);
            Object.DestroyImmediate(point.gameObject);
        }

        [Test]
        public void TargetFallsBackToSelfWhenPointNull()
        {
            typeof(FishingTask).GetField("fishingPoint", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(task, null);

            Assert.AreEqual(task.transform, task.Target);
        }

        [Test]
        public void AnimationAndTriggerNamesAreCorrect()
        {
            var animationName =
                (string)typeof(FishingTask).GetProperty("AnimationName", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(task);
            var interruptName =
                (string)typeof(FishingTask).GetProperty("InterruptTriggerName", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(task);
            var completionName =
                (string)typeof(FishingTask).GetProperty("CompletionTriggerName", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(task);

            Assert.AreEqual("Fishing", animationName);
            Assert.AreEqual("StopFishing", interruptName);
            Assert.AreEqual("CatchFish", completionName);
        }

        [Test]
        public void BlocksMovementIsTrue()
        {
            Assert.IsTrue(task.BlocksMovement);
        }
    }
}
