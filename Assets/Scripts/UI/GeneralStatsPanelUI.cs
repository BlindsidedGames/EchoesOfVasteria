using System.Text;
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

        private void OnEnable()
        {
            UpdateTexts();
            UITicker.Instance?.Subscribe(RefreshTick, updateInterval);
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
        }

        private void RefreshTick()
        {
            if (!IsPanelVisible()) return;
            UpdateTexts();
        }

        private bool IsPanelVisible()
        {
            if (references != null && references.distanceLongestTasksText != null)
                return references.distanceLongestTasksText.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        private void UpdateTexts()
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
        }
    }
}