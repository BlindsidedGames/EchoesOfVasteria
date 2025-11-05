#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TimelessEchoes.Buffs;
using TimelessEchoes.Skills;
using UnityEngine;

namespace TimelessEchoes.Tests.Buffs
{
    public class ExperienceBuffTests
    {
        [Test]
        public void ExperienceBuffAppliesToSkillExperience()
        {
            var managerObject = new GameObject("BuffManager_TestHarness");
            var controllerObject = new GameObject("SkillController_TestHarness");
            var recipe = ScriptableObject.CreateInstance<BuffRecipe>();
            var skill = ScriptableObject.CreateInstance<Skill>();

            try
            {
                var manager = managerObject.AddComponent<BuffManager>();
                recipe.durationType = BuffDurationType.Time;
                recipe.baseDuration = 5f;
                recipe.baseEffects = new List<BuffEffect>
                {
                    new BuffEffect { type = BuffEffectType.ExperienceBonusFraction, value = 0.5f }
                };

                Assert.IsTrue(manager.PurchaseBuff(recipe), "Buff should purchase successfully.");
                Assert.AreEqual(1.5f, manager.ExperienceGainMultiplier, 1e-4f, "Buff multiplier should be 1.5x.");

                var controller = controllerObject.AddComponent<SkillController>();

                skill.skillName = "Test Skill";

                var skillsField = typeof(SkillController).GetField("skills", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(skillsField, "Failed to access SkillController.skills field.");
                skillsField.SetValue(controller, new List<Skill> { skill });

                var appliedXp = controller.AddExperience(skill, 10f);
                Assert.AreEqual(15f, appliedXp, 1e-4f, "Experience gain should include buff multiplier.");

                var multiplier = controller.GetExperienceBonusMultiplier(skill);
                Assert.AreEqual(1.5f, multiplier, 1e-4f, "Experience multiplier query should reflect buff.");
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(controllerObject);
                ScriptableObject.DestroyImmediate(recipe);
                ScriptableObject.DestroyImmediate(skill);
            }
        }

        [Test]
        public void QuestExperienceBypassesMultipliers()
        {
            var managerObject = new GameObject("BuffManager_QuestXP");
            var controllerObject = new GameObject("SkillController_QuestXP");
            var recipe = ScriptableObject.CreateInstance<BuffRecipe>();
            var skill = ScriptableObject.CreateInstance<Skill>();

            try
            {
                var manager = managerObject.AddComponent<BuffManager>();
                recipe.durationType = BuffDurationType.Time;
                recipe.baseDuration = 5f;
                recipe.baseEffects = new List<BuffEffect>
                {
                    new BuffEffect { type = BuffEffectType.ExperienceBonusFraction, value = 1f }
                };

                Assert.IsTrue(manager.PurchaseBuff(recipe), "Buff should activate successfully.");
                Assert.Greater(manager.ExperienceGainMultiplier, 1f, "Buff multiplier should exceed 1.");

                var controller = controllerObject.AddComponent<SkillController>();

                skill.skillName = "Quest Skill";

                var skillsField = typeof(SkillController).GetField("skills", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(skillsField, "Failed to access SkillController.skills field.");
                skillsField.SetValue(controller, new List<Skill> { skill });

                var appliedXp = controller.GrantQuestExperience(skill, 20f);
                Assert.AreEqual(20f, appliedXp, 1e-4f, "Quest experience should bypass buffs and apply the raw value.");

                var progress = controller.GetProgress(skill);
                Assert.IsNotNull(progress, "GrantQuestExperience should create progress for the skill.");
                Assert.AreEqual(20f, progress.CurrentXP, 1e-4f, "Stored experience should match the raw quest reward.");
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(controllerObject);
                ScriptableObject.DestroyImmediate(recipe);
                ScriptableObject.DestroyImmediate(skill);
            }
        }
    }
}
#endif
