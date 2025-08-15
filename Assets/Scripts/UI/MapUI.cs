using TimelessEchoes.Buffs;
using TimelessEchoes.Stats;
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


        /// <summary>
        ///     Updates the UI with the distance the hero has reached.
        /// </summary>
        /// <param name="distance">The hero's X position.</param>
        public void UpdateDistance(float distance)
        {
            if (distanceText != null)
            {
                var x = Mathf.FloorToInt(distance);
                distanceText.text = x.ToString();
            }

            if (distanceSlider != null)
            {
                var tracker = GameplayStatTracker.Instance;
                var buff = BuffManager.Instance;
                var max = tracker != null
                    ? tracker.MaxRunDistance * (buff != null ? buff.MaxDistanceMultiplier : 1f) +
                      (buff != null ? buff.MaxDistanceFlatBonus : 0f)
                    : 1f;
                distanceSlider.value = Mathf.Clamp01(distance / max);
            }
        }
    }
}