using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;
using TimelessEchoes.Stats;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Displays recent run statistics on a bar graph and text fields.
    /// </summary>
    public class RunStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private RunBarUI[] runBars = new RunBarUI[50];
        [SerializeField] private TMP_Text longestRunText;
        [SerializeField] private TMP_Text averageRunText;
        [SerializeField] private Slider averageRunSlider;
        [SerializeField] private GameplayStatTracker statTracker;

        private void Awake()
        {
            if (statTracker == null)
                statTracker = FindFirstObjectByType<GameplayStatTracker>();
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (statTracker == null || runBars == null)
                return;

            float longest = statTracker.LongestRun;
            if (longestRunText != null)
                longestRunText.text = CalcUtils.FormatNumber(longest, true);

            if (averageRunText != null)
                averageRunText.text = CalcUtils.FormatNumber(statTracker.AverageRun, true);

            if (averageRunSlider != null)
            {
                float avgRatio = longest > 0f ? statTracker.AverageRun / longest : 0f;
                averageRunSlider.value = avgRatio * 100f;
            }

            var runs = statTracker.RecentRuns;
            int barCount = runBars.Length;
            for (int i = 0; i < barCount; i++)
            {
                if (runBars[i] == null) continue;

                int index = runs.Count - barCount + i;
                if (index >= 0 && index < runs.Count)
                {
                    float dist = runs[index].Distance;
                    float ratio = longest > 0f ? dist / longest : 0f;
                    runBars[i].SetFill(ratio);
                }
                else
                {
                    runBars[i].SetFill(0f);
                }
            }
        }
    }
}
