#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PortraitDragStaysWithinLandscapeWorldDriftsBackAndLeavesGameplayFramingUntouched()
        {
            var owner = new GameObject("Portrait pan contract");
            var mouse = InputSystem.AddDevice<Mouse>();
            Touchscreen touch = null;
            try
            {
                oracle.saveData.Character = TimelessEchoes.Characters.CharacterCatalog.Load().NewProfile();
                oracle.saveData.Character.Created = true;
                var game = owner.AddComponent<GameManager>(); game.enabled = false;
                typeof(GameManager).GetProperty("CurrentMap").SetValue(game, owner);
                typeof(GameManager).GetProperty("IsInTown").SetValue(game, false);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080, 1920, "Portrait pan");
                ToolkitResponsiveLayout.SetFocusView(true);
                var host = new GameObject("Pan output", typeof(Camera)); host.transform.SetParent(owner.transform); host.tag = "MainCamera";
                var camera = host.GetComponent<Camera>(); camera.orthographic = true; camera.depth = 20;
                host.AddComponent<AspectRatioBox>().targetCamera = camera;
                var brain = host.AddComponent<CinemachineBrain>(); brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                var rigHost = new GameObject("Ordinary follow camera", typeof(CinemachineCamera)); rigHost.transform.SetParent(owner.transform);
                rigHost.transform.position = new Vector3(40, 9, -10);
                var rig = rigHost.GetComponent<CinemachineCamera>(); rig.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic; rig.Lens.OrthographicSize = 9;
                var frame = RunCameraFrame.Attach(rig);
                var events = new GameObject("Pan events", typeof(EventSystem)); events.transform.SetParent(owner.transform);
                var blocker = new GameObject("Menu intercept", typeof(Canvas), typeof(GraphicRaycaster)); blocker.transform.SetParent(owner.transform);
                blocker.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var image = new GameObject("Full screen UI", typeof(RectTransform), typeof(Image)); image.transform.SetParent(blocker.transform, false);
                var rect = image.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                blocker.SetActive(false);
                for (var i = 0; i < 8; i++) { yield return null; brain.ManualUpdate(); }
                Assert.True(ToolkitResponsiveLayout.UserFocusView);
                Assert.True(frame.HasState);
                var normal = frame.State.GetFinalPosition();
                RunActivityArea.TryGetBounds(camera, out var min, out var max);
                var point = camera.pixelRect.center;
                IEnumerator Pointer(Vector2 position, bool pressed)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
                    yield return null; yield return null; brain.ManualUpdate();
                }
                yield return Pointer(point, true); yield return Pointer(point + Vector2.right * 200, true);
                var shifted = camera.transform.position.x;
                Assert.Less(shifted, normal.x - 1, "Dragging the world right must reveal the area to its left.");
                Assert.AreEqual(normal, frame.State.GetFinalPosition(), "The simulation camera must not move with a gesture.");
                RunActivityArea.TryGetBounds(camera, out var afterMin, out var afterMax);
                Assert.AreEqual(min, afterMin); Assert.AreEqual(max, afterMax);
                yield return Pointer(point + Vector2.right * 200, false);
                Assert.Less(camera.transform.position.x, normal.x - .5f, "Release must not snap back.");
                yield return new WaitForSecondsRealtime(.6f); brain.ManualUpdate();
                Assert.Greater(camera.transform.position.x, shifted, "The view drifts toward its follow position.");
                yield return new WaitForSecondsRealtime(7); brain.ManualUpdate();
                Assert.That(camera.transform.position.x, Is.EqualTo(normal.x).Within(.02f));
                blocker.SetActive(true); yield return null;
                yield return Pointer(point, true); yield return Pointer(point + Vector2.right * 200, true); yield return Pointer(point, false);
                Assert.That(camera.transform.position.x, Is.EqualTo(normal.x).Within(.02f), "UI input must not pan the world.");
                blocker.SetActive(false);
                touch = InputSystem.AddDevice<Touchscreen>();
                IEnumerator Finger(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
                {
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = position });
                    yield return null; yield return null; brain.ManualUpdate();
                }
                yield return Finger(1, UnityEngine.InputSystem.TouchPhase.Began, point);
                yield return Finger(1, UnityEngine.InputSystem.TouchPhase.Moved, point + Vector2.left * 150);
                Assert.Greater(camera.transform.position.x, normal.x + 1, "A real one-finger gesture must pan too.");
                var beforePinch = camera.transform.position.x;
                yield return Finger(2, UnityEngine.InputSystem.TouchPhase.Began, point + Vector2.right * 200);
                yield return Finger(1, UnityEngine.InputSystem.TouchPhase.Moved, point + Vector2.left * 300);
                Assert.That(camera.transform.position.x, Is.EqualTo(beforePinch).Within(.001f), "Multiple fingers must not introduce pinch zoom or continue dragging.");
                yield return Finger(2, UnityEngine.InputSystem.TouchPhase.Ended, point);
                yield return Finger(1, UnityEngine.InputSystem.TouchPhase.Ended, point);
                InputSystem.RemoveDevice(touch); touch = null;
                // Repeat a long gesture to reach the 16:9 envelope without revealing outside terrain.
                yield return Pointer(point + Vector2.right * 350, true); yield return Pointer(point - Vector2.right * 350, true);
                Assert.LessOrEqual(camera.ViewportToWorldPoint(new Vector3(1, .5f, 10)).x, max.x + .001f);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Landscape must not pan");
                for (var i = 0; i < 8; i++) { yield return null; brain.ManualUpdate(); }
                var landscape = camera.transform.position;
                point = camera.pixelRect.center;
                yield return Pointer(point, false); yield return Pointer(point, true); yield return Pointer(point + Vector2.right * 200, true);
                Assert.AreEqual(landscape, camera.transform.position);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); ToolkitResponsiveLayout.SetFocusView(false);
                if (touch != null) InputSystem.RemoveDevice(touch);
                Object.DestroyImmediate(owner);
                var presentation = Object.FindAnyObjectByType<WorldPresentation>(); if (presentation) Object.DestroyImmediate(presentation.gameObject);
            }
        }
    }
}
#endif
