using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Enemies;
using TimelessEchoes.NpcGeneration;
using Blindsided.SaveData;
using static Blindsided.Oracle;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Handles quest logic and progress tracking.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private EnemyKillTracker killTracker;
        [SerializeField] private GenerationManager generationManager;
        [SerializeField] private QuestUIManager uiManager;
        [SerializeField] private List<QuestData> startingQuests = new();

        private readonly Dictionary<string, QuestInstance> active = new();

        private class QuestInstance
        {
            public QuestData data;
            public QuestEntryUI ui;
            public Dictionary<EnemyStats, double> baselineKills = new();
        }

        private void Awake()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (killTracker == null)
                killTracker = FindFirstObjectByType<EnemyKillTracker>();
            if (generationManager == null)
                generationManager = FindFirstObjectByType<GenerationManager>();
            if (uiManager == null)
                uiManager = FindFirstObjectByType<QuestUIManager>();

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += UpdateAllProgress;
            if (killTracker != null)
                killTracker.OnKillRegistered += OnKill;

            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();

            foreach (var q in startingQuests)
                TryStartQuest(q);

            RefreshNoticeboard();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= UpdateAllProgress;
            if (killTracker != null)
                killTracker.OnKillRegistered -= OnKill;
        }

        private void OnKill(EnemyStats stats)
        {
            if (stats == null) return;
            foreach (var inst in active.Values)
                if (ContainsEnemy(inst.data, stats))
                    UpdateProgress(inst);
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
            float progress = 0f;
            int count = 0;
            foreach (var req in inst.data.requirements)
            {
                count++;
                float pct = 0f;
                if (req.type == QuestData.RequirementType.Resource)
                {
                    double have = resourceManager ? resourceManager.GetAmount(req.resource) : 0;
                    if (req.amount > 0)
                        pct = (float)(have / req.amount);
                }
                else if (req.type == QuestData.RequirementType.Kill)
                {
                    double total = 0;
                    foreach (var enemy in req.enemies)
                    {
                        double baseVal = 0;
                        if (inst.baselineKills.TryGetValue(enemy, out var b))
                            baseVal = b;
                        double current = killTracker ? killTracker.GetKills(enemy) : 0;
                        total += current - baseVal;
                    }
                    if (req.amount > 0)
                        pct = (float)(total / req.amount);
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
            QuestHandin(id);
        }

        private void TryStartQuest(QuestData quest)
        {
            if (quest == null) return;
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
                    double kills = killTracker ? killTracker.GetKills(enemy) : 0;
                    if (!rec.KillBaseline.ContainsKey(enemy.name))
                        rec.KillBaseline[enemy.name] = kills;
                    inst.baselineKills[enemy] = rec.KillBaseline[enemy.name];
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
    }
}
