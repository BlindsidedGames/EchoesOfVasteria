using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TimelessEchoes.Buffs;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;

namespace TimelessEchoes.Tests
{
    public class BuffTaskSpeedTests
    {
        private GameObject buffObj;
        private BuffManager manager;
        private BuffRecipe recipe;

        private GameObject controllerObj;
        private SkillController controller;
        private Skill skill;
        private TaskData data;
        private MiningTask task;
        private GameObject taskObj;

        [SetUp]
        public void SetUp()
        {
            buffObj = new GameObject();
            manager = buffObj.AddComponent<BuffManager>();
            recipe = ScriptableObject.CreateInstance<BuffRecipe>();
            recipe.taskSpeedPercent = 100f;

            var listField = typeof(BuffManager).GetField("activeBuffs", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (List<BuffManager.ActiveBuff>)listField.GetValue(manager);
            list.Add(new BuffManager.ActiveBuff { recipe = recipe, remaining = 5f });

            controllerObj = new GameObject();
            controller = controllerObj.AddComponent<SkillController>();
            skill = ScriptableObject.CreateInstance<Skill>();
            skill.taskSpeedPerLevel = 0f;

            var skillsField = typeof(SkillController).GetField("skills", BindingFlags.NonPublic | BindingFlags.Instance);
            skillsField.SetValue(controller, new List<Skill> { skill });

            var progressField = typeof(SkillController).GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (Dictionary<Skill, SkillController.SkillProgress>)progressField.GetValue(controller);
            dict[skill] = new SkillController.SkillProgress { Level = 0, CurrentXP = 0f };

            data = ScriptableObject.CreateInstance<TaskData>();
            data.taskDuration = 10f;

            taskObj = new GameObject();
            task = taskObj.AddComponent<MiningTask>();
            task.associatedSkill = skill;
            task.taskData = data;

            task.StartTask();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(taskObj);
            Object.DestroyImmediate(controllerObj);
            Object.DestroyImmediate(buffObj);
            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(recipe);
        }

        [UnityTest]
        public System.Collections.IEnumerator TickUsesBuffSpeedMultiplier()
        {
            yield return null;
            float dt = Time.deltaTime;

            task.Tick(null);

            var timerField = typeof(ContinuousTask).GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
            float timer = (float)timerField.GetValue(task);

            Assert.AreEqual(dt * 2f, timer, 0.0001f);
        }
    }
}

