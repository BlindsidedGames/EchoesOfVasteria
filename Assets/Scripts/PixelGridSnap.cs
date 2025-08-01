// Snaps the final camera to the pixel grid computed from its projection & internal height.

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelGridSnap : MonoBehaviour
{
    public int internalHeightPx = 576; // e.g., tilesHigh * PPU
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // World units per *screen* pixel given the camera's current orthographic size.
        var unitsPerPixel = cam.orthographicSize * 2f / internalHeightPx;

        var p = transform.position;
        p.x = Mathf.Round(p.x / unitsPerPixel) * unitsPerPixel;
        p.y = Mathf.Round(p.y / unitsPerPixel) * unitsPerPixel;
        transform.position = new Vector3(p.x, p.y, p.z);
    }
}