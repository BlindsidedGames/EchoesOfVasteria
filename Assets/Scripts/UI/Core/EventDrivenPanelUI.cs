using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.UI.Core
{
    /// <summary>
    /// Base class for event-driven UI panels that cache state while closed.
    /// Inherit from this to avoid boilerplate for visibility gating and event subscription.
    /// 
    /// Key features:
    /// - Automatic visibility tracking via IsWindowOpen
    /// - Event subscription management via SubscribeToEvent/UnsubscribeAll
    /// - Dirty flag system for deferred updates
    /// - SyncOnOpen pattern for state synchronization when window opens
    /// </summary>
    public abstract class EventDrivenPanelUI : MonoBehaviour
    {
        /// <summary>
        /// Tracks whether this window/panel is currently open/visible.
        /// </summary>
        public bool IsWindowOpen { get; protected set; }

        /// <summary>
        /// Dirty flag manager for deferred UI updates.
        /// </summary>
        protected readonly DirtyFlagManager DirtyFlags = new DirtyFlagManager();

        // Track subscriptions for automatic cleanup
        private readonly List<Action> _unsubscribeActions = new List<Action>();

        #region Unity Lifecycle

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeAll();
        }

        protected virtual void LateUpdate()
        {
            ProcessDirtyFlags();
        }

        #endregion

        #region Window State Management

        /// <summary>
        /// Call this when the window opens. Handles visibility flag and sync.
        /// Override OnWindowOpened for custom open behavior.
        /// </summary>
        public virtual void OpenWindow()
        {
            IsWindowOpen = true;
            DirtyFlags.ForceRefresh();
            OnWindowOpened();
            SyncOnOpen();
        }

        /// <summary>
        /// Call this when the window closes.
        /// Override OnWindowClosed for custom close behavior.
        /// </summary>
        public virtual void CloseWindow()
        {
            IsWindowOpen = false;
            OnWindowClosed();
        }

        /// <summary>
        /// Called when window opens. Override for setup logic.
        /// </summary>
        protected virtual void OnWindowOpened() { }

        /// <summary>
        /// Called when window closes. Override for cleanup logic.
        /// </summary>
        protected virtual void OnWindowClosed() { }

        /// <summary>
        /// Syncs UI state when window opens. Override to refresh all UI from cached state.
        /// </summary>
        protected virtual void SyncOnOpen() { }

        #endregion

        #region Event Subscription Management

        /// <summary>
        /// Override to subscribe to system events. Called in OnEnable.
        /// Use SubscribeToEvent helper for automatic cleanup.
        /// </summary>
        protected virtual void SubscribeToEvents() { }

        /// <summary>
        /// Helper to subscribe to an event with automatic unsubscription tracking.
        /// </summary>
        /// <example>
        /// SubscribeToEvent(
        ///     () => forgeSystem.OnCraftingProgress += HandleProgress,
        ///     () => forgeSystem.OnCraftingProgress -= HandleProgress
        /// );
        /// </example>
        protected void SubscribeToEvent(Action subscribe, Action unsubscribe)
        {
            subscribe?.Invoke();
            if (unsubscribe != null)
                _unsubscribeActions.Add(unsubscribe);
        }

        /// <summary>
        /// Helper to subscribe to a typed event with automatic cleanup.
        /// </summary>
        protected void SubscribeToEvent<T>(Action<T> handler, Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)
        {
            subscribe?.Invoke(handler);
            if (unsubscribe != null)
                _unsubscribeActions.Add(() => unsubscribe(handler));
        }

        /// <summary>
        /// Unsubscribes from all tracked events. Called automatically in OnDisable.
        /// </summary>
        protected void UnsubscribeAll()
        {
            foreach (var unsubscribe in _unsubscribeActions)
            {
                try
                {
                    unsubscribe?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[EventDrivenPanelUI] Error during unsubscribe: {ex.Message}");
                }
            }
            _unsubscribeActions.Clear();
        }

        #endregion

        #region Visibility-Gated Updates

        /// <summary>
        /// Marks a specific refresh type as needed. Override to handle multiple dirty flags.
        /// </summary>
        public virtual void MarkDirty()
        {
            DirtyFlags.MarkDirty();
        }

        /// <summary>
        /// Processes dirty flags in LateUpdate. Override to handle deferred updates.
        /// </summary>
        protected virtual void ProcessDirtyFlags()
        {
            if (!IsWindowOpen)
                return;

            if (DirtyFlags.CheckAndClear())
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// Override to implement full UI refresh. Called when dirty flags are processed.
        /// </summary>
        protected virtual void RefreshUI() { }

        /// <summary>
        /// Helper for visibility-gated event handlers.
        /// Caches the value, then updates UI only if window is open.
        /// </summary>
        /// <example>
        /// private float _cachedProgress;
        /// void HandleProgress(float progress) => UpdateIfVisible(progress, ref _cachedProgress, UpdateProgressBar);
        /// </example>
        protected void UpdateIfVisible<T>(T newValue, ref T cachedValue, Action<T> updateAction)
        {
            cachedValue = newValue; // Always cache
            if (!IsWindowOpen)
                return;
            updateAction?.Invoke(newValue);
        }

        /// <summary>
        /// Helper for visibility-gated event handlers that just mark dirty.
        /// </summary>
        protected void MarkDirtyIfVisible()
        {
            DirtyFlags.MarkDirty();
        }

        /// <summary>
        /// Helper that caches value and marks dirty (for LateUpdate processing).
        /// </summary>
        protected void CacheAndMarkDirty<T>(T newValue, ref T cachedValue)
        {
            cachedValue = newValue;
            DirtyFlags.MarkDirty();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Safe null check for Unity objects that may be destroyed.
        /// </summary>
        protected bool IsValid(UnityEngine.Object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Checks if this component and its GameObject are still valid.
        /// Use in event handlers during scene transitions.
        /// </summary>
        protected bool IsSelfValid()
        {
            return this != null && gameObject != null;
        }

        #endregion
    }

    /// <summary>
    /// Extended base class for panels that need to track a specific system's state.
    /// </summary>
    /// <typeparam name="TSystem">The system type this panel displays</typeparam>
    public abstract class EventDrivenPanelUI<TSystem> : EventDrivenPanelUI where TSystem : class
    {
        /// <summary>
        /// Reference to the backing system. Set in Awake or via serialization.
        /// </summary>
        protected TSystem System { get; set; }

        /// <summary>
        /// Override to get the system instance (e.g., from singleton).
        /// </summary>
        protected virtual TSystem GetSystem() => System;

        protected override void OnEnable()
        {
            System = GetSystem();
            if (System == null)
            {
                Debug.LogWarning($"[{GetType().Name}] System not found on enable");
            }
            base.OnEnable();
        }
    }
}
