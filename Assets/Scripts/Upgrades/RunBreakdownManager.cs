using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using TimelessEchoes;
using TimelessEchoes.Stats;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Manages run breakdown entries and keeps them updated while a run is active.
    /// </summary>
    public class RunBreakdownManager : MonoBehaviour
    {
        private const float UpdateIntervalSeconds = 1f;

        [SerializeField] private RunBreakdownEntryUI entryPrefab;
        [SerializeField] private Transform entryParent;
        [SerializeField] private TMP_Text runtimeText;
        [SerializeField] private TMP_Text closeHintText;
        [SerializeField] private TMP_Text performanceText;
        [SerializeField] private GameObject windowRoot;

        private readonly Dictionary<Resource, EntryData> entriesByResource = new();
        private readonly List<EntryData> orderedEntries = new();
        private readonly Queue<RunBreakdownEntryUI> entryPool = new();

        private ResourceManager resourceManager;
        private GameplayStatTracker statTracker;

        private float runStartTime;
        private bool runActive;
        private float updateTimer;
        private bool subscribedToRunStarted;

        private class EntryData
        {
            public EntryData(Resource resource, RunBreakdownEntryUI view)
            {
                Resource = resource;
                View = view;
            }

            public Resource Resource { get; }
            public RunBreakdownEntryUI View { get; }
            public double Amount { get; set; }
        }

        private void OnEnable()
        {
            Subscribe();
            SyncActiveRunState();
        }

        private void Start()
        {
            AttemptAttachStatTracker();
        }

        private void Update()
        {
            AttemptAttachStatTracker();

            if (WasRightClickPressed())
                TryCloseOnRightClick();

            if (!runActive)
                return;

            var elapsed = Time.time - runStartTime;
            UpdateRuntimeLabel(elapsed);
            UpdateSummaryLabels(elapsed);

            updateTimer += Time.unscaledDeltaTime;
            if (updateTimer >= UpdateIntervalSeconds)
            {
                updateTimer = 0f;
                RefreshEntries(elapsed);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var rm = ResourceManager.Instance;
            if (rm != null && resourceManager != rm)
            {
                if (resourceManager != null)
                    resourceManager.OnResourceAdded -= HandleResourceAdded;
                resourceManager = rm;
            }

            if (resourceManager != null)
                resourceManager.OnResourceAdded += HandleResourceAdded;

            if (!subscribedToRunStarted)
            {
                Blindsided.EventHandler.OnRunStarted += HandleRunStarted;
                subscribedToRunStarted = true;
            }

            AttemptAttachStatTracker();
        }

        private void Unsubscribe()
        {
            if (resourceManager != null)
            {
                resourceManager.OnResourceAdded -= HandleResourceAdded;
                resourceManager = null;
            }

            if (subscribedToRunStarted)
            {
                Blindsided.EventHandler.OnRunStarted -= HandleRunStarted;
                subscribedToRunStarted = false;
            }

            if (statTracker != null)
            {
                statTracker.OnRunEnded -= HandleRunEnded;
                statTracker = null;
            }
        }

        private void SyncActiveRunState()
        {
            var elapsed = 0f;
            if (statTracker != null && statTracker.RunInProgress)
            {
                runActive = true;
                elapsed = statTracker.CurrentRunElapsedSeconds;
                runStartTime = Time.time - elapsed;
            }
            else
            {
                runActive = false;
            }

            updateTimer = 0f;
            UpdateRuntimeLabel(elapsed);
            UpdateSummaryLabels(elapsed);

            if (runActive && orderedEntries.Count > 0)
                RefreshEntries(elapsed);
        }
        private void AttemptAttachStatTracker()
        {
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
                return;

            if (statTracker == tracker)
            {
                if (runActive != tracker.RunInProgress)
                    SyncActiveRunState();
                return;
            }

            if (statTracker != null)
                statTracker.OnRunEnded -= HandleRunEnded;

            statTracker = tracker;
            statTracker.OnRunEnded += HandleRunEnded;
            SyncActiveRunState();
        }

        private void HandleRunStarted()
        {
            ClearEntries();
            SyncActiveRunState();
        }

        private void HandleRunEnded(bool died)
        {
            var duration = statTracker != null ? statTracker.LastRunDuration : Time.time - runStartTime;
            RefreshEntries(duration);
            UpdateSummaryLabels(duration);
            runActive = false;
            updateTimer = 0f;

            if (died)
                CloseWindow();
        }

        private void HandleResourceAdded(Resource resource, double amount, bool bonus)
        {
            if (!runActive || resource == null || amount <= 0)
                return;

            if (resource.DisableAlterEcho || bonus)
                return;

            if (statTracker != null && !statTracker.RunInProgress)
                return;

            if (!entriesByResource.TryGetValue(resource, out var entry))
            {
                var view = GetEntryView();
                if (view == null)
                    return;

                view.Initialize(resource);
                entry = new EntryData(resource, view);
                entriesByResource[resource] = entry;

                var insertIndex = FindInsertIndex(resource.resourceID);
                orderedEntries.Insert(insertIndex, entry);
                ApplyEntrySiblingIndex(entry, insertIndex);
            }

            entry.Amount += amount;
            RefreshEntry(entry, Math.Max(Time.time - runStartTime, 0.0001f));
        }

        private int FindInsertIndex(int resourceId)
        {
            var low = 0;
            var high = orderedEntries.Count;
            while (low < high)
            {
                var mid = (low + high) / 2;
                var midEntry = orderedEntries[mid];
                var midResource = midEntry?.Resource;
                var midId = midResource != null ? midResource.resourceID : int.MaxValue;

                if (midId < resourceId)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private void ApplyEntrySiblingIndex(EntryData entry, int insertIndex)
        {
            if (entry?.View == null)
                return;

            var parent = entryParent != null ? entryParent : transform;
            var childCount = parent.childCount;
            if (childCount == 0)
                return;

            var clampedIndex = Mathf.Clamp(insertIndex, 0, childCount - 1);
            entry.View.transform.SetSiblingIndex(clampedIndex);
        }

        private RunBreakdownEntryUI GetEntryView()
        {
            var parent = entryParent != null ? entryParent : transform;

            RunBreakdownEntryUI view;
            if (entryPool.Count > 0)
            {
                view = entryPool.Dequeue();
            }
            else
            {
                if (entryPrefab == null)
                {
                    Debug.LogError("RunBreakdownManager missing entry prefab.", this);
                    return null;
                }

                view = Instantiate(entryPrefab, parent);
            }

            if (view.transform.parent != parent)
                view.transform.SetParent(parent, false);

            view.gameObject.SetActive(true);
            return view;
        }

        private void ReturnEntryToPool(RunBreakdownEntryUI view)
        {
            if (view == null)
                return;

            var parent = entryParent != null ? entryParent : transform;
            view.gameObject.SetActive(false);
            if (view.transform.parent != parent)
                view.transform.SetParent(parent, false);
            entryPool.Enqueue(view);
        }

        private void ClearEntries()
        {
            foreach (var entry in orderedEntries)
                ReturnEntryToPool(entry.View);

            orderedEntries.Clear();
            entriesByResource.Clear();
        }

        private void RefreshEntries(double elapsedSeconds)
        {
            var safeElapsed = Math.Max(elapsedSeconds, 0.0001d);
            foreach (var entry in orderedEntries)
                RefreshEntry(entry, safeElapsed);
        }

        private void RefreshEntry(EntryData entry, double elapsedSeconds)
        {
            if (entry?.View == null)
                return;

            var safeElapsed = Math.Max(elapsedSeconds, 0.0001d);
            var earned = entry.Amount;
            var perMinute = earned > 0 ? earned * 60d / safeElapsed : 0d;
            var retreatMultiplier = GetRetreatMultiplier();
            var projectedTotal = earned * (1d + retreatMultiplier);
            var projectedPerMinute = perMinute * (1d + retreatMultiplier);

            entry.View.UpdateDisplay(earned, projectedTotal, perMinute, projectedPerMinute);
        }

        private double GetRetreatMultiplier()
        {
            if (statTracker == null)
                return 0d;

            var kills = statTracker.CurrentRunKills;
            var gameManager = GameManager.Instance;
            if (gameManager == null)
                return 0d;

            return kills * gameManager.BonusPercentPerKill * 0.01d;
        }

        private void TryCloseOnRightClick()
        {
            if (windowRoot != null && windowRoot.activeSelf)
                CloseWindow();
        }

        private bool WasRightClickPressed()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.rightButton.wasPressedThisFrame;
        }

        private void UpdateSummaryLabels(double elapsedSeconds)
        {
            var safeElapsed = Math.Max(elapsedSeconds, 0.0001d);
            var distancePerMinute = 0d;
            var damagePerSecond = 0d;
            var killsPerMinute = 0d;

            if (statTracker != null)
            {
                var runDistance = Math.Max(statTracker.CurrentRunDistance, 0f);
                var runDamage = Math.Max(statTracker.CurrentRunDamageDealt, 0d);
                var runKills = Mathf.Max(statTracker.CurrentRunKills, 0);
                distancePerMinute = runDistance * 60d / safeElapsed;
                damagePerSecond = runDamage / safeElapsed;
                killsPerMinute = runKills * 60d / safeElapsed;
            }

            if (closeHintText != null)
            {
                closeHintText.text =
                    $"Tap anywhere outside this window to close it.{Environment.NewLine}Distance per minute: {FormatWholeNumber(distancePerMinute)}";
            }

            if (performanceText != null)
            {
                performanceText.text =
                    $"Damage per second: {FormatWholeNumber(damagePerSecond)}{Environment.NewLine}Kills per minute: {FormatWholeNumber(killsPerMinute)}";
            }
        }

        private static string FormatWholeNumber(double value)
        {
            var rounded = Math.Round(value);
            return rounded.ToString("N0", CultureInfo.InvariantCulture);
        }

        private void UpdateRuntimeLabel(double elapsedSeconds)
        {
            if (runtimeText == null)
                return;

            runtimeText.text = FormatRuntime(elapsedSeconds);
        }

        private static string FormatRuntime(double elapsedSeconds)
        {
            if (elapsedSeconds < 0)
                elapsedSeconds = 0;

            var span = TimeSpan.FromSeconds(elapsedSeconds);
            return span.TotalHours >= 1d
                ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes:00}:{span.Seconds:00}";
        }

        private void CloseWindow()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }
    }
}
