using System.Collections.Generic;
using System.Text;
using Blindsided.Utilities;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;


namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Displays progress for pinned quests.
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class PinnedQuestUIManager : MonoBehaviour
    {
        public static PinnedQuestUIManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public const int MaxPins = 5;

        [SerializeField] private QuestPinUI entryPrefab;
        [SerializeField] private Transform entryParent;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image stateImage;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;
        [SerializeField] private GameObject rootObject;

        [SerializeField] private TimelessEchoes.UI.Toolkit.ToolkitPinnedGoals toolkitView;

        private readonly Dictionary<string, QuestPinUI> entries = new();

        private void Awake()
        {
            if (!enabled) return; // Retired presentation must not subscribe or build hidden UI.
            Instance = this;

            if (toggleButton == null)
                toggleButton = GetComponent<Button>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(OnToggle);

            ApplySavedState();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnToggle);

            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            OnLoadData += OnLoadDataHandler;
        }

        private void OnDisable()
        {
            OnLoadData -= OnLoadDataHandler;
        }

        /// <summary>
        ///     Builds UI entries for all pinned quest IDs.
        /// </summary>
        public void RefreshPins()
        {
            if (toolkitView && toolkitView.IsConfigured) { if (rootObject) rootObject.SetActive(false); return; }
            if (entryPrefab == null || entryParent == null || oracle == null)
                return;

            UIUtils.ClearChildren(entryParent);
            entries.Clear();

            foreach (var id in oracle.saveData.PinnedQuests)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                var qm = QuestManager.Instance;
                var data = qm != null ? qm.GetQuestData(id) : null;
                var instant = false;
                if (data != null && data.requirements != null)
                    foreach (var req in data.requirements)
                        if (req != null && req.type == QuestData.RequirementType.Instant)
                        {
                            instant = true;
                            break;
                        }

                if (instant)
                    continue;
                var ui = Instantiate(entryPrefab, entryParent);
                entries[id] = ui;

                if (ui.progressText != null)
                    ui.progressText.spriteAsset = ResourceIconLookup.SpriteAsset;
            }

            if (rootObject != null)
                rootObject.SetActive(entries.Count > 0);

            UpdateProgress();
        }

        /// <summary>
        ///     Updates progress text for all pinned quests.
        /// </summary>
        public void UpdateProgress()
        {
            if (toolkitView && toolkitView.IsConfigured) return;
            if (oracle == null)
                return;

            var manager = QuestManager.Instance;
            var resourceManager = ResourceManager.Instance;
            var tracker = GameplayStatTracker.Instance;

            foreach (var pair in entries)
            {
                var id = pair.Key;
                var ui = pair.Value;
                if (ui == null || ui.progressText == null)
                    continue;

                var data = manager != null ? manager.GetQuestData(id) : null;
                if (data == null)
                {
                    ui.progressText.text = id;
                    if (ui.completedImage != null)
                        ui.completedImage.enabled = false;
                    continue;
                }

                oracle.saveData.Quests.TryGetValue(id, out var rec);
                var presentation = PinnedQuestPresentation.Build(data, rec);
                ui.progressText.text = presentation.text;
                if (ui.completedImage != null) ui.completedImage.enabled = presentation.ready;
            }
        }

        private void OnLoadDataHandler()
        {
            CoroutineUtils.RunNextFrame(this, RefreshPins);
            ApplySavedState();
        }

        private void OnToggle()
        {
            var newState = !entryParent.gameObject.activeSelf;
            entryParent.gameObject.SetActive(newState);
            UpdateToggleVisual(newState);
            ShowPinnedQuests = newState;
        }

        private void ApplySavedState()
        {
            var show = ShowPinnedQuests;
            if (entryParent != null)
                entryParent.gameObject.SetActive(show);
            UpdateToggleVisual(show);
        }

        private void UpdateToggleVisual(bool show)
        {
            if (stateImage != null)
                stateImage.sprite = show ? closeSprite : openSprite;
        }

        private string FormatForQuest(QuestData data, double value)
        {
            return data != null && data.useN0ForPinnedNumbers ? value.ToString("N0") : CalcUtils.FormatNumber(value, true);
        }
    }
}
