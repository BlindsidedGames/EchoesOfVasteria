using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Displays a single run breakdown entry for a resource.
    /// </summary>
    public class RunBreakdownEntryUI : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TMP_Text resourceDetails;

        public Resource Resource { get; private set; }

        public void Initialize(Resource resource)
        {
            Resource = resource;
            if (resourceIcon == null) return;
            resourceIcon.sprite = resource != null ? resource.icon : null;
            resourceIcon.enabled = resource != null && resource.icon != null;
        }

        public void UpdateDisplay(double earned, double projectedTotal, double perMinute, double projectedPerMinute)
        {
            if (resourceDetails == null) return;
            var earnedText = FormatNumber(earned, true);
            var projectedTotalText = FormatNumber(projectedTotal, true);
            var perMinuteText = FormatNumber(perMinute, true);
            var projectedPerMinuteText = FormatNumber(projectedPerMinute, true);

            resourceDetails.text = $"Earned: {earnedText} ({projectedTotalText})\nPer Minute: {perMinuteText} ({projectedPerMinuteText})";
        }
    }
}
