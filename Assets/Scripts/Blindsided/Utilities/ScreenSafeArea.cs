using UnityEngine;
using static Blindsided.SaveData.StaticReferences;

namespace Blindsided.Utilities
{
    public class ScreenSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Vector2 maxAnchor;
        private Vector2 minAnchor;
        private Rect safeArea;
        private Vector2Int lastResolution;
        private Rect lastSafeArea;

        private float ratioValue;

        const float minAspect = 16f / 9f;
        const float maxAspect = 32f / 9f;

        [Tooltip("Restrict the aspect ratio based on the saved preference")] public bool limitAspect = true;

        /// <summary>
        /// Normalised aspect ratio preference. 0 → 16:9, 1 → 32:9.
        /// Setting this value saves it and reapplies the safe area.
        /// </summary>
        public float RatioPreference
        {
            get => ratioValue;
            set
            {
                ratioValue = Mathf.Clamp01(value);
                SafeAreaRatio = ratioValue;
                ApplySafeArea();
            }
        }

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
            ratioValue = Mathf.Clamp01(SafeAreaRatio);
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastResolution.x || Screen.height != lastResolution.y)
            {
                ApplySafeArea();
            }
        }

        void ApplySafeArea()
        {
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
            if (extraBoarders)
            {
                _rectTransform.offsetMin = new Vector2(boarderLeft, boarderBottom); // Left, Bottom
                _rectTransform.offsetMax = new Vector2(-boarderRight, -boarderTop); // Right, Top
            }
#endif

            if (limitAspect)
            {
                float prefMax = Mathf.Lerp(minAspect, maxAspect, ratioValue);
                float safeAspect = safeArea.width / safeArea.height;
                if (safeAspect > prefMax)
                {
                    float targetWidth = safeArea.height * prefMax;
                    float delta = safeArea.width - targetWidth;
                    safeArea.xMin += delta * 0.5f;
                    safeArea.width = targetWidth;
                }
            }

            minAnchor = safeArea.position;
            maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            _rectTransform.anchorMin = minAnchor;
            _rectTransform.anchorMax = maxAnchor;

            lastSafeArea = Screen.safeArea;
            lastResolution = new Vector2Int(Screen.width, Screen.height);
        }
    }
}