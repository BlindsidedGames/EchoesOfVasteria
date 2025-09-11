using UnityEngine;
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
        // Only relevant when Game Services is available on mobile
#if UNITY_ANDROID || UNITY_IOS
        [SerializeField] private bool interactiveFallback = false;
#endif
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!GameServices.IsAuthenticated)
            {
                // Preferred silent auth (debounced)
                MobileAuthDebouncer.RequestSilentAuth("AutoSignIn.Start");
            }
#endif
        }

        /// <summary>
        /// Call this to request an interactive login if silent auth failed
        /// and you want to prompt the user.
        /// </summary>
        public void PromptLoginIfNeeded()
        {
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
