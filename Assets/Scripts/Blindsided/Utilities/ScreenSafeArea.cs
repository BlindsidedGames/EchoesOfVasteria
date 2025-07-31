using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
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
        private bool sliderSetup;

        [SerializeField] private Slider ratioSlider;
        [SerializeField] private Image previewImage;
        [SerializeField] private GameObject hideOnMobile;

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
            EventHandler.OnLoadData += OnLoadDataHandler;

            if (Application.isMobilePlatform)
            {
                if (hideOnMobile != null)
                    hideOnMobile.SetActive(false);
                RatioPreference = 1f;
            }

            if (previewImage != null)
                previewImage.enabled = false;
            StartCoroutine(InitRatio());
        }

        private void OnDisable()
        {
            EventHandler.OnLoadData -= OnLoadDataHandler;
            if (sliderSetup && ratioSlider != null)
            {
                ratioSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                sliderSetup = false;
            }
            if (previewImage != null)
                previewImage.enabled = false;
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastResolution.x || Screen.height != lastResolution.y)
            {
                ApplySafeArea();
            }
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(InitRatio());
        }

        private IEnumerator InitRatio()
        {
            yield return null; // wait one frame for data to load
            ratioValue = Mathf.Clamp01(SafeAreaRatio);
            if (ratioSlider != null)
            {
                if (!sliderSetup)
                {
                    ratioSlider.onValueChanged.AddListener(OnSliderValueChanged);
                    AddDragTriggers();
                    sliderSetup = true;
                }
                ratioSlider.value = ratioValue;
            }
            ApplySafeArea();
        }

        private void AddDragTriggers()
        {
            if (ratioSlider == null)
                return;
            var trigger = ratioSlider.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = ratioSlider.gameObject.AddComponent<EventTrigger>();

            AddEvent(trigger, EventTriggerType.BeginDrag, OnBeginDrag);
            AddEvent(trigger, EventTriggerType.EndDrag, OnEndDrag);
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            foreach (var e in trigger.triggers)
            {
                if (e.eventID == type)
                    return; // already added
            }
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void OnBeginDrag(BaseEventData _)
        {
            if (previewImage != null)
                previewImage.enabled = true;
        }

        private void OnEndDrag(BaseEventData _)
        {
            if (previewImage != null)
                previewImage.enabled = false;
        }

        private void OnSliderValueChanged(float value)
        {
            RatioPreference = value;
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