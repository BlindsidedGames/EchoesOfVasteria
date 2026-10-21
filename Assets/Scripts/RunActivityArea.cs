using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Keeps portrait run simulation at its normal landscape width even when
    /// the display shows only a narrower crop of that world.
    /// </summary>
    public static class RunActivityArea
    {
        public const float PortraitAspect = 16f / 9f;

        private static bool UsesLandscapeActivity =>
            Application.isPlaying && ToolkitResponsiveLayout.IsPortrait && !ToolkitResponsiveLayout.TownView;

        public static float Aspect(float displayAspect) =>
            UsesLandscapeActivity ? PortraitAspect : displayAspect;

        public static bool Contains(Camera camera, Vector3 worldPosition)
        {
            if (!camera) return false;
            var point = camera.WorldToViewportPoint(worldPosition);
            var halfWidth = camera.orthographic && UsesLandscapeActivity && camera.aspect > 0f
                ? .5f * PortraitAspect / camera.aspect
                : .5f;
            return point.x >= .5f - halfWidth && point.x <= .5f + halfWidth &&
                   point.y >= 0f && point.y <= 1f;
        }

        public static bool TryGetBounds(Camera camera, out Vector3 min, out Vector3 max)
        {
            min = max = Vector3.zero;
            if (!camera) return false;
            min = camera.ViewportToWorldPoint(Vector3.zero);
            max = camera.ViewportToWorldPoint(Vector3.one);
            if (camera.orthographic && UsesLandscapeActivity && camera.aspect > 0f)
            {
                var centerX = (min.x + max.x) * .5f;
                var halfWidth = (max.x - min.x) * .5f * PortraitAspect / camera.aspect;
                min.x = centerX - halfWidth;
                max.x = centerX + halfWidth;
            }
            return true;
        }
    }
}
