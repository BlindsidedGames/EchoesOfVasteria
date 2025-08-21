using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
	public static class CauldronPieUtils
	{
		public static void SetSlice(Image slice, float startAngle, float fill)
		{
			if (slice == null) return;
			slice.fillAmount = Mathf.Clamp01(fill);
			slice.transform.localRotation = Quaternion.Euler(0, 0, -startAngle);
		}
	}
}


