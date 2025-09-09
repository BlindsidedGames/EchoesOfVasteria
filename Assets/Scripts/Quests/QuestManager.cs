#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;
using static Blindsided.EventHandler;
using static Blindsided.SaveData.StaticReferences;
using static TimelessEchoes.TELogger;
using Resources = UnityEngine.Resources;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Handles quest logic and progress tracking.
    /// </summary>
    public class QuestManager : Singleton<QuestManager>
    {
        private ResourceManager resourceManager;
        private EnemyKillTracker killTracker;
        private DiscipleGenerationManager generationManager;
        private QuestUIManager uiManager;
        private GameplayStatTracker statTracker;
        private TimelessEchoes.Upgrades.CauldronManager cauldronManager;

        [SerializeField] private string questResourcePath = "Quests";
        private List<QuestData> quests = new();

        private readonly Dictionary<string, QuestInstance> active = new();
        private bool statEventsSubscribed;
        // Prevent re-entrant noticeboard rebuilds which can duplicate entries
        private bool _isRefreshingNoticeboard;
        private bool _pendingRefresh;

        private class QuestInstance
        {
            public QuestData data;
            public QuestEntryUI ui;
            public readonly Dictionary<EnemyData, double> killCounts = new();
            public readonly Dictionary<BuffRecipe, int> buffCastCounts = new();
            public double anyKillCount;
            public bool ReadyForTurnIn;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            quests = new List<QuestData>(Blindsided.Utilities.AssetCache.GetAll<QuestData>(questResourcePath));
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
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);

            if (resourceManager != null)
                resourceManager.OnInventoryChanged += OnInventoryChangedDebounced;
            if (killTracker != null)
                killTracker.OnKillRegistered += OnKill;
            EnsureStatTrackerSubscriptions();
            var bm = BuffManager.Instance;
            if (bm != null)
                bm.OnBuffCast += OnBuffCast;
            var rm = ResourceManager.Instance;
            if (rm != null)
                rm.OnResourceAdded += OnResourceAdded;
            cauldronManager = TimelessEchoes.Upgrades.CauldronManager.Instance;
            if (cauldronManager != null)
                cauldronManager.OnResourcesMixed += OnResourcesMixed;

            LoadState();
            StartCoroutine(DelayedProgressUpdate());
            StartCoroutine(SubscribeStatTrackerWhenReady());
            OnLoadData += OnLoadDataHandler;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= OnInventoryChangedDebounced;
            if (killTracker != null)
                killTracker.OnKillRegistered -= OnKill;
            if (statTracker != null && statEventsSubscribed)
            {
                statTracker.OnDistanceAdded -= OnDistanceAdded;
                statTracker.OnRunEnded -= OnRunEnded;
                statTracker.OnTaskCompletedEvent -= OnTaskCompleted;
                statEventsSubscribed = false;
            }
            var bm = BuffManager.Instance;
            if (bm != null)
                bm.OnBuffCast -= OnBuffCast;
            var rm = ResourceManager.Instance;
            if (rm != null)
                rm.OnResourceAdded -= OnResourceAdded;
            if (cauldronManager != null)
                cauldronManager.OnResourcesMixed -= OnResourcesMixed;

            OnLoadData -= OnLoadDataHandler;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            active.Clear();
            StartAvailableQuests();
            RefreshNoticeboard();
            UpdateCompletionPercentage();
        }

        private void OnKill(EnemyData stats)
        {
            if (stats == null) return;
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;

                var updated = false;

                // Any-kill requirements (no enemies listed)
                var hasAnyKillReq = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req != null && req.type == QuestData.RequirementType.Kill && (req.enemies == null || req.enemies.Count == 0))
                    {
                        hasAnyKillReq = true;
                        break;
                    }
                }
                if (hasAnyKillReq)
                {
                    inst.anyKillCount += 1;
                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var recAny))
                    {
                        recAny.KillProgress ??= new Dictionary<string, double>();
                        recAny.KillProgress["ANY"] = inst.anyKillCount;
                    }
                    updated = true;
                }

                // Specific-enemy kill requirements
                if (ContainsEnemy(inst.data, stats))
                {
                    if (!inst.killCounts.ContainsKey(stats))
                        inst.killCounts[stats] = 0;
                    inst.killCounts[stats] += 1;

                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                    {
                        rec.KillProgress ??= new Dictionary<string, double>();
                        rec.KillProgress[stats.name] = inst.killCounts[stats];
                    }
                    updated = true;
                }

                if (updated)
                    UpdateProgress(inst);
            }
        }

        private void OnDistanceAdded(float dist)
        {
            if (dist <= 0f) return;
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;
                var needsUpdate = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req == null) continue;
                    if (req.type == QuestData.RequirementType.DistanceTravel)
                    {
                        // Accumulate exact travel delta at quest granularity to avoid float cancellation
                        if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                            rec.DistanceTravelProgress += dist;
                        needsUpdate = true;
                    }
                }
                if (needsUpdate)
                    UpdateProgress(inst);
            }
        }

        private void OnRunEnded(bool died)
        {
            foreach (var inst in active.Values)
                UpdateProgress(inst);
        }

        private void OnTaskCompleted()
        {
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;
                var has = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req != null && req.type == QuestData.RequirementType.TasksCompleted)
                    {
                        has = true;
                        break;
                    }
                }
                if (has)
                    UpdateProgress(inst);
            }
        }

        private void OnResourcesMixed(int amount)
        {
            if (amount <= 0) return;
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;
                var has = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req != null && req.type == QuestData.RequirementType.CauldronMix)
                    {
                        has = true;
                        break;
                    }
                }
                if (!has) continue;

                if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                {
                    rec.CauldronMixProgress += amount;
                }
                UpdateProgress(inst);
            }
        }

        private void OnResourceAdded(Resource resource, double amount, bool bonus)
        {
            if (amount <= 0) return;
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;
                var hasGatherReq = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req != null && req.type == QuestData.RequirementType.ResourcesGathered)
                    {
                        hasGatherReq = true;
                        break;
                    }
                }
                if (hasGatherReq)
                    UpdateProgress(inst);
            }
        }

        private static bool ContainsEnemy(QuestData data, EnemyData stats)
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

        private void OnBuffCast(BuffRecipe recipe, bool isAuto)
        {
            if (recipe == null) return;
            foreach (var inst in active.Values)
            {
                if (inst?.data?.requirements == null) continue;
                var dirty = false;
                foreach (var req in inst.data.requirements)
                {
                    if (req == null || req.type != QuestData.RequirementType.BuffCast) continue;
                    // If specific buffs configured, only track those; else rely on baseline via UpdateProgress
                    if (req.buffs != null && req.buffs.Count > 0)
                    {
                        if (!req.buffs.Contains(recipe)) continue;
                        if (!req.includeAutoCasts && isAuto) continue;
                        if (!inst.buffCastCounts.ContainsKey(recipe))
                            inst.buffCastCounts[recipe] = 0;
                        inst.buffCastCounts[recipe] += 1;
                        if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                        {
                            rec.BuffCastProgress ??= new System.Collections.Generic.Dictionary<string, int>();
                            var key = recipe.name;
                            rec.BuffCastProgress[key] = inst.buffCastCounts[recipe];
                        }
                        dirty = true;
                    }
                }
                if (dirty)
                    UpdateProgress(inst);
            }
        }

        // Debounced variant to avoid recomputing quest progress many times in the same frame
        private int _lastProgressUpdateFrame = -1;
        private void OnInventoryChangedDebounced()
        {
            var frame = Time.frameCount;
            if (frame == _lastProgressUpdateFrame)
                return;
            _lastProgressUpdateFrame = frame;
            UpdateAllProgress();
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

        private static bool IsInstantQuest(QuestData quest)
        {
            if (quest == null || quest.requirements == null)
                return false;
            foreach (var req in quest.requirements)
                if (req != null && req.type == QuestData.RequirementType.Instant)
                    return true;
            return false;
        }

        private void StartAvailableQuests()
        {
            foreach (var q in quests)
            {
                if (q == null) continue;
                if (!string.IsNullOrEmpty(q.npcId) && !CompletedNpcTasks.Contains(q.npcId))
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
                    if (req.enemies != null && req.enemies.Count > 0)
                    {
                        foreach (var enemy in req.enemies)
                            if (inst.killCounts.TryGetValue(enemy, out var c))
                                total += c;
                    }
                    else
                    {
                        total = inst.anyKillCount;
                    }

                    if (req.amount > 0)
                        pct = (float)(total / req.amount);
                }
                else if (req.type == QuestData.RequirementType.DistanceRun)
                {
                    var tracker = GameplayStatTracker.Instance;
                    var best = tracker ? tracker.LongestRun : 0f;
                    if (req.amount > 0)
                        pct = best / req.amount;
                }
                else if (req.type == QuestData.RequirementType.DistanceTravel)
                {
                    double travelled = 0;
                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                        travelled = rec.DistanceTravelProgress;
                    if (req.amount > 0)
                        pct = (float)(travelled / req.amount);
                }
                else if (req.type == QuestData.RequirementType.BuffCast)
                {
                    if (req.buffs == null || req.buffs.Count == 0)
                    {
                        // Generic any-buff requirement uses baseline
                        var tracker = GameplayStatTracker.Instance;
                        var casts = tracker ? tracker.BuffsCast : 0;
                        if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec) && rec.BuffCastBaselineSet)
                            casts -= rec.BuffCastBaseline;
                        if (req.amount > 0)
                            pct = (float)casts / req.amount;
                    }
                    else
                    {
                        // Specific buff(s) requirement uses per-quest counts
                        double total = 0;
                        foreach (var b in req.buffs)
                        {
                            if (b == null) continue;
                            if (inst.buffCastCounts.TryGetValue(b, out var c))
                                total += c;
                        }
                        if (req.amount > 0)
                            pct = (float)(total / req.amount);
                    }
                }
                else if (req.type == QuestData.RequirementType.CriticalStrike)
                {
                    var tracker = GameplayStatTracker.Instance;
                    var crits = tracker ? tracker.CriticalHits : 0;
                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec) && rec.CriticalBaselineSet)
                        crits -= rec.CriticalBaseline;
                    if (req.amount > 0)
                        pct = (float)crits / req.amount;
                }
                else if (req.type == QuestData.RequirementType.ResourcesGathered)
                {
                    var tracker = GameplayStatTracker.Instance;
                    var total = tracker ? tracker.TotalResourcesGathered : 0;
                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec) && rec.ResourcesBaselineSet)
                        total -= rec.ResourcesBaseline;
                    if (req.amount > 0)
                        pct = (float)(total / req.amount);
                }
                else if (req.type == QuestData.RequirementType.TasksCompleted)
                {
                    var tracker = GameplayStatTracker.Instance;
                    var tasks = tracker ? tracker.TasksCompleted : 0;
                    if (req.amount > 0)
                        pct = (float)tasks / req.amount;
                }
                else if (req.type == QuestData.RequirementType.CauldronMix)
                {
                    double mixed = 0;
                    if (oracle.saveData.Quests.TryGetValue(inst.data.questId, out var rec))
                        mixed = rec.CauldronMixProgress;
                    if (req.amount > 0)
                        pct = (float)(mixed / req.amount);
                }
                else if (req.type == QuestData.RequirementType.Instant)
                {
                    pct = 1f;
                }
                else if (req.type == QuestData.RequirementType.Meet)
                {
                    if (!string.IsNullOrEmpty(req.meetNpcId) &&
                        CompletedNpcTasks.Contains(req.meetNpcId))
                        pct = 1f;
                }

                progress += Mathf.Clamp01(pct);
            }

            if (count > 0)
                progress /= count;

            inst.ui?.SetProgress(progress);
            inst.ui?.UpdateRequirementIcons();
            inst.ui?.UpdateGoalText(inst.data);

            var wasReady = inst.ReadyForTurnIn;
            inst.ReadyForTurnIn = progress >= 1f;
            if (wasReady != inst.ReadyForTurnIn)
            {
                // Rebuild and re-sort the noticeboard when readiness changes so ready quests float to the top.
                // If already rebuilding, defer a single refresh to avoid re-entrancy and duplicates.
                if (_isRefreshingNoticeboard)
                    _pendingRefresh = true;
                else
                    RefreshNoticeboard();
            }

            PinnedQuestUIManager.Instance?.UpdateProgress();
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

            foreach (var reward in inst.data.rewards)
                ResourceManager.Instance.Add(reward.resource, reward.amount, trackStats: false);

            record.Completed = true;
            record.CompletedTimestamp = DateTime.UtcNow.Ticks;
            // Trim transient quest progress to reduce save size
            if (record.KillProgress != null)
            {
                record.KillProgress.Clear();
                record.KillProgress = null;
            }
            if (record.BuffCastProgress != null)
            {
                record.BuffCastProgress.Clear();
                record.BuffCastProgress = null;
            }
            record.DistanceTravelProgress = 0;
            record.CauldronMixProgress = 0;
            record.BuffCastBaseline = 0;
            record.BuffCastBaselineSet = false;
            record.CriticalBaseline = 0;
            record.CriticalBaselineSet = false;
            record.ResourcesBaseline = 0;
            record.ResourcesBaselineSet = false;
            record.TasksBaseline = 0;
            record.TasksBaselineSet = false;
            if (inst.data.unlockBuffSlots > 0)
                BuffManager.Instance?.UnlockSlots(inst.data.unlockBuffSlots);
            if (inst.data.unlockAutoBuffSlots > 0)
                BuffManager.Instance?.UnlockAutoSlots(inst.data.unlockAutoBuffSlots);
            if (inst.data.maxDistanceIncrease > 0f)
                // Quest rewards should bypass the demo cap and apply to the true backing value
                // so players don't miss progression when moving to the main version.
                GameplayStatTracker.Instance?.IncreaseMaxRunDistance(inst.data.maxDistanceIncrease, oracle != null && oracle.demo);
            if (inst.data.disciplePercentReward > 0f)
            {
                oracle.saveData.DisciplePercent += inst.data.disciplePercentReward;
                DiscipleGenerationManager.Instance?.RefreshRates();
            }
            if (!string.IsNullOrEmpty(inst.data.npcId))
                CompletedNpcTasks.Add(inst.data.npcId);
            if (inst.ui != null)
                uiManager?.RemoveEntry(inst.ui);
            active.Remove(id);
            StartAvailableQuests();

            var pins = oracle.saveData.PinnedQuests;
            if (AutoPinActiveQuests && pins.Count < PinnedQuestUIManager.MaxPins)
            {
                var next = active.Values
                    .Select(q => q.data)
                    .FirstOrDefault(q => !pins.Contains(q.questId) && !IsInstantQuest(q));
                if (next != null)
                {
                    // Fill open pin slots with the next active quest when auto-pinning is enabled.
                    pins.Insert(0, next.questId);
                    PinnedQuestUIManager.Instance?.RefreshPins();
                }
            }

            RefreshNoticeboard();
            Log($"Quest {id} completed", TELogCategory.Quest, this);
            QuestHandin(id);
            UpdateCompletionPercentage();
            if (oracle.saveData.PinnedQuests.Remove(id))
                PinnedQuestUIManager.Instance?.RefreshPins();
            else
                PinnedQuestUIManager.Instance?.UpdateProgress();

            // Immediately persist rewards and quest state so saved data matches live inventory.
            try
            {
                SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Immediate save after quest completion failed: {ex}");
            }
        }

        /// <summary>
        ///     Called when an NPC with the given id is met. Starts any pending quests tied to that NPC.
        /// </summary>
        public void OnNpcMet(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            StartAvailableQuests();
#if !DISABLESTEAMWORKS
            var achievementManager = AchievementManager.Instance;
            achievementManager?.NotifyNpcMet(id);
#endif
            // Update progress first so a single noticeboard refresh reflects final readiness states.
            UpdateAllProgress();
            RefreshNoticeboard();
        }

        /// <summary>
        ///     Returns true if any active quest can be turned in.
        /// </summary>
        public bool HasQuestsReadyForTurnIn()
        {
            foreach (var inst in active.Values)
                if (inst != null && inst.ReadyForTurnIn)
                    return true;
            return false;
        }

        /// <summary>
        ///     Returns true if the quest with the given id is currently active.
        /// </summary>
        public bool IsQuestInProgress(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return false;
            return active.ContainsKey(questId);
        }

        public bool IsQuestCompleted(QuestData quest)
        {
            if (quest == null || oracle == null) return false;
            return oracle.saveData.Quests.TryGetValue(quest.questId, out var rec) && rec.Completed;
        }

        /// <summary>
        ///     Returns quest data for the given id or null if not found.
        /// </summary>
        public QuestData GetQuestData(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return null;
            foreach (var q in quests)
                if (q != null && q.questId == questId)
                    return q;
            return null;
        }

        /// <summary>
        ///     Toggle the pinned state of the quest with the given id.
        /// </summary>
        public void TogglePinned(string questId)
        {
            if (oracle == null || string.IsNullOrEmpty(questId))
                return;
            var data = GetQuestData(questId);
            if (IsInstantQuest(data))
                return;
            var pins = oracle.saveData.PinnedQuests;
            if (pins.Contains(questId))
            {
                pins.Remove(questId);
            }
            else
            {
                pins.Insert(0, questId);
                if (pins.Count > PinnedQuestUIManager.MaxPins)
                    pins.RemoveAt(pins.Count - 1);
            }
            PinnedQuestUIManager.Instance?.RefreshPins();
            RefreshNoticeboard();
        }

        private void TryStartQuest(QuestData quest)
        {
            if (quest == null) return;
            if (!string.IsNullOrEmpty(quest.npcId) && !CompletedNpcTasks.Contains(quest.npcId))
                return;
            if (!AreRequirementsMet(quest))
                return;
            if (active.ContainsKey(quest.questId))
                return;
            if (oracle.saveData.Quests.TryGetValue(quest.questId, out var rec) && rec.Completed)
                return;

            var inst = new QuestInstance { data = quest };
            var isNewQuest = false;
            if (!oracle.saveData.Quests.TryGetValue(quest.questId, out rec))
            {
                rec = new GameData.QuestRecord();
                oracle.saveData.Quests[quest.questId] = rec;
                isNewQuest = true;
            }

            // Initialize per-quest distance accumulator only for newly created quest records
            if (isNewQuest)
            {
                foreach (var req in quest.requirements)
                    if (req.type == QuestData.RequirementType.DistanceTravel)
                    {
                        rec.DistanceTravelProgress = 0;
                        break;
                    }
            }
            if (!rec.BuffCastBaselineSet)
                foreach (var req in quest.requirements)
                    if (req.type == QuestData.RequirementType.BuffCast)
                    {
                        rec.BuffCastBaseline = statTracker ? statTracker.BuffsCast : 0;
                        rec.BuffCastBaselineSet = true;
                        break;
                    }
            if (!rec.CriticalBaselineSet)
                foreach (var req in quest.requirements)
                    if (req.type == QuestData.RequirementType.CriticalStrike)
                    {
                        rec.CriticalBaseline = statTracker ? statTracker.CriticalHits : 0;
                        rec.CriticalBaselineSet = true;
                        break;
                    }
            if (!rec.ResourcesBaselineSet)
                foreach (var req in quest.requirements)
                    if (req.type == QuestData.RequirementType.ResourcesGathered)
                    {
                        rec.ResourcesBaseline = statTracker ? statTracker.TotalResourcesGathered : 0;
                        rec.ResourcesBaselineSet = true;
                        break;
                    }
            if (!rec.TasksBaselineSet)
                foreach (var req in quest.requirements)
                    if (req.type == QuestData.RequirementType.TasksCompleted)
                    {
                        rec.TasksBaseline = statTracker ? statTracker.TasksCompleted : 0;
                        rec.TasksBaselineSet = true;
                        break;
                    }

            foreach (var req in quest.requirements)
            {
                if (req.type != QuestData.RequirementType.Kill) continue;
                if (req.enemies != null && req.enemies.Count > 0)
                {
                    foreach (var enemy in req.enemies)
                    {
                        var count = 0d;
                        if (rec.KillProgress != null)
                            rec.KillProgress.TryGetValue(enemy.name, out count);
                        inst.killCounts[enemy] = count;
                    }
                }
                else
                {
                    if (rec.KillProgress != null && rec.KillProgress.TryGetValue("ANY", out var anyCount))
                        inst.anyKillCount = anyCount;
                    else
                        inst.anyKillCount = 0;
                }
            }

            // Preload per-buff counts for specific-buff requirements
            foreach (var req in quest.requirements)
            {
                if (req.type != QuestData.RequirementType.BuffCast) continue;
                if (req.buffs == null || req.buffs.Count == 0) continue;
                foreach (var b in req.buffs)
                {
                    if (b == null) continue;
                    var key = b.name;
                    var count = 0;
                    if (rec.BuffCastProgress != null)
                        rec.BuffCastProgress.TryGetValue(key, out count);
                    inst.buffCastCounts[b] = count;
                }
            }

            inst.ui = null;

            active[quest.questId] = inst;
            if (isNewQuest && (AutoPinActiveQuests || quest.autoPin) && !IsInstantQuest(quest))
            {
                var pins = oracle.saveData.PinnedQuests;
                if (!pins.Contains(quest.questId) && pins.Count < PinnedQuestUIManager.MaxPins)
                {
                    // Auto-pin newly started quests when there's room; skip if list is full.
                    pins.Insert(0, quest.questId);
                    PinnedQuestUIManager.Instance?.RefreshPins();
                }
            }

            UpdateProgress(inst);

            PinnedQuestUIManager.Instance?.UpdateProgress();
        }

        public void RefreshNoticeboard()
        {
            if (uiManager == null)
                return;
            // Guard against re-entrant rebuilds from progress updates during a refresh cycle
            if (_isRefreshingNoticeboard)
            {
                _pendingRefresh = true;
                return;
            }
            _isRefreshingNoticeboard = true;

            uiManager.Clear();
            foreach (var inst in active.Values)
                UpdateProgress(inst);

            var pins = oracle != null ? oracle.saveData?.PinnedQuests : null;

            // Ready category (pinned first in pin order, then unpinned by name)
            var readyCount = 0;
            var pinnedCount = 0;
            var activeCount = 0;
            var completedCount = 0;
            if (pins != null)
            {
                foreach (var pid in pins)
                {
                    if (string.IsNullOrEmpty(pid)) continue;
                    if (!active.TryGetValue(pid, out var pinnedInst) || pinnedInst == null) continue;
                    if (!pinnedInst.ReadyForTurnIn) continue;
                    readyCount++;
                    pinnedInst.ui = uiManager.CreateEntry(
                        pinnedInst.data,
                        () => CompleteQuest(pinnedInst),
                        showRequirements: true,
                        completed: false,
                        QuestUIManager.QuestCategory.Ready);
                    UpdateProgress(pinnedInst);
                }
            }

            var readyUnpinned = active.Values
                .Where(i => i != null && i.ReadyForTurnIn && !(pins?.Contains(i.data.questId) ?? false))
                .OrderBy(i => i.data.questName != null ? i.data.questName.GetLocalizedString() : string.Empty);
            foreach (var inst in readyUnpinned)
            {
                readyCount++;
                inst.ui = uiManager.CreateEntry(
                    inst.data,
                    () => CompleteQuest(inst),
                    showRequirements: true,
                    completed: false,
                    QuestUIManager.QuestCategory.Ready);
                UpdateProgress(inst);
            }

            // Pinned (not ready), in saved order
            if (pins != null)
            {
                foreach (var pid in pins)
                {
                    if (string.IsNullOrEmpty(pid)) continue;
                    if (!active.TryGetValue(pid, out var pinnedInst) || pinnedInst == null) continue;
                    if (pinnedInst.ReadyForTurnIn) continue; // already shown in Ready
                    pinnedCount++;
                    pinnedInst.ui = uiManager.CreateEntry(
                        pinnedInst.data,
                        () => CompleteQuest(pinnedInst),
                        showRequirements: true,
                        completed: false,
                        QuestUIManager.QuestCategory.Pinned);
                    UpdateProgress(pinnedInst);
                }
            }

            // Active (unpinned and not ready), order by name
            var orderedUnpinned = active.Values
                .Where(i => i != null && !i.ReadyForTurnIn && !(pins?.Contains(i.data.questId) ?? false))
                .OrderBy(i => i.data.questName != null ? i.data.questName.GetLocalizedString() : string.Empty);
            foreach (var inst in orderedUnpinned)
            {
                activeCount++;
                inst.ui = uiManager.CreateEntry(
                    inst.data,
                    () => CompleteQuest(inst),
                    showRequirements: true,
                    completed: false,
                    QuestUIManager.QuestCategory.Active);
                UpdateProgress(inst);
            }

            // Completed (most recent first)
            var orderedCompleted = quests
                .Where(q => q != null && !active.ContainsKey(q.questId))
                .Select(q => new { data = q, rec = oracle.saveData.Quests.TryGetValue(q.questId, out var r) ? r : null })
                .Where(x => x.rec != null && x.rec.Completed)
                .OrderByDescending(x => x.rec.CompletedTimestamp)
                .Select(x => x.data);
            foreach (var q in orderedCompleted)
            {
                completedCount++;
                uiManager.CreateEntry(q, null, false, true);
            }

            uiManager.UpdateCategoryVisibility(readyCount, pinnedCount, activeCount, completedCount);

            // End of guarded section
            _isRefreshingNoticeboard = false;
            if (_pendingRefresh)
            {
                _pendingRefresh = false;
                RefreshNoticeboard();
            }
        }


        private void OnLoadDataHandler()
        {
            LoadState();
            StartCoroutine(DelayedProgressUpdate());
            EnsureStatTrackerSubscriptions();
        }

        private IEnumerator DelayedProgressUpdate()
        {
            yield return null;
            UpdateAllProgress();
        }

        private void EnsureStatTrackerSubscriptions()
        {
            if (statEventsSubscribed)
                return;
            var st = statTracker ?? GameplayStatTracker.Instance;
            if (st == null)
                return;
            statTracker = st;
            statTracker.OnDistanceAdded += OnDistanceAdded;
            statTracker.OnRunEnded += OnRunEnded;
            statTracker.OnTaskCompletedEvent += OnTaskCompleted;
            statEventsSubscribed = true;
        }

        private IEnumerator SubscribeStatTrackerWhenReady()
        {
            // Try for a short period in case initialization order delays stat tracker creation
            for (var i = 0; i < 240 && !statEventsSubscribed; i++)
            {
                EnsureStatTrackerSubscriptions();
                if (statEventsSubscribed)
                    yield break;
                yield return null;
            }
        }
    }
}
