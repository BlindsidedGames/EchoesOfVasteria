using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace References.UI
{
    public class ResourceUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly List<ResourceUIReferences> instances = new();
        public Image questionMarkImage;
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;
        public Button selectButton;

        public event System.Action<ResourceUIReferences> PointerEnter;
        public event System.Action<ResourceUIReferences> PointerExit;
        public event System.Action<ResourceUIReferences, PointerEventData.InputButton> PointerClick;

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

        private void OnSelect()
        {
            foreach (var inst in instances)
                if (inst != null && inst.selectionImage != null)
                    inst.selectionImage.enabled = ReferenceEquals(inst, this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExit?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PointerClick?.Invoke(this, eventData.button);
        }
    }
}