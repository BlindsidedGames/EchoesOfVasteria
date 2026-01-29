using UnityEngine;

namespace TimelessEchoes.UI.Core
{
    /// <summary>
    /// Base class for UI panels that use UITicker for periodic updates.
    /// Provides automatic visibility gating and tick subscription management.
    /// 
    /// DEPRECATED: Use EventDrivenStatsPanelUI instead for better performance
    /// and debuggability. This class remains for backwards compatibility.
    /// 
    /// For new UI code, subscribe directly to system events instead of polling.
    /// See EventDrivenStatsPanelUI for the recommended pattern.
    /// </summary>
    [System.Obsolete("TickedPanelUI is deprecated. Use EventDrivenStatsPanelUI with direct event subscriptions for better performance.")]
    public abstract class TickedPanelUI : MonoBehaviour
    {
        [SerializeField] 
        [Tooltip("How often to refresh the UI in seconds")]
        protected float updateInterval = 0.1f;

        /// <summary>
        /// Override to define what makes this panel "visible".
        /// Default checks if the GameObject is active in hierarchy.
        /// </summary>
        protected virtual bool IsPanelVisible()
        {
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        protected virtual void OnEnable()
        {
            // Initial refresh
            OnRefresh();
            
            // Subscribe to ticker
            UITicker.Instance?.Subscribe(OnTickerCallback, updateInterval);
        }

        protected virtual void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(OnTickerCallback);
        }

        /// <summary>
        /// Called by UITicker. Handles visibility gating automatically.
        /// </summary>
        private void OnTickerCallback()
        {
            if (!IsPanelVisible())
                return;
            OnRefresh();
        }

        /// <summary>
        /// Override to implement the UI refresh logic.
        /// This is only called when the panel is visible.
        /// </summary>
        protected abstract void OnRefresh();

        /// <summary>
        /// Call to force an immediate refresh (bypasses visibility check).
        /// Useful when opening the panel.
        /// </summary>
        protected void ForceRefresh()
        {
            OnRefresh();
        }

        /// <summary>
        /// Updates the ticker interval. Useful for runtime configuration changes.
        /// </summary>
        protected void SetUpdateInterval(float newInterval)
        {
            if (Mathf.Approximately(updateInterval, newInterval))
                return;

            updateInterval = newInterval;

            // Re-subscribe with new interval if active
            if (isActiveAndEnabled && UITicker.Instance != null)
            {
                UITicker.Instance.Unsubscribe(OnTickerCallback);
                UITicker.Instance.Subscribe(OnTickerCallback, updateInterval);
            }
        }
    }

    /// <summary>
    /// Extended base for ticked panels with additional visibility controls.
    /// 
    /// DEPRECATED: Use EventDrivenWindowPanelUI instead.
    /// </summary>
    [System.Obsolete("TickedWindowPanelUI is deprecated. Use EventDrivenWindowPanelUI instead.")]
    public abstract class TickedWindowPanelUI : TickedPanelUI
    {
        /// <summary>
        /// Track window open state separately from GameObject active state.
        /// Use this when the panel GameObject stays active but content is hidden.
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
            ForceRefresh();
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
}
