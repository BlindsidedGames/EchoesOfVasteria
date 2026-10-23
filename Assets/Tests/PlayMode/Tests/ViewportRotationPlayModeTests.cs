#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using TimelessEchoes;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CameraStackRendersWorldAcrossOrientationAndMenuChanges()
        {
            var owner = new GameObject("Rendered rotation stack");
            var settings = Object.Instantiate(Resources.Load<PanelSettings>("UI/ToolkitPanel"));
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), 1);
            var material = new Material(Resources.Load<Material>("Characters/Composite"));
            material.SetTexture("_Layer0", Texture2D.whiteTexture);
            material.SetVector("_LayerRect0", new Vector4(0, 0, 1, 1));
            try
            {
                var lightObject = new GameObject("World light", typeof(Light2D));
                lightObject.transform.SetParent(owner.transform);
                var light = lightObject.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = 1;
                var camera = owner.AddComponent<Camera>();
                camera.depth = 10;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.cullingMask = 1;
                var world = new GameObject("Visible world", typeof(SpriteRenderer));
                world.transform.SetParent(owner.transform, false);
                world.transform.localPosition = new Vector3(0, 0, 10);
                world.transform.localScale = Vector3.one * 1000;
                var renderer = world.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.color = Color.green;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                owner.AddComponent<AspectRatioBox>().targetCamera = camera;
                var overlayObject = new GameObject("Rotation overlay camera");
                overlayObject.transform.SetParent(owner.transform);
                var overlay = overlayObject.AddComponent<Camera>();
                overlay.orthographic = true;
                overlay.cullingMask = 0;
                overlay.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                camera.GetUniversalAdditionalCameraData().cameraStack.Add(overlay);
                overlayObject.AddComponent<AspectRatioBox>().targetCamera = overlay;
                var documentObject = new GameObject("Rotation menu composition", typeof(UIDocument));
                documentObject.transform.SetParent(owner.transform);
                settings.sortingOrder = 100;
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1;
                var document = documentObject.GetComponent<UIDocument>();
                document.panelSettings = settings;
                var menu = new VisualElement();
                menu.style.position = Position.Absolute;
                menu.style.left = menu.style.right = 0;
                menu.style.bottom = 0;
                menu.style.height = 8;
                menu.style.backgroundColor = Color.gray;
                document.rootVisualElement.Add(menu);
                var evidence = Path.GetFullPath("Artifacts/ViewportRotation20261023");
                Directory.CreateDirectory(evidence);
                foreach (var stacked in new[] { true, false })
                for (var cycle = 0; cycle < 3; cycle++)
                foreach (var size in new[] { new Vector2Int(2622, 1206), new Vector2Int(1206, 2622) })
                foreach (var expanded in new[] { false, true, false })
                foreach (var menuOpen in new[] { false, true, false })
                {
                    overlay.enabled = stacked;
                    #if UNITY_EDITOR
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Rendered rotation");
#else
                    Screen.SetResolution(size.x > size.y ? 1564 : 720, size.x > size.y ? 720 : 1564, FullScreenMode.Windowed);
#endif
                    ToolkitResponsiveLayout.SetFocusView(expanded);
                    menu.style.display = menuOpen ? DisplayStyle.Flex : DisplayStyle.None;
                    var blur = new FilterFunction(FilterFunctionType.Blur);
                    blur.AddParameter(new FilterParameter(3f));
                    menu.style.backdropFilter = size.x > size.y ? new System.Collections.Generic.List<FilterFunction> { blur } : StyleKeyword.None;
                    for (var frame = 0; frame < 3; frame++) yield return null;
                    var capturePath = Path.Combine(evidence, "frame.png");
                    if (File.Exists(capturePath)) File.Delete(capturePath);
                    ScreenCapture.CaptureScreenshot(capturePath);
                    for (var wait = 0; wait < 120 && !File.Exists(capturePath); wait++) yield return null;
                    Assert.True(File.Exists(capturePath), "Screenshot must come from the displayed frame.");
                    var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        Assert.True(image.LoadImage(File.ReadAllBytes(capturePath)));
                        var pixels = camera.pixelRect;
                        for (var y = 1; y <= 9; y++)
                        for (var x = 1; x <= 9; x++)
                        {
                            var point = new Vector2(pixels.x + pixels.width * x / 10f, pixels.y + pixels.height * y / 10f);
                            var colour = image.GetPixel((int)point.x, (int)point.y);
                            if (colour.g > .5f && colour.r < .2f) continue;
                            var name = $"failed-{stacked}-{cycle}-{size.x}-{expanded}-{menuOpen}.png";
                            File.WriteAllBytes(Path.Combine(evidence, name), image.EncodeToPNG());
                            Assert.Fail($"World missing at {point}: {colour}; screen={size}, texture={image.width}x{image.height}, stacked={stacked}, expanded={expanded}, menu={menuOpen}, camera={pixels}, overlay={overlay.pixelRect}");
                        }
                        if (cycle == 0 && !menuOpen)
                            File.WriteAllBytes(Path.Combine(evidence, $"world-{size.x}-{expanded}.png"), image.EncodeToPNG());
                    }
                    finally { Object.DestroyImmediate(image); }
                }
            }
            finally
            {
                ToolkitResponsiveLayout.SetFocusView(false);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(material);
            }
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator WorldViewportIsCurrentWhenRenderingAfterLateSurfaceChanges()
        {
            var owner = new GameObject("Rotation viewport camera");
            owner.tag = "MainCamera";
            var camera = owner.AddComponent<Camera>();
            camera.orthographic = true;
            camera.backgroundColor = Color.green;
            var samples = 0;
            var rendered = Rect.zero;
            // Model a drawable/viewport change after ordinary LateUpdate. Register
            // before the owner so it can reconcile the final state before rendering.
            void ChangeDrawable(ScriptableRenderContext context, List<Camera> cameras)
            {
                if (cameras.Contains(camera)) camera.rect = new Rect(.25f, .25f, .5f, .5f);
            }
            void ObserveRender(ScriptableRenderContext context, Camera rendering)
            {
                if (rendering != camera) return;
                rendered = camera.rect;
                samples++;
            }
            RenderPipelineManager.beginContextRendering += ChangeDrawable;
            RenderPipelineManager.beginCameraRendering += ObserveRender;
            try
            {
                owner.AddComponent<AspectRatioBox>().targetCamera = camera;
                foreach (var size in new[] { new Vector2Int(1179, 2556), new Vector2Int(2556, 1179), new Vector2Int(1179, 2556) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Viewport rotation");
                    samples = 0;
                    for (var frame = 0; frame < 4; frame++) yield return null;
                    Assert.Greater(samples, 0, "The assertion must observe a real camera render.");
                    var expected = ToolkitResponsiveLayout.Portrait(size)
                        ? new Rect(0, 1 - size.x * 9f / (16f * size.y), 1, size.x * 9f / (16f * size.y))
                        : new Rect(0, 0, 1, 1);
                    Assert.That(rendered.x, Is.EqualTo(expected.x).Within(.0001f));
                    Assert.That(rendered.y, Is.EqualTo(expected.y).Within(.0001f));
                    Assert.That(rendered.width, Is.EqualTo(expected.width).Within(.0001f));
                    Assert.That(rendered.height, Is.EqualTo(expected.height).Within(.0001f));
                }
            }
            finally
            {
                RenderPipelineManager.beginContextRendering -= ChangeDrawable;
                RenderPipelineManager.beginCameraRendering -= ObserveRender;
                Object.DestroyImmediate(owner);
            }
        }
#endif
    }
}
#endif
