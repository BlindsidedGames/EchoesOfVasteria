using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        }

        private void Start()
        {
            AttemptAttachStatTracker();
        }

        private void Update()
        {
            AttemptAttachStatTracker();

            if (!runActive)
                return;

            var elapsed = Time.time - runStartTime;
            UpdateRuntimeLabel(elapsed);

            updateTimer += Time.deltaTime;
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

        private void AttemptAttachStatTracker()
        {
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null || statTracker == tracker)
                return;

            if (statTracker != null)
                statTracker.OnRunEnded -= HandleRunEnded;

            statTracker = tracker;
            statTracker.OnRunEnded += HandleRunEnded;
        }

        private void HandleRunStarted()
        {
            runActive = true;
            runStartTime = Time.time;
            updateTimer = 0f;
            ClearEntries();
            UpdateRuntimeLabel(0f);
        }

        private void HandleRunEnded(bool died)
        {
            var duration = statTracker != null ? statTracker.LastRunDuration : Time.time - runStartTime;
            RefreshEntries(duration);
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
                orderedEntries.Add(entry);
            }

            entry.Amount += amount;
            RefreshEntry(entry, Math.Max(Time.time - runStartTime, 0.0001f));
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
