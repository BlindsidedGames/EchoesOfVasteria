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
                screenSimulation.GetType().GetProperty("DeviceRotation").SetValue(screenSimulation, landscape ? 90 : 0);
                screenSimulation.GetType().GetMethod("RequestOrientation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(screenSimulation, new object[] { landscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait });
                screenSimulation.GetType().GetMethod("ApplyChanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(screenSimulation, null);
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
                            FieldsClick(toggle);
                            yield return null;
                            Assert.True(ToolkitResponsiveLayout.FocusView, "The expand control must open focus view.");
                            FieldsClick(toggle);
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
                        Assert.That(camera.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
                        Assert.True(ToolkitResponsiveLayout.SafeScreenArea.Contains(toggle.worldBound.center));
                        ToolkitResponsiveLayout.ApplyFocusPresentation(menu);
                        Assert.False(field.enabledInHierarchy, "Hidden menu controls must not accept input.");
                        Assert.False(menu.panel.visualTree.Query<VisualElement>().ToList().Exists(e => e.userData is GenericDropdownMenu), "Focus must dismiss detached dropdown portals.");
                        yield return new WaitForSecondsRealtime(.25f);
                        Assert.Greater(menu.worldBound.yMin, ToolkitResponsiveLayout.Size.y, "Suspended panels must actually slide out of view.");
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
                FieldsClick(toggle);
                for (var frame = 0; frame < 5; frame++) yield return null;
                ToolkitResponsiveLayout.ApplyFocusPresentation(menu);
                Assert.False(ToolkitResponsiveLayout.FocusView);
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
