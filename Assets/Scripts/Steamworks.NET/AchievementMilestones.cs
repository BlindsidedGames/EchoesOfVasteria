using System;

namespace TimelessEchoes
{
    public readonly struct AchievementProgress
    {
        public readonly double HighestDistance, Kilometers;
        public readonly int Tasks, Buffs, Deaths, Kills, EvaLevel, IvanLevel;
        public AchievementProgress(double highestDistance = 0, double kilometers = 0, int tasks = 0,
            int buffs = 0, int deaths = 0, int kills = 0, int evaLevel = 0, int ivanLevel = 0)
        {
            HighestDistance = highestDistance; Kilometers = kilometers; Tasks = tasks;
            Buffs = buffs; Deaths = deaths; Kills = kills; EvaLevel = evaLevel; IvanLevel = ivanLevel;
        }
    }

    /// <summary>Published milestone requirements, independent of the platform reporting API.</summary>
    public static class AchievementMilestones
    {
        public static void ReportEarned(in AchievementProgress progress, Action<string> report)
        {
            if (progress.HighestDistance >= 100f) report("Reached100");
            if (progress.HighestDistance >= 500f) report("Reached1000");
            if (progress.HighestDistance >= 1000f) report("Reached10000");
            if (progress.Kilometers >= 1.0) report("Kilometers1");
            if (progress.Kilometers >= 100.0) report("Kilometers100");
            if (progress.Kilometers >= 1000.0) report("Kilometers1000");
            if (progress.Tasks >= 100) report("Tasks100");
            if (progress.Tasks >= 1000) report("Tasks5k");
            if (progress.Tasks >= 25000) report("Tasks25k");
            if (progress.Buffs >= 10) report("Buffs1");
            if (progress.Buffs >= 100) report("Buffs2");
            if (progress.Buffs >= 1000) report("Buffs3");
            if (progress.Buffs >= 2500) report("Buffs4");
            if (progress.Buffs >= 9001) report("Buffs5");
            if (progress.Kills >= 50) report("Enemies1");
            if (progress.Kills >= 500) report("Enemies2");
            if (progress.Kills >= 2500) report("Enemies3");
            if (progress.Kills >= 5000) report("Enemies4");
            if (progress.Kills >= 10000) report("Enemies5");
            if (progress.Deaths >= 10) report("Die10");
            if (progress.Deaths >= 250) report("Die250");
            if (progress.Deaths >= 1000) report("Die1000");
            if (progress.EvaLevel >= 10) report("EvaLevel10");
            if (progress.EvaLevel >= 50) report("EvaLevel50");
            if (progress.EvaLevel >= 100) report("EvaLevel100");
            if (progress.EvaLevel >= 200) report("EvaLevel200");
            if (progress.EvaLevel >= 500) report("EvaLevel500");
            if (progress.IvanLevel >= 10) report("IvanLevel10");
            if (progress.IvanLevel >= 50) report("IvanLevel50");
            if (progress.IvanLevel >= 100) report("IvanLevel100");
            if (progress.IvanLevel >= 500) report("IvanLevel500");
            if (progress.IvanLevel >= 1000) report("IvanLevel1000");
        }
    }
}
