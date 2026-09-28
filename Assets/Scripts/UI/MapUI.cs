using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>Legacy presenter retained until its native toolbar is installed.</summary>
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private Slider distanceSlider;
        private RunProgressPresentation last;
        private bool hasPrevious;

        public void UpdateDistance(float distance)
        {
            var current = RunProgressPresentation.Read(distance);
            if (distanceText != null && (!hasPrevious || !current.SameText(last)))
                distanceText.text = current.FormatText();
            if (distanceSlider != null && (!hasPrevious || !Mathf.Approximately(last.normalized, current.normalized)))
                distanceSlider.value = current.normalized;
            last = current;
            hasPrevious = true;
        }
    }
}
