using System.Text;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Stats;
using TimelessEchoes.UI.Core;
using UnityEngine;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Event-driven general stats panel. Subscribes to GameplayStatTracker events
    /// instead of polling via UITicker.
    /// </summary>
    public class GeneralStatsPanelUI : EventDrivenStatsPanelUI
    {
        [SerializeField] private GeneralStatsUIReferences references;
        private GameplayStatTracker statTracker;

        // StringBuilder for allocation-free text building
        private readonly StringBuilder _sb = new StringBuilder(256);

        private void Awake()
        {
            if (references == null)
                references = GetComponent<GeneralStatsUIReferences>();
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);
        }

        protected override bool IsPanelVisible()
        {
            if (references != null && references.distanceLongestTasksText != null)
                return references.distanceLongestTasksText.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        protected override void SubscribeToEvents()
        {
            // Re-acquire tracker in case it wasn't ready at Awake
            if (statTracker == null)
                statTracker = GameplayStatTracker.Instance;
            
            if (statTracker != null)
            {
                statTracker.OnDistanceAdded += HandleDistanceAdded;
                statTracker.OnRunEnded += HandleRunEnded;
                statTracker.OnTaskCompletedEvent += HandleTaskCompleted;
                statTracker.OnMaxRunDistanceChanged += HandleMaxDistanceChanged;
            }
            
            // Also subscribe to kill events for kill stats
            var killTracker = EnemyKillTracker.Instance;
            if (killTracker != null)
            {
                killTracker.OnKillRegistered += HandleKillRegistered;
            }
        }

        protected override void UnsubscribeFromEvents()
        {
            if (statTracker != null)
            {
                statTracker.OnDistanceAdded -= HandleDistanceAdded;
                statTracker.OnRunEnded -= HandleRunEnded;
                statTracker.OnTaskCompletedEvent -= HandleTaskCompleted;
                statTracker.OnMaxRunDistanceChanged -= HandleMaxDistanceChanged;
            }
            
            var killTracker = EnemyKillTracker.Instance;
            if (killTracker != null)
            {
                killTracker.OnKillRegistered -= HandleKillRegistered;
            }
        }

        // Event handlers - just mark dirty and refresh if visible
        private void HandleDistanceAdded(float _) => OnDataChanged();
        private void HandleRunEnded(bool _) => OnDataChanged();
        private void HandleTaskCompleted() => OnDataChanged();
        private void HandleMaxDistanceChanged(float _) => OnDataChanged();
        private void HandleKillRegistered(TimelessEchoes.Enemies.EnemyData _) => OnDataChanged();

        protected override void RefreshUI()
        {
            if (references == null || statTracker == null) return;

            if (references.distanceLongestTasksText != null)
            {
                var isKillScaling = GameManager.Instance != null && GameManager.Instance.IsKillScalingMode;
                _sb.Clear();
                _sb.Append("Steps Taken: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.DistanceTravelled, true));
                _sb.Append('\n');
                if (isKillScaling)
                {
                    _sb.Append("Most Kills: ");
                    _sb.Append(CalcUtils.FormatNumber(statTracker.MostKillsSingleRun, true));
                }
                else
                {
                    _sb.Append("Longest Run: ");
                    _sb.Append(CalcUtils.FormatNumber(statTracker.LongestRun, true));
                }
                _sb.Append('\n');
                _sb.Append("Tasks Completed: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.TasksCompleted, true));
                _sb.Append('\n');
                _sb.Append("Resources Gathered: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.TotalResourcesGathered, true));
                _sb.Append('\n');
                _sb.Append("Reaping Distance: ");
                _sb.Append(statTracker.MaxRunDistance.ToString("N0"));
                references.distanceLongestTasksText.SetText(_sb);
            }

            if (references.killsDamageDeathsText != null)
            {
                _sb.Clear();
                _sb.Append("Kills: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.TotalKills, true));
                _sb.Append('\n');
                _sb.Append("Damage Dealt: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.DamageDealt, true));
                _sb.Append('\n');
                _sb.Append("Deaths: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.Deaths, true));
                _sb.Append('\n');
                _sb.Append("Damage Taken: ");
                _sb.Append(CalcUtils.FormatNumber(statTracker.DamageTaken, true));
                _sb.Append('\n');
                _sb.Append("Times Reaped: ");
                _sb.Append(statTracker.TimesReaped);
                references.killsDamageDeathsText.SetText(_sb);
            }
            
            isDirty = false;
        }
    }
}
