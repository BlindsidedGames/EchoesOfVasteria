using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;

namespace TimelessEchoes.Tests
{
    public class TaskSpeedTests
    {
        private GameObject controllerObj;
        private SkillController controller;
        private Skill skillLow;
        private Skill skillHigh;
        private TaskData data;
        private MiningTask taskLow;
        private MiningTask taskHigh;
        private GameObject objLow;
        private GameObject objHigh;

        [SetUp]
        public void SetUp()
        {
            controllerObj = new GameObject();
            controller = controllerObj.AddComponent<SkillController>();

            skillLow = ScriptableObject.CreateInstance<Skill>();
            skillLow.taskSpeedPerLevel = 0.5f;
            skillHigh = ScriptableObject.CreateInstance<Skill>();
            skillHigh.taskSpeedPerLevel = 0.5f;

            var skillsField = typeof(SkillController).GetField("skills", BindingFlags.NonPublic | BindingFlags.Instance);
            skillsField.SetValue(controller, new List<Skill> { skillLow, skillHigh });

            var progressField = typeof(SkillController).GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (Dictionary<Skill, SkillController.SkillProgress>)progressField.GetValue(controller);
            dict[skillLow] = new SkillController.SkillProgress { Level = 0, CurrentXP = 0f };
            dict[skillHigh] = new SkillController.SkillProgress { Level = 2, CurrentXP = 0f };

            data = ScriptableObject.CreateInstance<TaskData>();
            data.taskDuration = 10f;

            objLow = new GameObject();
            taskLow = objLow.AddComponent<MiningTask>();
            taskLow.associatedSkill = skillLow;
            taskLow.taskData = data;

            objHigh = new GameObject();
            taskHigh = objHigh.AddComponent<MiningTask>();
            taskHigh.associatedSkill = skillHigh;
            taskHigh.taskData = data;

            taskLow.StartTask();
            taskHigh.StartTask();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(objLow);
            Object.DestroyImmediate(objHigh);
            Object.DestroyImmediate(controllerObj);
            Object.DestroyImmediate(skillLow);
            Object.DestroyImmediate(skillHigh);
            Object.DestroyImmediate(data);
        }

        [UnityTest]
        public System.Collections.IEnumerator TickUsesSkillSpeedMultiplier()
        {
            yield return null; // wait a frame to get valid deltaTime
            float dt = Time.deltaTime;

            taskLow.Tick(null);
            taskHigh.Tick(null);

            var timerField = typeof(ContinuousTask).GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
            float lowTimer = (float)timerField.GetValue(taskLow);
            float highTimer = (float)timerField.GetValue(taskHigh);

            Assert.AreEqual(dt * 1f, lowTimer, 0.0001f);
            Assert.AreEqual(dt * 2f, highTimer, 0.0001f);
        }
    }
}
