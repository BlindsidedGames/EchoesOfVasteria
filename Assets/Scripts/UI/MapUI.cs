using TMPro;
using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the map UI with the hero's distance reached.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private Blindsided.Utilities.SlicedFilledImage distanceSlider;
        [SerializeField] private GameObject distanceSliderPrefab;

        private void Awake()
        {
            if (distanceSlider == null && distanceSliderPrefab != null)
            {
                var obj = Instantiate(distanceSliderPrefab, transform);
                distanceSlider = obj.GetComponent<Blindsided.Utilities.SlicedFilledImage>();
            }
        }

        /// <summary>
        ///     Updates the UI with the distance the hero has reached.
        /// </summary>
        /// <param name="distance">The hero's X position.</param>
        public void UpdateDistance(float distance)
        {
            if (distanceText != null)
            {
                int x = Mathf.FloorToInt(distance);
                distanceText.text = $"Distance Reached: {x}";
            }

            if (distanceSlider != null)
            {
                var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
                var max = tracker != null ? tracker.MaxRunDistance : 1f;
                distanceSlider.fillAmount = Mathf.Clamp01(distance / max);
            }
        }
    }
}
