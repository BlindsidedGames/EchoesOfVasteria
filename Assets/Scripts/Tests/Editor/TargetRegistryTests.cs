using NUnit.Framework;
using UnityEngine;
using System.Linq;

namespace TimelessEchoes.Tests
{
    public class TargetRegistryTests
    {
        private GameObject registryObject;
        private TargetRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registryObject = new GameObject();
            registry = registryObject.AddComponent<TargetRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(registryObject);
        }

        [Test]
        public void RegisterAddsTarget()
        {
            var target = new GameObject("Target").transform;
            registry.Register(target);
            var targets = registry.GetTargets(~0).ToList();
            Assert.Contains(target, targets);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void UnregisterRemovesTarget()
        {
            var target = new GameObject("Target").transform;
            registry.Register(target);
            registry.Unregister(target);
            var targets = registry.GetTargets(~0).ToList();
            Assert.IsFalse(targets.Contains(target));
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void FindClosestReturnsNearestTarget()
        {
            var t1 = new GameObject("A").transform;
            var t2 = new GameObject("B").transform;
            t1.position = Vector3.zero;
            t2.position = new Vector3(5f, 0f, 0f);
            registry.Register(t1);
            registry.Register(t2);

            var closest = registry.FindClosest(new Vector3(1f, 0f, 0f), ~0);
            Assert.AreEqual(t1, closest);

            Object.DestroyImmediate(t1.gameObject);
            Object.DestroyImmediate(t2.gameObject);
        }

        [Test]
        public void GetTargetsFiltersByLayer()
        {
            var a = new GameObject("A");
            var b = new GameObject("B");
            int layerA = 7;
            int layerB = 8;
            a.layer = layerA;
            b.layer = layerB;
            registry.Register(a.transform);
            registry.Register(b.transform);

            var mask = 1 << layerA;
            var targets = registry.GetTargets(mask).ToList();
            Assert.Contains(a.transform, targets);
            Assert.IsFalse(targets.Contains(b.transform));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
