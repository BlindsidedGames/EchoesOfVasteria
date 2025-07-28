// AspectRatioBox.cs
using UnityEngine;

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
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            originalRect = targetCamera.rect;
            UpdateViewport();
        }
    }

    void OnDisable()
    {
        if (targetCamera != null)
            targetCamera.rect = originalRect;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        UpdateViewport();
    }

    void UpdateViewport()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float targetAspect = Mathf.Clamp(windowAspect, minAspect, maxAspect);
        float scale = windowAspect / targetAspect;

        Rect rect;

        if (scale < 1f)               // window too tall → letter-box
        {
            rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        }
        else                          // window too wide → pillar-box
        {
            float invScale = 1f / scale;
            rect = new Rect((1f - invScale) * 0.5f, 0f, invScale, 1f);
        }

        targetCamera.rect = rect;
    }
}
}
