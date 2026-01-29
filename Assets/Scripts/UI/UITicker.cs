using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Centralized UI ticker that invokes registered callbacks at requested intervals.
    /// Lives across scenes. Designed for zero per-frame GC allocations.
    /// </summary>
    public class UITicker : MonoBehaviour
    {
        private static bool _isQuitting;

        // Using struct to avoid per-subscription heap allocation
        // The List<T> will box these, but only on Subscribe (not per-frame)
        private struct Subscription
        {
            public Action Callback;
            public float Interval;
            public float NextTime;
        }

        private static UITicker _instance;
        public static bool HasInstance => _instance != null;
        public static UITicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_isQuitting)
                        return null;
                    var go = new GameObject("UITicker");
                    _instance = go.AddComponent<UITicker>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly List<Subscription> _subscriptions = new List<Subscription>();

        // Opt-in profiling (only enable when actively debugging)
        [SerializeField] private bool enableProfiling = false;
        private const float SLOW_CALLBACK_WARN_MS = 4f;
        private const float WARN_REPEAT_COOLDOWN_SEC = 5f;
        private readonly Dictionary<Action, float> _lastWarnTime = new Dictionary<Action, float>();

        public void Subscribe(Action callback, float interval)
        {
            if (callback == null || interval <= 0f) return;

            // Check for existing subscription and update it
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                var sub = _subscriptions[i];
                if (sub.Callback == callback)
                {
                    sub.Interval = interval;
                    sub.NextTime = Time.unscaledTime + interval;
                    _subscriptions[i] = sub; // Write back since it's a struct
                    return;
                }
            }

            _subscriptions.Add(new Subscription
            {
                Callback = callback,
                Interval = interval,
                NextTime = Time.unscaledTime + interval
            });
        }

        public void Unsubscribe(Action callback)
        {
            if (callback == null) return;
            // Reverse iterate to safely remove during iteration
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                if (_subscriptions[i].Callback == callback)
                {
                    _subscriptions.RemoveAt(i);
                    return; // Each callback should only be subscribed once
                }
            }
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            var count = _subscriptions.Count;

            for (int i = 0; i < count; i++)
            {
                var sub = _subscriptions[i];
                if (now >= sub.NextTime)
                {
                    // Invoke callback with minimal overhead
                    // No try-catch in release builds to avoid exception handling overhead
                    #if UNITY_EDITOR
                    if (enableProfiling)
                    {
                        InvokeWithProfiling(sub.Callback, now);
                    }
                    else
                    {
                        try { sub.Callback?.Invoke(); }
                        catch (Exception) { /* swallow to avoid breaking the loop */ }
                    }
                    #else
                    try { sub.Callback?.Invoke(); }
                    catch (Exception) { /* swallow to avoid breaking the loop */ }
                    #endif

                    // Update next time - must write back since it's a struct
                    sub.NextTime = now + sub.Interval;
                    _subscriptions[i] = sub;
                }
            }
        }

        #if UNITY_EDITOR
        private void InvokeWithProfiling(Action callback, float now)
        {
            var t0 = Time.realtimeSinceStartup;
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UITicker] Subscriber threw: {ex}");
            }

            var elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;
            if (elapsedMs >= SLOW_CALLBACK_WARN_MS)
            {
                if (!_lastWarnTime.TryGetValue(callback, out var lastWarn) || now - lastWarn >= WARN_REPEAT_COOLDOWN_SEC)
                {
                    _lastWarnTime[callback] = now;
                    // Avoid reflection in hot path - just log the time
                    Debug.LogWarning($"[UITicker] Slow callback took {elapsedMs:F2} ms");
                }
            }
        }
        #endif

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
