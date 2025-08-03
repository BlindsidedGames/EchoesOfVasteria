using System;
using System.Collections;
using System.Globalization;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventHandler = Blindsided.EventHandler;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Controls the Settings panel UI.
    ///     Currently handles switching between window modes.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject VersionNumberObject;

        [TabGroup("Settings", "Window")] [SerializeField] [Space]
        private Button fullscreenWindowButton;

        [TabGroup("Settings", "Window")] [SerializeField]
        private Button windowButton;

        [TabGroup("Settings", "Performance")] [SerializeField]
        private Button fpsButton;

        [TabGroup("Settings", "Performance")] [SerializeField]
        private TMP_Text fpsButtonText;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Slider dropTextDurationSlider;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private TMP_Text dropTextDurationText;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Slider playerDamageDurationSlider;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private TMP_Text playerDamageDurationText;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Slider enemyDamageDurationSlider;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private TMP_Text enemyDamageDurationText;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Button playerDamageButton;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Button enemyDamageButton;

        [TabGroup("Settings", "Floating Text")] [SerializeField]
        private Button dropTextButton;

        [TabGroup("Settings", "Sprites")] [SerializeField]
        private Sprite onSprite;

        [TabGroup("Settings", "Sprites")] [SerializeField]
        private Sprite offSprite;

        private Image playerDamageImage;
        private Image enemyDamageImage;
        private Image dropTextImage;

        [TabGroup("Settings", "Save Files")] [SerializeField]
        private SaveSlotReferences saveSlot1;

        [TabGroup("Settings", "Save Files")] [SerializeField]
        private SaveSlotReferences saveSlot2;

        [TabGroup("Settings", "Save Files")] [SerializeField]
        private SaveSlotReferences saveSlot3;

        private SaveSlotReferences[] saveSlots;

        private const int Fps60 = 60;
        private const int Fps120 = 120;

        private void Awake()
        {
            saveSlots = new[] { saveSlot1, saveSlot2, saveSlot3 };

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

            if (saveSlots != null)
            {
                for (var i = 0; i < saveSlots.Length; i++)
                {
                    var index = i;
                    var slot = saveSlots[index];
                    if (slot == null)
                        continue;
                    if (slot.saveButton != null)
                        slot.saveButton.onClick.AddListener(() => OnSave(index));
                    if (slot.loadDeleteButton != null)
                        slot.loadDeleteButton.onClick.AddListener(() => OnLoadOrDelete(index));
                    if (slot.toggleSafetyButton != null)
                    {
                        slot.toggleSafetyButton.onClick.AddListener(() => ToggleSafety(index));
                        slot.safetyToggleImage = slot.toggleSafetyButton.GetComponent<Image>();
                        UpdateButtonVisual(slot.safetyToggleImage, slot.safetyEnabled);
                        if (slot.loadDeleteText != null)
                            slot.loadDeleteText.text = slot.safetyEnabled ? "Delete" : "Load";
                    }
                }

                for (var i = 0; i < saveSlots.Length; i++)
                {
                    StartCoroutine(LoadSlotAsync(i));
                }
            }

            EventHandler.OnLoadData += ApplyFps;
            EventHandler.OnLoadData += RefreshAllSlots;
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
            if (saveSlots != null)
                foreach (var slot in saveSlots)
                {
                    if (slot == null)
                        continue;
                    if (slot.saveButton != null)
                        slot.saveButton.onClick.RemoveAllListeners();
                    if (slot.loadDeleteButton != null)
                        slot.loadDeleteButton.onClick.RemoveAllListeners();
                    if (slot.toggleSafetyButton != null)
                        slot.toggleSafetyButton.onClick.RemoveAllListeners();
                }

            EventHandler.OnLoadData -= ApplyFps;
            EventHandler.OnLoadData -= RefreshAllSlots;
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

        private void Update()
        {
            UpdateSlotInfo();
        }

        private void UpdateSlotInfo()
        {
            if (saveSlots == null)
                return;
            for (var i = 0; i < saveSlots.Length; i++)
                UpdateSlotDynamic(i);
        }

        private IEnumerator LoadSlotAsync(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                yield break;

            var slot = saveSlots[index];
            if (slot == null)
                yield break;

            var oracle = Oracle.oracle;
            var prefix = oracle.beta ? $"Beta{oracle.betaSaveIteration}" : "";
            var fileName = $"{prefix}Sd{index}.es3";

            if (index != oracle.CurrentSlot)
            {
                if (slot.fileNameText != null)
                    slot.fileNameText.text = $"File {index + 1} | Loading...";

                if (!SteamCloudManager.DownloadFile(fileName) && !ES3.FileExists(fileName))
                {
                    RefreshSlot(index);
                    yield break;
                }

                while (!ES3.FileExists(fileName))
                    yield return null;

                ES3.CacheFile(fileName);
            }

            RefreshSlot(index);
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

        private void OnSave(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            SaveSlot(index);
            RefreshSlot(index);
        }

        private void OnLoadOrDelete(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            var slot = saveSlots[index];
            if (slot == null)
                return;

            if (slot.safetyEnabled)
            {
                DeleteSlot(index);
                if (index == Oracle.oracle.CurrentSlot)
                {
                    Oracle.oracle.WipeCloudData();
                }
                else
                {
                    RefreshSlot(index);
                }
            }
            else
            {
                if (index == Oracle.oracle.CurrentSlot)
                    return;
                SaveSlot(Oracle.oracle.CurrentSlot);
                Oracle.oracle.SelectSlot(index);
                EventHandler.ResetData();
                EventHandler.LoadData();
                RefreshAllSlots();
            }
        }

        private void ToggleSafety(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            var slot = saveSlots[index];
            if (slot == null)
                return;
            slot.safetyEnabled = !slot.safetyEnabled;
            UpdateButtonVisual(slot.safetyToggleImage, slot.safetyEnabled);
            if (slot.loadDeleteText != null)
                slot.loadDeleteText.text = slot.safetyEnabled ? "Delete" : "Load";
            UpdateSlotInteractivity(index);
        }

        private void SaveSlot(int index)
        {
            var oracle = Oracle.oracle;
            EventHandler.SaveData();
            oracle.saveData.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            var prefix = oracle.beta ? $"Beta{oracle.betaSaveIteration}" : "";
            var dataName = $"{prefix}Data{index}";
            var fileName = $"{prefix}Sd{index}.es3";
            var settings = new ES3Settings(fileName, ES3.Location.Cache);
            ES3.Save(dataName, oracle.saveData, settings);
            ES3.StoreCachedFile(fileName);
            SteamCloudManager.UploadFile(fileName);
        }

        private void DeleteSlot(int index)
        {
            var oracle = Oracle.oracle;
            var prefix = oracle.beta ? $"Beta{oracle.betaSaveIteration}" : "";
            var fileName = $"{prefix}Sd{index}.es3";
            ES3.DeleteFile(fileName, new ES3Settings(ES3.Location.Cache));
            ES3.DeleteFile(fileName);
            SteamCloudManager.DeleteFile(fileName);
        }

        private void UpdateSlotInteractivity(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            var slot = saveSlots[index];
            if (slot == null)
                return;

            var isCurrent = index == Oracle.oracle.CurrentSlot;
            var safety = slot.safetyEnabled;

            if (slot.saveButton != null)
                slot.saveButton.interactable = isCurrent || safety;

            if (slot.loadDeleteButton != null)
            {
                var inTown = GameManager.Instance == null || GameManager.Instance.CurrentMap == null;
                slot.loadDeleteButton.interactable = safety || (inTown && !isCurrent);
            }
        }

        private void RefreshAllSlots()
        {
            if (saveSlots == null)
                return;
            for (var i = 0; i < saveSlots.Length; i++)
                RefreshSlot(i);
        }

        private void RefreshSlot(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            var slot = saveSlots[index];
            if (slot == null)
                return;

            var oracle = Oracle.oracle;
            var prefix = oracle.beta ? $"Beta{oracle.betaSaveIteration}" : "";
            var fileName = $"{prefix}Sd{index}.es3";
            var dataName = $"{prefix}Data{index}";

            float completion = 0f;

            if (index == oracle.CurrentSlot)
            {
                completion = oracle.saveData.CompletionPercentage;
                slot.lastPlayed = string.IsNullOrEmpty(oracle.saveData.DateQuitString)
                    ? null
                    : DateTime.Parse(oracle.saveData.DateQuitString, CultureInfo.InvariantCulture);
            }
            else if (ES3.FileExists(fileName))
            {
                try
                {
                    var data = ES3.Load<GameData>(dataName, new ES3Settings(fileName));
                    completion = data.CompletionPercentage;
                    if (slot.playtimeText != null)
                        slot.playtimeText.text = data.PlayTime > 0
                            ? $"Playtime: {CalcUtils.FormatTime(data.PlayTime, shortForm: true)}"
                            : "Playtime: None";
                    slot.lastPlayed = string.IsNullOrEmpty(data.DateQuitString)
                        ? null
                        : DateTime.Parse(data.DateQuitString, CultureInfo.InvariantCulture);
                }
                catch
                {
                    if (slot.playtimeText != null)
                        slot.playtimeText.text = "Playtime: None";
                    slot.lastPlayed = null;
                }
            }
            else
            {
                if (slot.playtimeText != null)
                    slot.playtimeText.text = "Playtime: None";
                slot.lastPlayed = null;
            }

            slot.completionPercentage = completion;

            var display = $"File {index + 1} | {slot.completionPercentage:0}%";
            if (index == oracle.CurrentSlot)
                display += " - Active";
            if (slot.fileNameText != null)
                slot.fileNameText.text = display;

            UpdateSlotDynamic(index);

            if (slot.loadDeleteText != null)
                slot.loadDeleteText.text = slot.safetyEnabled ? "Delete" : "Load";

            UpdateSlotInteractivity(index);
        }

        private void UpdateSlotDynamic(int index)
        {
            if (saveSlots == null || index >= saveSlots.Length)
                return;
            var slot = saveSlots[index];
            if (slot == null)
                return;

            if (index == Oracle.oracle.CurrentSlot)
            {
                slot.completionPercentage = Oracle.oracle.saveData.CompletionPercentage;
                if (slot.fileNameText != null)
                    slot.fileNameText.text =
                        $"File {index + 1} | {slot.completionPercentage:0}% - Active";

                var playtime = Oracle.oracle.saveData.PlayTime;
                if (slot.playtimeText != null)
                    slot.playtimeText.text = playtime > 0
                        ? $"Playtime: {CalcUtils.FormatTime(playtime, shortForm: true)}"
                        : "Playtime: None";

                if (slot.lastPlayed.HasValue)
                {
                    var diff = DateTime.UtcNow - slot.lastPlayed.Value;
                    slot.lastPlayedText.text =
                        $"Last Save: {CalcUtils.FormatTime(diff.TotalSeconds, shortForm: true)}";
                }
                else
                {
                    slot.lastPlayedText.text = "Last Save: Never";
                }
            }
            else
            {
                if (slot.fileNameText != null)
                    slot.fileNameText.text =
                        $"File {index + 1} | {slot.completionPercentage:0}%";

                if (slot.lastPlayed.HasValue)
                {
                    var date = slot.lastPlayed.Value.ToLocalTime().ToString("g");
                    var diff = DateTime.UtcNow - slot.lastPlayed.Value;
                    slot.lastPlayedText.text =
                        $"Last Played: {date} • {CalcUtils.FormatTime(diff.TotalSeconds, shortForm: true)} ago";
                }
                else
                {
                    slot.lastPlayedText.text = "Last Played: Never";
                }
            }
        }
    }
}