using System.Collections;
using System.Collections.Generic;
using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Displays progress for pinned quests.
    /// </summary>
    public class PinnedQuestUIManager : MonoBehaviour
    {
        public static PinnedQuestUIManager Instance { get; private set; }

        [SerializeField] private QuestPinUI entryPrefab;
        [SerializeField] private Transform entryParent;

        private readonly Dictionary<string, QuestPinUI> entries = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
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

            foreach (Transform child in entryParent)
                Destroy(child.gameObject);
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
            }

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
            var resourceManager = TimelessEchoes.Upgrades.ResourceManager.Instance;
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;

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
                bool completed = rec != null && rec.Completed;

                float progress = 0f;
                int reqCount = 0;

                var sb = new StringBuilder();
                sb.AppendLine(data.questName);

                foreach (var req in data.requirements)
                {
                    double current = 0;
                    double target = req.amount;
                    float pct = 0f;

                    reqCount++;

                    switch (req.type)
                    {
                        case QuestData.RequirementType.Resource:
                            current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                            if (target > 0)
                                pct = (float)(current / target);
                            break;
                        case QuestData.RequirementType.Kill:
                            if (rec != null && req.enemies != null)
                            {
                                foreach (var enemy in req.enemies)
                                {
                                    if (rec.KillProgress.TryGetValue(enemy.name, out var c))
                                        current += c;
                                }
                            }
                            if (target > 0)
                                pct = (float)(current / target);
                            break;
                        case QuestData.RequirementType.DistanceRun:
                            current = tracker ? tracker.LongestRun : 0f;
                            if (target > 0)
                                pct = (float)current / (float)target;
                            break;
                        case QuestData.RequirementType.DistanceTravel:
                            current = tracker ? tracker.DistanceTravelled : 0f;
                            if (rec != null)
                                current -= rec.DistanceBaseline;
                            if (target > 0)
                                pct = (float)current / (float)target;
                            break;
                        case QuestData.RequirementType.BuffCast:
                            current = tracker ? tracker.BuffsCast : 0;
                            if (rec != null)
                                current -= rec.BuffCastBaseline;
                            if (target > 0)
                                pct = (float)current / (float)target;
                            break;
                        case QuestData.RequirementType.Instant:
                            current = target;
                            pct = 1f;
                            break;
                        case QuestData.RequirementType.Meet:
                            if (!string.IsNullOrEmpty(req.meetNpcId) && StaticReferences.CompletedNpcTasks.Contains(req.meetNpcId))
                            {
                                current = target;
                                pct = 1f;
                            }
                            else
                            {
                                current = 0;
                            }
                            break;
                    }

                    progress += Mathf.Clamp01(pct);

                    if (req.type == QuestData.RequirementType.Resource)
                    {
                        var name = req.resource ? req.resource.name : "";
                        if (target <= 0)
                            sb.AppendLine($"<size=90%>{name}: {CalcUtils.FormatNumber(current, true)}</size>");
                        else
                            sb.AppendLine($"<size=90%>{name}: {CalcUtils.FormatNumber(current, true)} / {CalcUtils.FormatNumber(target, true)}</size>");
                    }
                    else if (req.type == QuestData.RequirementType.Kill && !string.IsNullOrEmpty(req.killName))
                    {
                        if (target <= 0)
                            sb.AppendLine($"<size=80%>Kill {req.killName}: {CalcUtils.FormatNumber(current, true)}</size>");
                        else
                            sb.AppendLine($"<size=80%>Kill {req.killName}: {CalcUtils.FormatNumber(current, true)} / {CalcUtils.FormatNumber(target, true)}</size>");
                    }
                    else
                    {
                        if (target <= 0)
                            sb.AppendLine($"<size=80%>{CalcUtils.FormatNumber(current, true)}</size>");
                        else
                            sb.AppendLine($"<size=80%>{CalcUtils.FormatNumber(current, true)} / {CalcUtils.FormatNumber(target, true)}</size>");
                    }
                }

                ui.progressText.text = sb.ToString();

                if (reqCount > 0)
                    progress /= reqCount;
                bool ready = progress >= 1f;

                if (ui.completedImage != null)
                    ui.completedImage.enabled = completed || ready;
            }
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DeferredRefresh());
        }

        private IEnumerator DeferredRefresh()
        {
            yield return null; // wait one frame so data is loaded
            RefreshPins();
        }
    }
}

