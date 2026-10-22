#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        private sealed class FocusTask : ITask
        {
            public Transform Target { get; set; }
            public bool BlocksMovement => true;
            public bool IsComplete() => false;
            public void StartTask() { }
            public void OnArrival(HeroBase hero) { }
            public void Tick(HeroBase hero) { }
            public void OnInterrupt(HeroBase hero) { }
        }

        [UnityTest]
        public IEnumerator PortraitFocusRestoresMenusAndKeepsOffscreenEchoTasksAvailable()
        {
            var owner = new GameObject("Focus view protected run");
            GameObject view = null;
            var inactive = new GameObject("Inactive Echo fixture"); inactive.SetActive(false);
            var simulatorType = System.Type.GetType("UnityEditor.DeviceSimulation.SimulatorWindow, UnityEditor.DeviceSimulatorModule");
            var simulator = EditorWindow.GetWindow(simulatorType);
            var gameView = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView, UnityEditor"));
            var simulatorMain = simulatorType.GetField("m_Main", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulator);
            var screenSimulation = simulatorMain.GetType().GetProperty("ScreenSimulation").GetValue(simulatorMain);
            void Orientation(bool landscape)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                simulatorMain.GetType().GetMethod("Enable", flags).Invoke(simulatorMain, null);
                screenSimulation.GetType().GetProperty("DeviceRotation").SetValue(screenSimulation, landscape ? 90 : 0);
                screenSimulation.GetType().GetMethod("RequestOrientation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(screenSimulation, new object[] { landscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait });
                screenSimulation.GetType().GetMethod("ApplyChanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(screenSimulation, null);
                simulator.Focus();
                simulatorType.GetMethod("RepaintImmediately", flags).Invoke(simulator, null);
                System.Type.GetType("UnityEditor.PlayModeView, UnityEditor").GetProperty("targetSize", flags)
                    .SetValue(simulator, new Vector2(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height));
                simulatorType.GetMethod("RenderView", flags).Invoke(simulator, new object[] { Vector2.zero, false });
            }
            void ClickControl(Button button)
            {
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = button.worldBound.center })) button.SendEvent(move);
                FieldsClick(button);
            }
            void CaptureSimulator(string name)
            {
                var render = (RenderTexture)simulatorType.BaseType.GetMethod("RenderView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(simulator, new object[] { Vector2.zero, false });
                var previous = RenderTexture.active;
                var image = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = render;
                    image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
                    // The editor's render surface is vertically inverted on D3D.
                    if (SystemInfo.graphicsUVStartsAtTop)
                    {
                        var pixels = image.GetPixels32();
                        for (var y = 0; y < render.height / 2; y++)
                            for (var x = 0; x < render.width; x++)
                            {
                                var top = y * render.width + x;
                                var bottom = (render.height - y - 1) * render.width + x;
                                (pixels[top], pixels[bottom]) = (pixels[bottom], pixels[top]);
                            }
                        image.SetPixels32(pixels);
                    }
                    image.Apply();
                    var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "eov-focus-review");
                    System.IO.Directory.CreateDirectory(directory);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name), image.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; Object.DestroyImmediate(image); }
            }
            try
            {
                var gm = owner.AddComponent<GameManager>(); gm.enabled = false;
                typeof(GameManager).GetProperty("CurrentMap").SetValue(gm, owner);
                typeof(GameManager).GetProperty("IsInTown").SetValue(gm, false);
                var cameraObject = new GameObject("Focus camera", typeof(Camera));
                cameraObject.transform.SetParent(owner.transform); cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true;
                camera.orthographicSize = 9; camera.transform.position = new Vector3(30, 0, -10);
                camera.backgroundColor = new Color(.17f, .3f, .17f);
                cameraObject.AddComponent<AspectRatioBox>();
                var controller = owner.AddComponent<TaskController>(); controller.enabled = false;
                var virtualCamera = new GameObject("Focus virtual camera", typeof(CinemachineCamera));
                virtualCamera.transform.SetParent(owner.transform);
                typeof(TaskController).GetField("mapCamera", Private).SetValue(controller, virtualCamera.GetComponent<CinemachineCamera>());
                var echo = inactive.AddComponent<EchoController>();
                var destination = new GameObject("Offscreen task"); destination.transform.SetParent(owner.transform);
                var task = new FocusTask { Target = destination.transform }; controller.tasks.Add(task);
                view = CreateGuidanceRunView();
                var navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                navigation.transform.SetParent(owner.transform);
                var navigationUi = navigation.GetComponent<UIDocument>().rootVisualElement;
                var ui = view.GetComponent<UIDocument>().rootVisualElement;
                var toggle = ui.Q<Button>("run-focus-toggle");
                Assert.NotNull(toggle);
                var menu = new VisualElement(); ui.Add(menu);
                ToolkitGameplay.Apply(menu, AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"));
                menu.style.position = Position.Absolute; menu.style.top = 300;
                menu.style.width = 300; menu.style.height = 200;
                var field = new TextField { value = "Preserved selection" }; menu.Add(field);
                var dropdown = new DropdownField(new System.Collections.Generic.List<string> { "First", "Second" }, 0);
                menu.Add(dropdown);

                foreach (var landscape in new[] { false, true, false })
                {
                    Orientation(landscape);
                    simulator.Focus();
                    for (var frame = 0; frame < 5; frame++) yield return null;
                    if (landscape)
                    {
                        Assert.False(ToolkitResponsiveLayout.FocusView);
                        Assert.AreEqual(DisplayStyle.None, toggle.parent.style.display.value);
                    }
                    else
                    {
                        if (!ToolkitResponsiveLayout.FocusView)
                        {
                            ClickControl(toggle);
                            yield return null;
                            Assert.True(ToolkitResponsiveLayout.FocusView, "The expand control must open focus view.");
                            ClickControl(toggle);
                            yield return null;
                            dropdown.Focus();
                            using var submit = NavigationSubmitEvent.GetPooled();
                            dropdown.SendEvent(submit);
                            Assert.True(menu.panel.visualTree.Query<VisualElement>().ToList().Exists(e => e.userData is GenericDropdownMenu), "Exercise a real open dropdown portal.");
                            // A mode change must dismiss a portal even when it
                            // originates outside the dropdown's own panel.
                            ToolkitResponsiveLayout.SetFocusView(true);
                        }
                        for (var frame = 0; frame < 5; frame++) yield return null;
                        Assert.True(ToolkitResponsiveLayout.FocusView);
                        Assert.True(navigationUi.Q("navigation-buttons").enabledInHierarchy, "Bottom navigation must stay usable in focus view.");
                        var progress = navigationUi.Q("run-progress");
                        Assert.AreEqual(DisplayStyle.Flex, progress.resolvedStyle.display, "Run progress must remain visible.");
                        Assert.Less(progress.worldBound.yMax, ui.Q("run-stats-toggle").worldBound.yMin, "Progress and ability controls need separate rows.");
                        Assert.Greater(camera.rect.yMin, 0, "Focus reserves the bottom panel instead of drawing behind it.");
                        Assert.Less(ToolkitResponsiveLayout.WorldArea.yMax, ui.Q("run-stats-toggle").worldBound.yMin);
                        Assert.True(ToolkitResponsiveLayout.SafeScreenArea.Contains(toggle.worldBound.center));
                        ToolkitResponsiveLayout.ApplyFocusPresentation(menu);
                        yield return new WaitForSecondsRealtime(.25f);
                        Assert.Greater(menu.worldBound.yMin, ToolkitResponsiveLayout.Size.y, "Suspended panels must actually slide out of view.");
                        for (var frame = 0; frame < 3; frame++) yield return null;
                        var restoreIcon = ui.Q("run-focus-icon");
                        Assert.Greater(restoreIcon.worldBound.width, restoreIcon.worldBound.height, "Restore icon should indicate the wide compact viewport.");
                        var viewport = camera.rect;
                        ClickControl(ui.Q<Button>("run-stats-toggle"));
                        for (var frame = 0; frame < 5; frame++) yield return null;
                        Assert.True(ToolkitResponsiveLayout.FocusView, "Stats must float over expanded play without restoring the menu dock.");
                        Assert.AreEqual(viewport, camera.rect, "Opening stats must not resize the gameplay viewport.");
                        var stats = ui.Q<ScrollView>("portrait-combat-stats");
                        Assert.AreEqual(DisplayStyle.Flex, stats.resolvedStyle.display);
                        Assert.Less(stats.worldBound.yMax, progress.worldBound.yMin, "Floating stats must sit above the reaper strip.");
                        Assert.True(ToolkitResponsiveLayout.SafeScreenArea.Contains(stats.worldBound.min));
                        Assert.True(ToolkitResponsiveLayout.SafeScreenArea.Contains(stats.worldBound.max));
                        Assert.Greater(stats.resolvedStyle.backgroundColor.a, .9f, "Stats need an opaque readable surface over gameplay.");
                        Assert.False(stats.worldBound.Overlaps(toggle.worldBound), "Floating stats must leave the view toggle reachable.");
                        var previousRowBottom = float.NegativeInfinity;
                        foreach (var row in stats.Query<VisualElement>(className: "hero-stat-row").ToList())
                        {
                            Assert.GreaterOrEqual(row.worldBound.yMin, previousRowBottom, "Expanded stats need one readable stat per row.");
                            previousRowBottom = row.worldBound.yMax;
                        }
                        ClickControl(ui.Q<Button>("run-stats-toggle"));
                        for (var frame = 0; frame < 5; frame++) yield return null;
                        Assert.False(view.GetComponent<ToolkitRunScreen>().HasOpenHeroStats, "Second Stats tap closes the popup.");
                        Assert.AreEqual(DisplayStyle.None, stats.resolvedStyle.display);
                        ToolkitResponsiveLayout.ApplyFocusPresentation(menu);
                        Assert.False(field.enabledInHierarchy, "Hidden menu controls must not accept input.");
                        Assert.False(menu.panel.visualTree.Query<VisualElement>().ToList().Exists(e => e.userData is GenericDropdownMenu), "Focus must dismiss detached dropdown portals.");
                    }
                    CaptureSimulator(landscape ? "focus-landscape.png" : "focus-portrait-expanded.png");
                    yield return null; yield return null;
                    foreach (var offset in new[] { -12f, 12f })
                    {
                        destination.transform.position = new Vector3(30 + offset, 0, 0);
                        if (!landscape) Assert.False(camera.pixelRect.Contains(camera.WorldToScreenPoint(destination.transform.position)), "Exercise a task outside the displayed portrait crop.");
                        Assert.True(controller.HasVisibleTasksForHero(echo), "Echo must retain tasks on both sides of the full working area.");
                        echo.SetTask(null);
                        controller.SelectEarliestTask(echo, (TimelessEchoes.Skills.Skill)null);
                        Assert.AreSame(task, echo.CurrentTask, "The real Echo task selector must assign offscreen work, not just report it available.");
                    }
                    destination.transform.position = new Vector3(80, 0, 0);
                    Assert.False(controller.HasVisibleTasksForHero(echo), "Focus must not remove the existing bounded working range.");
                }
                ClickControl(toggle);
                for (var frame = 0; frame < 5; frame++) yield return null;
                ToolkitResponsiveLayout.ApplyFocusPresentation(menu);
                Assert.False(ToolkitResponsiveLayout.FocusView);
                var expandIcon = ui.Q("run-focus-icon");
                Assert.Greater(expandIcon.worldBound.height, expandIcon.worldBound.width, "Expand icon should indicate the tall portrait viewport.");
                Assert.True(field.enabledInHierarchy);
                Assert.AreEqual("Preserved selection", field.value);
                Assert.Greater(camera.rect.y, 0, "Restoring menus must restore the top 16:9 viewport.");
                CaptureSimulator("focus-portrait-restored.png"); yield return null; yield return null;
                simulator.Close(); gameView.Focus();
                yield return null; yield return null;
            }
            finally
            {
                ToolkitResponsiveLayout.SetFocusView(false);
                if (view) Object.DestroyImmediate(view);
                Object.DestroyImmediate(inactive); Object.DestroyImmediate(owner);
                simulator.Close();
                gameView.Focus();
            }
        }
    }
}
#endif
