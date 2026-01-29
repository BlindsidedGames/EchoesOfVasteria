using System;
using UnityEngine;

namespace TimelessEchoes.UI.Core
{
    /// <summary>
    /// Base class for event-driven UI panels that respond to system events rather than polling.
    /// This replaces the UITicker broadcast pattern with targeted event subscriptions.
    /// 
    /// Key principles:
    /// 1. Subscribe to specific system events (not global tick)
    /// 2. Always cache state when events fire (even if invisible)
    /// 3. Only update UI when visible
    /// 4. Sync UI state when panel becomes visible
    /// </summary>
    public abstract class EventDrivenStatsPanelUI : MonoBehaviour
    {
        /// <summary>
        /// Tracks whether state has changed since last UI refresh.
        /// Set to true when events fire; cleared after RefreshUI().
        /// </summary>
        protected bool isDirty = true;

        /// <summary>
        /// Override to define what makes this panel "visible".
        /// Default checks if the GameObject is active in hierarchy.
        /// </summary>
        protected virtual bool IsPanelVisible()
        {
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        /// <summary>
        /// Called by derived classes when their subscribed events fire.
        /// Marks the panel dirty and refreshes UI if visible.
        /// </summary>
        protected void OnDataChanged()
        {
            isDirty = true;
            
            if (!IsPanelVisible())
                return;
            
            RefreshUI();
        }

        /// <summary>
        /// Call this when the panel becomes visible (e.g., window opens).
        /// Forces a full refresh regardless of dirty state.
        /// </summary>
        protected void SyncOnVisible()
        {
            isDirty = false;
            RefreshUI();
        }

        /// <summary>
        /// Derived classes subscribe to their relevant system events here.
        /// </summary>
        protected abstract void SubscribeToEvents();

        /// <summary>
        /// Derived classes unsubscribe from their system events here.
        /// </summary>
        protected abstract void UnsubscribeFromEvents();

        /// <summary>
        /// Implement the actual UI update logic here.
        /// This is only called when the panel is visible or syncing.
        /// </summary>
        protected abstract void RefreshUI();

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
            // Initial refresh when enabled
            SyncOnVisible();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }
    }

    /// <summary>
    /// Extended base for panels that track window open/close state separately.
    /// Use when the GameObject stays active but content should only update when "open".
    /// </summary>
    public abstract class EventDrivenWindowPanelUI : EventDrivenStatsPanelUI
    {
        /// <summary>
        /// Track window open state separately from GameObject active state.
        /// </summary>
        public bool IsWindowOpen { get; protected set; }

        protected override bool IsPanelVisible()
        {
            return base.IsPanelVisible() && IsWindowOpen;
        }

        /// <summary>
        /// Call when window opens.
        /// </summary>
        public virtual void OpenWindow()
        {
            IsWindowOpen = true;
            SyncOnVisible();
            OnWindowOpened();
        }

        /// <summary>
        /// Call when window closes.
        /// </summary>
        public virtual void CloseWindow()
        {
            IsWindowOpen = false;
            OnWindowClosed();
        }

        /// <summary>
        /// Override for custom open behavior.
        /// </summary>
        protected virtual void OnWindowOpened() { }

        /// <summary>
        /// Override for custom close behavior.
        /// </summary>
        protected virtual void OnWindowClosed() { }
    }

    /// <summary>
    /// Helper for panels that need periodic updates in addition to event-driven updates.
    /// Use sparingly - prefer pure event-driven patterns when possible.
    /// 
    /// This is useful for panels displaying progress bars or time-based data
    /// that needs smooth animation while visible.
    /// </summary>
    public abstract class HybridStatsPanelUI : EventDrivenStatsPanelUI
    {
        [SerializeField]
        [Tooltip("How often to refresh time-sensitive UI elements (seconds). Set to 0 to disable.")]
        protected float periodicUpdateInterval = 0.1f;

        private float nextPeriodicUpdate;
        private bool hasPeriodicUpdate => periodicUpdateInterval > 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (hasPeriodicUpdate)
                nextPeriodicUpdate = Time.unscaledTime + periodicUpdateInterval;
        }

        protected virtual void Update()
        {
            if (!hasPeriodicUpdate) return;
            if (!IsPanelVisible()) return;
            
            if (Time.unscaledTime >= nextPeriodicUpdate)
            {
                nextPeriodicUpdate = Time.unscaledTime + periodicUpdateInterval;
                OnPeriodicUpdate();
            }
        }

        /// <summary>
        /// Called periodically when panel is visible.
        /// Use for smooth progress bar animations or time displays.
        /// </summary>
        protected virtual void OnPeriodicUpdate()
        {
            RefreshUI();
        }
    }
}
