using TMPro;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Reference holder for a single Core slot button in the Forge UI.
    /// Handles selection visuals and shows core discovery/amount using ResourceManager.
    /// </summary>
    public class CoreSlotUIReferences : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Core this slot represents (optional; used for pre-placed core slots).")]
        [SerializeField] private TimelessEchoes.Gear.CoreSO core;
        [Header("Resources")]
        [Tooltip("Resource representing the required ingot for this core (used for preview/count).")]
        [SerializeField] private TimelessEchoes.Upgrades.Resource ingotResource;

        [Header("Wiring")]
        [SerializeField] private Button selectSlotButton;
        [SerializeField] private Image selectionImage;
        [SerializeField] private Image coreImage;
        [SerializeField] private TMP_Text coreCountText;

        [Header("Core Resource (for discovery/amount)")]
        [SerializeField] private Resource coreResource;

        public Button SelectSlotButton => selectSlotButton;
        public Image SelectionImage => selectionImage;
        public Image CoreImage => coreImage;
        public TMP_Text CoreCountText => coreCountText;
        public TimelessEchoes.Gear.CoreSO Core
        {
            get => core;
            set => core = value;
        }
        public Resource CoreResource
        {
            get => coreResource;
            set => coreResource = value;
        }
        public Resource IngotResource
        {
            get => ingotResource;
            set => ingotResource = value;
        }

        private void OnEnable()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.OnInventoryChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.OnInventoryChanged -= Refresh;
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionImage != null)
                selectionImage.enabled = isSelected;
        }

        /// <summary>
        /// Refreshes discovery visibility and amount text from the ResourceManager.
        /// If no resource is assigned, the core image remains as-is and count is cleared.
        /// </summary>
        public void Refresh()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var hasResource = coreResource != null;
            var discovered = hasResource && rm != null && rm.IsUnlocked(coreResource);

            if (coreImage != null)
                coreImage.enabled = discovered;

            if (coreCountText != null)
            {
                if (hasResource && rm != null)
                {
                    var amount = rm.GetAmount(coreResource);
                    coreCountText.text = amount > 0 ? amount.ToString("0") : "0";
                }
                else
                {
                    coreCountText.text = string.Empty;
                }
            }
        }
    }
}


