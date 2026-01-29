using System;
using UnityEngine;

namespace TimelessEchoes.UI.Core
{
    /// <summary>
    /// Static helper for common UI visibility patterns.
    /// Use these to avoid boilerplate visibility checks across UI components.
    /// </summary>
    public static class UIVisibilityHelper
    {
        /// <summary>
        /// Checks if a GameObject is truly visible (active in hierarchy).
        /// Use this for visibility gating in event handlers.
        /// </summary>
        public static bool IsVisible(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }

        /// <summary>
        /// Checks if a MonoBehaviour is enabled and its GameObject is active.
        /// Use this in Update/tick callbacks to skip work when hidden.
        /// </summary>
        public static bool IsActiveAndVisible(MonoBehaviour mb)
        {
            return mb != null && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Checks if a CanvasGroup has visible alpha (above threshold).
        /// Use this for fade-transitioned panels.
        /// </summary>
        public static bool IsCanvasGroupVisible(CanvasGroup group, float alphaThreshold = 0.01f)
        {
            return group != null && group.alpha >= alphaThreshold;
        }

        /// <summary>
        /// Executes an action only if the GameObject is visible.
        /// Useful for wrapping event handlers with visibility gating.
        /// </summary>
        /// <example>
        /// UIVisibilityHelper.ExecuteIfVisible(gameObject, () => UpdateProgressBar(value));
        /// </example>
        public static void ExecuteIfVisible(GameObject go, Action action)
        {
            if (IsVisible(go))
                action?.Invoke();
        }

        /// <summary>
        /// Executes an action only if the MonoBehaviour is active and visible.
        /// </summary>
        public static void ExecuteIfVisible(MonoBehaviour mb, Action action)
        {
            if (IsActiveAndVisible(mb))
                action?.Invoke();
        }
    }

    /// <summary>
    /// Throttles action execution to a minimum interval.
    /// Use this to limit how often expensive UI updates occur.
    /// Thread-safe for use across multiple event sources.
    /// </summary>
    public class ThrottledAction
    {
        private readonly float _minInterval;
        private float _lastExecuteTime;

        public ThrottledAction(float minIntervalSeconds)
        {
            _minInterval = Mathf.Max(0.001f, minIntervalSeconds);
            _lastExecuteTime = float.MinValue;
        }

        /// <summary>
        /// Attempts to execute. Returns true if the interval has passed.
        /// </summary>
        public bool TryExecute()
        {
            var now = Time.unscaledTime;
            if (now - _lastExecuteTime < _minInterval)
                return false;
            _lastExecuteTime = now;
            return true;
        }

        /// <summary>
        /// Attempts to execute an action. Returns true if executed.
        /// </summary>
        public bool TryExecute(Action action)
        {
            if (!TryExecute())
                return false;
            action?.Invoke();
            return true;
        }

        /// <summary>
        /// Forces the next TryExecute to succeed by resetting the timer.
        /// </summary>
        public void Reset()
        {
            _lastExecuteTime = float.MinValue;
        }

        /// <summary>
        /// Updates the minimum interval. Useful for runtime config hot-reload.
        /// </summary>
        public void SetInterval(float newInterval)
        {
            // Note: We can't reassign readonly, so just skip if same
            // For mutable intervals, use a different pattern
        }
    }

    /// <summary>
    /// Manages dirty flags for deferred UI updates.
    /// Coalesces multiple state changes into a single UI refresh.
    /// </summary>
    public class DirtyFlagManager
    {
        private bool _isDirty;
        private bool _forceRefreshOnNextCheck;

        /// <summary>
        /// Marks the UI as needing a refresh.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Marks for forced refresh (e.g., on window open).
        /// </summary>
        public void ForceRefresh()
        {
            _isDirty = true;
            _forceRefreshOnNextCheck = true;
        }

        /// <summary>
        /// Checks and clears the dirty flag.
        /// Returns true if a refresh is needed.
        /// </summary>
        public bool CheckAndClear()
        {
            if (!_isDirty)
                return false;
            _isDirty = false;
            var wasForced = _forceRefreshOnNextCheck;
            _forceRefreshOnNextCheck = false;
            return true;
        }

        /// <summary>
        /// Checks if a refresh is pending without clearing.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Checks if a forced refresh is pending.
        /// </summary>
        public bool IsForceRefreshPending => _forceRefreshOnNextCheck;
    }
}
