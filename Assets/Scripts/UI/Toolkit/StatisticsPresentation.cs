using System.Collections.Generic;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    // Presentation-only values. No mutation of progression or run accounting.
    public static class StatisticsPresentation
    {
        public static (string left, string right) General(GameplayStatTracker stats, bool killScaling)
        {
            string Number(double value) => CalcUtils.FormatNumber(value, true);
            return ($"Steps Taken: {Number(stats.DistanceTravelled)}\n{(killScaling ? "Most Kills: " + Number(stats.MostKillsSingleRun) : "Longest Run: " + Number(stats.LongestRun))}\nTasks Completed: {Number(stats.TasksCompleted)}\nResources Gathered: {Number(stats.TotalResourcesGathered)}\nReaping Distance: {stats.MaxRunDistance:N0}",
                $"Kills: {Number(stats.TotalKills)}\nDamage Dealt: {Number(stats.DamageDealt)}\nDeaths: {Number(stats.Deaths)}\nDamage Taken: {Number(stats.DamageTaken)}\nTimes Reaped: {stats.TimesReaped}");
        }
        public static (string left, string right) Map(GameData.MapStatistics stats, bool killScaling)
        {
            string Number(double value) => CalcUtils.FormatNumber(value, true);
            return ($"Steps Taken: {Number(stats.StepsAsDouble)}\n{(killScaling ? "Most Kills: " + Number(stats.MostKillsSingleRun) : "Longest Run: " + Number(stats.LongestTrekAsDouble))}\nTasks Completed: {Number(stats.TasksCompleted)}\nResources Gathered: {Number(stats.ResourcesGathered)}",
                $"Kills: {Number(stats.Kills)}\nDamage Dealt: {Number(stats.DamageDealtAsDouble)}\nDeaths: {Number(stats.Deaths)}\nDamage Taken: {Number(stats.DamageTakenAsDouble)}");
        }
        public static double Value(GameData.RunRecord run, RunStatsPanelUI.GraphMode mode) => mode switch
        {
            RunStatsPanelUI.GraphMode.Duration => run.Duration,
            RunStatsPanelUI.GraphMode.Resources => run.ResourcesCollected,
            RunStatsPanelUI.GraphMode.Kills => run.EnemiesKilled,
            _ => run.Distance
        };
        public static (double maximum, double average) Range(IReadOnlyList<GameData.RunRecord> runs, RunStatsPanelUI.GraphMode mode)
        {
            double maximum = 0, sum = 0;
            foreach (var run in runs) { var value = Value(run, mode); maximum = System.Math.Max(maximum, value); sum += value; }
            return (maximum, runs.Count > 0 ? sum / runs.Count : 0);
        }
        public static string RunDetails(GameData.RunRecord run)
        {
            var resources = $"Resources: {run.ResourcesCollected:N0}";
            var bonus = Mathf.FloorToInt((float)run.BonusResourcesCollected);
            if (bonus >= 1) resources += $" (+{bonus:N0})";
            var status = run.Abandoned ? "Abandoned" : run.Reaped ? "Reaped" : run.Died ? "Died" : "Retreated";
            return $"Run {run.RunNumber}\nMap: {(string.IsNullOrEmpty(run.MapType) ? "Unknown" : run.MapType)}\nDuration: {CalcUtils.FormatTime(run.Duration)}\nDistance: {run.Distance:N0}\nTasks: {run.TasksCompleted:N0}\n{resources}\nKills: {run.EnemiesKilled:N0}\nDamage Dealt: {run.DamageDealtAsDouble:N0}\nDamage Taken: {run.DamageTakenAsDouble:N0}\nStatus: {status}";
        }
    }
}
