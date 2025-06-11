using UnityEngine;

namespace Blindsided.Utilities
{
    public class ScreenSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Vector2 maxAnchor;
        private Vector2 minAnchor;
        private Rect safeArea;

#if UNITY_EDITOR
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

#if UNITY_EDITOR
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