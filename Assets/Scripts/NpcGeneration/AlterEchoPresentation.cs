using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    public static class AlterEchoPresentation
    {
        public static string Rate(AlterEchoGenerator generator)
        {
            if (generator == null || generator.Resource == null) return string.Empty;
            var cm = CauldronManager.Instance;
            var multiplier = cm != null ? cm.GetResourceAlterEchoMultiplier(generator.Resource.name) : 1f;
            var cardPercent = Mathf.Max(0f, (multiplier - 1f) * 100f);
            if (generator.Interval > 0)
            {
                var time = CalcUtils.FormatTime(generator.Interval, showDecimal: generator.Interval < 60f, shortForm: true);
                return $"{CalcUtils.FormatNumber(generator.CycleAmount, true)} / {time} +{cardPercent:0}% Card Power";
            }
            return $"{CalcUtils.FormatNumber(0, true)} +{cardPercent:0}% Card Power";
        }
    }
}
