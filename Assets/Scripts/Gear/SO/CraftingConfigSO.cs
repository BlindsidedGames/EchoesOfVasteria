using Sirenix.OdinInspector;
using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes.Gear
{
    [CreateAssetMenu(fileName = "CraftingConfig", menuName = "SO/Gear/Crafting Config")]
    public class CraftingConfigSO : ScriptableObject
    {
        [Title("Pity Thresholds")]
        [MinValue(1)] public int pityRareWithin = 10;
        [MinValue(1)] public int pityEpicWithin = 40;
        [MinValue(1)] public int pityLegendaryWithin = 120;
        [MinValue(1)] public int pityMythicWithin = 300;

			[Title("Crafting")] public int craftHistoryLimit = 10;

			[Title("Slot Protection")]
        [Tooltip("If enabled, bias away from recently rolled slots to reduce streaks.")]
        public bool enableSmartSlotProtection = true;
        [Range(0f, 1f)] public float recentSlotPenalty = 0.25f;
        [MinValue(1)] public int recentWindow = 4;

			[Title("Level Scaling")]
			[Tooltip("Weights scale per-core using RarityWeight.weightPerLevel × IvanLevel; XP gain per craft handled in service.")]
			public bool enableLevelScaling = false;

		[Title("Ivan XP Gains")]
		[Tooltip("Baseline XP per core tier index (0=Common..). Index beyond list uses last value.")]
		public int[] baselineXpPerCoreTier = new int[] { 2, 3, 5, 8, 13, 21, 34, 55 };

		[Tooltip("XP bonus per rarity step above the core's tier (e.g., +3 XP per step).")]
		public int xpPerRarityStepAbove = 3;

		[Tooltip("If true, ignore bonus steps and grant XP equal to the rolled rarity's tier-based value.")]
		public bool useRolledRarityXp = false;

		[Tooltip("XP needed for Level 1→2; next levels scale by exponent.")]
		public float xpForFirstLevel = 10f;

		[Tooltip("Exponent for XP curve (e.g., 1.25 means slowly increasing).")]
		public float xpLevelMultiplier = 1.25f;
    }
}


