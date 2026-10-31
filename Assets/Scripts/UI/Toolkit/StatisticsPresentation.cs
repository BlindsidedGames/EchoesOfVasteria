using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    // Presentation-only values. No mutation of progression or run accounting.
    public static class StatisticsPresentation
    {
        public static string PreviousHistoryTitle(RunHistoryTotals totals) =>
            ToolkitLocalization.Text("history.previous", "Previous History ({0} runs)", LocaleNumbers.Integer(totals.Runs));

        public static string PreviousHistoryDetails(RunHistoryTotals totals)
            => string.Join("\n", PreviousHistoryFields(totals).Select(field => field.name + ": " + field.value));

        public static IEnumerable<(string name, string value)> PreviousHistoryFields(RunHistoryTotals totals)
        {
            (string, string) Row(string key, string label, double value) => (ToolkitLocalization.Text(key, label), ItemStatisticsPresentation.Number(value));
            yield return (ToolkitLocalization.Text("graph.duration", "Duration"), CalcUtils.FormatTime(totals.Duration));
            yield return Row("graph.distance", "Distance", totals.Distance);
            yield return Row("graph.tasks", "Tasks", totals.Tasks);
            yield return Row("graph.resources", "Resources", totals.Resources);
            yield return Row("graph.bonus-resources", "Bonus resources", totals.BonusResources);
            yield return Row("graph.kills", "Kills", totals.Kills);
            yield return Row("graph.damage-dealt", "Damage dealt", totals.DamageDealt);
            yield return Row("graph.damage-taken", "Damage taken", totals.DamageTaken);
            yield return Row("run.outcome-died", "Died", totals.Deaths);
            yield return Row("run.outcome-reaped", "Reaped", totals.Reaps);
            yield return Row("run.outcome-abandoned", "Abandoned", totals.Abandoned);
            yield return Row("run.outcome-retreated", "Retreated", totals.Runs - totals.Deaths - totals.Reaps - totals.Abandoned);
        }

        public static (string left, string right) General(GameplayStatTracker stats, bool killScaling)
        {
            string Number(double value) => CalcUtils.FormatNumber(value, true);
            return ($"{ToolkitLocalization.Text("statistics.steps", "Steps Taken")}: {Number(stats.DistanceTravelled)}\n{(killScaling ? ToolkitLocalization.Text("statistics.most-kills", "Most Kills") + ": " + Number(stats.MostKillsSingleRun) : ToolkitLocalization.Text("statistics.longest-run", "Longest Run") + ": " + Number(stats.LongestRun))}\n{ToolkitLocalization.Text("statistics.tasks-completed", "Tasks Completed")}: {Number(stats.TasksCompleted)}\n{ToolkitLocalization.Text("statistics.resources-gathered", "Resources Gathered")}: {Number(stats.TotalResourcesGathered)}\n{ToolkitLocalization.Text("statistics.reaping-distance", "Reaping Distance")}: {LocaleNumbers.Integer(stats.MaxRunDistance)}",
                $"{ToolkitLocalization.Text("statistics.kills", "Kills")}: {Number(stats.TotalKills)}\n{ToolkitLocalization.Text("statistics.damage-dealt", "Damage Dealt")}: {Number(stats.DamageDealt)}\n{ToolkitLocalization.Text("statistics.deaths", "Deaths")}: {Number(stats.Deaths)}\n{ToolkitLocalization.Text("statistics.damage-taken", "Damage Taken")}: {Number(stats.DamageTaken)}\n{ToolkitLocalization.Text("statistics.times-reaped", "Times Reaped")}: {stats.TimesReaped}");
        }
        public static (string left, string right) Map(GameData.MapStatistics stats, bool killScaling)
        {
            string Number(double value) => CalcUtils.FormatNumber(value, true);
            return ($"{ToolkitLocalization.Text("statistics.steps", "Steps Taken")}: {Number(stats.StepsAsDouble)}\n{(killScaling ? ToolkitLocalization.Text("statistics.most-kills", "Most Kills") + ": " + Number(stats.MostKillsSingleRun) : ToolkitLocalization.Text("statistics.longest-run", "Longest Run") + ": " + Number(stats.LongestTrekAsDouble))}\n{ToolkitLocalization.Text("statistics.tasks-completed", "Tasks Completed")}: {Number(stats.TasksCompleted)}\n{ToolkitLocalization.Text("statistics.resources-gathered", "Resources Gathered")}: {Number(stats.ResourcesGathered)}",
                $"{ToolkitLocalization.Text("statistics.kills", "Kills")}: {Number(stats.Kills)}\n{ToolkitLocalization.Text("statistics.damage-dealt", "Damage Dealt")}: {Number(stats.DamageDealtAsDouble)}\n{ToolkitLocalization.Text("statistics.deaths", "Deaths")}: {Number(stats.Deaths)}\n{ToolkitLocalization.Text("statistics.damage-taken", "Damage Taken")}: {Number(stats.DamageTakenAsDouble)}");
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
            var resources = ToolkitLocalization.Text("statistics.run-resources", "Resources: {0:N0}", run.ResourcesCollected);
            var bonus = Mathf.FloorToInt((float)run.BonusResourcesCollected);
            if (bonus >= 1) resources += LocaleNumbers.Format($" (+{bonus:N0})");
            var status = run.Abandoned ? ToolkitLocalization.Text("run.outcome-abandoned", "Abandoned") : run.Reaped ? ToolkitLocalization.Text("run.outcome-reaped", "Reaped") : run.Died ? ToolkitLocalization.Text("run.outcome-died", "Died") : ToolkitLocalization.Text("run.outcome-retreated", "Retreated");
            return ToolkitLocalization.Text("statistics.run-details", "Run {0}\nMap: {1}\nDuration: {2}\nDistance: {3:N0}\nTasks: {4:N0}\n{5}\nKills: {6:N0}\nDamage Dealt: {7:N0}\nDamage Taken: {8:N0}\nStatus: {9}", run.RunNumber, string.IsNullOrEmpty(run.MapType) ? ToolkitLocalization.Text("common.unknown", "Unknown") : ToolkitLocalization.Text("map." + run.MapType, run.MapType), CalcUtils.FormatTime(run.Duration), run.Distance, run.TasksCompleted, resources, run.EnemiesKilled, run.DamageDealtAsDouble, run.DamageTakenAsDouble, status);
        }
    }
}
