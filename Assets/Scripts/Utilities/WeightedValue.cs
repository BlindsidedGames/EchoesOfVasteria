using System;
using UnityEngine;

namespace TimelessEchoes.Utilities
{
	/// <summary>
	/// A weight that grows linearly with level: weight = base + perLevel * (level-1).
	/// Clamped at >= 0.
	/// </summary>
	[Serializable]
	public struct WeightedValue
	{
		[Min(0f)] public float baseWeight;
		public float weightPerLevel;

		public float Evaluate(int level)
		{
			var lvl = Mathf.Max(1, level);
			return Mathf.Max(0f, baseWeight + weightPerLevel * (lvl - 1));
		}
	}
}


