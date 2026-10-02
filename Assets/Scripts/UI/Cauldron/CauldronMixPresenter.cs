using System.Collections.Generic;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI.Cauldron
{
    /// <summary>
    /// Handles mixing slot selection and display.
    /// </summary>
    public class CauldronMixPresenter : MonoBehaviour
    {
        [SerializeField] private List<CauldronMixItemUIReferences> mixSlots;
        [SerializeField] private CauldronMixItemUIReferences slot1;
        [SerializeField] private CauldronMixItemUIReferences slot2;
        [SerializeField] private Button mixButton;
        [SerializeField] private Button mixAllButton;
        [SerializeField] private TMP_Text predictedStewText;
        [SerializeField] private Image mixArrowImage;
        [SerializeField] private Sprite mixArrowGreenSprite;
        [SerializeField] private Sprite mixArrowRedSprite;

        private LegacyCauldronConversion conversion;

        public void Initialize()
        {
            conversion ??= new LegacyCauldronConversion(mixSlots, slot1, slot2,
                mixButton, mixAllButton, predictedStewText, mixArrowImage);
        }
        public void RefreshSlots(List<Resource> eligibleFoods, ResourceManager resources)
        { Initialize(); conversion.Refresh(); }
        public void RefreshSelectedDisplaySlots() => conversion?.Refresh();
        public void RefreshMixButton(ResourceManager resources) => conversion?.Refresh();
        public void UpdateMixAllButtonState(List<Resource> eligibleFoods, ResourceManager resources) => conversion?.Refresh();
        public void ClearSelection() => conversion?.Clear();
        public static List<Resource> BuildEligibleFoodsList(ResourceManager resources) => CauldronMixingPresentation.BuildEligibleFoods(resources);
    }
}
