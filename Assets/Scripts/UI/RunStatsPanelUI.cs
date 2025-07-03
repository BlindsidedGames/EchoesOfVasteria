using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using TimelessEchoes.References.UI;

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
        [SerializeField] private RunStatUIReferences runStatUI;
        [SerializeField] private Vector2 statOffset = Vector2.zero;

        private void Awake()
        {
            if (statTracker == null)
                statTracker = FindFirstObjectByType<GameplayStatTracker>();
            if (runStatUI == null)
                runStatUI = FindFirstObjectByType<RunStatUIReferences>(true);
            foreach (var bar in runBars)
            {
                if (bar != null)
                {
                    bar.PointerEnter += OnBarEnter;
                    bar.PointerExit += OnBarExit;
                }
            }
            if (runStatUI != null)
                runStatUI.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var bar in runBars)
            {
                if (bar != null)
                {
                    bar.PointerEnter -= OnBarEnter;
                    bar.PointerExit -= OnBarExit;
                }
            }
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        private void OnDisable()
        {
            if (runStatUI != null)
                runStatUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateUI();
        }

        private void OnBarEnter(RunBarUI bar)
        {
            if (runStatUI == null || statTracker == null || bar == null)
                return;

            var runs = statTracker.RecentRuns;
            int index = bar.BarIndex;
            if (index < 0 || index >= runs.Count)
            {
                runStatUI.gameObject.SetActive(false);
                return;
            }

            var record = runs[index];

            Vector3 pos = bar.transform.position;
            pos.x += statOffset.x;
            pos.y = Input.mousePosition.y + statOffset.y;
            runStatUI.transform.position = pos;

            if (runStatUI.runIdText != null)
                runStatUI.runIdText.text = $"Run {index + 1}";

            if (runStatUI.distanceTasksResourcesText != null)
            {
                string dist = CalcUtils.FormatNumber(record.Distance, true);
                string tasks = CalcUtils.FormatNumber(record.TasksCompleted, true);
                string resources = CalcUtils.FormatNumber(record.ResourcesCollected, true);
                runStatUI.distanceTasksResourcesText.text =
                    $"Distance: {dist}\nTasks: {tasks}\nResources: {resources}";
            }

            if (runStatUI.killsDamageDoneDamageTakenText != null)
            {
                string kills = CalcUtils.FormatNumber(record.EnemiesKilled, true);
                string dealt = CalcUtils.FormatNumber(record.DamageDealt, true);
                string taken = CalcUtils.FormatNumber(record.DamageTaken, true);
                runStatUI.killsDamageDoneDamageTakenText.text =
                    $"Kills: {kills}\nDamage Dealt: {dealt}\nDamage Taken: {taken}";
            }

            runStatUI.gameObject.SetActive(true);
        }

        private void OnBarExit(RunBarUI bar)
        {
            if (runStatUI != null)
                runStatUI.gameObject.SetActive(false);
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
                averageRunSlider.value = Mathf.Clamp01(avgRatio);
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
                    runBars[i].BarIndex = index;
                }
                else
                {
                    runBars[i].SetFill(0f);
                    runBars[i].BarIndex = -1;
                }
            }
        }
    }
}
