#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Stats;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [Test]
        public void LoadingAnUntrimmedHistoryTwiceDoesNotArchiveItsOverflowTwice()
        {
            oracle.saveData.General.RecentRuns = Enumerable.Range(1, 11)
                .Select(i => new GameData.RunRecord { MapType = "River", RunNumber = i, Distance = 100, EnemiesKilled = i }).ToList();
            var owner = new GameObject("Protected repeated history load");
            try
            {
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                typeof(GameplayStatTracker).GetMethod("LoadState", Private).Invoke(tracker, null);
                Assert.AreEqual(10, tracker.RecentRuns.Count);
                Assert.AreEqual(1, tracker.PreviousRuns["River"].Runs);
                Assert.AreEqual(1, tracker.PreviousRuns["River"].Kills);
                Assert.AreEqual(10, oracle.saveData.General.RecentRuns.Count);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator MapHistoryRolloverReloadAndResponsiveSummaryUseTheRealTracker()
        {
            var trackerObject = new GameObject("Protected map history owner");
            var config = ScriptableObject.CreateInstance<MapGenerationConfig>();
            var focusField = typeof(ToolkitResponsiveLayout).GetField("focusRequested", BindingFlags.Static | BindingFlags.NonPublic);
            var previousFocus = (bool)focusField.GetValue(null);
            // This fixture has no TownWindowManager to suspend the full-screen run surface.
            ToolkitResponsiveLayout.SetFocusView(false);
            GameObject view = null;
            try
            {
                var tracker = trackerObject.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                for (var i = 0; i < 60; i++)
                {
                    config.name = i < 10 ? "River" : "Woods";
                    tracker.BeginRun(config, GameManager.MapScalingMode.DistanceBased);
                    tracker.RecordHeroPosition(new Vector3(100 + i, 0));
                    tracker.AddResources(20); tracker.AddKill(null);
                    tracker.EndRun(false);
                }
                typeof(GameplayStatTracker).GetMethod("SaveState", Private).Invoke(tracker, null);
                oracle.saveData = CurrentSaveCodec.Clone(oracle.saveData);
                typeof(GameplayStatTracker).GetMethod("LoadState", Private).Invoke(tracker, null);
                Assert.AreEqual(20, tracker.RecentRuns.Count);
                Assert.AreEqual(10, tracker.RecentRuns.Count(r => r.MapType == "River"));
                Assert.AreEqual(40, tracker.PreviousRuns["Woods"].Runs);
                Assert.AreEqual(60, tracker.TotalKills);
                Assert.AreEqual(50, tracker.MapStats["Woods"].Kills);
                Assert.AreEqual(1200, tracker.TotalResourcesGathered);

                view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = view.GetComponent<ToolkitStatisticsScreen>(); Assert.True(screen.Show());
                var evidence = Path.GetFullPath("Artifacts/MapRunHistory20261031"); Directory.CreateDirectory(evidence);
                foreach (var portrait in new[] { false, true, false })
                {
                    PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 2340u : 1080u, "Map history review");
                    for (var i = 0; i < 6; i++) yield return null;
                    screen.SelectTab(0); screen.SelectJournalMap(-1);
                    for (var i = 0; i < 6; i++) yield return null;
                    var ui = view.GetComponent<UIDocument>().rootVisualElement;
                    var previous = ui.Q<Button>("journal-previous-history"); Assert.NotNull(previous);
                    yield return RevealHistoryControl(previous);
                    Assert.Greater(previous.worldBound.width, 120);
                    Assert.GreaterOrEqual(previous.worldBound.xMin, 0);
                    Assert.LessOrEqual(previous.worldBound.xMax, ui.worldBound.xMax + 1);
                    for (var ancestor = previous.parent; ancestor != null; ancestor = ancestor.parent)
                        if (ancestor is ScrollView scroll) Assert.True(scroll.contentViewport.worldBound.Contains(previous.worldBound.center),
                            "History control must be inside " + scroll.name + " in " + (portrait ? "portrait" : "landscape") + ": " + previous.worldBound + " / " + scroll.contentViewport.worldBound);
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, portrait ? "portrait-before-activation.png" : "landscape-before-activation.png"));
                    yield return null; yield return null;
                    FieldsClick(previous); yield return null;
                    var detail = ui.Q<Label>("journal-previous-detail"); Assert.NotNull(detail, "Summary activation failed in " + (portrait ? "portrait" : "landscape"));
                    yield return RevealHistoryControl(detail);
                    Assert.LessOrEqual(detail.worldBound.xMax, ui.worldBound.xMax + 1);
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, portrait ? "portrait-history.png" : "landscape-history.png"));
                    yield return null; yield return null;

                    screen.SelectTab(2);
                    for (var i = 0; i < 5; i++) yield return null;
                    previous = ui.Q<Button>("graph-previous-history"); Assert.NotNull(previous);
                    var chart = ui.Q(className: "graph-plot");
                    var bars = ui.Query<Button>(className: "graph-run-bar").ToList().Count;
                    var sample = ui.Q<Label>(className: "graph-summary").text;
                    yield return RevealHistoryControl(previous);
                    FieldsClick(previous); yield return null;
                    Assert.AreEqual(sample, ui.Q<Label>(className: "graph-summary").text, "Archived totals must not enter chart averages or its axis.");
                    Assert.GreaterOrEqual(chart.worldBound.height, 60);
                    Assert.AreEqual(bars, ui.Query<Button>(className: "graph-run-bar").ToList().Count);
                    Assert.AreEqual(DisplayStyle.Flex, ui.Q(className: "graph-previous-details").resolvedStyle.display);
                    Assert.That(previous.text, Does.Contain("40"));
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, portrait ? "portrait-graph-history.png" : "landscape-graph-history.png"));
                    yield return null; yield return null;
                }
            }
            finally
            {
                if (view) Object.DestroyImmediate(view);
                ToolkitResponsiveLayout.SetFocusView(previousFocus);
                Object.DestroyImmediate(trackerObject); Object.DestroyImmediate(config);
            }
        }

        private static IEnumerator RevealHistoryControl(VisualElement element)
        {
            // Portrait puts the recent-runs scroll inside the main journal scroll.
            for (var parent = element.parent; parent != null; parent = parent.parent)
                if (parent is ScrollView scroll)
                {
                    scroll.ScrollTo(element); yield return null; yield return null;
                }
        }
    }
}
#endif
