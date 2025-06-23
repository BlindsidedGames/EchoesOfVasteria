using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace References.UI
{
    public class ResourceUIReferences : MonoBehaviour
    {
        public Image questionMarkImage;
        public Image iconImage;
        public TMP_Text countText;
        public Image selectionImage;
        public Button selectButton;

        private static readonly List<ResourceUIReferences> instances = new();

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
    }
}