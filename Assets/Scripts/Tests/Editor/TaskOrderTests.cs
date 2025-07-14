using NUnit.Framework;
using UnityEngine;
using TimelessEchoes.Tasks;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Tests
{
    public class TaskOrderTests
    {
        private GameObject controllerObj;
        private TaskController controller;
        private GameObject heroObj;

        [SetUp]
        public void SetUp()
        {
            controllerObj = new GameObject();
            controller = controllerObj.AddComponent<TaskController>();
            heroObj = new GameObject();
            var hero = heroObj.AddComponent<HeroController>();
            controller.hero = hero;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controllerObj);
            Object.DestroyImmediate(heroObj);
        }

        [Test]
        public void TasksSortedByProximity()
        {
            Vector3 top = new Vector3(0f, 1f, 0f);
            Vector3 bottom = new Vector3(0f, -1f, 0f);

            var t1 = new GameObject("T1");
            t1.transform.position = top + new Vector3(0f, 0f, 0f);
            var b2 = new GameObject("B2");
            b2.transform.position = bottom + new Vector3(1f, 0f, 0f);
            var t3 = new GameObject("T3");
            t3.transform.position = top + new Vector3(2f, 0f, 0f);
            var b4 = new GameObject("B4");
            b4.transform.position = bottom + new Vector3(8f, 0f, 0f);
            var t5 = new GameObject("T5");
            t5.transform.position = top + new Vector3(4f, 0f, 0f);

            controller.AddTaskObject(t1.AddComponent<MiningTask>());
            controller.AddTaskObject(b2.AddComponent<MiningTask>());
            controller.AddTaskObject(t3.AddComponent<MiningTask>());
            controller.AddTaskObject(b4.AddComponent<MiningTask>());
            controller.AddTaskObject(t5.AddComponent<MiningTask>());

            controller.ResetTasks();

            Assert.AreEqual("T1", controller.TaskObjects[0].name);
            Assert.AreEqual("T3", controller.TaskObjects[1].name);
            Assert.AreEqual("T5", controller.TaskObjects[2].name);
            Assert.AreEqual("B2", controller.TaskObjects[3].name);
            Assert.AreEqual("B4", controller.TaskObjects[4].name);

            Object.DestroyImmediate(t1);
            Object.DestroyImmediate(b2);
            Object.DestroyImmediate(t3);
            Object.DestroyImmediate(b4);
            Object.DestroyImmediate(t5);
        }
    }
}
