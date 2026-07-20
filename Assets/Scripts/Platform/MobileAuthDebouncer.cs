using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using VoxelBusters.EssentialKit;
#endif

namespace TimelessEchoes.Platform
{
    /// <summary>
    /// Debounces mobile Game Services authentication attempts to avoid re-entrant
    /// and rapid repeated logins that can cause instability when app focus changes.
    /// </summary>
    public static class MobileAuthDebouncer
    {
#if UNITY_ANDROID || UNITY_IOS
        // Config (approved): 2s cooldown, 12s max in-flight TTL
        private const float MinCooldownSeconds = 2f;
        private const float InFlightTtlSeconds = 12f;

        // Runtime state
        private static bool _subscribed;
        private static bool _inFlight;
        private static float _lastAttemptTime = -9999f;
        private static float _inFlightStartTime = -9999f;

        // Toggleable debug logging (default off per request)
        public static bool EnableDebugLogs { get; set; } = false;

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameServices.OnAuthStatusChange -= HandleAuthStatusChange;
            GameServices.OnAuthStatusChange += HandleAuthStatusChange;
        }

        private static void HandleAuthStatusChange(GameServicesAuthStatusChangeResult result, VoxelBusters.CoreLibrary.Error _)
        {
            // Clear in-flight and start cooldown on any auth state update
            _inFlight = false;
            _inFlightStartTime = -9999f;
            _lastAttemptTime = Time.realtimeSinceStartup;

            if (EnableDebugLogs)
            {
                Debug.Log($"[AuthDebounce] Auth status: {result.AuthStatus}, inFlight cleared");
            }
        }

        /// <summary>
        /// Requests an authentication attempt with debouncing.
        /// Returns true if an auth attempt was initiated, false if skipped.
        /// </summary>
        public static bool RequestAuth(bool interactive, string reason = null)
        {
            EnsureSubscribed();

            // If already authenticated, do not attempt
            if (GameServices.IsAuthenticated)
            {
                if (EnableDebugLogs)
                    Debug.Log("[AuthDebounce] Skip: already authenticated");
                return false;
            }

            var now = Time.realtimeSinceStartup;

            // If an auth is in-flight, allow TTL to unlock; else skip
            if (_inFlight)
            {
                if (now - _inFlightStartTime <= InFlightTtlSeconds)
                {
                    if (EnableDebugLogs)
                        Debug.Log($"[AuthDebounce] Skip: in-flight ({now - _inFlightStartTime:0.00}s) reason={reason}");
                    return false;
                }
                else
                {
                    // TTL exceeded, clear the stuck state
                    if (EnableDebugLogs)
                        Debug.Log("[AuthDebounce] In-flight TTL exceeded; unlocking");
                    _inFlight = false;
                    _inFlightStartTime = -9999f;
                }
            }

            // Enforce minimum cooldown between attempts
            if (now - _lastAttemptTime < MinCooldownSeconds)
            {
                if (EnableDebugLogs)
                    Debug.Log($"[AuthDebounce] Skip: cooldown ({MinCooldownSeconds - (now - _lastAttemptTime):0.00}s left) reason={reason}");
                return false;
            }

            // Initiate authentication
            _inFlight = true;
            _inFlightStartTime = now;
            _lastAttemptTime = now;

            if (EnableDebugLogs)
                Debug.Log($"[AuthDebounce] Authenticate(interactive={interactive}) reason={reason}");

            GameServices.Authenticate(interactive: interactive);
            return true;
        }

        /// <summary>
        /// Convenience for silent auth.
        /// </summary>
        public static bool RequestSilentAuth(string reason = null)
        {
            return RequestAuth(interactive: false, reason: reason);
        }
#else
        // Non-mobile platforms: no-op to keep call sites simple.
        public static bool RequestAuth(bool interactive, string reason = null) => false;
        public static bool RequestSilentAuth(string reason = null) => false;
        public static bool EnableDebugLogs { get; set; } = false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
#if UNITY_ANDROID || UNITY_IOS
            GameServices.OnAuthStatusChange -= HandleAuthStatusChange;
            _subscribed = false;
            _inFlight = false;
            _lastAttemptTime = -9999f;
            _inFlightStartTime = -9999f;
#endif
            EnableDebugLogs = false;
        }
    }
}

