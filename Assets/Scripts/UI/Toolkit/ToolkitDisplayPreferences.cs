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
