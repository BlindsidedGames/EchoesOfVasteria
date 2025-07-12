using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Regen;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.Serialization;
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
        private DiscipleGenerationManager generationManager;
        private QuestUIManager uiManager;

        [FormerlySerializedAs("startingQuests")] [SerializeField]
        private List<QuestData> quests = new();

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
                Log("ResourceManager missing", TELogCategory.Resource, this);
            killTracker = EnemyKillTracker.Instance;
            if (killTracker == null)
                Log("EnemyKillTracker missing", TELogCategory.Combat, this);
            generationManager = DiscipleGenerationManager.Instance;
            if (generationManager == null)
                Log("DiscipleGenerationManager missing", TELogCategory.General, this);
            uiManager = QuestUIManager.Instance;
            if (uiManager == null)
                Log("QuestUIManager missing", TELogCategory.Quest, this);

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
            StartAvailableQuests();
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

        private bool AreRequirementsMet(QuestData quest)
        {
            if (quest == null)
                return false;
            foreach (var req in quest.requiredQuests)
            {
                if (req == null) continue;
                if (!oracle.saveData.Quests.TryGetValue(req.questId, out var rec) || !rec.Completed)
                    return false;
            }

            return true;
        }

        private void StartAvailableQuests()
        {
            foreach (var q in quests)
            {
                if (q == null) continue;
                if (!string.IsNullOrEmpty(q.npcId) && !StaticReferences.CompletedNpcTasks.Contains(q.npcId))
                    continue;
                TryStartQuest(q);
            }
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
                        if (inst.killCounts.TryGetValue(enemy, out var c))
                            total += c;

                    if (req.amount > 0)
                        pct = (float)(total / req.amount);
                }
                else if (req.type == QuestData.RequirementType.Donation)
                {
                    var donated = RegenManager.Instance ? RegenManager.Instance.GetDonationTotal(req.resource) : 0;
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
                foreach (var req in inst.data.requirements)
                    if (req.type == QuestData.RequirementType.Resource)
                        resourceManager.Spend(req.resource, req.amount);

            record.Completed = true;
            if (inst.data.unlockPrefab != null)
                Instantiate(inst.data.unlockPrefab);
            foreach (var obj in inst.data.unlockObjects)
                if (obj != null)
                    obj.SetActive(true);
            if (inst.data.unlockBuffSlots > 0)
                BuffManager.Instance?.UnlockSlots(inst.data.unlockBuffSlots);
            if (!string.IsNullOrEmpty(inst.data.npcId))
                StaticReferences.CompletedNpcTasks.Add(inst.data.npcId);
            if (inst.ui != null)
                uiManager?.RemoveEntry(inst.ui);
            active.Remove(id);
            StartAvailableQuests();

            RefreshNoticeboard();
            Log($"Quest {id} completed", TELogCategory.Quest, this);
            QuestHandin(id);
        }

        /// <summary>
        ///     Called when an NPC with the given id is met. Starts any pending quests tied to that NPC.
        /// </summary>
        public void OnNpcMet(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            StartAvailableQuests();
            var achievementManager = AchievementManager.Instance;
            achievementManager?.NotifyNpcMet(id);
            RefreshNoticeboard();
        }

        private void TryStartQuest(QuestData quest)
        {
            if (quest == null) return;
            if (!string.IsNullOrEmpty(quest.npcId) && !StaticReferences.CompletedNpcTasks.Contains(quest.npcId))
                return;
            if (!AreRequirementsMet(quest))
                return;
            if (active.ContainsKey(quest.questId))
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
    }
}