using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace References.UI
{
    public class StatUIReferences : MonoBehaviour, ISelectableSlot
    {
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;
        public Button selectButton;

        public event Action<ISelectableSlot> PointerEnter;
        public event Action<ISelectableSlot> PointerExit;
        public event Action<ISelectableSlot, PointerEventData.InputButton> PointerClick;

        Button ISelectableSlot.SelectButton => selectButton;
        Image ISelectableSlot.SelectionImage => selectionImage;
        Transform ISelectableSlot.Transform => transform;
    }
}