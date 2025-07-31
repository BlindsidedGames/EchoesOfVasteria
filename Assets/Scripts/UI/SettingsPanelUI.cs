using System.Collections;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
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
        [SerializeField] private GameObject VersionNumberObject;
        [SerializeField] [Space] private Button fullscreenWindowButton;
        [SerializeField] private Button windowButton;
        [SerializeField] private Button fpsButton;
        [SerializeField] private TMP_Text fpsButtonText;
        [SerializeField] private Slider dropTextDurationSlider;
        [SerializeField] private TMP_Text dropTextDurationText;
        [SerializeField] private Slider playerDamageDurationSlider;
        [SerializeField] private TMP_Text playerDamageDurationText;
        [SerializeField] private Slider enemyDamageDurationSlider;
        [SerializeField] private TMP_Text enemyDamageDurationText;
        [SerializeField] private Button playerDamageButton;
        [SerializeField] private Button enemyDamageButton;
        [SerializeField] private Button dropTextButton;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        private Image playerDamageImage;
        private Image enemyDamageImage;
        private Image dropTextImage;

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
            if (dropTextDurationSlider != null)
                dropTextDurationSlider.onValueChanged.AddListener(OnDropDurationChanged);
            if (playerDamageDurationSlider != null)
                playerDamageDurationSlider.onValueChanged.AddListener(OnPlayerDurationChanged);
            if (enemyDamageDurationSlider != null)
                enemyDamageDurationSlider.onValueChanged.AddListener(OnEnemyDurationChanged);
            if (playerDamageButton != null)
                playerDamageButton.onClick.AddListener(TogglePlayerDamage);
            if (enemyDamageButton != null)
                enemyDamageButton.onClick.AddListener(ToggleEnemyDamage);
            if (dropTextButton != null)
                dropTextButton.onClick.AddListener(ToggleDropText);

            playerDamageImage = playerDamageButton != null ? playerDamageButton.GetComponent<Image>() : null;
            enemyDamageImage = enemyDamageButton != null ? enemyDamageButton.GetComponent<Image>() : null;
            dropTextImage = dropTextButton != null ? dropTextButton.GetComponent<Image>() : null;

            EventHandler.OnLoadData += ApplyFps;
            ApplyFps();
            StartCoroutine(DeferredInit());
        }

        private void OnDestroy()
        {
            if (fullscreenWindowButton != null)
                fullscreenWindowButton.onClick.RemoveListener(SetFullscreenWindow);
            if (windowButton != null)
                windowButton.onClick.RemoveListener(SetWindowed);
            if (fpsButton != null)
                fpsButton.onClick.RemoveListener(ToggleFps);
            if (dropTextDurationSlider != null)
                dropTextDurationSlider.onValueChanged.RemoveListener(OnDropDurationChanged);
            if (playerDamageDurationSlider != null)
                playerDamageDurationSlider.onValueChanged.RemoveListener(OnPlayerDurationChanged);
            if (enemyDamageDurationSlider != null)
                enemyDamageDurationSlider.onValueChanged.RemoveListener(OnEnemyDurationChanged);
            if (playerDamageButton != null)
                playerDamageButton.onClick.RemoveListener(TogglePlayerDamage);
            if (enemyDamageButton != null)
                enemyDamageButton.onClick.RemoveListener(ToggleEnemyDamage);
            if (dropTextButton != null)
                dropTextButton.onClick.RemoveListener(ToggleDropText);
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

        private IEnumerator DeferredInit()
        {
            yield return null; // wait one frame for data load
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (dropTextDurationSlider != null)
                dropTextDurationSlider.value = StaticReferences.DropFloatingTextDuration / 10f;
            if (playerDamageDurationSlider != null)
                playerDamageDurationSlider.value = StaticReferences.PlayerDamageTextDuration / 2f;
            if (enemyDamageDurationSlider != null)
                enemyDamageDurationSlider.value = StaticReferences.EnemyDamageTextDuration / 2f;
            UpdateDurationTexts();
            UpdateButtonVisual(playerDamageImage, StaticReferences.PlayerFloatingDamage);
            UpdateButtonVisual(enemyDamageImage, StaticReferences.EnemyFloatingDamage);
            UpdateButtonVisual(dropTextImage, StaticReferences.ItemDropFloatingText);
        }

        private void OnDropDurationChanged(float value)
        {
            StaticReferences.DropFloatingTextDuration = value * 10f;
            UpdateDurationTexts();
        }

        private void OnPlayerDurationChanged(float value)
        {
            StaticReferences.PlayerDamageTextDuration = value * 2f;
            UpdateDurationTexts();
        }

        private void OnEnemyDurationChanged(float value)
        {
            StaticReferences.EnemyDamageTextDuration = value * 2f;
            UpdateDurationTexts();
        }

        private void TogglePlayerDamage()
        {
            StaticReferences.PlayerFloatingDamage = !StaticReferences.PlayerFloatingDamage;
            UpdateButtonVisual(playerDamageImage, StaticReferences.PlayerFloatingDamage);
        }

        private void ToggleEnemyDamage()
        {
            StaticReferences.EnemyFloatingDamage = !StaticReferences.EnemyFloatingDamage;
            UpdateButtonVisual(enemyDamageImage, StaticReferences.EnemyFloatingDamage);
        }

        private void ToggleDropText()
        {
            StaticReferences.ItemDropFloatingText = !StaticReferences.ItemDropFloatingText;
            UpdateButtonVisual(dropTextImage, StaticReferences.ItemDropFloatingText);
        }

        private void UpdateDurationTexts()
        {
            if (dropTextDurationText != null)
                dropTextDurationText.text =
                    $"Drops | {CalcUtils.FormatTime(StaticReferences.DropFloatingTextDuration, true, shortForm: true)}";
            if (playerDamageDurationText != null)
                playerDamageDurationText.text =
                    $"Enemies damage | {CalcUtils.FormatTime(StaticReferences.PlayerDamageTextDuration, true, shortForm: true)}";
            if (enemyDamageDurationText != null)
                enemyDamageDurationText.text =
                    $"Caleb's damage | {CalcUtils.FormatTime(StaticReferences.EnemyDamageTextDuration, true, shortForm: true)}";
        }

        private void UpdateButtonVisual(Image img, bool on)
        {
            if (img != null)
                img.sprite = on ? onSprite : offSprite;
        }
    }
}