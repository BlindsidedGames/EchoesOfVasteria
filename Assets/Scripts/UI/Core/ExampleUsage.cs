// This file contains example usage patterns for the UI Core helpers.
// It is NOT meant to be used in production - just for reference.
// Delete or move to a documentation folder as needed.

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace TimelessEchoes.UI.Core.Examples
{
    /// <summary>
    /// Example 1: Simple ticked panel with visibility gating.
    /// Use this pattern for stat displays that poll data periodically.
    /// </summary>
    public class ExampleTickedStatsPanel : TickedPanelUI
    {
        // Reference to the system we're displaying
        private SomeStatTracker _tracker;

        private void Awake()
        {
            _tracker = SomeStatTracker.Instance;
        }

        protected override bool IsPanelVisible()
        {
            // Custom visibility logic - could check a specific child object
            return base.IsPanelVisible();
        }

        protected override void OnRefresh()
        {
            // This is only called when the panel is visible
            // UpdateStatsText(_tracker.GetStats());
        }
    }

    /// <summary>
    /// Example 2: Event-driven window with cached state.
    /// Use this pattern for windows that react to system events.
    /// </summary>
    public class ExampleEventDrivenWindow : EventDrivenPanelUI
    {
        // Cached state - always updated, even when window is closed
        private float _cachedProgress;
        private string _cachedStatusText;
        private bool _isCrafting;

        // Reference to the system (would be set in inspector or found in Awake)
        private SomeCraftingSystem _craftingSystem;

        protected override void SubscribeToEvents()
        {
            if (_craftingSystem == null) return;

            // Use the helper for automatic cleanup
            SubscribeToEvent(
                () => _craftingSystem.OnProgressChanged += HandleProgressChanged,
                () => _craftingSystem.OnProgressChanged -= HandleProgressChanged
            );

            SubscribeToEvent(
                () => _craftingSystem.OnStatusChanged += HandleStatusChanged,
                () => _craftingSystem.OnStatusChanged -= HandleStatusChanged
            );
        }

        private void HandleProgressChanged(float progress)
        {
            // Pattern: Cache + Gate + Update
            _cachedProgress = progress;  // Always cache
            
            if (!IsWindowOpen) return;   // Gate on visibility
            
            UpdateProgressBar(progress); // Update UI
        }

        private void HandleStatusChanged(string status)
        {
            // Alternative: Use the helper method
            UpdateIfVisible(status, ref _cachedStatusText, UpdateStatusText);
        }

        protected override void SyncOnOpen()
        {
            // Called when window opens - sync UI with cached state
            UpdateProgressBar(_cachedProgress);
            UpdateStatusText(_cachedStatusText);
        }

        protected override void RefreshUI()
        {
            // Called when dirty flags are processed
            UpdateProgressBar(_cachedProgress);
            UpdateStatusText(_cachedStatusText);
        }

        private void UpdateProgressBar(float progress)
        {
            // progressBar.value = progress;
        }

        private void UpdateStatusText(string status)
        {
            // statusText.text = status;
        }
    }

    /// <summary>
    /// Example 3: Using dirty flags for batched updates.
    /// Use this pattern when multiple events might fire in quick succession.
    /// </summary>
    public class ExampleDirtyFlagWindow : EventDrivenPanelUI
    {
        // Multiple dirty flags for different UI sections
        private bool _headerNeedsRefresh;
        private bool _listNeedsRefresh;
        private bool _footerNeedsRefresh;

        protected override void SubscribeToEvents()
        {
            // Subscribe to events that mark specific sections dirty
            // ResourceManager.OnInventoryChanged += () => _listNeedsRefresh = true;
            // PlayerStats.OnLevelUp += () => _headerNeedsRefresh = true;
        }

        protected override void ProcessDirtyFlags()
        {
            if (!IsWindowOpen) return;

            // Process each dirty flag independently
            if (_headerNeedsRefresh)
            {
                _headerNeedsRefresh = false;
                RefreshHeader();
            }

            if (_listNeedsRefresh)
            {
                _listNeedsRefresh = false;
                RefreshList();
            }

            if (_footerNeedsRefresh)
            {
                _footerNeedsRefresh = false;
                RefreshFooter();
            }
        }

        private void RefreshHeader() { }
        private void RefreshList() { }
        private void RefreshFooter() { }
    }

    /// <summary>
    /// Example 4: Using ThrottledAction for expensive operations.
    /// </summary>
    public class ExampleThrottledPanel : MonoBehaviour
    {
        private ThrottledAction _updateThrottle;
        private DirtyFlagManager _dirtyFlags;

        private void Awake()
        {
            // Only allow updates every 0.2 seconds
            _updateThrottle = new ThrottledAction(0.2f);
            _dirtyFlags = new DirtyFlagManager();
        }

        private void OnSomeFrequentEvent()
        {
            // Mark dirty - will be processed in LateUpdate
            _dirtyFlags.MarkDirty();
        }

        private void LateUpdate()
        {
            // Only update if dirty AND throttle allows
            if (_dirtyFlags.IsDirty && _updateThrottle.TryExecute())
            {
                _dirtyFlags.CheckAndClear();
                ExpensiveUIUpdate();
            }
        }

        private void ExpensiveUIUpdate()
        {
            // Rebuild complex UI elements
        }
    }

    // Dummy classes for compilation
    internal class SomeStatTracker
    {
        public static SomeStatTracker Instance => null;
    }

    internal class SomeCraftingSystem
    {
        public event Action<float> OnProgressChanged;
        public event Action<string> OnStatusChanged;
    }
}
#endif
