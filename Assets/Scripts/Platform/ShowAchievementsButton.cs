using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID || UNITY_IOS
using VoxelBusters.EssentialKit;
#endif
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
using Steamworks;
#endif

namespace TimelessEchoes.Platform
{
    /// <summary>
    /// UI helper to show native achievements UI on mobile via Essential Kit,
    /// and optionally opens the Steam overlay on desktop.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShowAchievementsButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Guard: only open UI if already authenticated; otherwise request auth (debounced)
            if (GameServices.IsAuthenticated)
            {
                GameServices.ShowAchievements((result, error) => { /* no-op */ });
            }
            else
            {
                MobileAuthDebouncer.RequestAuth(interactive: true, reason: "ShowAchievements.Click");
            }
#else
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
            if (SteamManager.Initialized)
            {
                SteamFriends.ActivateGameOverlay("Achievements");
                return;
            }
#endif
            Debug.Log("Achievements UI unavailable on this platform.");
#endif
        }
    }
}
