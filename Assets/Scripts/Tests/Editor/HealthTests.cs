using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Tests
{
    public class HealthTests
    {
        private GameObject obj;
        private Health health;

        [SetUp]
        public void SetUp()
        {
            obj = new GameObject();
            health = obj.AddComponent<Health>();
            health.Init(10);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void InitSetsHealthValues()
        {
            Assert.AreEqual(10f, health.CurrentHealth);
            Assert.AreEqual(10f, health.MaxHealth);
        }

        [Test]
        public void TakeDamageReducesHealth()
        {
            health.TakeDamage(3f);
            Assert.AreEqual(7f, health.CurrentHealth);
        }
    }
}
