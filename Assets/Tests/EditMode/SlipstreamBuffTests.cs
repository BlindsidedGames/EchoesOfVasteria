#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes.Buffs;
using UnityEngine;

namespace TimelessEchoes.Tests.Buffs
{
    public class SlipstreamBuffTests
    {
        [Test]
        public void DistanceDurationEffectControlsDistancePercent()
        {
            var recipe = ScriptableObject.CreateInstance<BuffRecipe>();
            try
            {
                recipe.durationType = BuffDurationType.DistancePercent;
                recipe.baseDuration = 0f;
                recipe.baseEffects = new List<BuffEffect>
                {
                    new BuffEffect { type = BuffEffectType.DistanceDurationPercent, value = 0.25f }
                };

                var effects = recipe.GetAggregatedEffects();
                Assert.IsNotNull(effects);
                Assert.IsTrue(effects.Exists(e => e.type == BuffEffectType.DistanceDurationPercent));
                var distanceEffect = effects.Find(e => e.type == BuffEffectType.DistanceDurationPercent);
                Assert.AreEqual(0.25f, distanceEffect.value, 1e-4f);
                Assert.AreEqual(0.25f, recipe.GetDuration(), 1e-4f);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void TimeScaleEffectsAdjustGlobalTimeScale()
        {
            var originalScale = Time.timeScale;
            var originalFixedDelta = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            var managerObject = new GameObject("BuffManager_TestHarness");
            var recipe = ScriptableObject.CreateInstance<BuffRecipe>();
            try
            {
                var manager = managerObject.AddComponent<BuffManager>();
                recipe.baseDuration = 5f;
                recipe.durationType = BuffDurationType.Time;
                recipe.baseEffects = new List<BuffEffect>
                {
                    new BuffEffect { type = BuffEffectType.TimeScalePercent, value = 0.5f }
                };

                Assert.IsTrue(manager.PurchaseBuff(recipe));
                Assert.AreEqual(1.5f, Time.timeScale, 1e-4f);
                Assert.AreEqual(0.02f * 1.5f, Time.fixedDeltaTime, 1e-4f);

                manager.ClearActiveBuffs(false);
                Assert.AreEqual(1f, Time.timeScale, 1e-4f);
                Assert.AreEqual(0.02f, Time.fixedDeltaTime, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                ScriptableObject.DestroyImmediate(recipe);
                Time.timeScale = originalScale;
                Time.fixedDeltaTime = originalFixedDelta;
            }
        }
    }
}
#endif
