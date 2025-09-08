using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the map UI with the hero's distance reached.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private Slider distanceSlider;

        // Caches to avoid redundant UI work per refresh
        private int _lastDistanceInt = int.MinValue;
        private int _lastReapInt = int.MinValue;
        private int _lastBaseInt = int.MinValue;
        private bool _lastShowBase;
        private float _lastSliderValue = -1f;


        /// <summary>
        ///     Updates the UI with the distance the hero has reached.
        /// </summary>
        /// <param name="distance">The hero's X position.</param>
        public void UpdateDistance(float distance)
        {
            var buff = BuffManager.Instance;
            var baseReapDistance = Oracle.oracle?.saveData?.General.MaxRunDistance ?? 1f;
            var reapDistance = baseReapDistance * (buff != null ? buff.MaxDistanceMultiplier : 1f) +
                               (buff != null ? buff.MaxDistanceFlatBonus : 0f);
            // Clamp UI to demo cap so the slider/text reflect actual reachable distance in demo
            var isDemo = Oracle.oracle != null && Oracle.oracle.demo;
            if (isDemo) reapDistance = Mathf.Min(reapDistance, 300f);

            // Text update using TMP's SetText formatting (allocation-free)
            if (distanceText != null)
            {
                var currentInt = Mathf.FloorToInt(distance);
                var reapInt = Mathf.FloorToInt(reapDistance);
                var showBase = !Mathf.Approximately(reapDistance, baseReapDistance);
                var baseShown = Mathf.FloorToInt(Mathf.Min(baseReapDistance, isDemo ? 300f : baseReapDistance));

                // Only refresh the text when the displayed values actually change
                if (currentInt != _lastDistanceInt || reapInt != _lastReapInt ||
                    showBase != _lastShowBase || (showBase && baseShown != _lastBaseInt))
                {
                    if (showBase)
                        distanceText.SetText("{0:N0} / {1:N0} ({2:N0})", currentInt, reapInt, baseShown);
                    else
                        distanceText.SetText("{0:N0} / {1:N0}", currentInt, reapInt);

                    _lastDistanceInt = currentInt;
                    _lastReapInt = reapInt;
                    _lastBaseInt = baseShown;
                    _lastShowBase = showBase;
                }
            }

            // Avoid redundant slider writes when unchanged
            if (distanceSlider != null)
            {
                var normalized = reapDistance > 0f ? Mathf.Clamp01(distance / reapDistance) : 0f;
                if (!Mathf.Approximately(normalized, _lastSliderValue))
                {
                    distanceSlider.value = normalized;
                    _lastSliderValue = normalized;
                }
            }
        }
    }
}
