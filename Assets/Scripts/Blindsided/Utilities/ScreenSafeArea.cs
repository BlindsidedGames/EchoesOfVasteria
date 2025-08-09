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

        private Vector2Int lastScreenSize;
        private Rect lastSafeArea;

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
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;

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

        private void OnLoadDataHandler()
        {
            StartCoroutine(InitRatio());
        }

        private void Update()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y ||
                Screen.safeArea != lastSafeArea)
            {
                lastScreenSize = new Vector2Int(Screen.width, Screen.height);
                lastSafeArea = Screen.safeArea;
                OnResolutionChanged();
            }
        }

        private void OnResolutionChanged()
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
        ///     Guarantees the usable area never becomes narrower than the baseline safe aspect (or 16:9).
        ///     On desktop, it preserves height and widens first by reducing horizontal padding down to the min,
        ///     only then letterboxes vertically if necessary.
        /// </summary>
        private void ApplySafeArea()
        {
            if (_rectTransform == null) return;

            var root = _rectTransform.root as RectTransform;
            if (root == null) return;
            var canvas = _rectTransform.GetComponentInParent<Canvas>();
            var scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

            // Use the actual canvas rect (not Screen.*), so Game View/letterboxing/editor panels are respected.
            var canvasRect = root.rect; // already in canvas units
            var canvasW = canvasRect.width;
            var canvasH = canvasRect.height;

            // Map device safe area to canvas units using scale factors per axis.
            var deviceSafe = Screen.safeArea; // in pixels
            var scaleX = canvasW / Screen.width;
            var scaleY = canvasH / Screen.height;

            var devLeft = deviceSafe.xMin * scaleX;
            var devRight = (Screen.width - deviceSafe.xMax) * scaleX;
            var devTop = (Screen.height - deviceSafe.yMax) * scaleY;
            var devBottom = deviceSafe.yMin * scaleY;

            // Start from device insets, then enforce user min padding (in canvas units)
            var left = Mathf.Max(devLeft, minPaddingLeft);
            var right = Mathf.Max(devRight, minPaddingRight);
            var top = Mathf.Max(devTop, minPaddingTop);
            var bottom = Mathf.Max(devBottom, minPaddingBottom);

            if (limitAspect)
            {
                // Working sizes in canvas units
                var width = canvasW - left - right;
                var height = canvasH - top - bottom;

                // --- Enforce MIN aspect (≥ baseline safe-aspect) ---
                // Baseline is whatever the canvas can use after device + min padding.
                // This guarantees we never add extra L/R padding beyond safe area at 16:9,
                // and allows slightly-wider-than-16:9 when T/B padding reduces height.
                var baselineAspect = width / height; // current safe aspect from insets
                var minAllowedAspect = Mathf.Max(minAspect, baselineAspect);
                var desiredMinWidth = height * minAllowedAspect;

                if (width < desiredMinWidth)
                {
                    // Prefer to widen (reduce L/R) only down to device/min padding.
                    var trueMinLeft = Mathf.Max(devLeft, minPaddingLeft);
                    var trueMinRight = Mathf.Max(devRight, minPaddingRight);

                    var reducibleLeft = Mathf.Max(0f, left - trueMinLeft);
                    var reducibleRight = Mathf.Max(0f, right - trueMinRight);
                    var totalReducible = reducibleLeft + reducibleRight;

                    var needed = desiredMinWidth - width;
                    if (totalReducible > 0f)
                    {
                        var reduce = Mathf.Min(needed, totalReducible);
                        var reduceLeft = totalReducible > 0f ? reduce * (reducibleLeft / totalReducible) : 0f;
                        var reduceRight = reduce - reduceLeft;

                        left -= reduceLeft;
                        right -= reduceRight;

                        width += reduce;
                        needed = desiredMinWidth - width;
                    }

                    // If still narrower than the baseline (e.g., hard device notch), letterbox (increase T/B).
                    if (needed > 0f)
                    {
                        var targetHeight = width / minAllowedAspect;
                        var delta = height - targetHeight;
                        if (delta > 0f)
                        {
                            top += delta * 0.5f;
                            bottom += delta * 0.5f;
                            height = targetHeight;
                        }
                    }
                }

                // --- Enforce MAX aspect (≤ user slider up to 32:9) ---
                var maxAllowedAspect = Mathf.Lerp(minAspect, maxAspect, ratioValue);

// Recompute after any min-aspect fixes
                width = canvasW - left - right;
                height = canvasH - top - bottom;

                const float aspectEpsilon = 0.0005f;
                var isNative16by9 = Mathf.Abs(Screen.width / (float)Screen.height - minAspect) < aspectEpsilon;

// If we're on a native 16:9 screen, DO NOT bring the sides in beyond the safe/min padding.
                if (!isNative16by9)
                {
                    var currentAspect = width / height;
                    if (currentAspect > maxAllowedAspect)
                    {
                        var targetWidth = height * maxAllowedAspect;
                        var delta = width - targetWidth;

                        // Distribute padding equally
                        left += delta * 0.5f;
                        right += delta * 0.5f;

                        // Ensure we don't go below min/safe-area padding
                        var minLeft = Mathf.Max(devLeft, minPaddingLeft);
                        var minRight = Mathf.Max(devRight, minPaddingRight);
                        if (left < minLeft) left = minLeft;
                        if (right < minRight) right = minRight;
                    }
                }


                // Stretch anchors are required; warn if misconfigured.
                if (_rectTransform.anchorMin != Vector2.zero || _rectTransform.anchorMax != Vector2.one)
                    Debug.LogWarning(
                        "[ScreenSafeArea] The target RectTransform is not stretch-stretch. Offsets will not behave as intended.");

                // Apply in canvas units
                _rectTransform.offsetMin = new Vector2(left, bottom);
                _rectTransform.offsetMax = new Vector2(-right, -top);

                // Ensure layout has stabilized; if a layout system changes sizes after this call, schedule a re-apply.
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}