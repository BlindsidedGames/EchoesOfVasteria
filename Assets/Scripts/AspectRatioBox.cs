using Screen = UnityEngine.Device.Screen;
// AspectRatioBox.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Fills narrow displays with a cropped world view. Extremely wide displays
/// retain pillarboxing beyond the supported maximum aspect ratio.
/// </summary>
namespace TimelessEchoes
{
public class AspectRatioBox : MonoBehaviour
{
    [Tooltip("Camera that should be boxed. Leave null → main camera.")]
    public Camera targetCamera;

    const float maxAspect = 32f / 9f; // 3.555...

    Rect originalRect;

    void OnEnable()
    {
        RenderPipelineManager.beginContextRendering += BeforeRendering;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            originalRect = targetCamera.rect;
            UpdateViewport();
        }
    }

    void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeforeRendering;
        if (targetCamera != null)
            targetCamera.rect = originalRect;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        UpdateViewport();
    }

    void BeforeRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Reconcile the drawable at the render boundary too. A surface/orientation
        // update after LateUpdate must not leave URP using the previous viewport.
        if (targetCamera != null)
            UpdateViewport();
    }

    void UpdateViewport()
    {
        var size = new Vector2Int(Screen.width, Screen.height);
        // Keep the last usable viewport while the platform replaces its drawable.
        if (size.x <= 0 || size.y <= 0)
            return;

        Rect rect;
        if (UI.Toolkit.ToolkitResponsiveLayout.Portrait(size))
        {
            var pixels = UI.Toolkit.ToolkitResponsiveLayout.WorldPixels(size, Screen.safeArea);
            rect = new Rect(pixels.x / size.x, pixels.y / size.y, pixels.width / size.x, pixels.height / size.y);
        }
        else
        {
            var bounds = UI.Toolkit.ToolkitResponsiveLayout.WorldPixels(size, Screen.safeArea);
            float windowAspect = bounds.width / bounds.height;
            float targetAspect = Mathf.Min(windowAspect, maxAspect);
            float scale = windowAspect / targetAspect;

            // A tablet keeps the full height and crops horizontally, just like
            // expanded portrait. RunActivityArea retains the wider Echo bounds.
            float invScale = 1f / scale;
            rect = new Rect((bounds.x + bounds.width * (1f - invScale) * .5f) / size.x,
                bounds.y / size.y, bounds.width * invScale / size.x, bounds.height / size.y);
        }

        if (rect.width > 0 && rect.height > 0 &&
            !float.IsNaN(rect.x) && !float.IsNaN(rect.y) &&
            !float.IsInfinity(rect.x) && !float.IsInfinity(rect.y) &&
            !float.IsInfinity(rect.width) && !float.IsInfinity(rect.height) &&
            targetCamera.rect != rect)
            targetCamera.rect = rect;
    }
}
}
