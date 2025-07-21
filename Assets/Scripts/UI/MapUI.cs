using TMPro;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the map UI with the hero's distance travelled.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;

        /// <summary>
        ///     Updates the UI with the distance the hero has travelled.
        /// </summary>
        /// <param name="distance">The hero's X position.</param>
        public void UpdateDistance(float distance)
        {
            if (distanceText == null) return;
            int x = Mathf.FloorToInt(distance);
            distanceText.text = $"Distance Travelled: {x}";
        }
    }
}
