#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tests
{
    public class ResourceDropTests
    {
        private Resource resource;

        [SetUp]
        public void SetUp() => resource = ScriptableObject.CreateInstance<Resource>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(resource);

        [Test]
        public void LegacySerializedDistanceLimitsDoNotRestrictRewards()
        {
            var drop = JsonUtility.FromJson<ResourceDrop>(
                "{\"minX\":2500,\"maxX\":3000,\"weight\":1,\"dropRange\":{\"x\":2,\"y\":2}}");
            drop.resource = resource;
            var result = DropResolver.RollDrops(new[] { drop }, null, rand: () => 0f);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].count, Is.EqualTo(2));
        }

        [Test]
        public void SkillRequirementStillBlocksRewardWithoutMatchingSkill()
        {
            var drop = new ResourceDrop { resource = resource, requiredSkillLevel = 10 };
            Assert.That(DropResolver.RollDrops(new[] { drop }, null), Is.Empty);
            Assert.That(DropResolver.RollDrops(new[] { drop }, null, ignoreSkillLevel: true).Count,
                Is.EqualTo(1));
        }

        [TestCase(0f, 1)]
        [TestCase(0.99f, 2)]
        public void RelativeWeightsStillSelectTheExpectedReward(float roll, int expectedCount)
        {
            var drops = new[]
            {
                new ResourceDrop { resource = resource, weight = 3, dropRange = new Vector2Int(1, 1) },
                new ResourceDrop { resource = resource, weight = 1, dropRange = new Vector2Int(2, 2) },
                new ResourceDrop { resource = resource, weight = 0, dropRange = new Vector2Int(99, 99) }
            };
            var result = DropResolver.RollDrops(drops, null, rand: () => roll);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].count, Is.EqualTo(expectedCount));
        }

        [Test]
        public void ExtraSlotsStillDrawWithoutReplacement()
        {
            var drops = new[]
            {
                new ResourceDrop { resource = resource, dropRange = new Vector2Int(1, 1) },
                new ResourceDrop { resource = resource, dropRange = new Vector2Int(2, 2) }
            };
            var result = DropResolver.RollDrops(drops, new List<float> { 1f, 1f }, rand: () => 0f);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].count + result[1].count, Is.EqualTo(3));
        }

        [Test]
        public void ExistingWheatAssetPaysItsRewardWithoutADistanceInput()
        {
            var wheat = UnityEditor.AssetDatabase.LoadAssetAtPath<TaskData>(
                "Assets/Resources/Tasks/Farming/Wheat.asset");
            Assert.That(wheat, Is.Not.Null);
            Assert.That(wheat.GetEffectiveMinX(), Is.EqualTo(10f));
            var result = DropResolver.RollDrops(wheat.resourceDrops, wheat.additionalLootChances,
                wheat.associatedSkill, ignoreSkillLevel: true, rand: () => 0f);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].resource, Is.EqualTo(wheat.resourceDrops[0].resource));
            Assert.That(result[0].count, Is.GreaterThan(0));
        }
    }
}
#endif
