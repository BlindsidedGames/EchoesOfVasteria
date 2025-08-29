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

            if (distanceText != null)
            {
                var current = Mathf.FloorToInt(distance);
                var text = $"{current:N0} / {reapDistance:N0}";
                if (!Mathf.Approximately(reapDistance, baseReapDistance))
                {
                    text += $" ({baseReapDistance:N0})";
                }

                distanceText.text = text;
            }

            if (distanceSlider != null)
            {
                distanceSlider.value = Mathf.Clamp01(distance / reapDistance);
            }
        }
    }
}