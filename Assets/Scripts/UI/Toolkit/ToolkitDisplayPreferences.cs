using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    public static class ToolkitDisplayPreferences
    {
        public static int Orientation
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("Display.Orientation", 0), 0, 2);
            set { PlayerPrefs.SetInt("Display.Orientation", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); ApplyOrientation(); }
        }

        public static bool DefaultPortraitFullscreen
        {
            get => PlayerPrefs.GetInt("Display.DefaultPortraitFullscreen", 1) == 1;
            set { PlayerPrefs.SetInt("Display.DefaultPortraitFullscreen", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool KeepWorldInSafeArea
        {
            get => PlayerPrefs.GetInt("Display.SafeArea", 0) == 1;
            set { PlayerPrefs.SetInt("Display.SafeArea", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool ShowStatusBar
        {
            get => PlayerPrefs.GetInt("Display.ShowStatusBar", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("Display.ShowStatusBar", value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyStatusBar();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeStatusBar()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            ApplyStatusBar();
            Application.focusChanged -= RestoreStatusBarOnFocus;
            Application.focusChanged += RestoreStatusBarOnFocus;
#endif
        }

        private static void RestoreStatusBarOnFocus(bool focused)
        {
            if (focused) ApplyStatusBar();
        }

        private static void ApplyStatusBar()
        {
#if UNITY_IOS && !UNITY_EDITOR
            EOVSetStatusBarVisible(ShowStatusBar ? 1 : 0);
#elif UNITY_ANDROID && !UNITY_EDITOR
            var insets = UnityEngine.Android.AndroidApplication.currentWindowInsets;
            var statusBars = UnityEngine.Android.AndroidWindowInsets.Type.StatusBars;
            if (ShowStatusBar) insets.Show(statusBars);
            else insets.Hide(statusBars);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void EOVSetStatusBarVisible(int visible);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOrientation()
        {
            if (!Application.isMobilePlatform) return;
            Screen.autorotateToPortrait = Orientation != 2;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = Orientation != 1;
            Screen.orientation = Orientation == 1 ? ScreenOrientation.Portrait : ScreenOrientation.AutoRotation;
        }
    }
}
