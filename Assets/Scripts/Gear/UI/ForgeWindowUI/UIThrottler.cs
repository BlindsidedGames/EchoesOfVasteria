using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Utility for throttling UI updates to reduce unnecessary refreshes.
    /// </summary>
    public class UIThrottler
    {
        private float _lastUpdateTime;
        private int _lastUpdateFrame;
        private readonly float _minTimeBetweenUpdates;
        private readonly int _minFramesBetweenUpdates;

        /// <summary>
        /// Creates a new UI throttler.
        /// </summary>
        /// <param name="minTimeBetweenUpdates">Minimum time in seconds between updates</param>
        /// <param name="minFramesBetweenUpdates">Minimum frames between updates</param>
        public UIThrottler(float minTimeBetweenUpdates = 0.05f, int minFramesBetweenUpdates = 1)
        {
            _minTimeBetweenUpdates = minTimeBetweenUpdates;
            _minFramesBetweenUpdates = minFramesBetweenUpdates;
        }

        /// <summary>
        /// Checks if an update should be allowed based on throttling rules.
        /// </summary>
        /// <returns>True if update should proceed, false if should be skipped</returns>
        public bool ShouldUpdate()
        {
            var currentTime = Time.time;
            var currentFrame = Time.frameCount;

            if (currentTime - _lastUpdateTime < _minTimeBetweenUpdates)
                return false;

            if (currentFrame - _lastUpdateFrame < _minFramesBetweenUpdates)
                return false;

            return true;
        }

        /// <summary>
        /// Marks the throttler as having completed an update.
        /// Call this after performing the update.
        /// </summary>
        public void MarkUpdated()
        {
            _lastUpdateTime = Time.time;
            _lastUpdateFrame = Time.frameCount;
        }

        /// <summary>
        /// Checks if update should proceed and marks as updated if so.
        /// </summary>
        /// <returns>True if update should proceed</returns>
        public bool TryUpdate()
        {
            if (!ShouldUpdate())
                return false;

            MarkUpdated();
            return true;
        }

        /// <summary>
        /// Forces the throttler to allow the next update.
        /// </summary>
        public void Reset()
        {
            _lastUpdateTime = 0f;
            _lastUpdateFrame = 0;
        }
    }
}
