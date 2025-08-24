using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Centralized UI ticker that invokes registered callbacks at requested intervals.
    /// Lives across scenes.
    /// </summary>
    public class UITicker : MonoBehaviour
    {
        private static bool _isQuitting;
        private class Subscription
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
        private readonly List<Subscription> _toRemove = new List<Subscription>();
        // Diagnostics (editor only)
        private const float SLOW_CALLBACK_WARN_MS = 4f; // warn if a single callback exceeds this
        private const float WARN_REPEAT_COOLDOWN_SEC = 5f; // don't spam the same warning
        private const float HIGH_FREQUENCY_INTERVAL_SEC = 0.02f; // ~50Hz; UI should rarely need faster
        private readonly Dictionary<Action, float> _lastWarnTime = new Dictionary<Action, float>();
        private readonly Dictionary<Action, float> _lastExceptionWarnTime = new Dictionary<Action, float>();
        private readonly HashSet<Action> _warnedHighFrequency = new HashSet<Action>();

        public void Subscribe(Action callback, float interval)
        {
            if (callback == null || interval <= 0f) return;
            // Avoid duplicate subscriptions of the same delegate
            foreach (var sub in _subscriptions)
            {
                if (sub.Callback == callback)
                {
                    sub.Interval = interval;
                    sub.NextTime = Time.unscaledTime + interval;
                    return;
                }
            }
            _subscriptions.Add(new Subscription
            {
                Callback = callback,
                Interval = interval,
                NextTime = Time.unscaledTime + interval
            });

            // Warn about overly aggressive update rates in editor
            #if UNITY_EDITOR
            if (interval < HIGH_FREQUENCY_INTERVAL_SEC && !_warnedHighFrequency.Contains(callback))
            {
                _warnedHighFrequency.Add(callback);
                try
                {
                    var method = callback.Method;
                    var owner = method != null ? method.DeclaringType : null;
                    Debug.LogWarning($"[UITicker] High-frequency subscription: {owner?.Name}.{method?.Name} every {interval * 1000f:F1} ms");
                }
                catch { /* best-effort only */ }
            }
            #endif
        }

        public void Unsubscribe(Action callback)
        {
            if (callback == null) return;
            _toRemove.Clear();
            foreach (var sub in _subscriptions)
                if (sub.Callback == callback)
                    _toRemove.Add(sub);
            if (_toRemove.Count > 0)
                foreach (var sub in _toRemove)
                    _subscriptions.Remove(sub);
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                var sub = _subscriptions[i];
                if (now >= sub.NextTime)
                {
                    #if UNITY_EDITOR
                    {
                        var t0 = Time.realtimeSinceStartup;
                        try
                        {
                            sub.Callback?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            if (!_lastExceptionWarnTime.TryGetValue(sub.Callback, out var lastExTime) || now - lastExTime >= WARN_REPEAT_COOLDOWN_SEC)
                            {
                                _lastExceptionWarnTime[sub.Callback] = now;
                                try
                                {
                                    var method = sub.Callback?.Method;
                                    var owner = method != null ? method.DeclaringType : null;
                                    Debug.LogError($"[UITicker] Subscriber threw: {owner?.Name}.{method?.Name} → {ex}");
                                }
                                catch { /* ignore */ }
                            }
                        }
                        var elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;
                        if (elapsedMs >= SLOW_CALLBACK_WARN_MS)
                        {
                            if (!_lastWarnTime.TryGetValue(sub.Callback, out var lastWarn) || now - lastWarn >= WARN_REPEAT_COOLDOWN_SEC)
                            {
                                _lastWarnTime[sub.Callback] = now;
                                try
                                {
                                    var method = sub.Callback?.Method;
                                    var owner = method != null ? method.DeclaringType : null;
                                    Debug.LogWarning($"[UITicker] Slow callback: {owner?.Name}.{method?.Name} took {elapsedMs:F2} ms (interval {sub.Interval * 1000f:F1} ms)");
                                }
                                catch { /* ignore */ }
                            }
                        }
                    }
                    #else
                    {
                        try { sub.Callback?.Invoke(); }
                        catch (Exception) { /* swallow to avoid breaking the loop */ }
                    }
                    #endif
                    sub.NextTime = now + sub.Interval;
                }
            }
        }

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