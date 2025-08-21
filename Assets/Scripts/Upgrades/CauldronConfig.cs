using System;
using UnityEngine;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Upgrades
{
	[CreateAssetMenu(fileName = "CauldronConfig", menuName = "SO/Cauldron Config")]
	public class CauldronConfig : ScriptableObject
	{
		[Header("Tasting")] public float rollsPerSecond = 10f;
		[Min(0f)] public float stewPerRoll = 1f;

		[Header("Tier Thresholds (counts required per tier, length 8)")]
		[Tooltip("Resource (Alter-Echo) card thresholds per tier. Tier 1 is index 0. Unknown uses Tier 1 sprite.")]
		public int[] resourceTierThresholds = new int[8] { 1, 5, 20, 50, 100, 200, 350, 500 };
		[Tooltip("Buff card thresholds per tier. Tier 1 is index 0. Unknown uses Tier 1 sprite.")]
		public int[] buffTierThresholds = new int[8] { 1, 3, 10, 25, 50, 100, 200, 300 };

		[Header("Per-Tier Bonuses (%), index 0 = Tier 1")]
		[Tooltip("Additional Alter-Echo power for a resource at each tier (percent). Applies multiplicatively to Echo Power.")]
		public float[] resourcePowerBonusPerTier = new float[8] { 10f, 25f, 50f, 75f, 120f, 180f, 250f, 400f };
		[Tooltip("Buff cooldown reduction per tier (percent). 100 means instant/no cooldown.")]
		public float[] buffCooldownReductionPerTier = new float[8] { 5f, 10f, 15f, 25f, 40f, 60f, 80f, 100f };
		[Tooltip("Buff effect power bonus per tier (percent). Starts at Tier 3 per design.")]
		public float[] buffPowerBonusPerTier = new float[8] { 0f, 0f, 5f, 10f, 15f, 20f, 25f, 30f };

		[Header("Weights (Base/PerLevel)")]
		public WeightedValue weightNothing;
		public WeightedValue weightAlterEchoCard;
		public WeightedValue weightBuffCard;
		public WeightedValue weightLowestCountCard;
		public WeightedValue weightEvasBlessingX2;
		public WeightedValue weightVastSurgeX10;

		public Color sliceNothing = new Color(0.3f, 0.3f, 0.3f);
		public Color sliceAlterEcho = new Color(0.2f, 0.7f, 1f);
		public Color sliceBuff = new Color(0.9f, 0.7f, 0.2f);
		public Color sliceLowest = new Color(0.6f, 0.9f, 0.3f);
		public Color sliceEvas = new Color(0.8f, 0.4f, 1f);
		public Color sliceVast = new Color(1f, 0.3f, 0.3f);

		public float GetTotalWeight(int evaLevel)
		{
			return weightNothing.Evaluate(evaLevel)
			       + weightAlterEchoCard.Evaluate(evaLevel)
			       + weightBuffCard.Evaluate(evaLevel)
			       + weightLowestCountCard.Evaluate(evaLevel)
			       + weightEvasBlessingX2.Evaluate(evaLevel)
			       + weightVastSurgeX10.Evaluate(evaLevel);
		}
	}
}


