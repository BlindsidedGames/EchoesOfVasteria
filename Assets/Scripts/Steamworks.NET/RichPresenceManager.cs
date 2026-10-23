#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
using TimelessEchoes.Platform;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace TimelessEchoes
{
    /// <summary>
    ///     Handles Steam rich presence status updates.
    ///     Shows whether the player is in town or in a run and
    ///     displays the current distance when in a run.
    /// </summary>
    public class RichPresenceManager : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private static RichPresenceManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        /// <summary>
        ///     Singleton instance accessor.
        /// </summary>
        public static RichPresenceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<RichPresenceManager>();
                    if (instance == null)
                        instance = new GameObject("RichPresenceManager").AddComponent<RichPresenceManager>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SetInTown();
        }

        /// <summary>
        ///     Sets rich presence to indicate the player is in town.
        /// </summary>
        public void SetInTown()
        {
            if (!SteamManager.Initialized)
                return;
            SteamFriends.SetRichPresence("status", "In Town");
            SteamFriends.SetRichPresence("steam_display", "#Status_InTown");
            SetWindowTitle("In Town");
        }

        /// <summary>
        ///     Sets rich presence to indicate the player has started a run.
        /// </summary>
        public void SetInRun()
        {
            if (!SteamManager.Initialized)
                return;
            SteamFriends.SetRichPresence("status", "Exploring");
            SteamFriends.SetRichPresence("steam_display", "#Status_InRun");
            SetWindowTitle("Exploring");
        }

        /// <summary>
        ///     Updates the distance value while in a run.
        /// </summary>
        public void UpdateDistance(float distance)
        {
            if (!SteamManager.Initialized)
                return;
            var d = Mathf.FloorToInt(distance);
            SteamFriends.SetRichPresence("status", $"Distance: {d}");
            SteamFriends.SetRichPresence("distance", d.ToString());
            SteamFriends.SetRichPresence("steam_display", "#Status_Distance");
            SetWindowTitle($"Distance: {d}");
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void SetWindowTitle(string status)
        {
            GameBranding.SetWindowTitle(status);
        }

#endif
    }
}
