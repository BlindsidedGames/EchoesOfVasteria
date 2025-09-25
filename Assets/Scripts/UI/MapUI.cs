using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes;
using TimelessEchoes.Buffs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the map UI with the hero's distance reached.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private Slider distanceSlider;

        // Caches to avoid redundant UI work per refresh
        private int _lastDistanceInt = int.MinValue;
        private int _lastReapInt = int.MinValue;
        private int _lastBaseInt = int.MinValue;
        private bool _lastShowBase;
        private float _lastSliderValue = -1f;
        private bool _lastKillMode;
        private int _lastKillTotal = int.MinValue;
        private int _lastKillThreshold = int.MinValue;
        private int _lastKillLevel = int.MinValue;
        private int _lastKillLevelsPerTier = int.MinValue;

        /// <summary>
        ///     Updates the UI with the distance the hero has reached.
        /// </summary>
        /// <param name="distance">The hero's X position.</param>
        public void UpdateDistance(float distance)
        {
            var manager = GameManager.Instance;
            int totalKills = 0;
            int killsPerLevel = 0;
            int currentKillLevel = 1;
            var hasKillProgress = manager != null &&
                                  manager.TryGetKillProgress(out totalKills, out killsPerLevel, out currentKillLevel) &&
                                  killsPerLevel > 0;

            if (hasKillProgress)
            {
                int perLevel = Mathf.Max(1, killsPerLevel);

                if (!_lastKillMode)
                {
                    _lastKillMode = true;
                    _lastKillTotal = int.MinValue;
                    _lastKillThreshold = int.MinValue;
                    _lastKillLevel = int.MinValue;
                    _lastKillLevelsPerTier = int.MinValue;
                    _lastSliderValue = -1f;
                }

                int threshold = perLevel * currentKillLevel;
                var levelsPerTier = Mathf.Max(1, manager.KillLevelIncreasePerTier);
                if (distanceText != null)
                {
                    if (totalKills != _lastKillTotal || threshold != _lastKillThreshold || currentKillLevel != _lastKillLevel || levelsPerTier != _lastKillLevelsPerTier || Mathf.FloorToInt(distance) != _lastDistanceInt)
                    {
                        var killsText = CalcUtils.FormatNumber(totalKills, true);
                        var thresholdText = CalcUtils.FormatNumber(threshold, true);
                        var distanceInt = Mathf.FloorToInt(distance);
                        var distanceFormatted = distanceInt.ToString("N0");
                        var effectiveLevelTiers = Mathf.Max(0, currentKillLevel - 1);
                        var effectiveLevels = effectiveLevelTiers * levelsPerTier;
                        var effectiveLevelsText = CalcUtils.FormatNumber(effectiveLevels, true);
                        distanceText.text = $"{killsText} / {thresholdText} | +{effectiveLevelsText} Enemy levels\n{distanceFormatted} Distance";
                        _lastKillTotal = totalKills;
                        _lastKillThreshold = threshold;
                        _lastKillLevel = currentKillLevel;
                        _lastKillLevelsPerTier = levelsPerTier;
                        _lastDistanceInt = distanceInt;
                    }
                }

                if (distanceSlider != null)
                {
                    var killsIntoLevel = totalKills % perLevel;
                    var normalized = Mathf.Clamp01((float)killsIntoLevel / perLevel);
                    if (!Mathf.Approximately(normalized, _lastSliderValue))
                    {
                        distanceSlider.value = normalized;
                        _lastSliderValue = normalized;
                    }
                }

                return;
            }

            if (_lastKillMode)
            {
                _lastKillMode = false;
                _lastKillTotal = int.MinValue;
                _lastKillThreshold = int.MinValue;
                _lastKillLevel = int.MinValue;
                _lastKillLevelsPerTier = int.MinValue;
                _lastSliderValue = -1f;
            }

            var buff = BuffManager.Instance;
            var baseReapDistance = Oracle.oracle?.saveData?.General.MaxRunDistance ?? 1f;
            var reapDistance = baseReapDistance * (buff != null ? buff.MaxDistanceMultiplier : 1f) +
                               (buff != null ? buff.MaxDistanceFlatBonus : 0f);
            var isDemo = Oracle.oracle != null && Oracle.oracle.demo;
            if (isDemo) reapDistance = Mathf.Min(reapDistance, 300f);

            if (distanceText != null)
            {
                var currentInt = Mathf.FloorToInt(distance);
                var reapInt = Mathf.FloorToInt(reapDistance);
                var showBase = !Mathf.Approximately(reapDistance, baseReapDistance);
                var baseShown = Mathf.FloorToInt(Mathf.Min(baseReapDistance, isDemo ? 300f : baseReapDistance));

                if (currentInt != _lastDistanceInt || reapInt != _lastReapInt ||
                    showBase != _lastShowBase || (showBase && baseShown != _lastBaseInt))
                {
                    if (showBase)
                        distanceText.text = string.Format("{0} / {1} ({2})",
                            currentInt.ToString("N0"),
                            reapInt.ToString("N0"),
                            baseShown.ToString("N0"));
                    else
                        distanceText.text = string.Format("{0} / {1}",
                            currentInt.ToString("N0"),
                            reapInt.ToString("N0"));

                    _lastDistanceInt = currentInt;
                    _lastReapInt = reapInt;
                    _lastBaseInt = baseShown;
                    _lastShowBase = showBase;
                }
            }

            if (distanceSlider != null)
            {
                var normalized = reapDistance > 0f ? Mathf.Clamp01(distance / reapDistance) : 0f;
                if (!Mathf.Approximately(normalized, _lastSliderValue))
                {
                    distanceSlider.value = normalized;
                    _lastSliderValue = normalized;
                }
            }
        }
    }
}


