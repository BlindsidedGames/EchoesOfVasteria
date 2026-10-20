#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities.Pooling;
using NUnit.Framework;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    [PrebuildSetup(typeof(IsolatedPlayModeScene))]
    [PostBuildCleanup(typeof(IsolatedPlayModeScene))]
    public sealed class MapLoadingPlayModeTests
    {
        [UnityTest]
        public IEnumerator RestartRetentionIsBoundedReusableAndCanBeFullyReleased()
        {
            var prefabs = new List<GameObject>();
            var instances = new List<GameObject>();
            GameObject live = null;
            try
            {
                PoolManager.TrimInactive();
                var scripted = new GameObject("stateful template");
                scripted.AddComponent<MapLoadingTask>();
                prefabs.Add(scripted);
                var statefulInstance = PoolManager.Get(scripted);
                PoolManager.Release(statefulInstance);
                var activePrefab = new GameObject("still active template");
                prefabs.Add(activePrefab);
                live = PoolManager.Get(activePrefab);
                for (int p = 0; p < 5; p++)
                {
                    var prefab = new GameObject("restart-template-" + p);
                    prefabs.Add(prefab);
                    var first = PoolManager.Get(prefab);
                    var second = PoolManager.Get(prefab);
                    instances.Add(first); instances.Add(second);
                    PoolManager.Release(first); PoolManager.Release(second);
                }
                PoolManager.TrimInactive(2, 4);
                yield return null;
                var survivors = instances.Where(x => x).ToArray();
                Assert.False(statefulInstance, "Stateful gameplay components must not survive a restart in the scenery cache.");
                Assert.True(live && live.activeSelf, "Trimming must never destroy an active object.");
                Assert.AreEqual(4, survivors.Length, "Retain a bounded useful population, not the full high-water mark.");
                int reused = 0;
                foreach (var prefab in prefabs)
                {
                    var item = PoolManager.Get(prefab);
                    if (survivors.Contains(item)) reused++;
                    Assert.True(item.activeSelf);
                    PoolManager.Release(item);
                }
                Assert.AreEqual(2, reused, "The two retained pools must actually reuse their objects.");
                PoolManager.Release(live);
                PoolManager.TrimInactive();
                yield return null;
                Assert.True(survivors.All(x => !x), "Returning to town must still release retained objects.");
            }
            finally
            {
                if (live) PoolManager.Release(live);
                PoolManager.TrimInactive();
                foreach (var prefab in prefabs) Object.DestroyImmediate(prefab);
            }
        }

        [UnityTest]
        public IEnumerator InitialTaskRegistrationSortsOnlyWhenTheMapIsReady()
        {
            var root = new GameObject("initial task batch");
            try
            {
                var controller = root.AddComponent<TaskController>();
                controller.BeginInitialGeneration();
                foreach (var x in new[] { 30f, 10f, 20f })
                {
                    var child = new GameObject("task " + x);
                    child.transform.SetParent(root.transform);
                    child.transform.position = new Vector3(x, 0, 0);
                    controller.AddRuntimeTaskObject(child.AddComponent<MapLoadingTask>());
                }
                yield return null;
                yield return null;
                Assert.AreEqual(30f, controller.tasks[0].Target.position.x, "Do not repeatedly reorder incomplete generation batches.");
                controller.EndInitialGeneration();
                CollectionAssert.AreEqual(new[] { 10f, 20f, 30f }, controller.tasks.Select(t => t.Target.position.x).ToArray());
            }
            finally { Object.DestroyImmediate(root); }
        }
    }

    public sealed class MapLoadingTask : MonoBehaviour, ITask
    {
        public Transform Target => transform;
        public bool BlocksMovement => false;
        public bool IsComplete() => false;
        public void StartTask() { }
        public void OnArrival(HeroBase hero) { }
        public void Tick(HeroBase hero) { }
        public void OnInterrupt(HeroBase hero) { }
    }
}
#endif
