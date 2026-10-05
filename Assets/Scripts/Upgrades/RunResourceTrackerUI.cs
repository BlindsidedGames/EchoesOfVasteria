using TimelessEchoes.UI.Toolkit;
using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;
using TMPro;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Tracks resources gained during a run and displays them when returning to town.
    /// </summary>
    public class RunResourceTrackerUI : MonoBehaviour
    {
        [SerializeField] private TimelessEchoes.UI.Toolkit.ToolkitRunScreen nativeView;
        [SerializeField] private Transform slotParent;
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private GameObject window;
            [SerializeField] private TMP_Text runSummaryText;

        private readonly Dictionary<Resource, double> amounts = new();
        private readonly Dictionary<Resource, double> bonusAmounts = new();
        private ResourceManager resourceManager;
        private float startingReapDistance;

        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            if (slotParent == null)
                slotParent = transform;
            if (window != null)
                window.SetActive(false);
            ClearSlots();
        }

        private void OnEnable()
        {
            ToolkitLocalization.Changed += RefreshVisibleSummary;
            if (resourceManager != null)
                resourceManager.OnResourceAdded += OnResourceAdded;
        }

        private void OnDisable()
        {
            ToolkitLocalization.Changed -= RefreshVisibleSummary;
            if (resourceManager != null)
                resourceManager.OnResourceAdded -= OnResourceAdded;
        }

        /// <summary>
        ///     Clears all recorded resource amounts. Call this when a run begins.
        /// </summary>
        public void BeginRun()
        {
            nativeView?.HideDetails();
            amounts.Clear();
            bonusAmounts.Clear();
            ClearSlots();
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            startingReapDistance = tracker != null ? tracker.MaxRunDistance : 0f;
            if (window != null)
                window.SetActive(false);
            if (runSummaryText != null)
            {
                runSummaryText.text = string.Empty;
                runSummaryText.gameObject.SetActive(false);
            }
        }

        private void ClearSlots()
        {
            if (!nativeView) UIUtils.ClearChildren(slotParent);
        }

        private void OnResourceAdded(Resource resource, double amount, bool bonus)
        {
            if (resource == null || amount <= 0)
                return;
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (tracker?.RunInProgress != true || resource.DisableAlterEcho)
                return;
            if (amounts.ContainsKey(resource))
                amounts[resource] += amount;
            else
                amounts[resource] = amount;
            if (bonus)
            {
                if (bonusAmounts.ContainsKey(resource))
                    bonusAmounts[resource] += amount;
                else
                    bonusAmounts[resource] = amount;
            }
        }


        public List<KeyValuePair<Resource, double>> GetBaseResourceTotals()
        {
            var results = new List<KeyValuePair<Resource, double>>();
            foreach (var pair in amounts)
            {
                var baseAmount = pair.Value;
                if (bonusAmounts.TryGetValue(pair.Key, out var bonus))
                    baseAmount -= bonus;
                if (baseAmount <= 0d)
                    continue;
                results.Add(new KeyValuePair<Resource, double>(pair.Key, baseAmount));
            }

            return results;
        }

        /// <summary>
        ///     Displays the recorded resources and amounts.
        /// </summary>
        public void ShowWindow()
        {
            if (nativeView && nativeView.IsConfigured)
            {
                nativeView.ShowSummary(BuildSummaryText(), amounts, bonusAmounts);
                return;
            }
            if (slotParent == null || slotPrefab == null)
                return;
            if (amounts.Count == 0)
            {
                if (window != null)
                    window.SetActive(false);
                // Still show summary line even if there are no resource slots
                UpdateSummaryText();
                return;
            }

            ClearSlots();
            foreach (var pair in amounts)
            {
                var slot = Instantiate(slotPrefab, slotParent);
                bonusAmounts.TryGetValue(pair.Key, out var bonus);
                SetupSlot(slot, pair.Key, pair.Value - bonus, bonus);
            }

            UpdateSummaryText();
            if (window != null)
                window.SetActive(true);
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                if (window != null && window.activeSelf)
                    window.SetActive(false);
        }

        private void SetupSlot(ResourceUIReferences slot, Resource res, double amount, double bonus)
        {
            if (slot == null)
                return;
            if (slot.iconImage)
            {
                slot.iconImage.sprite = res ? res.icon : null;
                slot.iconImage.enabled = res != null && res.icon != null;
            }

            if (slot.countText)
            {
                slot.countText.text = FormatNumber(amount, true);
                if (bonus >= 1)
                    slot.countText.text += $" (+{FormatNumber(bonus, true)})";
                slot.countText.gameObject.SetActive(true);
            }
        }

        private void UpdateSummaryText()
        {
            if (runSummaryText == null) return;
            runSummaryText.text = BuildSummaryText();
            runSummaryText.gameObject.SetActive(true);
        }

        private void RefreshVisibleSummary()
        {
            if (window != null && window.activeInHierarchy) UpdateSummaryText();
        }

        public string BuildSummaryText()
        {
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (tracker == null) return string.Empty;

            var lines = new System.Collections.Generic.List<string>();

            // Line 1: Travelled time and steps (session totals)
            var sessionDuration = Mathf.Max(0f, tracker.SessionDuration);
            var sessionSteps = Mathf.Max(0f, tracker.SessionSteps);
            var timeStr = Blindsided.Utilities.CalcUtils.FormatTime(sessionDuration);
            var stepsStr = Blindsided.Utilities.CalcUtils.FormatNumber(sessionSteps, true);
            lines.Add(ToolkitLocalization.Text("run.summary.travel", "You travelled for {0}, walking a total of {1} steps" , timeStr, stepsStr));

            // Line 2: Died/Reaped (session totals)
            var diedTotal = tracker.SessionDeaths;
            var reapedTotal = tracker.SessionReaps;
            lines.Add(ToolkitLocalization.Text("run.summary.deaths", "Died {0} times, Reaped by Carl {1} times" , diedTotal, reapedTotal));

            if (startingReapDistance != 0f && !Mathf.Approximately(startingReapDistance, tracker.MaxRunDistance))
            {
                var oldDist = $"{startingReapDistance:N0}";
                var newDist = $"{tracker.MaxRunDistance:N0}";
                lines.Add(ToolkitLocalization.Text("run.summary.reaping-distance", "Reaping Distance: {0}<sprite=9>{1}" , oldDist, newDist));
            }

            // Line 3 (optional): Retreated with K kills for P% bonus (only if last run retreated)
            var runs = tracker.RecentRuns;
            if (runs != null && runs.Count > 0)
            {
                var last = runs[runs.Count - 1];
                bool retreated = !last.Died && !last.Reaped && !last.Abandoned;
                if (retreated)
                {
                    int kills = Mathf.FloorToInt((float)last.EnemiesKilled);
                    float perKill = TimelessEchoes.GameManager.Instance != null
                        ? TimelessEchoes.GameManager.Instance.BonusPercentPerKill
                        : 2f;
                    float bonusPercent = kills * perKill;
                    lines.Add(ToolkitLocalization.Text("run.summary.retreat", "Retreated with {0} kills for {1:0}% bonus" , kills, bonusPercent));
                }
            }

            return string.Join("\n", lines);
        }
    }
}