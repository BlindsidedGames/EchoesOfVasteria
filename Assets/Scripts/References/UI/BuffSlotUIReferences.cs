using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MPUIKIT;

namespace References.UI
{
    public class BuffSlotUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button activateButton;
        public Image iconImage;
        public TMP_Text durationText;
        public Image autoCastImage;
        public MPImageBasic radialFillImage;

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
