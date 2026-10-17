#if UNITY_INCLUDE_TESTS
using System;
using Blindsided;
using NUnit.Framework;
using TimelessEchoes;
using UnityEngine;

namespace Tests.EditMode
{
    [UnityEngine.TestTools.PrebuildSetup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    [UnityEngine.TestTools.PostBuildCleanup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    public class ConsoleUsageTests
    {
        [Test]
        public void ConsoleAuthMarksSaveDataWhenCommandsUsed()
        {
            var oracleGo = new GameObject("OracleTest");
            try
            {
                var oracle = oracleGo.AddComponent<Oracle>();
                oracle.saveData = new Blindsided.SaveData.GameData
                {
                    General = new Blindsided.SaveData.GameData.GeneralStats()
                };

                Assert.DoesNotThrow(() => ConsoleAuth.Login("Matt"));
                Assert.IsTrue(oracle.saveData.General.ConsoleUsed, "Console usage should be recorded in save provenance.");
            }
            finally
            {
                ConsoleAuth.Logout();
                UnityEngine.Object.DestroyImmediate(oracleGo);
            }
        }

    }
}
#endif
