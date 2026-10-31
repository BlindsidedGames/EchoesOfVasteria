#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using NUnit.Framework;
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
        [UnityTest]
        public IEnumerator GraphDropdownsSelectRealOptionsAndFitBothOrientationsAndThemes()
        {
            Assert.True(TimelessEchoes.Platform.StartupDiagnostics.SkipOnlineServices);
            var focusField = typeof(ToolkitResponsiveLayout).GetField("focusRequested", BindingFlags.Static | BindingFlags.NonPublic);
            var oldFocus = (bool)focusField.GetValue(null);
            var oldLight = ToolkitColorTheme.IsLight;
            var hadLight = PlayerPrefs.HasKey("EoV.Interface.LightMode");
            var owner = new GameObject("Protected graph controls owner"); GameObject view = null;
            try
            {
                ToolkitResponsiveLayout.SetFocusView(false);
                var amounts = new[] { 1800, 2500, 4200, 3100, 7600, 6400, 12000, 9200, 18000, 14500 };
                var number = 1;
                foreach (var map in new[] { "Farmlands", "River", "Woods", "Mines", "Haloween" })
                    for (var i = 0; i < 10; i++) oracle.saveData.General.RecentRuns.Add(new GameData.RunRecord
                    {
                        RunNumber = number++, MapType = map, Duration = 300 + 42 * i, Distance = 200 + 120 * i,
                        ResourcesCollected = amounts[i] * 3, BonusResourcesCollected = amounts[i] * .3,
                        EnemiesKilled = 20 + i * 6, TasksCompleted = 45 + i * 8,
                        DamageDealtDouble = 1400 + i * 850, DamageTakenDouble = 80 + i * 12,
                        Died = i % 4 == 1 || i % 4 == 2, Reaped = i % 4 == 1, Abandoned = i == 0
                    });
                oracle.saveData.General.PreviousRuns["Woods"] = new RunHistoryTotals { Runs = 120, Duration = 100000, Distance = 250000, Resources = 3800000, Kills = 15000, Tasks = 24000, Reaps = 75, Deaths = 20 };
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = view.GetComponent<ToolkitStatisticsScreen>(); Assert.True(screen.Show()); screen.SelectTab(2);
                var directory = Path.GetFullPath("Artifacts/GraphControls20261031"); Directory.CreateDirectory(directory);
                foreach (var light in new[] { false, true })
                {
                    ToolkitColorTheme.SetLight(light);
                    foreach (var portrait in new[] { false, true, false })
                    {
                        PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 2340u : 1080u, "Graph controls review");
                        for (var i = 0; i < 8; i++) yield return null;
                        var ui = view.GetComponent<UIDocument>().rootVisualElement;
                        var map = ui.Q<DropdownField>("graph-map-picker"); var metric = ui.Q<DropdownField>("graph-metric-picker");
                        Assert.NotNull(map); Assert.NotNull(metric);
                        var prefix = (portrait ? "portrait" : "landscape") + (light ? "-light" : "-dark");
                        yield return OpenGraphPicker(map);
                        var row = GraphOption(map, ToolkitLocalization.Text("map.Woods", "Woods"));
                        Assert.NotNull(row); Assert.GreaterOrEqual(row.worldBound.height, portrait ? 31 : 25);
                        AssertGraphPopupInPanel(map, light);
                        CaptureGraphPanel(ui, Path.Combine(directory, prefix + "-maps.png"));
                        ClickGraphElement(row); for (var i = 0; i < 4; i++) yield return null;
                        Assert.AreEqual(ToolkitLocalization.Text("map.Woods", "Woods"), map.value);
                        Assert.AreEqual(10, ui.Query<Button>(className: "graph-run-bar").ToList().Count);
                        Assert.That(ui.Q<Label>(className: "graph-detail-title").text, Does.Contain("Woods"));

                        yield return OpenGraphPicker(metric);
                        AssertGraphPopupInPanel(metric, light);
                        CaptureGraphPanel(ui, Path.Combine(directory, prefix + "-metrics.png"));
                        ClickGraphElement(GraphOption(metric, ToolkitLocalization.Text("graph.duration", "Duration")));
                        for (var i = 0; i < 3; i++) yield return null;
                        Assert.AreEqual(DisplayStyle.None, ui.Q(className: "graph-units").resolvedStyle.display);
                        yield return OpenGraphPicker(metric);
                        ClickGraphElement(GraphOption(metric, ToolkitLocalization.Text("graph.resources", "Resources")));
                        for (var i = 0; i < 3; i++) yield return null;
                        Assert.AreEqual(DisplayStyle.Flex, ui.Q(className: "graph-units").resolvedStyle.display);
                        Assert.LessOrEqual(map.worldBound.xMax, metric.worldBound.xMin + 1);
                        Assert.LessOrEqual(metric.worldBound.xMax, ui.Q("statistics").worldBound.xMax);
                        if (!portrait) Assert.That(map.worldBound.yMin, Is.EqualTo(ui.Q<Button>("graph-rates").worldBound.yMin).Within(2));
                        Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("graph-reset-zoom").resolvedStyle.display);
                        CaptureGraphPanel(ui, Path.Combine(directory, prefix + ".png"));
                        FieldsClick(ui.Q<Button>("graph-rates")); yield return null;
                        Assert.That(ui.Q<Label>(className: "graph-summary").text, Does.Contain("/ min"));
                        FieldsClick(ui.Q<Button>("graph-zoom-in")); yield return null;
                        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Button>("graph-reset-zoom").resolvedStyle.display);
                        CaptureGraphPanel(ui, Path.Combine(directory, prefix + "-zoom-rate.png"));
                        FieldsClick(ui.Q<Button>("graph-reset-zoom")); FieldsClick(ui.Q<Button>("graph-totals"));
                        yield return null;
                    }
                }
            }
            finally
            {
                if (view) Object.DestroyImmediate(view);
                Object.DestroyImmediate(owner); ToolkitResponsiveLayout.SetFocusView(oldFocus);
                ToolkitColorTheme.SetLight(oldLight);
                if (!hadLight) { PlayerPrefs.DeleteKey("EoV.Interface.LightMode"); PlayerPrefs.Save(); }
            }
        }

        private static IEnumerator OpenGraphPicker(DropdownField field)
        {
            field.Focus(); using (var submit = NavigationSubmitEvent.GetPooled()) field.SendEvent(submit);
            for (var i = 0; i < 4; i++) yield return null;
        }
        private static VisualElement GraphOption(DropdownField field, string text) => field.panel.visualTree
            .Query<VisualElement>(className: "unity-base-dropdown__item").ToList()
            .FirstOrDefault(row => row.Q<Label>(className: "unity-base-dropdown__label")?.text == text);
        private static void AssertGraphPopupInPanel(DropdownField field, bool light)
        {
            var popup = field.panel.visualTree.Q(className: "graph-dropdown"); Assert.NotNull(popup, "The native popup must receive the graph's styling.");
            Assert.AreEqual(light, popup.ClassListContains("light"));
            var bounds = popup.Q(className: "unity-base-dropdown__container-outer").worldBound;
            Assert.GreaterOrEqual(bounds.xMin, -1); Assert.GreaterOrEqual(bounds.yMin, -1);
            Assert.LessOrEqual(bounds.xMax, field.panel.visualTree.worldBound.xMax + 1);
            Assert.LessOrEqual(bounds.yMax, field.panel.visualTree.worldBound.yMax + 1);
            var label = popup.Q<Label>(className: "unity-base-dropdown__label");
            Assert.That(label.resolvedStyle.color.r, light ? Is.LessThan(.4f) : Is.GreaterThan(.8f));
        }
        private static void ClickGraphElement(VisualElement element)
        {
            Assert.NotNull(element);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = element.worldBound.center })) element.SendEvent(move);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = element.worldBound.center })) element.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = element.worldBound.center })) element.SendEvent(up);
        }
        private static void CaptureGraphPanel(VisualElement ui, string path)
        {
            // Export the actual rendered window at native pixel scale, without the
            // empty test-scene background. Popups are checked to fit this area.
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            var rect = ui.Q("statistics").worldBound;
            var scale = frame.width / ui.panel.visualTree.worldBound.width;
            var x = Mathf.Max(0, Mathf.FloorToInt(rect.xMin * scale));
            var y = Mathf.Max(0, Mathf.FloorToInt(frame.height - rect.yMax * scale));
            var width = Mathf.Min(frame.width - x, Mathf.CeilToInt(rect.width * scale));
            var height = Mathf.Min(frame.height - y, Mathf.CeilToInt(rect.height * scale));
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.SetPixels(frame.GetPixels(x, y, width, height)); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image); Object.DestroyImmediate(frame);
        }
    }
}
#endif
