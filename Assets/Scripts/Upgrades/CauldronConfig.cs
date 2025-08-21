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


