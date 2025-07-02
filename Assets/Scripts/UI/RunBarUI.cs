using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Controls a single run bar by setting the fill amount on an Image.
    /// </summary>
    public class RunBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        /// <summary>
        /// Sets the bar fill based on a 0-1 ratio.
        /// </summary>
        /// <param name="ratio">Fill ratio from 0 to 1.</param>
        public void SetFill(float ratio)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
