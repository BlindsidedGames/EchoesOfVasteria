using System.Collections;
using System.Collections.Generic;
using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TMPro;
using UnityEngine;
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

        [SerializeField] private TMP_Text entryPrefab;
        [SerializeField] private Transform entryParent;

        private readonly Dictionary<string, TMP_Text> entries = new();

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
                var txt = Instantiate(entryPrefab, entryParent);
                entries[id] = txt;
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
                var text = pair.Value;
                if (text == null)
                    continue;

                var data = manager != null ? manager.GetQuestData(id) : null;
                if (data == null)
                {
                    text.text = id;
                    continue;
                }

                oracle.saveData.Quests.TryGetValue(id, out var rec);

                var sb = new StringBuilder();
                sb.AppendLine(data.questName);

                foreach (var req in data.requirements)
                {
                    double current = 0;
                    double target = req.amount;

                    switch (req.type)
                    {
                        case QuestData.RequirementType.Resource:
                            current = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
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
                            break;
                        case QuestData.RequirementType.DistanceRun:
                            current = tracker ? tracker.LongestRun : 0f;
                            break;
                        case QuestData.RequirementType.DistanceTravel:
                            current = tracker ? tracker.DistanceTravelled : 0f;
                            if (rec != null)
                                current -= rec.DistanceBaseline;
                            break;
                        case QuestData.RequirementType.Instant:
                            current = target;
                            break;
                        case QuestData.RequirementType.Meet:
                            current = !string.IsNullOrEmpty(req.meetNpcId) &&
                                      StaticReferences.CompletedNpcTasks.Contains(req.meetNpcId)
                                ? target
                                : 0;
                            break;
                    }

                    if (req.type == QuestData.RequirementType.Resource)
                    {
                        var name = req.resource ? req.resource.name : "";
                        if (target <= 0)
                            sb.AppendLine($"<size=80%>{name}: {CalcUtils.FormatNumber(current, true)}</size>");
                        else
                            sb.AppendLine($"<size=80%>{name}: {CalcUtils.FormatNumber(current, true)} / {CalcUtils.FormatNumber(target, true)}</size>");
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

                text.text = sb.ToString();
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

