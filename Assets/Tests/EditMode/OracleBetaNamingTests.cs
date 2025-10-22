#if UNITY_INCLUDE_TESTS
using Blindsided;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class OracleBetaNamingTests
    {
        [TearDown]
        public void TearDown()
        {
            if (Oracle.oracle != null)
            {
                Object.DestroyImmediate(Oracle.oracle.gameObject);
                Oracle.oracle = null;
            }
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void BetaDirectoryNamesIncludeIteration()
        {
            var go = new GameObject("OracleBetaTest");
            var oracle = go.AddComponent<Oracle>();
            oracle.beta = true;
            oracle.betaSaveIteration = 3;

            Assert.AreEqual("Beta3Save1", oracle.GetSlotDirectoryName(0));
            Assert.AreEqual("Beta3Save3", oracle.GetSlotDirectoryName(2));
        }

        [Test]
        public void PlayerPrefsKeysAreIsolatedForBeta()
        {
            var go = new GameObject("OracleBetaPrefsTest");
            var oracle = go.AddComponent<Oracle>();
            oracle.beta = true;
            oracle.betaSaveIteration = 2;

            Assert.AreEqual("Beta2Slot1_Completion", oracle.GetSlotPlayerPrefsKey(1, "Completion"));
            Assert.AreEqual("Beta2Slot0_Deleted", oracle.GetSlotDeletedKey(0));
        }

        [Test]
        public void BetaIterationDefaultsToOneWhenUnset()
        {
            var go = new GameObject("OracleBetaClampTest");
            var oracle = go.AddComponent<Oracle>();
            oracle.beta = true;
            oracle.betaSaveIteration = 0;

            Assert.AreEqual("Beta1Save1", oracle.GetSlotDirectoryName(0));
            Assert.AreEqual("Beta1Slot0_Deleted", oracle.GetSlotDeletedKey(0));
        }
    }
}
#endif
