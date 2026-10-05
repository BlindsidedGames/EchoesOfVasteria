#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator KnownOrchardsShowWholeTreesInTaskStatisticsAndKeepFruitResourceArt()
        {
            Assert.IsNull(GameplayStatTracker.Instance);
            var trackerObject = new GameObject("Isolated orchard statistics owner");
            GameObject screenObject = null;
            try
            {
                var tracker = trackerObject.AddComponent<GameplayStatTracker>();
                tracker.enabled = false;
                foreach (var species in new[] { "Apple", "Pear", "Peach", "Cherry" })
                    tracker.RegisterTaskComplete(Resources.Load<TaskData>("Tasks/Farming/Orchard/" + species + " Harvest"), 3, 8);
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = screenObject.GetComponent<ToolkitStatisticsScreen>();
                Assert.IsTrue(screen.Show());
                screen.SelectTab(4);
                yield return null;
                var ui = screenObject.GetComponent<UIDocument>().rootVisualElement;
                foreach (var species in new[] { "Apple", "Pear", "Peach", "Cherry" })
                {
                    var task = Resources.Load<TaskData>("Tasks/Farming/Orchard/" + species + " Harvest");
                    var tree = (Sprite)new SerializedObject(task.taskPrefab).FindProperty("fruitingSprite").objectReferenceValue;
                    var fruit = Resources.Load<Resource>("Resource Items/" + species);
                    var row = ui.Q("task-stat-" + task.name);
                    Assert.NotNull(row);
                    var icon = row.Q<Image>(className: "eov-stat-entry-art");
                    Assert.AreSame(tree, icon.sprite, species + " statistics must represent the tree task.");
                    Assert.AreNotSame(fruit.icon, icon.sprite);
                    Assert.AreSame(fruit, task.resourceDrops[0].resource, "Drop identity remains harvested fruit.");
                    Assert.AreEqual(ScaleMode.ScaleToFit, icon.scaleMode);
                    Assert.LessOrEqual(icon.resolvedStyle.width, 32f);
                    Assert.LessOrEqual(icon.resolvedStyle.height, 32f);
                }
                var cherry = ui.Q("task-stat-Cherry Harvest");
                cherry.GetFirstAncestorOfType<ScrollView>().ScrollTo(cherry);
                yield return null;
                CaptureFeedback("orchard-task-statistics.png");
                yield return null;
                yield return null;
            }
            finally
            {
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(trackerObject);
            }
        }
    }
}
#endif
