using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Stats;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Tests
{
    public class EnemyKillTrackerTests
    {
        private GameObject obj;
        private EnemyKillTracker tracker;
        private EnemyStats enemyStats;

        [SetUp]
        public void SetUp()
        {
            obj = new GameObject();
            tracker = obj.AddComponent<EnemyKillTracker>();
            enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(enemyStats);
        }

        [Test]
        public void RegisterKillIncreasesCount()
        {
            tracker.RegisterKill(enemyStats);
            tracker.RegisterKill(enemyStats);
            Assert.AreEqual(2, tracker.GetKills(enemyStats));
        }

        [Test]
        public void RevealLevelMatchesThreshold()
        {
            for (int i = 0; i < 10; i++)
                tracker.RegisterKill(enemyStats);
            Assert.AreEqual(1, tracker.GetRevealLevel(enemyStats));
        }

        [Test]
        public void DamageMultiplierScalesWithRevealLevel()
        {
            for (int i = 0; i < 110; i++)
                tracker.RegisterKill(enemyStats);
            // After 110 kills reveal level should be 2 (>=100)
            Assert.AreEqual(2, tracker.GetRevealLevel(enemyStats));
            Assert.AreEqual(1f + 0.25f * 2, tracker.GetDamageMultiplier(enemyStats));
        }
    }
}
