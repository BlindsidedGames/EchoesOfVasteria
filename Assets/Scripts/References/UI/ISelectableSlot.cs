using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace References.UI
{
    public interface ISelectableSlot
    {
        Button SelectButton { get; }
        Image SelectionImage { get; }
        Transform Transform { get; }

        event Action<ISelectableSlot> PointerEnter;
        event Action<ISelectableSlot> PointerExit;
        event Action<ISelectableSlot, PointerEventData.InputButton> PointerClick;
    }
}
