using Screen = UnityEngine.Device.Screen;
// AspectRatioBox.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Keeps the camera within a supported aspect ratio range. Adds black bars on
/// the sides (pillar-box) or top/bottom (letter-box) if the window falls
/// outside the range.
/// </summary>
namespace TimelessEchoes
{
public class AspectRatioBox : MonoBehaviour
{
    [Tooltip("Camera that should be boxed. Leave null → main camera.")]
    public Camera targetCamera;

    // Supported aspect ratios: minimum 16:9 and maximum 32:9. Keep literal
    // values to avoid floating-point errors.
    const float minAspect = 16f / 9f; // 1.777...
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
            float windowAspect = (float)size.x / size.y;
            float targetAspect = Mathf.Clamp(windowAspect, minAspect, maxAspect);
            float scale = windowAspect / targetAspect;

            if (scale < 1f)               // window too tall → letter-box
            {
                rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
            }
            else                          // window too wide → pillar-box
            {
                float invScale = 1f / scale;
                rect = new Rect((1f - invScale) * 0.5f, 0f, invScale, 1f);
            }
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
