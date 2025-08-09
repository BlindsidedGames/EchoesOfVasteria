using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using UnityEngine;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.UI
{
    public class GeneralStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private GeneralStatsUIReferences references;
        private GameplayStatTracker statTracker;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<GeneralStatsUIReferences>();
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);
        }

        private void OnEnable()
        {
            UpdateTexts();
            nextUpdateTime = Time.unscaledTime + updateInterval;
            UITicker.Instance?.Subscribe(RefreshTick, updateInterval);
        }

        private void Update()
        {
            if (UITicker.Instance != null) return;
            if (Time.unscaledTime >= nextUpdateTime)
            {
                UpdateTexts();
                nextUpdateTime = Time.unscaledTime + updateInterval;
            }
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
        }

        private void RefreshTick()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            if (references == null || statTracker == null) return;

            if (references.distanceLongestTasksText != null)
            {
                var dist = CalcUtils.FormatNumber(statTracker.DistanceTravelled, true);
                var longest = CalcUtils.FormatNumber(statTracker.LongestRun, true);
                var shortest = CalcUtils.FormatNumber(statTracker.ShortestRun, true);
                var average = CalcUtils.FormatNumber(statTracker.AverageRun, true);
                var tasks = CalcUtils.FormatNumber(statTracker.TasksCompleted, true);
                var resources = CalcUtils.FormatNumber(statTracker.TotalResourcesGathered, true);
                var reapDist = CalcUtils.FormatNumber(statTracker.MaxRunDistance, true);
                references.distanceLongestTasksText.text =
                    $"Steps Taken: {dist}\nLongest Trek: {longest}\nTasks Completed: {tasks}\nResources Gathered: {resources}\nReaping Distance: {reapDist}";
            }

            if (references.killsDamageDeathsText != null)
            {
                var kills = CalcUtils.FormatNumber(statTracker.TotalKills, true);
                var dealt = CalcUtils.FormatNumber(statTracker.DamageDealt, true);
                var deaths = CalcUtils.FormatNumber(statTracker.Deaths, true);
                var taken = CalcUtils.FormatNumber(statTracker.DamageTaken, true);
                var reaps = statTracker.TimesReaped.ToString();
                references.killsDamageDeathsText.text =
                    $"Kills: {kills}\nDamage Dealt: {dealt}\nDeaths: {deaths}\nDamage Taken: {taken}\nTimes Reaped: {reaps}";
            }
        }
    }
}