using Blindsided;
using Blindsided.SaveData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Controls the Settings panel UI.
    ///     Currently handles switching between window modes.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private Button fullscreenWindowButton;
        [SerializeField] private Button windowButton;
        [SerializeField] private Button fpsButton;
        [SerializeField] private TMP_Text fpsButtonText;

        private const int Fps60 = 60;
        private const int Fps120 = 120;

        private void Awake()
        {
            if (fullscreenWindowButton != null)
                fullscreenWindowButton.onClick.AddListener(SetFullscreenWindow);
            if (windowButton != null)
                windowButton.onClick.AddListener(SetWindowed);
            if (fpsButton != null)
                fpsButton.onClick.AddListener(ToggleFps);
            EventHandler.OnLoadData += ApplyFps;
            ApplyFps();
        }

        private void OnDestroy()
        {
            if (fullscreenWindowButton != null)
                fullscreenWindowButton.onClick.RemoveListener(SetFullscreenWindow);
            if (windowButton != null)
                windowButton.onClick.RemoveListener(SetWindowed);
            if (fpsButton != null)
                fpsButton.onClick.RemoveListener(ToggleFps);
            EventHandler.OnLoadData -= ApplyFps;
        }

        private static void SetFullscreenWindow()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        private static void SetWindowed()
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        private void ToggleFps()
        {
            StaticReferences.TargetFps = StaticReferences.TargetFps == Fps60 ? Fps120 : Fps60;
            ApplyFps();
        }

        private void ApplyFps()
        {
            if (StaticReferences.TargetFps == 0)
                StaticReferences.TargetFps = Fps60;
            Application.targetFrameRate = StaticReferences.TargetFps;
            UpdateFpsButtonText();
        }

        private void UpdateFpsButtonText()
        {
            if (fpsButtonText != null)
                fpsButtonText.text = $"FPS: {StaticReferences.TargetFps}";
        }
    }
}
