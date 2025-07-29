using UnityEngine;

namespace Blindsided.Utilities
{
    public class ScreenSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Vector2 maxAnchor;
        private Vector2 minAnchor;
        private Rect safeArea;

        [Tooltip("Minimum space from the top edge in pixels")] public float minPaddingTop;
        [Tooltip("Minimum space from the bottom edge in pixels")] public float minPaddingBottom;
        [Tooltip("Minimum space from the left edge in pixels")] public float minPaddingLeft;
        [Tooltip("Minimum space from the right edge in pixels")] public float minPaddingRight;

#if UNITY_EDITOR || UNITY_STANDALONE
        public bool extraBoarders;
        public float boarderTop = 40;
        public float boarderBottom = 40;
        public float boarderLeft;
        public float boarderRight;
#endif
        private void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            safeArea = Screen.safeArea;

            float left = safeArea.xMin;
            float right = Screen.width - safeArea.xMax;
            float top = Screen.height - safeArea.yMax;
            float bottom = safeArea.yMin;

            left = Mathf.Max(left, minPaddingLeft);
            right = Mathf.Max(right, minPaddingRight);
            top = Mathf.Max(top, minPaddingTop);
            bottom = Mathf.Max(bottom, minPaddingBottom);

            safeArea = new Rect(left, bottom, Screen.width - left - right, Screen.height - top - bottom);

#if UNITY_EDITOR || UNITY_STANDALONE
            // Subtract 40 from the left, right, and bottom
            if (extraBoarders)
            {
                _rectTransform.offsetMin = new Vector2(boarderLeft, boarderBottom); // Left, Bottom
                _rectTransform.offsetMax = new Vector2(-boarderRight, -boarderTop); // Right, Top
            }
#endif

            minAnchor = safeArea.position;
            maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            _rectTransform.anchorMin = minAnchor;
            _rectTransform.anchorMax = maxAnchor;
        }
    }
}