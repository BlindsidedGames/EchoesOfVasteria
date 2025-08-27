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
        public const int MaxPins = 5;

        [SerializeField] private QuestPinUI entryPrefab;
        [SerializeField] private Transform entryParent;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image stateImage;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;
        [SerializeField] private GameObject rootObject;

        private readonly Dictionary<string, QuestPinUI> entries = new();

        private void Awake()
        {
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
            if (entryPrefab == null || entryParent == null || oracle == null)
                return;

            UIUtils.ClearChildren(entryParent);
            entries.Clear();

            foreach (var id in oracle.saveData.PinnedQuests)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                var qm = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
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
            if (oracle == null)
                return;

            var manager = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
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
                var completed = rec != null && rec.Completed;

                var progress = 0f;
                var reqCount = 0;

                var sb = new StringBuilder(QuestTextFormatter.BuildGoalText(data, rec, includeTitleLine: true));

                foreach (var req in data.requirements)
                {
                    var pct = 0f;
                    if (req.type == QuestData.RequirementType.Resource)
                    {
                        var current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)(current / target);
                    }
                    else if (req.type == QuestData.RequirementType.Kill)
                    {
                        double current = 0;
                        if (rec != null)
                        {
                            if (req.enemies != null && req.enemies.Count > 0)
                            {
                                foreach (var enemy in req.enemies)
                                    if (rec.KillProgress.TryGetValue(enemy.name, out var c))
                                        current += c;
                            }
                            else if (rec.KillProgress.TryGetValue("ANY", out var any))
                            {
                                current = any;
                            }
                        }
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)(current / target);
                    }
                    else if (req.type == QuestData.RequirementType.DistanceRun)
                    {
                        var current = tracker ? tracker.LongestRun : 0f;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.DistanceTravel)
                    {
                        var current = rec != null ? rec.DistanceTravelProgress : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.BuffCast)
                    {
                        double current;
                        if (req.buffs == null || req.buffs.Count == 0)
                        {
                            current = tracker ? tracker.BuffsCast : 0;
                            if (rec != null)
                                current -= rec.BuffCastBaseline;
                        }
                        else
                        {
                            current = 0;
                            if (rec != null && rec.BuffCastProgress != null)
                            {
                                foreach (var b in req.buffs)
                                {
                                    if (b == null) continue;
                                    if (rec.BuffCastProgress.TryGetValue(b.name, out var c))
                                        current += c;
                                }
                            }
                        }
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.CriticalStrike)
                    {
                        var current = tracker ? tracker.CriticalHits : 0;
                        if (rec != null)
                            current -= rec.CriticalBaseline;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.ResourcesGathered)
                    {
                        var current = tracker ? tracker.TotalResourcesGathered : 0;
                        if (rec != null)
                            current -= rec.ResourcesBaseline;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.TasksCompleted)
                    {
                        var current = tracker ? tracker.TasksCompleted : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.CauldronMix)
                    {
                        double current = rec != null ? rec.CauldronMixProgress : 0;
                        var target = req.amount;
                        if (target > 0)
                            pct = (float)current / (float)target;
                    }
                    else if (req.type == QuestData.RequirementType.Instant)
                    {
                        pct = 1f;
                    }
                    else if (req.type == QuestData.RequirementType.Meet)
                    {
                        if (!string.IsNullOrEmpty(req.meetNpcId) && CompletedNpcTasks.Contains(req.meetNpcId))
                            pct = 1f;
                    }

                    progress += Mathf.Clamp01(pct);
                    reqCount++;
                }

                if (reqCount > 0)
                    progress /= reqCount;
                var ready = progress >= 1f;

                if (completed || ready)
                {
                    var title = data.questName.GetLocalizedString();
                    ui.progressText.text = string.IsNullOrEmpty(title)
                        ? "<size=80%>Complete</size>"
                        : $"{title}\n<size=80%>Complete</size>";
                }
                else
                {
                    ui.progressText.text = sb.ToString();
                }

                if (ui.completedImage != null)
                    ui.completedImage.enabled = completed || ready;
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