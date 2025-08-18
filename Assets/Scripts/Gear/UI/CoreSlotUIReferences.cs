using System;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    ///     Reference holder for a single Core slot button in the Forge UI.
    ///     Handles selection visuals and shows core discovery, core count, and craftable count using ResourceManager.
    /// </summary>
    public class CoreSlotUIReferences : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Core this slot represents (optional; used for pre-placed core slots).")]
        [SerializeField]
        private CoreSO core;

        [Header("Resources")]
        [Tooltip("Resource representing the required ingot for this core (used for preview/count).")]
        [SerializeField]
        private Resource ingotResource;

        [Header("Wiring")] [SerializeField] private Button selectSlotButton;
        [SerializeField] private Image selectionImage;
        [SerializeField] private Image coreImage;
        [SerializeField] private TMP_Text coreCountText;
        [SerializeField] private TMP_Text craftCountText;

        [Header("Core Resource (for discovery/amount)")] [SerializeField]
        private Resource coreResource;

        public Button SelectSlotButton => selectSlotButton;
        public Image SelectionImage => selectionImage;
        public Image CoreImage => coreImage;
        public TMP_Text CoreCountText => coreCountText;
        public TMP_Text CraftCountText => craftCountText;

        public CoreSO Core
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

        private Sprite baseCoreSprite;

        private void Awake()
        {
            if (coreImage != null)
                baseCoreSprite = coreImage.sprite;
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
        ///     Refreshes discovery visibility, core count, and craftable count from the ResourceManager.
        ///     If no resource is assigned, the count displays 0.
        /// </summary>
        public void Refresh()
        {
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            var hasCoreResource = coreResource != null;
            var discovered = hasCoreResource && rm != null && rm.IsUnlocked(coreResource);

            if (coreImage != null)
            {
                if (discovered && hasCoreResource && rm != null)
                {
                    if (baseCoreSprite == null)
                        baseCoreSprite = coreImage.sprite;
                    var haveAtLeastOne = rm.GetAmount(coreResource) >= 1;
                    coreImage.sprite = haveAtLeastOne
                        ? (baseCoreSprite != null ? baseCoreSprite : coreResource.icon)
                        : coreResource.UnknownIcon;
                }


                coreImage.enabled = discovered;
            }

            var coreAmount = hasCoreResource && rm != null ? rm.GetAmount(coreResource) : 0;

            if (coreCountText != null)
                coreCountText.text = Math.Floor(coreAmount).ToString("0");

            if (craftCountText != null)
            {
                var crafts = Math.Floor(coreAmount);
                if (rm != null)
                {
                    var ingotAmount = ingotResource != null ? rm.GetAmount(ingotResource) : 0;
                    var cost = core != null ? core.ingotCost : 1;
                    if (cost > 0)
                        crafts = Math.Min(crafts, Math.Floor(ingotAmount) / cost);
                }

                craftCountText.text = crafts.ToString("0");
            }
        }
    }
}