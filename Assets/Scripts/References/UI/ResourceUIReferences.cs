using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace References.UI
{
    public class ResourceUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly List<ResourceUIReferences> instances = new();
        public Image questionMarkImage;
        public Image highlightImage;
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;
        public Button selectButton;

        private void Awake()
        {
            instances.Add(this);
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelect);
        }

        private void OnDestroy()
        {
            instances.Remove(this);
            if (selectButton != null)
                selectButton.onClick.RemoveListener(OnSelect);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PointerClick?.Invoke(this, eventData.button);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExit?.Invoke(this);
        }

        public event Action<ResourceUIReferences> PointerEnter;
        public event Action<ResourceUIReferences> PointerExit;
        public event Action<ResourceUIReferences, PointerEventData.InputButton> PointerClick;

        private void OnSelect()
        {
            foreach (var inst in instances)
                if (inst != null && inst.selectionImage != null)
                    inst.selectionImage.enabled = ReferenceEquals(inst, this);
        }
    }
}