using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;

namespace Blindsided.Utilities
{
    public class ScreenSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Coroutine applySafeAreaRoutine;

        private float ratioValue;
        private bool sliderSetup;

        [SerializeField] private Slider ratioSlider;
        [SerializeField] private Image previewImage;
        [SerializeField] private GameObject hideOnMobile;

        private const float minAspect = 16f / 9f;
        private const float maxAspect = 32f / 9f;

        [Tooltip("Restrict the aspect ratio based on the saved preference")]
        public bool limitAspect = true;

        /// <summary>
        ///     Normalised aspect ratio preference. 0 → 16:9, 1 → 32:9.
        ///     Setting this value saves it and reapplies the safe area.
        /// </summary>
        public float RatioPreference
        {
            get => ratioValue;
            set
            {
                ratioValue = Mathf.Clamp01(value);
                SafeAreaRatio = ratioValue; // Assuming this is saved elsewhere
                ApplySafeArea();
            }
        }

        [Tooltip("Minimum space from the top edge in pixels")]
        public float minPaddingTop = 6f;

        [Tooltip("Minimum space from the bottom edge in pixels")]
        public float minPaddingBottom = 6f;

        [Tooltip("Minimum space from the left edge in pixels")]
        public float minPaddingLeft = 6f;

        [Tooltip("Minimum space from the right edge in pixels")]
        public float minPaddingRight = 6f;

        private void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            EventHandler.OnLoadData += OnLoadDataHandler;
            Screen.onResolutionChanged += OnResolutionChanged;

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
            Screen.onResolutionChanged -= OnResolutionChanged;
            if (sliderSetup && ratioSlider != null)
            {
                ratioSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                sliderSetup = false;
            }

            if (previewImage != null)
                previewImage.enabled = false;
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(InitRatio());
        }

        private void OnResolutionChanged(int width, int height)
        {
            if (applySafeAreaRoutine != null)
                StopCoroutine(applySafeAreaRoutine);
            applySafeAreaRoutine = StartCoroutine(ApplySafeAreaNextFrame());
        }

        private IEnumerator ApplySafeAreaNextFrame()
        {
            yield return null;
            ApplySafeArea();
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

        private static void AddEvent(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            foreach (var e in trigger.triggers)
                if (e.eventID == type)
                    return; // already added
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

        /// <summary>
        ///     Applies padding to the RectTransform based on the screen's safe area,
        ///     minimum padding values, and aspect ratio preferences.
        /// </summary>
        private void ApplySafeArea()
        {
            if (_rectTransform == null) return;

            var deviceSafeArea = Screen.safeArea;
            var canvas = _rectTransform.GetComponentInParent<Canvas>();
            var scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

            // 1. Start with the device's safe area, but immediately convert to Canvas units.
            var left = deviceSafeArea.xMin / scaleFactor;
            var right = (Screen.width - deviceSafeArea.xMax) / scaleFactor;
            var top = (Screen.height - deviceSafeArea.yMax) / scaleFactor;
            var bottom = deviceSafeArea.yMin / scaleFactor;

            // 2. Enforce the minimum padding IN CANVAS UNITS.
            // This ensures the final value is never less than the minimum, regardless of scale factor.
            left = Mathf.Max(left, minPaddingLeft);
            right = Mathf.Max(right, minPaddingRight);
            top = Mathf.Max(top, minPaddingTop);
            bottom = Mathf.Max(bottom, minPaddingBottom);

            // 3. Correct for aspect ratio by adjusting the padding variables directly (already in canvas units).
            if (limitAspect)
            {
                var maxAllowedAspect = Mathf.Lerp(minAspect, maxAspect, ratioValue);

                // Get screen dimensions in canvas units to perform the calculation in the correct space.
                var screenWidthInCanvasUnits = Screen.width / scaleFactor;
                var screenHeightInCanvasUnits = Screen.height / scaleFactor;

                var currentWidth = screenWidthInCanvasUnits - left - right;
                var currentHeight = screenHeightInCanvasUnits - top - bottom;
                var currentAspect = currentWidth / currentHeight;

                if (currentAspect > maxAllowedAspect)
                {
                    // The area is too wide, so we need to add horizontal padding.
                    var targetWidth = currentHeight * maxAllowedAspect;
                    var delta = currentWidth - targetWidth;

                    // Distribute the change equally to the left and right padding variables.
                    left += delta * 0.5f;
                    right += delta * 0.5f;
                }
            }

            // DEBUG: Log the final calculated values that will be applied to the RectTransform.
            /*Debug.Log(
                $"[ScreenSafeArea] Final Padding in Canvas Units (L,R,T,B): ({left:F2}, {right:F2}, {top:F2}, {bottom:F2})");
                */

            // 4. Apply the final, correct padding values (which are already in canvas units) to the RectTransform's offsets.
            _rectTransform.offsetMin = new Vector2(left, bottom);
            _rectTransform.offsetMax = new Vector2(-right, -top);

        }
    }
}