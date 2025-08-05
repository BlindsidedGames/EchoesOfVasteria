#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
using TimelessEchoes.Stats;
using TimelessEchoes.Utilities;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
#endif
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(IntPtr hWnd, string text);
#endif

#if !DISABLESTEAMWORKS
        private static RichPresenceManager instance;

        /// <summary>
        ///     Singleton instance accessor.
        /// </summary>
        public static RichPresenceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<RichPresenceManager>();
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            if (tracker != null)
                tracker.OnRunEnded += OnRunEnded;
#endif
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            WindowsTaskbar.Clear();
#endif
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            var maxDistance = tracker != null ? tracker.MaxRunDistance : 1f;
            WindowsTaskbar.SetProgress(distance / maxDistance);
#endif
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            if (tracker != null)
                tracker.OnRunEnded -= OnRunEnded;
#endif
        }

        private void SetWindowTitle(string status)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var handle = GetActiveWindow();
            if (handle != IntPtr.Zero)
                SetWindowText(handle, $"{Application.productName} - {status}");
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void OnRunEnded(bool died)
        {
            if (died)
            {
                WindowsTaskbar.SetState(WindowsTaskbar.State.Error);
                WindowsTaskbar.SetProgress(1f);
                WindowsTaskbar.Flash(3);
            }
            else
            {
                WindowsTaskbar.Clear();
            }
        }
#endif
#endif
    }
}
