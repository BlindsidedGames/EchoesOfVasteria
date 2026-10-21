#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PortraitFocusPreservesComposerWorldDeadzoneAndAuthoredSettings()
        {
            var owner = new GameObject("Protected focus camera tracking");
            var previousDeltaTime = CinemachineCore.UniformDeltaTimeOverride;
            try
            {
                var manager = owner.AddComponent<GameManager>(); manager.enabled = false;
                typeof(GameManager).GetProperty("CurrentMap").SetValue(manager, owner);
                typeof(GameManager).GetProperty("IsInTown").SetValue(manager, false);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1179, 2556, "Portrait camera tracking");
                ToolkitResponsiveLayout.SetFocusView(false);

                var output = new GameObject("Focus tracking output", typeof(Camera));
                output.transform.SetParent(owner.transform); output.tag = "MainCamera";
                var camera = output.GetComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 9;
                output.transform.position = new Vector3(60, 9, -10);
                output.AddComponent<AspectRatioBox>().targetCamera = camera;
                var brain = output.AddComponent<CinemachineBrain>();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;

                var target = new GameObject("Tracked hero"); target.transform.SetParent(owner.transform);
                target.transform.position = new Vector3(60, 9, 0);
                var rig = new GameObject("Focus tracking rig", typeof(CinemachineCamera));
                rig.transform.SetParent(owner.transform);
                rig.transform.position = output.transform.position;
                var virtualCamera = rig.GetComponent<CinemachineCamera>();
                virtualCamera.Target.TrackingTarget = target.transform;
                virtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
                virtualCamera.Lens.OrthographicSize = 9;
                var composer = rig.AddComponent<CinemachinePositionComposer>();
                composer.CameraDistance = 10;
                composer.CenterOnActivate = true;
                composer.Damping = Vector3.zero;
                composer.Lookahead.Enabled = false;
                composer.Composition.ScreenPosition = Vector2.zero;
                composer.Composition.DeadZone.Enabled = true;
                composer.Composition.DeadZone.Size = new Vector2(.2f, 1);
                composer.Composition.HardLimits.Enabled = false;
                var authored = composer.Composition;
                rig.AddComponent<CameraClampExtension>();

                for (var frame = 0; frame < 5; frame++) { yield return null; brain.ManualUpdate(); }
                Assert.True(ToolkitResponsiveLayout.IsPortrait, "Exercise actual portrait screen geometry.");
                var baselineX = camera.transform.position.x;
                target.transform.position += Vector3.right * 2;
                for (var frame = 0; frame < 3; frame++) { yield return null; brain.ManualUpdate(); }
                Assert.That(camera.transform.position.x, Is.EqualTo(baselineX).Within(.001f),
                    "The target starts inside the existing 6.4-unit landscape deadzone.");

                foreach (var expanded in new[] { true, false, true, false })
                {
                    ToolkitResponsiveLayout.SetFocusView(expanded);
                    for (var frame = 0; frame < 4; frame++) { yield return null; brain.ManualUpdate(); }
                    Assert.That(camera.aspect, expanded ? Is.LessThan(1f) : Is.GreaterThan(1.7f),
                        "The real output camera must switch between portrait crop and landscape viewport.");
                    Assert.That(camera.transform.position.x, Is.EqualTo(baselineX).Within(.001f),
                        "Changing only the display must not move the camera or its Echo activity region.");
                    Assert.That(composer.Composition.ScreenPosition, Is.EqualTo(authored.ScreenPosition));
                    Assert.That(composer.Composition.DeadZone.Size, Is.EqualTo(authored.DeadZone.Size));
                    Assert.That(composer.Composition.HardLimits.Size, Is.EqualTo(authored.HardLimits.Size),
                        "Runtime compensation must leave authored composition settings intact.");
                }
                composer.Lookahead.Enabled = true;
                composer.Lookahead.Time = 1f;
                composer.Lookahead.Smoothing = 30;
                CinemachineCore.UniformDeltaTimeOverride = 1f / 60f;
                foreach (var mode in new[] { 0, 1, 2 })
                {
                    var portrait = mode != 2;
                    if (!portrait) UnityEditor.PlayModeWindow.SetCustomRenderingResolution(2556, 1179, "Landscape camera tracking");
                    ToolkitResponsiveLayout.SetFocusView(mode == 1);
                    for (var frame = 0; frame < 4; frame++) { yield return null; brain.ManualUpdate(); }
                    foreach (var speed in new[] { 10f, 120f, 600f })
                    {
                        var startX = target.transform.position.x;
                        for (var frame = 0; frame < 60; frame++)
                        {
                            target.transform.position += Vector3.right * speed / 60f;
                            yield return null;
                            brain.ManualUpdate();
                            var visible = camera.WorldToViewportPoint(target.transform.position);
                            Assert.That(visible.x, portrait ? Is.InRange(.15f, .85f) : Is.InRange(.04f, .96f),
                                "Lookahead must retain the player on screen even at high speed.");
                            Assert.AreEqual(1f, composer.Lookahead.Time, "Do not rewrite authored landscape lookahead settings.");
                        }
                        Assert.Greater(target.transform.position.x - startX, speed * .9f, "The test must actually move the tracked player.");
                        if (speed >= 120)
                            Assert.Greater(camera.transform.position.x, target.transform.position.x,
                                "Portrait should retain forward lead rather than always centering the player.");
                    }
                }
            }
            finally
            {
                ToolkitResponsiveLayout.SetFocusView(false);
                CinemachineCore.UniformDeltaTimeOverride = previousDeltaTime;
                Object.DestroyImmediate(owner);
            }
        }
    }
}
#endif
