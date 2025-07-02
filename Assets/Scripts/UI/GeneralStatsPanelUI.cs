using UnityEngine;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using Blindsided.Utilities;

namespace TimelessEchoes.UI
{
    public class GeneralStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private GeneralStatsUIReferences references;
        [SerializeField] private GameplayStatTracker statTracker;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<GeneralStatsUIReferences>();
            if (statTracker == null)
                statTracker = FindFirstObjectByType<GameplayStatTracker>();
        }

        private void OnEnable()
        {
            UpdateTexts();
        }

        private void Update()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            if (references == null || statTracker == null) return;

            if (references.distanceLongestTasksText != null)
            {
                string dist = CalcUtils.FormatNumber(statTracker.DistanceTravelled, true, 400f, false);
                string longest = CalcUtils.FormatNumber(statTracker.LongestRun, true, 400f, false);
                string shortest = CalcUtils.FormatNumber(statTracker.ShortestRun, true, 400f, false);
                string average = CalcUtils.FormatNumber(statTracker.AverageRun, true, 400f, false);
                string tasks = CalcUtils.FormatNumber(statTracker.TasksCompleted, true, 400f, false);
                string resources = CalcUtils.FormatNumber(statTracker.TotalResourcesGathered, true, 400f, false);
                references.distanceLongestTasksText.text =
                    $"Distance Travelled: {dist}\nLongest Run: {longest}\nShortest Run: {shortest}\nAverage Run: {average}\nTasks Completed: {tasks}\nResources Gathered: {resources}";
            }

            if (references.killsDamageDeathsText != null)
            {
                string kills = CalcUtils.FormatNumber(statTracker.TotalKills, true, 400f, false);
                string dealt = CalcUtils.FormatNumber(statTracker.DamageDealt, true, 400f, false);
                string deaths = CalcUtils.FormatNumber(statTracker.Deaths, true, 400f, false);
                string taken = CalcUtils.FormatNumber(statTracker.DamageTaken, true, 400f, false);
                references.killsDamageDeathsText.text = $"Kills: {kills}\nDamage Dealt: {dealt}\nDeaths: {deaths}\nDamage Taken: {taken}";
            }
        }
    }
}
