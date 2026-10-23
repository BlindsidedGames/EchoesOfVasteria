using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

namespace TimelessEchoes.Platform
{
    /// <summary>
    /// Public branding is separate from Unity's legacy product name, which remains
    /// unchanged because it identifies existing saves, preferences and cloud sessions.
    /// </summary>
    public static class GameBranding
    {
        public const string DisplayName = "Echoes of Vatheria";

        public static void SetWindowTitle(string status)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            currentTitle = string.IsNullOrEmpty(status) ? DisplayName : DisplayName + " - " + status;
            TryApplyWindowTitle();
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static string currentTitle = DisplayName;
        private static IntPtr windowHandle;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(IntPtr handle, string title);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr handle);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeWindowTitle()
        {
            currentTitle = DisplayName;
            windowHandle = IntPtr.Zero;
            new GameObject("Game window title").AddComponent<WindowTitleInitializer>();
        }

        internal static bool TryApplyWindowTitle()
        {
            if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
            {
                using var process = Process.GetCurrentProcess();
                process.Refresh();
                windowHandle = process.MainWindowHandle;
            }

            return windowHandle != IntPtr.Zero && SetWindowText(windowHandle, currentTitle);
        }

        private sealed class WindowTitleInitializer : MonoBehaviour
        {
            private IEnumerator Start()
            {
                // The native window may be created after the first scene loads.
                for (var attempt = 0; attempt < 60 && !TryApplyWindowTitle(); attempt++)
                    yield return null;
                Destroy(gameObject);
            }
        }
#endif
    }
}
