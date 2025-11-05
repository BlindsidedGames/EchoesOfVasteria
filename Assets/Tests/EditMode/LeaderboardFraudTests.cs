#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using Blindsided;
using NUnit.Framework;
using TimelessEchoes;
using UnityEngine;

namespace Tests.EditMode
{
    public class LeaderboardFraudTests
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

                Assert.DoesNotThrow(() => ConsoleAuth.Login("MattsTheBest"));
                Assert.IsTrue(oracle.saveData.General.ConsoleUsed, "Console usage should mark the save as disqualified.");
            }
            finally
            {
                ConsoleAuth.Logout();
                UnityEngine.Object.DestroyImmediate(oracleGo);
            }
        }

        [Test]
        public void FraudDetectionFlagsConsoleUsageImmediately()
        {
            var reporterGo = new GameObject("Reporter");
            try
            {
                var reporter = reporterGo.AddComponent<Blindsided.UGS.UgsLeaderboardsReporter>();
                var method = typeof(Blindsided.UGS.UgsLeaderboardsReporter)
                    .GetMethod("EvaluateFraud", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(method, "EvaluateFraud reflection lookup failed.");

                var result = method.Invoke(reporter, new object[] { 5000d, 4000d, true });
                Assert.AreEqual("ConsoleUsed", result.ToString(), "Console use should be flagged as fraudulent.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reporterGo);
            }
        }

        [Test]
        public void FraudDetectionFlagsUnderOneHourCompletion()
        {
            var reporterGo = new GameObject("Reporter");
            try
            {
                var reporter = reporterGo.AddComponent<Blindsided.UGS.UgsLeaderboardsReporter>();
                var method = typeof(Blindsided.UGS.UgsLeaderboardsReporter)
                    .GetMethod("EvaluateFraud", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(method, "EvaluateFraud reflection lookup failed.");

                var result = method.Invoke(reporter, new object[] { 1200d, 4000d, false });
                Assert.AreEqual("BelowMinimumThreshold", result.ToString(), "Sub hour completions must be flagged.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reporterGo);
            }
        }
    }
}
#endif
