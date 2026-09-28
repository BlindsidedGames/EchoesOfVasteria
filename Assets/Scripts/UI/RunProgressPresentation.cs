using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>Read-only run progress shared by the original and native toolbar presenters.</summary>
    public struct RunProgressPresentation
    {
        public bool killMode, showBase;
        public int distance, maximum, baseMaximum, kills, threshold, level, levelsPerTier;
        public float normalized;

        public static RunProgressPresentation Read(float heroX)
        {
            var result = new RunProgressPresentation { distance = Mathf.FloorToInt(heroX) };
            var manager = GameManager.Instance;
            int total = 0, perLevel = 0, currentLevel = 1;
            result.killMode = manager != null && manager.TryGetKillProgress(out total, out perLevel, out currentLevel) && perLevel > 0;
            if (result.killMode)
            {
                perLevel = Mathf.Max(1, perLevel);
                result.kills = total;
                result.threshold = perLevel * currentLevel;
                result.level = currentLevel;
                result.levelsPerTier = Mathf.Max(1, manager.KillLevelIncreasePerTier);
                result.normalized = Mathf.Clamp01((float)(total % perLevel) / perLevel);
                return result;
            }
            var buff = BuffManager.Instance;
            var baseDistance = Oracle.oracle?.saveData?.General.MaxRunDistance ?? 1f;
            var maximum = baseDistance * (buff != null ? buff.MaxDistanceMultiplier : 1f) +
                          (buff != null ? buff.MaxDistanceFlatBonus : 0f);
            var demo = Oracle.oracle != null && Oracle.oracle.demo;
            if (demo) maximum = Mathf.Min(maximum, 300f);
            result.maximum = Mathf.FloorToInt(maximum);
            result.showBase = !Mathf.Approximately(maximum, baseDistance);
            result.baseMaximum = Mathf.FloorToInt(Mathf.Min(baseDistance, demo ? 300f : baseDistance));
            result.normalized = maximum > 0 ? Mathf.Clamp01(heroX / maximum) : 0;
            return result;
        }

        public bool SameText(RunProgressPresentation other) => killMode == other.killMode &&
            distance == other.distance && (killMode
                ? kills == other.kills && threshold == other.threshold && level == other.level && levelsPerTier == other.levelsPerTier
                : maximum == other.maximum && showBase == other.showBase && (!showBase || baseMaximum == other.baseMaximum));

        public string FormatText()
        {
            if (killMode)
                return $"{CalcUtils.FormatNumber(kills, true)} / {CalcUtils.FormatNumber(threshold, true)} | +{CalcUtils.FormatNumber(Mathf.Max(0, level - 1) * levelsPerTier, true)} Enemy levels\n{distance:N0} Distance";
            return showBase ? $"{distance:N0} / {maximum:N0} ({baseMaximum:N0})" : $"{distance:N0} / {maximum:N0}";
        }
    }
}
