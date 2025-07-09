using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Tests
{
    public class ResourceGainTests
    {
        private GameObject controllerObj;
        private SkillController controller;
        private GameObject managerObj;
        private ResourceManager manager;
        private Skill combat;
        private Skill mining;
        private Resource resource;
        private TaskData data;
        private MiningTask task;
        private GameObject taskObj;

        [SetUp]
        public void SetUp()
        {
            controllerObj = new GameObject();
            controller = controllerObj.AddComponent<SkillController>();

            managerObj = new GameObject();
            manager = managerObj.AddComponent<ResourceManager>();

            combat = ScriptableObject.CreateInstance<Skill>();
            mining = ScriptableObject.CreateInstance<Skill>();

            var skillsField = typeof(SkillController).GetField("skills", BindingFlags.NonPublic | BindingFlags.Instance);
            skillsField.SetValue(controller, new List<Skill> { combat, mining });

            var combatField = typeof(SkillController).GetField("combatSkill", BindingFlags.NonPublic | BindingFlags.Instance);
            combatField.SetValue(controller, combat);

            var progressField = typeof(SkillController).GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (Dictionary<Skill, SkillController.SkillProgress>)progressField.GetValue(controller);
            dict[combat] = new SkillController.SkillProgress { Level = 3, CurrentXP = 0f };
            dict[mining] = new SkillController.SkillProgress { Level = 1, CurrentXP = 0f };

            resource = ScriptableObject.CreateInstance<Resource>();
            data = ScriptableObject.CreateInstance<TaskData>();
            data.resourceDrops = new List<ResourceDrop> { new ResourceDrop { resource = resource, dropRange = new Vector2Int(1,1), dropChance = 1f } };

            taskObj = new GameObject();
            task = taskObj.AddComponent<MiningTask>();
            task.associatedSkill = mining;
            task.taskData = data;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(taskObj);
            Object.DestroyImmediate(controllerObj);
            Object.DestroyImmediate(managerObj);
            Object.DestroyImmediate(combat);
            Object.DestroyImmediate(mining);
            Object.DestroyImmediate(resource);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void ResourceGainIncludesCombatBonus()
        {
            var method = typeof(ResourceGeneratingTask).GetMethod("GenerateDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(task, null);

            double expected = 1 * (1 + 3 * combat.taskSpeedPerLevel);
            Assert.AreEqual(expected, manager.GetAmount(resource), 0.0001);
        }
    }
}
