using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Controls a single run bar by setting the fill amount on an Image.
    /// </summary>
    public class RunBarUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image overlayImage;

        /// <summary>
        /// Gets or sets the fill color.
        /// </summary>
        public Color FillColor
        {
            get => fillImage != null ? fillImage.color : Color.white;
            set
            {
                if (fillImage != null)
                    fillImage.color = value;
            }
        }

        public Color OverlayColor
        {
            get => overlayImage != null ? overlayImage.color : Color.white;
            set
            {
                if (overlayImage != null)
                    overlayImage.color = value;
            }
        }

        public int BarIndex { get; set; } = -1;

        public event Action<RunBarUI, PointerEventData> PointerEnter;
        public event Action<RunBarUI, PointerEventData> PointerExit;

        /// <summary>
        /// Sets the bar fill based on a 0-1 ratio.
        /// </summary>
        /// <param name="ratio">Fill ratio from 0 to 1.</param>
        public void SetFill(float ratio)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void SetOverlayFill(float ratio)
        {
            if (overlayImage != null)
                overlayImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnter?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExit?.Invoke(this, eventData);
        }
    }
}
