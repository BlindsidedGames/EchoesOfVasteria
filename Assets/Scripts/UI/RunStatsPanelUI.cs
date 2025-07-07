using Blindsided.Utilities;
using TimelessEchoes.References.UI;
using TimelessEchoes.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Displays recent run statistics on a bar graph and text fields.
    /// </summary>
    public class RunStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private RunBarUI[] runBars = new RunBarUI[50];
        [SerializeField] private TMP_Text longestRunText;
        [SerializeField] private TMP_Text averageRunText;
        [SerializeField] private Slider averageRunSlider;
        [SerializeField] private TMP_Text oldestRunNumberText;
        [SerializeField] private TMP_Text middleRunNumberText;
        [SerializeField] private TMP_Text mostRecentRunNumberText;
        [SerializeField] private TMP_Text graphLabelText;
        [SerializeField] private GameplayStatTracker statTracker;
        [SerializeField] private RunStatUIReferences runStatUI;
        [SerializeField] private Vector2 statOffset = Vector2.zero;
        [SerializeField] private Color deathBarColor = Color.red;
        [SerializeField] private Color retreatBarColor = Color.green;

        public enum GraphMode
        {
            Distance,
            Resources
        }

        [SerializeField] private GraphMode graphMode = GraphMode.Distance;

        public void SetGraphMode(GraphMode mode)
        {
            graphMode = mode;
            UpdateGraphLabel();
            UpdateUI();
        }

        private void UpdateGraphLabel()
        {
            if (graphLabelText == null) return;
            graphLabelText.text = graphMode == GraphMode.Distance
                ? "Distance from Town"
                : "Resources Gathered";
        }

        private void Awake()
        {
            if (statTracker == null)
                statTracker = FindFirstObjectByType<GameplayStatTracker>();
            if (runStatUI == null)
                runStatUI = FindFirstObjectByType<RunStatUIReferences>();
            foreach (var bar in runBars)
                if (bar != null)
                {
                    bar.PointerEnter += OnBarEnter;
                    bar.PointerExit += OnBarExit;
                }

            if (runStatUI != null)
                runStatUI.gameObject.SetActive(false);

            UpdateGraphLabel();
        }

        private void OnDestroy()
        {
            foreach (var bar in runBars)
                if (bar != null)
                {
                    bar.PointerEnter -= OnBarEnter;
                    bar.PointerExit -= OnBarExit;
                }
        }

        private void OnEnable()
        {
            UpdateGraphLabel();
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
            var index = bar.BarIndex;
            if (index < 0 || index >= runs.Count)
            {
                runStatUI.gameObject.SetActive(false);
                return;
            }

            var record = runs[index];

            var pos = bar.transform.position;
            pos.x += statOffset.x;

            var canvas = runStatUI.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;
            var screenPoint = Input.mousePosition;
            if (cam != null)
                screenPoint.z = Mathf.Abs(cam.transform.position.z - runStatUI.transform.position.z);
            var mouseWorld = cam != null
                ? cam.ScreenToWorldPoint(screenPoint)
                : runStatUI.transform.position;

            pos.y = mouseWorld.y + statOffset.y;
            runStatUI.transform.position = pos;

            if (runStatUI.runIdText != null)
                runStatUI.runIdText.text = $"Run {record.RunNumber}";

            if (runStatUI.distanceTasksResourcesText != null)
            {
                var time = CalcUtils.FormatTime(record.Duration);
                var dist = CalcUtils.FormatNumber(record.Distance, true);
                var tasks = CalcUtils.FormatNumber(record.TasksCompleted, true);
                var resources = CalcUtils.FormatNumber(record.ResourcesCollected, true);
                runStatUI.distanceTasksResourcesText.text =
                    $"Duration: {time}\nDistance: {dist}\nTasks: {tasks}\nResources: {resources}";
            }

            if (runStatUI.killsDamageDoneDamageTakenText != null)
            {
                var kills = CalcUtils.FormatNumber(record.EnemiesKilled, true);
                var dealt = CalcUtils.FormatNumber(record.DamageDealt, true);
                var taken = CalcUtils.FormatNumber(record.DamageTaken, true);
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

            var runs = statTracker.RecentRuns;

            double longest;
            double average;

            if (graphMode == GraphMode.Distance)
            {
                longest = statTracker.LongestRun;
                average = statTracker.AverageRun;
            }
            else
            {
                longest = 0f;
                double sum = 0f;
                foreach (var r in runs)
                {
                    if (r.ResourcesCollected > longest)
                        longest = r.ResourcesCollected;
                    sum += r.ResourcesCollected;
                }
                average = runs.Count > 0 ? sum / runs.Count : 0f;
            }

            if (longestRunText != null)
                longestRunText.text = CalcUtils.FormatNumber(longest, true);

            if (averageRunText != null)
                averageRunText.text = CalcUtils.FormatNumber(average, true);

            if (averageRunSlider != null)
            {
                var avgRatio = longest > 0f ? average / longest : 0f;
                averageRunSlider.value = Mathf.Clamp01((float)avgRatio);
            }

            if (runs.Count > 0)
            {
                var oldestNumber = runs[0].RunNumber;
                var newestNumber = runs[runs.Count - 1].RunNumber;

                if (oldestRunNumberText != null)
                    oldestRunNumberText.text = oldestNumber.ToString();

                if (middleRunNumberText != null)
                {
                    var middleNumber = Mathf.FloorToInt((oldestNumber + newestNumber) * 0.5f) + 1;
                    if (newestNumber >= 50)
                        middleRunNumberText.text = middleNumber.ToString();
                    else
                        middleRunNumberText.text = string.Empty;
                }

                if (mostRecentRunNumberText != null)
                    mostRecentRunNumberText.text = newestNumber.ToString();
            }

            var barCount = runBars.Length;
            for (var i = 0; i < barCount; i++)
            {
                if (runBars[i] == null) continue;

                var index = runs.Count - barCount + i;
                if (index >= 0 && index < runs.Count)
                {
                    double value = graphMode == GraphMode.Distance
                        ? runs[index].Distance
                        : runs[index].ResourcesCollected;
                    var ratio = longest > 0f ? value / longest : 0f;
                    runBars[i].SetFill((float)ratio);
                    runBars[i].BarIndex = index;
                    var color = runs[index].Died ? deathBarColor : retreatBarColor;
                    runBars[i].FillColor = color;
                }
                else
                {
                    runBars[i].SetFill(0f);
                    runBars[i].BarIndex = -1;
                    runBars[i].FillColor = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
    }
}