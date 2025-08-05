#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
using TimelessEchoes.Stats;
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

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_TRAY = 0x2;
        private const uint FLASHW_TIMERNOFG = 0xC;

        private static IntPtr windowHandle;
        private static ITaskbarList3 taskbar;

        [ComImport]
        [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EA6C9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
            void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
        }

        private enum TBPFLAG
        {
            TBPF_NOPROGRESS = 0,
            TBPF_INDETERMINATE = 0x1,
            TBPF_NORMAL = 0x2,
            TBPF_ERROR = 0x4,
            TBPF_PAUSED = 0x8
        }

        [ComImport]
        [Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
        private class CTaskbarList
        {
        }

        private static void InitTaskbar()
        {
            if (taskbar != null)
                return;
            taskbar = (ITaskbarList3)new CTaskbarList();
            taskbar.HrInit();
            windowHandle = GetActiveWindow();
        }

        private static void ResetTaskbarProgress()
        {
            InitTaskbar();
            if (windowHandle != IntPtr.Zero)
                taskbar.SetProgressState(windowHandle, TBPFLAG.TBPF_NOPROGRESS);
        }

        private static void FlashTaskbarError()
        {
            InitTaskbar();
            if (windowHandle == IntPtr.Zero)
                return;
            taskbar.SetProgressState(windowHandle, TBPFLAG.TBPF_ERROR);
            taskbar.SetProgressValue(windowHandle, 1, 1);
            var fw = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = windowHandle,
                dwFlags = FLASHW_TRAY | FLASHW_TIMERNOFG,
                uCount = 3,
                dwTimeout = 0
            };
            FlashWindowEx(ref fw);
        }

        public void SetTaskbarProgress(float current, float max)
        {
            InitTaskbar();
            if (windowHandle == IntPtr.Zero)
                return;
            taskbar.SetProgressState(windowHandle, TBPFLAG.TBPF_NORMAL);
            taskbar.SetProgressValue(windowHandle, (ulong)current, (ulong)max);
        }
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
            ResetTaskbarProgress();
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
            SetTaskbarProgress(distance, maxDistance);
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
                FlashTaskbarError();
            else
                ResetTaskbarProgress();
        }
#endif
#endif
    }
}
