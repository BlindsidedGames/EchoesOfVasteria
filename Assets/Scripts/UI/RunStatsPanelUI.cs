using Blindsided.Utilities;
using TimelessEchoes.References.UI;
using TimelessEchoes.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;

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
        private GameplayStatTracker statTracker;
        [SerializeField] private RunStatUIReferences runStatUI;
        [SerializeField] private Vector2 statOffset = Vector2.zero;
        [SerializeField] private Color deathBarColor = Color.red;
        [SerializeField] private Color retreatBarColor = Color.green;
        [SerializeField] private Color abandonedBarColor = Color.gray;
        [SerializeField] private Color reapedBarColor = Color.magenta;
        [SerializeField] private Color resourcesDeathBarColor = Color.red;
        [SerializeField] private Color resourcesRetreatBarColor = Color.green;
        [SerializeField] private Color resourcesAbandonedBarColor = Color.gray;
        [SerializeField] private Color resourcesReapedBarColor = Color.magenta;
        [SerializeField] private Color resourcesBonusBarColor = Color.yellow;
        [SerializeField] private Color killsDeathBarColor = Color.red;
        [SerializeField] private Color killsRetreatBarColor = Color.green;
        [SerializeField] private Color killsAbandonedBarColor = Color.gray;
        [SerializeField] private Color killsReapedBarColor = Color.magenta;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        public enum GraphMode
        {
            Distance,
            Duration,
            Resources,
            Kills
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
            switch (graphMode)
            {
                case GraphMode.Distance:
                    graphLabelText.text = "Distance from Town";
                    break;
                case GraphMode.Duration:
                    graphLabelText.text = "Run Time";
                    break;
                case GraphMode.Resources:
                    graphLabelText.text = "Resources Gathered";
                    break;
                case GraphMode.Kills:
                    graphLabelText.text = "Enemies Killed";
                    break;
            }
        }

        private void Awake()
        {
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
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
            nextUpdateTime = Time.unscaledTime + updateInterval;
        }

        private void OnDisable()
        {
            if (runStatUI != null)
                runStatUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextUpdateTime)
            {
                UpdateUI();
                nextUpdateTime = Time.unscaledTime + updateInterval;
            }
        }

        private void OnBarEnter(RunBarUI bar, PointerEventData eventData)
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
            var screenPoint = eventData != null ? (Vector3)eventData.position :
                (Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero);
            if (cam != null)
                screenPoint.z = Mathf.Abs(cam.transform.position.z - runStatUI.transform.position.z);
            var mouseWorld = cam != null
                ? cam.ScreenToWorldPoint(screenPoint)
                : runStatUI.transform.position;

            pos.y = mouseWorld.y + statOffset.y;
            runStatUI.transform.position = pos;

            if (runStatUI.runIdText != null)
                runStatUI.runIdText.text = $"Trek {record.RunNumber}";

            if (runStatUI.distanceTasksResourcesText != null)
            {
                var time = CalcUtils.FormatTime(record.Duration);
                var dist = CalcUtils.FormatNumber(record.Distance, true);
                var tasks = CalcUtils.FormatNumber(record.TasksCompleted, true);
                var resources = CalcUtils.FormatNumber(record.ResourcesCollected, true);
                var bonus = CalcUtils.FormatNumber(record.BonusResourcesCollected, true);
                runStatUI.distanceTasksResourcesText.text =
                    $"Duration: {time}\nDistance: {dist}\nTasks: {tasks}\nResources: {resources} (+{bonus})";
            }


            if (runStatUI.killsDamageDoneDamageTakenText != null)
            {
                var kills = CalcUtils.FormatNumber(record.EnemiesKilled, true);
                var dealt = CalcUtils.FormatNumber(record.DamageDealt, true);
                var taken = CalcUtils.FormatNumber(record.DamageTaken, true);
                runStatUI.killsDamageDoneDamageTakenText.text =
                    $"Kills: {kills}\nDamage Dealt: {dealt}\nDamage Taken: {taken}";
            }

            if (runStatUI.statusText != null)
            {
                if (record.Abandoned)
                    runStatUI.statusText.text = "Status: Abandoned";
                else if (record.Reaped)
                    runStatUI.statusText.text = "Status: Reaped";
                else
                    runStatUI.statusText.text = record.Died ? "Status: Died" : "Status: Retreated";
            }

            runStatUI.gameObject.SetActive(true);
        }

        private void OnBarExit(RunBarUI bar, PointerEventData eventData)
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
            else if (graphMode == GraphMode.Duration)
            {
                longest = 0f;
                double sum = 0f;
                foreach (var r in runs)
                {
                    if (r.Duration > longest)
                        longest = r.Duration;
                    sum += r.Duration;
                }
                average = runs.Count > 0 ? sum / runs.Count : 0f;
            }
            else if (graphMode == GraphMode.Resources)
            {
                longest = 0f;
                double sum = 0f;
                foreach (var r in runs)
                {
                    var total = r.ResourcesCollected;
                    if (total > longest)
                        longest = total;
                    sum += total;
                }
                average = runs.Count > 0 ? sum / runs.Count : 0f;
            }
            else
            {
                longest = 0f;
                double sum = 0f;
                foreach (var r in runs)
                {
                    if (r.EnemiesKilled > longest)
                        longest = r.EnemiesKilled;
                    sum += r.EnemiesKilled;
                }
                average = runs.Count > 0 ? sum / runs.Count : 0f;
            }

            if (longestRunText != null)
            {
                longestRunText.text = graphMode == GraphMode.Duration
                    ? CalcUtils.FormatTime(longest)
                    : CalcUtils.FormatNumber(longest, true);
            }

            if (averageRunText != null)
            {
                averageRunText.text = graphMode == GraphMode.Duration
                    ? CalcUtils.FormatTime(average)
                    : CalcUtils.FormatNumber(average, true);
            }

            if (averageRunSlider != null)
            {
                var avgRatio = longest > 0f ? average / longest : 0f;
                averageRunSlider.value = Mathf.Clamp01((float)avgRatio);
                if (averageRunSlider.value < 0.05f)
                    averageRunSlider.gameObject.SetActive(false);
                else
                    averageRunSlider.gameObject.SetActive(true);
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
                    double value;
                    double overlay = 0f;
                    if (graphMode == GraphMode.Distance)
                        value = runs[index].Distance;
                    else if (graphMode == GraphMode.Duration)
                        value = runs[index].Duration;
                    else if (graphMode == GraphMode.Resources)
                    {
                        value = runs[index].ResourcesCollected;
                        overlay = runs[index].BonusResourcesCollected;
                    }
                    else
                        value = runs[index].EnemiesKilled;
                    var ratio = longest > 0f ? value / longest : 0f;
                    runBars[i].SetFill((float)ratio);
                    var overlayRatio = longest > 0f ? overlay / longest : 0f;
                    runBars[i].SetOverlayFill((float)overlayRatio);
                    runBars[i].BarIndex = index;
                    Color color;
                    if (graphMode == GraphMode.Distance || graphMode == GraphMode.Duration)
                    {
                        if (runs[index].Abandoned)
                            color = abandonedBarColor;
                        else if (runs[index].Reaped)
                            color = reapedBarColor;
                        else
                            color = runs[index].Died ? deathBarColor : retreatBarColor;
                    }
                    else if (graphMode == GraphMode.Resources)
                    {
                        if (runs[index].Abandoned)
                            color = resourcesAbandonedBarColor;
                        else if (runs[index].Reaped)
                            color = resourcesReapedBarColor;
                        else
                            color = runs[index].Died ? resourcesDeathBarColor : resourcesRetreatBarColor;
                        runBars[i].OverlayColor = resourcesBonusBarColor;
                    }
                    else
                    {
                        if (runs[index].Abandoned)
                            color = killsAbandonedBarColor;
                        else if (runs[index].Reaped)
                            color = killsReapedBarColor;
                        else
                            color = runs[index].Died ? killsDeathBarColor : killsRetreatBarColor;
                    }
                    runBars[i].FillColor = color;
                    if (graphMode != GraphMode.Resources)
                        runBars[i].SetOverlayFill(0f);
                }
                else
                {
                    runBars[i].SetFill(0f);
                    runBars[i].SetOverlayFill(0f);
                    runBars[i].BarIndex = -1;
                    runBars[i].FillColor = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
    }
}