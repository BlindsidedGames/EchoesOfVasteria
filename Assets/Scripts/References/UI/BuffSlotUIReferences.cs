using System;
using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace References.UI
{
    public class BuffSlotUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button activateButton;
        public Image iconImage;
        public TMP_Text durationText;
        public Image autoCastImage;
        public MPImage radialFillImage;

        private void Awake()
        {
            if (radialFillImage != null)
                radialFillImage.StrokeWidth = 1f;
        }

        public event Action<BuffSlotUIReferences> PointerEnter;
        public event Action<BuffSlotUIReferences> PointerExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExit?.Invoke(this);
        }
    }
}