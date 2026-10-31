using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Stats
{
    public partial class GameplayStatTracker
    {
        private float activeSeconds, runStartingDistance, estimatedSeconds;
        private int echoTasks, echoKills;
        private double taskRemainder, killRemainder;

        public void RecordEchoWork(bool combat)
        {
            if (!RunInProgress) return;
            if (combat) echoKills++; else echoTasks++;
        }

        public void AdvanceEstimatedRun(BackgroundRunEstimator.Step step)
        {
            AddDistance(Mathf.Max(0, (float)step.Distance - CurrentRunDistance));
            CurrentRunDistance = (float)step.Distance;
            HighestDistance = Mathf.Max(HighestDistance, CurrentRunDistance);
            AddDamageTaken((float)step.DamageTaken);
            runStartTime -= (float)step.Seconds;
            runScaledDuration += (float)step.Seconds;
            activeSeconds += (float)step.Seconds;
            estimatedSeconds += (float)step.Seconds;
        }

        public void SetEstimatedRemainders(BackgroundRunEstimator.Result result)
        {
            taskRemainder = result.TaskRemainder;
            killRemainder = result.KillRemainder;
        }
    }
}
