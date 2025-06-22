// AspectRatioBox.cs
using UnityEngine;

/// <summary>
/// Keeps the target camera locked to 16:9. Adds black bars on the sides
/// (pillar-box) or top/bottom (letter-box) when the window is any other ratio.
/// </summary>
namespace TimelessEchoes
{
public class AspectRatioBox : MonoBehaviour
{
    [Tooltip("Camera that should be boxed. Leave null → main camera.")]
    public Camera targetCamera;

    // 16:9 = 1.777…  ;  the numbers don’t change, so keep them literal
    const float targetAspect = 16f / 9f;

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
        if (Mathf.Approximately(
                (float)Screen.width / Screen.height,
                targetCamera.rect.width / targetCamera.rect.height)) return;

        UpdateViewport();
    }

    void UpdateViewport()
    {
        float windowAspect = (float)Screen.width / Screen.height;
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
