using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Reusable action throttler that limits execution frequency.
    /// Use this to prevent high-frequency operations from executing too often.
    /// </summary>
    /// <example>
    /// <code>
    /// private ThrottledAction _statsThrottle = new ThrottledAction(0.2f);
    ///
    /// void Update()
    /// {
    ///     if (_statsThrottle.TryExecute())
    ///     {
    ///         // This code runs at most 5 times per second
    ///         EmitStats();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ThrottledAction
    {
        private float _nextAllowedTime;
        private float _minInterval;

        /// <summary>
        /// Creates a new throttled action with the specified minimum interval.
        /// </summary>
        /// <param name="minIntervalSeconds">Minimum time between executions in seconds. Clamped to at least 0.001s.</param>
        public ThrottledAction(float minIntervalSeconds)
        {
            _minInterval = Mathf.Max(0.001f, minIntervalSeconds);
        }

        /// <summary>
        /// Gets the current minimum interval between executions.
        /// </summary>
        public float MinInterval => _minInterval;

        /// <summary>
        /// Returns true if enough time has passed since last execution.
        /// Automatically updates the next allowed time when returning true.
        /// </summary>
        /// <returns>True if the action should execute, false if it should be skipped.</returns>
        public bool TryExecute()
        {
            var now = Time.unscaledTime;
            if (now >= _nextAllowedTime)
            {
                _nextAllowedTime = now + _minInterval;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Force the next TryExecute to return true by resetting the timer.
        /// </summary>
        public void Reset() => _nextAllowedTime = 0f;

        /// <summary>
        /// Update the minimum interval between executions.
        /// </summary>
        /// <param name="seconds">New minimum interval in seconds. Clamped to at least 0.001s.</param>
        public void SetInterval(float seconds)
        {
            _minInterval = Mathf.Max(0.001f, seconds);
        }
    }
}
