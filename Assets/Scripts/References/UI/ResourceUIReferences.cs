using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace References.UI
{
    public class ResourceUIReferences : MonoBehaviour
    {
        private static readonly List<ResourceUIReferences> instances = new();
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;
        public Button highlightButton;

        private void Awake()
        {
            instances.Add(this);
        }

        private void OnDestroy()
        {
            instances.Remove(this);
        }

        private void OnSelect()
        {
            foreach (var inst in instances)
                if (inst != null && inst.selectionImage != null)
                    inst.selectionImage.enabled = ReferenceEquals(inst, this);
        }
    }
}