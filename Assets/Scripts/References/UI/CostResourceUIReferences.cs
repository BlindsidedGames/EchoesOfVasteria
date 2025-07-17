using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace References.UI
{
    public class CostResourceUIReferences : MonoBehaviour, IPointerClickHandler
    {
        private static readonly List<CostResourceUIReferences> instances = new();
        public Resource resource;
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;

        public event System.Action<CostResourceUIReferences, PointerEventData.InputButton> PointerClick;

        private void Awake()
        {
            instances.Add(this);
        }

        private void OnDestroy()
        {
            instances.Remove(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PointerClick?.Invoke(this, eventData.button);
            if (eventData.button == PointerEventData.InputButton.Left)
                OnSelect();
        }
        private void OnSelect()
        {
            foreach (var inst in instances)
                if (inst != null && inst.selectionImage != null)
                    inst.selectionImage.enabled = ReferenceEquals(inst, this);
        }
    }
}