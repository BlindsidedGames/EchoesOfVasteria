#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TimelessEchoes.Utilities
{
    public static class WindowsTaskbar
    {
        // Taskbar progress states
        public enum State : uint
        {
            NoProgress    = 0x0,
            Indeterminate = 0x1,  // Marquee
            Normal        = 0x2,  // Green
            Error         = 0x4,  // Red
            Paused        = 0x8   // Yellow
        }

        // --- COM interop (ITaskbarList3) ---
        [ComImport]
        [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        private class CTaskbarList { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEA84")]
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
            void SetProgressState(IntPtr hwnd, State state);
        }

        private static ITaskbarList3 _taskbar;
        private static IntPtr _hwnd;

        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

        // Flashing
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pfwi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_STOP        = 0x0;
        private const uint FLASHW_CAPTION     = 0x1;
        private const uint FLASHW_TRAY        = 0x2;
        private const uint FLASHW_ALL         = FLASHW_CAPTION | FLASHW_TRAY;
        private const uint FLASHW_TIMER       = 0x4;
        private const uint FLASHW_TIMERNOFG   = 0xC;

        private static bool EnsureInit()
        {
            if (_taskbar == null)
            {
                try
                {
                    _taskbar = (ITaskbarList3)new CTaskbarList();
                    _taskbar.HrInit();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"WindowsTaskbar: Unable to create ITaskbarList3. {e.Message}");
                    return false;
                }
            }

            if (_hwnd == IntPtr.Zero)
            {
                // Prefer the active window; fall back to foreground.
                _hwnd = GetActiveWindow();
                if (_hwnd == IntPtr.Zero)
                    _hwnd = GetForegroundWindow();

                if (_hwnd == IntPtr.Zero)
                {
                    Debug.LogWarning("WindowsTaskbar: Could not obtain a window handle (HWND).");
                    return false;
                }
            }

            return true;
        }

        /// <summary>Set the taskbar progress state (NoProgress, Normal, Paused, Error, Indeterminate).</summary>
        public static void SetState(State state)
        {
            if (!EnsureInit()) return;
            try { _taskbar.SetProgressState(_hwnd, state); }
            catch (Exception e) { Debug.LogWarning($"WindowsTaskbar.SetState failed: {e.Message}"); }
        }

        /// <summary>Set the taskbar progress value [0..1]. Will clamp and switch state to Normal if needed.</summary>
        public static void SetProgress(float normalized)
        {
            if (!EnsureInit()) return;

            var clamped = Mathf.Clamp01(normalized);
            ulong total = 1000;
            ulong done  = (ulong)Mathf.RoundToInt(clamped * 1000f);

            try
            {
                _taskbar.SetProgressState(_hwnd, State.Normal);
                _taskbar.SetProgressValue(_hwnd, done, total);
            }
            catch (Exception e) { Debug.LogWarning($"WindowsTaskbar.SetProgress failed: {e.Message}"); }
        }

        /// <summary>Clear the taskbar progress.</summary>
        public static void Clear()
        {
            if (!EnsureInit()) return;
            try { _taskbar.SetProgressState(_hwnd, State.NoProgress); }
            catch (Exception e) { Debug.LogWarning($"WindowsTaskbar.Clear failed: {e.Message}"); }
        }

        /// <summary>Flash the taskbar button (tray). If count==0, flashes until window gains focus.</summary>
        public static void Flash(uint count = 10, uint timeoutMs = 0)
        {
            if (!EnsureInit()) return;

            var fw = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = _hwnd,
                dwFlags = FLASHW_TRAY | ((count == 0) ? FLASHW_TIMERNOFG : FLASHW_TIMER),
                uCount = count,
                uTimeout = timeoutMs
            };
            FlashWindowEx(ref fw);
        }

        /// <summary>Stop flashing the taskbar button.</summary>
        public static void StopFlash()
        {
            if (!EnsureInit()) return;

            var fw = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = _hwnd,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                uTimeout = 0
            };
            FlashWindowEx(ref fw);
        }
    }
}
#endif
