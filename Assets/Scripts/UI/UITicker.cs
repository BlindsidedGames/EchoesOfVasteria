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
        private class Subscription
        {
            public Action Callback;
            public float Interval;
            public float NextTime;
        }

        private static UITicker _instance;
        public static UITicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UITicker");
                    _instance = go.AddComponent<UITicker>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private readonly List<Subscription> _toRemove = new List<Subscription>();

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
                    try { sub.Callback?.Invoke(); }
                    catch (Exception) { /* swallow to avoid breaking the loop */ }
                    sub.NextTime = now + sub.Interval;
                }
            }
        }
    }
}