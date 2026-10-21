#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
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
    }
}
#endif
