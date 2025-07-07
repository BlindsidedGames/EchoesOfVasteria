using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Enemies;
using Blindsided.Utilities;
using System.Reflection;

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

        [Test]
        public void FillAmountClampedToMinimum()
        {
            var barObj = new GameObject();
            var bar = barObj.AddComponent<SlicedFilledImage>();
            var barField = typeof(Health).GetField("healthBar", BindingFlags.NonPublic | BindingFlags.Instance);
            barField.SetValue(health, bar);
            var minField = typeof(Health).GetField("minFillPercent", BindingFlags.NonPublic | BindingFlags.Instance);
            minField.SetValue(health, 0.2f);

            health.TakeDamage(10f);

            Assert.AreEqual(0.2f, bar.fillAmount);
            Object.DestroyImmediate(barObj);
        }

        [Test]
        public void SpriteChangesWithHealthPercent()
        {
            var barObj = new GameObject();
            var bar = barObj.AddComponent<SlicedFilledImage>();
            var barField = typeof(Health).GetField("healthBar", BindingFlags.NonPublic | BindingFlags.Instance);
            barField.SetValue(health, bar);

            var sprite1 = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            var sprite2 = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.zero);

            var options = new Health.HealthBarSpriteOption[2];
            options[0] = new Health.HealthBarSpriteOption { sprite = sprite1, minPercent = 0.25f };
            options[1] = new Health.HealthBarSpriteOption { sprite = sprite2, minPercent = 0f };
            var optField = typeof(Health).GetField("barSprites", BindingFlags.NonPublic | BindingFlags.Instance);
            optField.SetValue(health, options);

            health.TakeDamage(0f); // update bar
            Assert.AreEqual(sprite1, bar.sprite);

            health.TakeDamage(8f); // drop below 25%
            Assert.AreEqual(sprite2, bar.sprite);
            Object.DestroyImmediate(barObj);
        }
    }
}
