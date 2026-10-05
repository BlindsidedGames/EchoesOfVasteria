#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Skills;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class FieldsHeroGateTests
    {
        private GameObject fixture;
        private Oracle previousOracle;
        private SkillController previousSkills;
        private Oracle ownerOracle;
        private SkillController controller;
        private FarmContent content;
        private FarmRecipe corn;
        private Skill farming;
        private static readonly FieldInfo InstanceField = typeof(SkillController).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo LoadState = typeof(SkillController).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameData Bank(int level)
        {
            var bank = new GameData();
            bank.SkillData[farming.name] = new GameData.SkillProgress { Level = level };
            bank.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
            return bank;
        }

        [SetUp]
        public void SetUp()
        {
            previousOracle = Oracle.oracle;
            previousSkills = SkillController.Instance;
            content = FarmContent.Load();
            Assert.IsNotNull(content);
            corn = content.Recipe("corn");
            if (corn == null) corn = content.recipes.Find(recipe => recipe.source != null && recipe.source.name == "Corn");
            Assert.IsNotNull(corn, "Use the actual authored Corn source contract.");
            farming = corn.source.associatedSkill;
            Assert.AreEqual(7, corn.source.requiredSkillLevel);
            fixture = new GameObject("Fields hero gate isolated fixture");
            fixture.SetActive(false); // Neither Oracle nor SkillController may bootstrap persistence.
            ownerOracle = fixture.AddComponent<Oracle>();
            controller = fixture.AddComponent<SkillController>();
            Oracle.oracle = ownerOracle;
            InstanceField.SetValue(null, controller);
            typeof(SkillController).GetField("skills", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, new List<Skill> { farming });
            ownerOracle.saveData = Bank(1);
            LoadState.Invoke(controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
            Oracle.oracle = previousOracle;
            InstanceField.SetValue(null, previousSkills);
        }

        [Test]
        public void LiveLevelUnlocksCornBeforeSavedSnapshotCatchesUpAndDetachedBankUsesSavedGate()
        {
            var loaded = ownerOracle.saveData;
            Assert.IsFalse(content.CanPlant(loaded, corn));
            controller.GetProgress(farming).Level = 7;
            Assert.AreEqual(1, loaded.SkillData[farming.name].Level);
            Assert.AreEqual(7, FarmContent.SkillLevel(loaded, farming.name));
            Assert.IsTrue(content.CanPlant(loaded, corn));
            var detached = Bank(3);
            Assert.AreEqual(3, FarmContent.SkillLevel(detached, farming.name));
            Assert.IsFalse(content.CanPlant(detached, corn));
        }

        [Test]
        public void SwitchingOracleBankCannotBorrowPreviousBanksLiveLevelBeforeStateReload()
        {
            var oldBank = ownerOracle.saveData;
            controller.GetProgress(farming).Level = 7;
            Assert.IsTrue(content.CanPlant(oldBank, corn));
            var nextBank = Bank(1);
            ownerOracle.saveData = nextBank;
            Assert.AreEqual(1, FarmContent.SkillLevel(nextBank, farming.name));
            Assert.IsFalse(content.CanPlant(nextBank, corn));
            Assert.AreEqual(1, FarmContent.SkillLevel(oldBank, farming.name));
            LoadState.Invoke(controller, null);
            Assert.AreEqual(1, controller.GetProgress(farming).Level);
            Assert.IsFalse(content.CanPlant(nextBank, corn));
            nextBank.SkillData[farming.name].Level = 7;
            LoadState.Invoke(controller, null);
            Assert.IsTrue(content.CanPlant(nextBank, corn));
            Assert.IsFalse(content.CanPlant(oldBank, corn));
        }
    }
}
#endif
