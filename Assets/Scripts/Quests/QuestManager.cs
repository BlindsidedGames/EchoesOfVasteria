using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Enemies;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Regen;
using UnityEngine;
using static Blindsided.Oracle;
using static Blindsided.EventHandler;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Handles quest logic and progress tracking.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        private ResourceManager resourceManager;
        private EnemyKillTracker killTracker;
        private GenerationManager generationManager;
        private QuestUIManager uiManager;
        [SerializeField] private List<QuestData> startingQuests = new();

        private readonly Dictionary<string, QuestInstance> active = new();

        private class QuestInstance
        {
            public QuestData data;
            public QuestEntryUI ui;
            public readonly Dictionary<EnemyStats, double> killCounts = new();
        }

        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            killTracker = EnemyKillTracker.Instance;
            if (killTracker == null)
                TELogger.Log("EnemyKillTracker missing", TELogCategory.Combat, this);
            generationManager = GenerationManager.Instance;
            if (generationManager == null)
                TELogger.Log("GenerationManager missing", TELogCategory.General, this);
            uiManager = QuestUIManager.Instance;
            if (uiManager == null)
                TELogger.Log("QuestUIManager missing", TELogCategory.Quest, this);

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += UpdateAllProgress;
            if (killTracker != null)
                killTracker.OnKillRegistered += OnKill;

            LoadState();
            StartCoroutine(DelayedProgressUpdate());
            OnLoadData += OnLoadDataHandler;
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= UpdateAllProgress;
            if (killTracker != null)
                killTracker.OnKillRegistered -= OnKill;
            OnLoadData -= OnLoadDataHandler;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            active.Clear();
            foreach (var q in startingQuests)
            {
                if (q == null) continue;
                if (string.IsNullOrEmpty(q.npcId) || StaticReferences.CompletedNpcTasks.Contains(q.npcId))
                    TryStartQuest(q);
            }
            foreach (var q in startingQuests)
                StartNextIfCompleted(q);
            RefreshNoticeboard();
        }

        private void OnKill(EnemyStats stats)
        {
            if (stats == null) return;
            foreach (var inst in active.Values)
            {
                if (!ContainsEnemy(inst.data, stats))
                    continue;

                if (!inst.killCounts.ContainsKey(stats))
                    inst.killCounts[stats] = 0;
                inst.killCounts[stats] += 1;

                if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                {
                    rec.KillProgress ??= new Dictionary<string, double>();
                    rec.KillProgress[stats.name] = inst.killCounts[stats];
                }

                UpdateProgress(inst);
            }
        }

        private static bool ContainsEnemy(QuestData data, EnemyStats stats)
        {
            foreach (var req in data.requirements)
                if (req.type == QuestData.RequirementType.Kill && req.enemies.Contains(stats))
                    return true;
            return false;
        }

        private void UpdateAllProgress()
        {
            foreach (var inst in active.Values)
                UpdateProgress(inst);
        }

        private void UpdateProgress(QuestInstance inst)
        {
            var progress = 0f;
            var count = 0;
            foreach (var req in inst.data.requirements)
            {
                count++;
                var pct = 0f;
                if (req.type == QuestData.RequirementType.Resource)
                {
                    var have = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                    if (req.amount > 0)
                        pct = (float)(have / req.amount);
                }
                else if (req.type == QuestData.RequirementType.Kill)
                {
                    double total = 0;
                    foreach (var enemy in req.enemies)
                    {
                        if (inst.killCounts.TryGetValue(enemy, out var c))
                            total += c;
                    }

                    if (req.amount > 0)
                        pct = (float)(total / req.amount);
                }
                else if (req.type == QuestData.RequirementType.Donation)
                {
                    double donated = RegenManager.Instance ?
                        RegenManager.Instance.GetDonationTotal(req.resource) : 0;
                    if (req.amount > 0)
                        pct = (float)(donated / req.amount);
                }

                progress += Mathf.Clamp01(pct);
            }

            if (count > 0)
                progress /= count;

            inst.ui?.SetProgress(progress);
        }

        private void CompleteQuest(QuestInstance inst)
        {
            if (inst == null) return;
            var id = inst.data.questId;
            if (!oracle.saveData.Quests.TryGetValue(id, out var record))
            {
                record = new GameData.QuestRecord();
                oracle.saveData.Quests[id] = record;
            }

            if (resourceManager != null)
            {
                foreach (var req in inst.data.requirements)
                    if (req.type == QuestData.RequirementType.Resource)
                        resourceManager.Spend(req.resource, req.amount);
            }

            record.Completed = true;
            if (inst.data.unlockPrefab != null)
                Instantiate(inst.data.unlockPrefab);
            foreach (var obj in inst.data.unlockObjects)
                if (obj != null)
                    obj.SetActive(true);
            if (!string.IsNullOrEmpty(inst.data.npcId))
                StaticReferences.CompletedNpcTasks.Add(inst.data.npcId);
            if (inst.ui != null)
                uiManager?.RemoveEntry(inst.ui);
            active.Remove(id);
            if (inst.data.nextQuest != null)
                TryStartQuest(inst.data.nextQuest);

            RefreshNoticeboard();
            TELogger.Log($"Quest {id} completed", TELogCategory.Quest, this);
            QuestHandin(id);
        }

        /// <summary>
        /// Called when an NPC with the given id is met. Starts any pending quests tied to that NPC.
        /// </summary>
        public void OnNpcMet(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (var q in startingQuests)
            {
                if (q == null) continue;
                if (q.npcId == id)
                    TryStartQuest(q);
            }
            var achievementManager = AchievementManager.Instance;
            achievementManager?.NotifyNpcMet(id);
            RefreshNoticeboard();
        }

        private void TryStartQuest(QuestData quest)
        {
            if (quest == null) return;
            if (!string.IsNullOrEmpty(quest.npcId) && !StaticReferences.CompletedNpcTasks.Contains(quest.npcId))
                return;
            if (oracle.saveData.Quests.TryGetValue(quest.questId, out var rec) && rec.Completed)
                return;

            var inst = new QuestInstance { data = quest };
            if (!oracle.saveData.Quests.TryGetValue(quest.questId, out rec))
            {
                rec = new GameData.QuestRecord();
                oracle.saveData.Quests[quest.questId] = rec;
            }

            foreach (var req in quest.requirements)
            {
                if (req.type != QuestData.RequirementType.Kill) continue;
                foreach (var enemy in req.enemies)
                {
                    if (!rec.KillProgress.TryGetValue(enemy.name, out var count))
                        count = 0;
                    inst.killCounts[enemy] = count;
                }
            }

            inst.ui = null;
            active[quest.questId] = inst;
            UpdateProgress(inst);
        }

        private void RefreshNoticeboard()
        {
            if (uiManager == null)
                return;
            uiManager.Clear();
            foreach (var inst in active.Values)
            {
                inst.ui = uiManager.CreateEntry(inst.data, () => CompleteQuest(inst));
                UpdateProgress(inst);
            }
        }

        private void StartNextIfCompleted(QuestData quest)
        {
            if (quest == null || quest.nextQuest == null)
                return;

            if (oracle.saveData.Quests.TryGetValue(quest.questId, out var rec) && rec.Completed)
            {
                var next = quest.nextQuest;
                if (!oracle.saveData.Quests.TryGetValue(next.questId, out var nextRec) || !nextRec.Completed)
                {
                    if (!active.ContainsKey(next.questId))
                        TryStartQuest(next);
                }

                // recursively process the chain in case multiple quests were completed
                StartNextIfCompleted(next);
            }
        }

        private void OnLoadDataHandler()
        {
            LoadState();
            StartCoroutine(DelayedProgressUpdate());
        }

        private IEnumerator DelayedProgressUpdate()
        {
            yield return null;
            UpdateAllProgress();
        }
    }}