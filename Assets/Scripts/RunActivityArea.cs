using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Keeps run simulation on its ordinary tracking view even while the output
    /// camera is cropped or presenting dialogue, with at least landscape width.
    /// </summary>
    public static class RunActivityArea
    {
        public const float PortraitAspect = 16f / 9f;

        private static bool UsesLandscapeActivity =>
            Application.isPlaying && !ToolkitResponsiveLayout.TownView;

        public static float Aspect(float displayAspect) =>
            UsesLandscapeActivity ? Mathf.Max(PortraitAspect, displayAspect) : displayAspect;

        public static bool Contains(Camera camera, Vector3 worldPosition)
        {
            if (RunCameraFrame.TryGetCurrent(out var frame))
            {
                if (!frame.HasState) return false;
                var bounds = frame.ActivityBounds;
                return worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x &&
                       worldPosition.y >= bounds.min.y && worldPosition.y <= bounds.max.y;
            }
            if (!camera) return false;
            var point = camera.WorldToViewportPoint(worldPosition);
            var halfWidth = camera.orthographic && UsesLandscapeActivity && camera.aspect > 0f
                ? .5f * Aspect(camera.aspect) / camera.aspect
                : .5f;
            return point.x >= .5f - halfWidth && point.x <= .5f + halfWidth &&
                   point.y >= 0f && point.y <= 1f;
        }

        public static bool TryGetBounds(Camera camera, out Vector3 min, out Vector3 max)
        {
            min = max = Vector3.zero;
            if (RunCameraFrame.TryGetCurrent(out var frame))
            {
                var bounds = frame.ActivityBounds;
                min = bounds.min; max = bounds.max;
                return frame.HasState;
            }
            if (!camera) return false;
            min = camera.ViewportToWorldPoint(Vector3.zero);
            max = camera.ViewportToWorldPoint(Vector3.one);
            if (camera.orthographic && UsesLandscapeActivity && camera.aspect > 0f)
            {
                var centerX = (min.x + max.x) * .5f;
                var halfWidth = (max.x - min.x) * .5f * Aspect(camera.aspect) / camera.aspect;
                min.x = centerX - halfWidth;
                max.x = centerX + halfWidth;
            }
            return true;
        }
    }
}
