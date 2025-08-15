using System.Collections.Generic;
using MPUIKIT;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    public partial class ForgeWindowUI
    {
        [Header("UI References")] [SerializeField]
        private Button craftButton;

        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button replaceButton;
        [SerializeField] private Button salvageButton;
        [SerializeField] private Button craftUntilUpgradeButton;
        [SerializeField] private TMP_Text craftUntilUpgradeButtonText;

        [Header("Gear Slot UI")]
        [Tooltip("References to each visible gear slot in this window. Their Button will be wired to SelectSlot.")]
        [SerializeField] private List<GearSlotUIReferences> gearSlots = new();

        [Header("Core Slot UI")]
        [Tooltip("Pre-placed core slot references in the scene. No prefab route is used.")]
        [SerializeField] private List<CoreSlotUIReferences> coreSlots = new();

        [Header("Odds UI")] [SerializeField] private TMP_Text rarityOddsLeftText;
        [SerializeField] private TMP_Text rarityOddsRightText;
        [SerializeField] private List<MPImageBasic> oddsPieSlices = new();

        [Header("Core Weight Tooltip")] [SerializeField] private Image coreWeightHoverImage;
        [SerializeField] private TMP_Text coreWeightHoverText;
        [SerializeField] private GameObject coreWeightHoverObject;

        [Header("Ivan XP UI")] [SerializeField]
        private SlicedFilledImage ivanXpBar;

        [SerializeField] private TMP_Text ivanXpText;
        [SerializeField] private TMP_Text ivanLevelText;

        [Header("Craft UI")] [SerializeField] private CraftSection2x1UIReferences craftSection;

        [Header("Ingot Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences ingotConversionSection;

        [Header("Crystal Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences crystalConversionSection;

        [Header("Chunk Conversion UI")] [SerializeField]
        private CraftSection2x1UIReferences chunkConversionSection;

        [Header("Additional Resource References")] [SerializeField]
        private Resource slimeResource;

        [SerializeField] private Resource stoneResource;

        [Header("Selected Slot UI")]
        [Tooltip("Text to display the stats of the currently equipped gear in the selected slot.")]
        [SerializeField] private TMP_Text selectedSlotStatsText;

        [Header("Unknown Gear Sprites (by slot order)")]
        [Tooltip("Fallback unknown sprites for each gear slot: Weapon, Helmet, Chest, Boots")]
        [SerializeField] private List<Sprite> unknownGearSprites = new();
    }
}
