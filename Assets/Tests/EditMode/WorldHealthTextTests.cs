#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Globalization;
using NUnit.Framework;
using TimelessEchoes.Enemies;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class WorldHealthTextTests
    {
        [Test]
        public void HealthLabelTracksFractionalDamageHealingReinitializationAndRebinding()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var actor = new GameObject("Health label actor");
            var replacement = new GameObject("Replacement health");
            try
            {
                var health = actor.AddComponent<EnemyHealth>();
                var anchor = actor.AddComponent<ToolkitWorldAnchor>();
                anchor.health = health;
                var element = new ToolkitWorldAnchor.Element { value = ToolkitWorldAnchor.Value.EnemyHealth, text = "Ready" };
                health.Init(100);
                Assert.AreEqual("100 / 100", anchor.Text(element));
                health.TakeDamage(.1f);
                Assert.AreEqual("99 / 100", anchor.Text(element));
                health.Heal(.1f);
                Assert.AreEqual("100 / 100", anchor.Text(element));
                health.TakeDamage(99.9f);
                Assert.AreEqual("1 / 100", anchor.Text(element), "Living enemies below one HP still display one.");
                health.Init(0);
                Assert.AreEqual("0 / 0", anchor.Text(element));
                health.Init(250);
                Assert.AreEqual("250 / 250", anchor.Text(element), "Reusing a pooled actor must replace its previous maximum.");
                var other = replacement.AddComponent<EnemyHealth>();
                other.Init(42);
                anchor.health = other;
                Assert.AreEqual("42 / 42", anchor.Text(element));
                anchor.health = null;
                Assert.AreEqual("Ready", anchor.Text(element));
                anchor.health = health;
                element.value = ToolkitWorldAnchor.Value.Static;
                Assert.AreEqual("Ready", anchor.Text(element));
                element.value = ToolkitWorldAnchor.Value.EnemyHealth;
                var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                CultureInfo.CurrentCulture = culture;
                health.Init(-5);
                Assert.AreEqual("0 / -5", anchor.Text(element));
                culture.NumberFormat.NegativeSign = "~";
                Assert.AreEqual("0 / ~5", anchor.Text(element), "An in-place number-format change must remain visible.");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(replacement);
            }
        }
    }
}
#endif
