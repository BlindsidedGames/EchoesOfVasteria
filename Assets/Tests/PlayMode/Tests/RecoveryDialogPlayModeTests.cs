#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator RecoveryScreenShowsAndCopiesTheSameIncidentInBothLayouts()
        {
            const string id = "EOV-52CDA7104BE9";
            var reports = typeof(Oracle).GetField("recoveryReports", Private);
            reports.SetValue(oracle, JsonUtility.FromJson(
                "{\"items\":[{\"slot\":" + oracle.CurrentSlot + ",\"id\":\"" + id + "\"}]}", reports.FieldType));
            SetOracle("_recoveryRequired", true);
            SetOracle("recoveryViewPrefab",
                UnityEditor.AssetDatabase.LoadAssetAtPath<ToolkitDialogScreen>("Assets/UI/Toolkit/Recovery.prefab"));
            var show = typeof(Oracle).GetMethod("ShowRecoveryUi", Private);
            var output = Path.Combine(Path.GetTempPath(), "eov-save-recovery-review");
            Directory.CreateDirectory(output);
            var clipboard = GUIUtility.systemCopyBuffer;
            try
            {
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Recovery review");
                    show.Invoke(oracle, null);
                    for (var frame = 0; frame < 5; frame++) yield return null;
                    var ui = oracle.GetComponentInChildren<UIDocument>().rootVisualElement;
                    var actions = ui.Q<ScrollView>("dialog-scroll").Query<Button>().ToList();
                    var send = actions.Single(button => button.text == "Send Diagnostics");
                    var copy = actions.Single(button => button.text.Contains(id));
                    var town = ui.Q<Button>("dialog-secondary");
                    Assert.AreEqual("Load in Town", town.text);
                    Assert.LessOrEqual(send.worldBound.yMax, copy.worldBound.yMin);
                    Assert.LessOrEqual(copy.worldBound.yMax, town.worldBound.yMin);
                    Assert.GreaterOrEqual(copy.worldBound.height, 48);
                    GUIUtility.systemCopyBuffer = "";
                    FieldsClick(copy);
                    Assert.AreEqual(id, GUIUtility.systemCopyBuffer);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,
                        size.x > size.y ? "diagnostics-landscape.png" : "diagnostics-portrait.png"));
                    yield return null;
                }
            }
            finally
            {
                GUIUtility.systemCopyBuffer = clipboard;
                typeof(Oracle).GetMethod("HideRecoveryUi", Private).Invoke(oracle, null);
                SetOracle("_recoveryRequired", false);
            }
        }

        [UnityTest]
        public IEnumerator TownRecoveryKeepsVerifiedProgressAndPreservesPendingHardcoreDeath()
        {
            var slot = oracle.GetSlotDirectoryName(oracle.CurrentSlot);
            var completion = oracle.GetSlotPlayerPrefsKey(oracle.CurrentSlot, "Completion");
            var playtime = oracle.GetSlotPlayerPrefsKey(oracle.CurrentSlot, "Playtime");
            var date = oracle.GetSlotPlayerPrefsKey(oracle.CurrentSlot, "Date");
            var oldCompletion = PlayerPrefs.GetFloat(completion, -1);
            var oldPlaytime = PlayerPrefs.GetFloat(playtime, -1);
            var oldDate = PlayerPrefs.GetString(date, null);
            try
            {
                foreach (var hardcore in new[] { false, true })
                {
                    oracle.saveData.Resources["Stone"] = new GameData.ResourceEntry { Amount = 37, Earned = true };
                    oracle.saveData.Quests["Meeting Ivan"] = new GameData.QuestRecord { Completed = true };
                    oracle.saveData.Hardcore = hardcore;
                    oracle.saveData.ActiveRun = new RunSaveData
                    {
                        Map = "Mines", Distance = 250, Dead = hardcore,
                        CheckpointUtcTicks = System.DateTime.UtcNow.Ticks
                    };
                    var written = SaveManager.Instance.SaveDetailedAsync(oracle.saveData, slot);
                    while (!written.IsCompleted) yield return null;
                    Assert.IsTrue(written.GetAwaiter().GetResult().Succeeded);
                    oracle.saveData.Resources["Stone"].Amount = 999; // Uncommitted reward must not survive.

                    var recover = (IEnumerator)typeof(Oracle).GetMethod("LoadCurrentSlot", Private)
                        .Invoke(oracle, new object[] { true, true });
                    yield return recover;

                    Assert.IsTrue(oracle.HasLoadedCurrentSlotData);
                    Assert.AreEqual(37d, oracle.saveData.Resources["Stone"].Amount);
                    Assert.IsTrue(oracle.saveData.Quests["Meeting Ivan"].Completed);
                    if (hardcore)
                        Assert.IsTrue(oracle.saveData.ActiveRun?.Dead == true);
                    else
                        Assert.IsNull(oracle.saveData.ActiveRun);
                    Assert.IsFalse(oracle.SaveToSlot(oracle.CurrentSlot),
                        "Lifecycle and normal checkpoint saves must wait for the new scene's runtime owners.");
                    var disk = SaveManager.Instance.LoadDetailedAsync(slot);
                    while (!disk.IsCompleted) yield return null;
                    Assert.AreEqual(250f, disk.GetAwaiter().GetResult().Data.ActiveRun.Distance,
                        "The original run must still be on disk until town activation succeeds.");
                }
            }
            finally
            {
                if (oldCompletion >= 0) PlayerPrefs.SetFloat(completion, oldCompletion); else PlayerPrefs.DeleteKey(completion);
                if (oldPlaytime >= 0) PlayerPrefs.SetFloat(playtime, oldPlaytime); else PlayerPrefs.DeleteKey(playtime);
                if (oldDate != null) PlayerPrefs.SetString(date, oldDate); else PlayerPrefs.DeleteKey(date);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator FailedHardcoreDeathCaptureStaysTerminalWhenTheVerifiedSlotReloads()
        {
            var slot = oracle.GetSlotDirectoryName(oracle.CurrentSlot);
            var marker = oracle.GetSlotPlayerPrefsKey(oracle.CurrentSlot, "PendingHardcoreDeath");
            var oldMarker = PlayerPrefs.GetString(marker, null);
            const string reports = "SaveRecovery.Diagnostics.v1";
            var oldReports = PlayerPrefs.GetString(reports, null);
            PlayerPrefs.DeleteKey(marker);
            PlayerPrefs.DeleteKey(reports);
            var previousScene = SceneManager.GetActiveScene();
            var loadingScene = SceneManager.CreateScene("Loading");
            SceneManager.SetActiveScene(loadingScene);
            SetOracle("recoveryViewPrefab",
                UnityEditor.AssetDatabase.LoadAssetAtPath<ToolkitDialogScreen>("Assets/UI/Toolkit/Recovery.prefab"));
            oracle.saveData.Hardcore = true;
            oracle.saveData.ActiveRun = new RunSaveData { Map = "Mines", Distance = 250, HealthFraction = 1 };
            System.Action failCapture = () => throw new System.InvalidOperationException("Injected contributor failure");
            try
            {
                var original = SaveManager.Instance.SaveDetailedAsync(oracle.saveData, slot);
                while (!original.IsCompleted) yield return null;
                Assert.True(original.GetAwaiter().GetResult().Succeeded);
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save cancelled because"));
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("The hardcore death could not be saved"));
                Blindsided.EventHandler.OnSaveData += failCapture;
                try { Assert.False(oracle.FinishHardcoreRun("Mines", 300)); }
                finally { Blindsided.EventHandler.OnSaveData -= failCapture; }

                var beforeRetry = SaveManager.Instance.LoadDetailedAsync(slot);
                while (!beforeRetry.IsCompleted) yield return null;
                Assert.False(beforeRetry.GetAwaiter().GetResult().Data.HardcoreFallen);

                var recover = (IEnumerator)typeof(Oracle).GetMethod("LoadCurrentSlot", Private)
                    .Invoke(oracle, new object[] { false, true });
                yield return recover;
                Assert.True(oracle.HasLoadedCurrentSlotData);
                Assert.True(oracle.saveData.HardcoreFallen);
                Assert.IsNull(oracle.saveData.ActiveRun);
                var afterRetry = SaveManager.Instance.LoadDetailedAsync(slot);
                while (!afterRetry.IsCompleted) yield return null;
                Assert.AreEqual(300, afterRetry.GetAwaiter().GetResult().Data.HardcoreDeath.Distance);
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                SceneManager.UnloadSceneAsync(loadingScene);
                if (oldMarker == null) PlayerPrefs.DeleteKey(marker); else PlayerPrefs.SetString(marker, oldMarker);
                if (oldReports == null) PlayerPrefs.DeleteKey(reports); else PlayerPrefs.SetString(reports, oldReports);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator LockedSnapshotQueuesTheReadExceptionWithoutThePrivatePath()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("This fault injection uses Windows file sharing.");
            var slot = oracle.GetSlotDirectoryName(oracle.CurrentSlot);
            var written = SaveManager.Instance.SaveDetailedAsync(oracle.saveData, slot);
            while (!written.IsCompleted) yield return null;
            var snapshot = written.GetAwaiter().GetResult();
            Assert.True(snapshot.Succeeded);

            const string reports = "SaveRecovery.Diagnostics.v1";
            var oldReports = PlayerPrefs.GetString(reports, null);
            PlayerPrefs.DeleteKey(reports);
            var previousScene = SceneManager.GetActiveScene();
            var loadingScene = SceneManager.CreateScene("Loading");
            SceneManager.SetActiveScene(loadingScene);
            SetOracle("recoveryViewPrefab",
                UnityEditor.AssetDatabase.LoadAssetAtPath<ToolkitDialogScreen>("Assets/UI/Toolkit/Recovery.prefab"));
            try
            {
                using var locked = new FileStream(snapshot.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("requires save recovery \\(Unavailable\\)"));
                yield return (IEnumerator)typeof(Oracle).GetMethod("LoadCurrentSlot", Private)
                    .Invoke(oracle, new object[] { false, true });
                var queued = PlayerPrefs.GetString(reports);
                StringAssert.Contains("SnapshotUnavailableException", queued);
                StringAssert.Contains("System.IO.IOException", queued);
                StringAssert.DoesNotContain(Path.GetFileName(snapshot.Path), queued);
                StringAssert.DoesNotContain("Save file is temporarily unavailable after retries", queued);
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                SceneManager.UnloadSceneAsync(loadingScene);
                if (oldReports == null) PlayerPrefs.DeleteKey(reports); else PlayerPrefs.SetString(reports, oldReports);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator RecoveryChoicesFitBothLayoutsAndRespectBusyState()
        {
            var instance = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Recovery.prefab"));
            var screen = instance.GetComponent<ToolkitDialogScreen>();
            var chosen = 0;
            var output = Path.Combine(Path.GetTempPath(), "eov-save-recovery-review");
            Directory.CreateDirectory(output);
            try
            {
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Recovery review");
                    screen.Show("Save Recovery Required", "More than one version of File 3 was found. Choose the progress to keep. All original snapshots remain saved.",
                        "Retry", "Keep Old Files & Start Fresh", () => {}, () => {}, choices: new[]
                        {
                            new ToolkitDialogScreen.Choice { Text = "Keep Adventurer\n21/10/2026 8:00 AM · 96.57% complete · 2381.4 hours played", Select = () => chosen++ },
                            new ToolkitDialogScreen.Choice { Text = "Keep Adventurer\n20/10/2026 1:46 PM · 88.06% complete · 2366.0 hours played", Select = () => chosen++ }
                        });
                    for (var frame = 0; frame < 5; frame++) yield return null;
                    var document = instance.GetComponent<UIDocument>();
                    var ui = document.rootVisualElement;
                    var choices = ui.Q<ScrollView>("dialog-scroll").Query<Button>().ToList();
                    Assert.AreEqual(2, choices.Count);
                    Assert.LessOrEqual(choices[0].worldBound.yMax, choices[1].worldBound.yMin);
                    Assert.LessOrEqual(choices[1].worldBound.yMax, ui.Q<Button>("dialog-primary").worldBound.yMin);
                    foreach (var choice in choices)
                    {
                        Assert.Greater(choice.worldBound.height, 0);
                        Assert.GreaterOrEqual(choice.worldBound.xMin, 0);
                        Assert.LessOrEqual(choice.worldBound.xMax, ui.worldBound.xMax);
                    }
                    FieldsClick(choices[0]);
                    Assert.AreEqual(size.x == 1920 ? 1 : 2, chosen);
                    screen.SetInteractable(false);
                    Assert.True(choices.All(c => !c.enabledInHierarchy));
                    screen.SetInteractable(true);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, size.x == 1920 ? "landscape.png" : "portrait.png"));
                    yield return null;
                }
                screen.Hide();
                Assert.IsNull(instance.GetComponent<UIDocument>().panelSettings);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Game");
            }
        }
    }
}
#endif
