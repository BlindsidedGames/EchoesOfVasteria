using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace References.UI
{
    public class SkillUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly List<SkillUIReferences> instances = new();

        public Image iconImage;
        public TMP_Text levelText;
        public Image selectionImage;
        public Image highlightImage;
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

        public event Action<SkillUIReferences> PointerEnter;
        public event Action<SkillUIReferences> PointerExit;
        public event Action<SkillUIReferences, PointerEventData.InputButton> PointerClick;

        private void OnSelect()
        {
            foreach (var inst in instances)
                if (inst != null && inst.selectionImage != null)
                    inst.selectionImage.enabled = ReferenceEquals(inst, this);
        }
    }
}
