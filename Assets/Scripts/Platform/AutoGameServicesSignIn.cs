using UnityEngine;
using System.Collections;
#if UNITY_ANDROID || UNITY_IOS
using VoxelBusters.EssentialKit;
#endif

namespace TimelessEchoes.Platform
{
    /// <summary>
    /// Authenticates the local player with Game Services on startup.
    /// Persists across scenes.
    /// </summary>
    public class AutoGameServicesSignIn : MonoBehaviour
    {
        private static AutoGameServicesSignIn instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        // Only relevant when Game Services is available on mobile
#if UNITY_ANDROID || UNITY_IOS
        [SerializeField] private bool interactiveFallback = false;
#endif
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private IEnumerator Start()
        {
            while (StartupDiagnostics.Enabled && !StartupDiagnostics.ChoiceReady) yield return null;
            if (StartupDiagnostics.SkipGameCenter) { StartupDiagnostics.SetGameServicesStatus("Skipped by selected check"); yield break; }
#if UNITY_ANDROID || UNITY_IOS
            StartupDiagnostics.SetGameServicesStatus("Checking authentication");
            if (!GameServices.IsAuthenticated)
            {
                // Preferred silent auth (debounced)
                MobileAuthDebouncer.RequestSilentAuth("AutoSignIn.Start");
            }
            else StartupDiagnostics.SetGameServicesStatus("Authenticated");
#endif
            yield break;
        }

        /// <summary>
        /// Call this to request an interactive login if silent auth failed
        /// and you want to prompt the user.
        /// </summary>
        public void PromptLoginIfNeeded()
        {
            if (StartupDiagnostics.SkipGameCenter) return;
#if UNITY_ANDROID || UNITY_IOS
            if (interactiveFallback && !GameServices.IsAuthenticated)
            {
                // Interactive prompt (debounced)
                MobileAuthDebouncer.RequestAuth(interactive: true, reason: "AutoSignIn.PromptLoginIfNeeded");
            }
#endif
        }
    }
}
