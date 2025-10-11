using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Handles skill experience, milestone tiers, and active slot management.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class SkillController : MonoBehaviour
    {
        private const int ActiveSlotUnlockLevel = 50;
        private const int MaxActiveSlots = 6;

        public static SkillController Instance { get; private set; }

        [SerializeField] private Skill combatSkill;
        [SerializeField] private List<Skill> skills = new();
        [SerializeField] private List<MilestoneSetDefinition> setDefinitions = new();

        private readonly Dictionary<Skill, SkillProgress> progress = new();
        private readonly Dictionary<Skill, Dictionary<MilestoneDefinition, MilestoneRuntimeState>> milestoneStates = new();
        private readonly MilestoneEffectAggregator effectAggregator = new();
        private readonly Dictionary<MilestoneSet, MilestoneSetDefinition> setLookup = new();
        private readonly HashSet<Skill> skillsAtActiveThreshold = new();
        private readonly Dictionary<MilestoneSet, int> activeSetCounts = new();

        private int totalActiveSlots;
        private int activeSlotsUsed;

        public Skill CombatSkill => combatSkill;
        public int TotalActiveSlots => totalActiveSlots;
        public int ActiveSlotsUsed => activeSlotsUsed;
        public MilestoneEffectAggregator Aggregator => effectAggregator;

        public event Action<Skill, float, float> OnExperienceGained;
        public event Action<Skill, int> OnLevelUp;
        public event Action<Skill, MilestoneDefinition> OnMilestoneUnlocked;
        public event Action<Skill, MilestoneDefinition, int> OnMilestoneTierChanged;
        public event Action<Skill, MilestoneDefinition, bool> OnMilestoneActivationChanged;
        public event Action OnMilestoneDataChanged;
        public event Action<int, int> OnActiveSlotsChanged;

        private void Awake()
        {
            Instance = this;
            BuildSetLookup();
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        private void BuildSetLookup()
        {
            setLookup.Clear();
            foreach (var set in setDefinitions)
            {
                if (set == null || set.Set == MilestoneSet.None)
                    continue;

                setLookup[set.Set] = set;
            }
        }

        public SkillProgress GetProgress(Skill skill)
        {
            if (skill == null)
                return null;

            progress.TryGetValue(skill, out var prog);
            return prog;
        }

        public int GetLevel(Skill skill)
        {
            return GetProgress(skill)?.Level ?? 1;
        }

        public IReadOnlyList<MilestoneDefinition> GetMilestones(Skill skill)
        {
            return skill != null ? skill.milestones : Array.Empty<MilestoneDefinition>();
        }

        public MilestoneRuntimeState GetMilestoneState(Skill skill, MilestoneDefinition definition)
        {
            if (skill == null || definition == null)
                return null;

            if (!milestoneStates.TryGetValue(skill, out var dict))
                return null;

            dict.TryGetValue(definition, out var state);
            return state;
        }

        public bool TrySetMilestoneActive(Skill skill, MilestoneDefinition definition, bool active)
        {
            if (skill == null || definition == null)
                return false;

            if (!progress.TryGetValue(skill, out var prog))
                return false;

            var state = EnsureState(skill, definition);
            if (state.TierIndex < 0 || !definition.CanActivate)
                return false;

            if (state.IsActive == active)
                return true;

            if (active && ActiveSlotsUsed >= TotalActiveSlots)
                return false;

            state.IsActive = active;
            RebuildEffectCache();
            OnMilestoneActivationChanged?.Invoke(skill, definition, active);
            return true;
        }

        public float AddExperience(Skill skill, float xpAmount)
        {
            if (skill == null || xpAmount <= 0f)
                return 0f;

            float xpMultiplier = 1f + effectAggregator.GetExperienceBonus(skill);
            if (xpMultiplier < 0f)
                xpMultiplier = 0f;
            var appliedXp = xpAmount * xpMultiplier;

            if (!progress.TryGetValue(skill, out var prog))
            {
                prog = CreateDefaultProgress();
                progress[skill] = prog;
            }

            prog.CurrentXP += appliedXp;

            var currentLevel = prog.Level;
            float xpNeeded = GetXpForLevel(skill, currentLevel);
            bool leveled = false;

            while (prog.CurrentXP >= xpNeeded)
            {
                prog.CurrentXP -= xpNeeded;
                prog.Level++;
                leveled = true;
                OnLevelUp?.Invoke(skill, prog.Level);
                xpNeeded = GetXpForLevel(skill, prog.Level);
            }

            if (leveled)
            {
                if (prog.Level >= ActiveSlotUnlockLevel && skillsAtActiveThreshold.Add(skill))
                {
                    var newTotal = Mathf.Min(skillsAtActiveThreshold.Count, MaxActiveSlots);
                    if (newTotal != totalActiveSlots)
                    {
                        totalActiveSlots = newTotal;
                        EnforceActiveSlotLimits();
                        RaiseActiveSlotsChanged();
                    }
                }

                EvaluateMilestones(skill, prog, true);
                RebuildEffectCache();
            }

            OnExperienceGained?.Invoke(skill, prog.CurrentXP, xpNeeded);
            return appliedXp;
        }

        public bool IsMilestoneUnlocked(Skill skill, MilestoneDefinition definition)
        {
            return GetMilestoneState(skill, definition)?.IsUnlocked ?? false;
        }

        public bool RollForProc(Skill skill, MilestoneProcType procType)
        {
            if (skill == null)
                return false;

            if (procType == MilestoneProcType.DoubleResources || procType == MilestoneProcType.DoubleXP)
                return GetStackingMultiplier(skill, procType) > 1;

            float chance = effectAggregator.GetProcChance(skill, procType);
            return chance > 0f && UnityEngine.Random.value <= Mathf.Clamp01(chance);
        }

        public int GetStackingMultiplier(Skill skill, MilestoneProcType procType)
        {
            if (skill == null)
                return 1;

            float totalChance = effectAggregator.GetProcChance(skill, procType);
            int multiplier = 1;
            while (totalChance >= 1f)
            {
                multiplier++;
                totalChance -= 1f;
            }

            if (UnityEngine.Random.value <= Mathf.Clamp01(totalChance))
                multiplier++;

            return multiplier;
        }

        public float GetFlatStatBonus(BaseStat stat)
        {
            return effectAggregator.GetFlatStatBonus(stat);
        }

        public float GetPercentStatBonus(BaseStat stat)
        {
            return effectAggregator.GetPercentStatBonus(stat);
        }

        public IReadOnlyList<SpawnEchoEntry> GetSpawnEntries(Skill skill)
        {
            return effectAggregator.GetSpawnEntries(skill);
        }

        public float GetTaskSpeedMultiplier(Skill skill)
        {
            if (skill == null)
                return 1f;
            if (!progress.TryGetValue(skill, out var prog))
                return 1f;

            return 1f + prog.Level * skill.taskSpeedPerLevel;
        }

        public float GetResourceGainMultiplier()
        {
            if (combatSkill == null)
                return 1f;
            if (!progress.TryGetValue(combatSkill, out var prog))
                return 1f;

            return 1f + prog.Level * combatSkill.taskSpeedPerLevel;
        }

        public float GetResourceBonusMultiplier(Skill skill)
        {
            if (skill == null)
            {
                float globalOnly = 1f + effectAggregator.GetResourceBonus(null);
                return globalOnly < 0f ? 0f : globalOnly;
            }

            float bonus = effectAggregator.GetResourceBonus(skill);
            float multiplier = 1f + bonus;
            return multiplier < 0f ? 0f : multiplier;
        }

        public float GetExperienceBonusMultiplier(Skill skill)
        {
            if (skill == null)
            {
                float globalOnly = 1f + effectAggregator.GetExperienceBonus(null);
                return globalOnly < 0f ? 0f : globalOnly;
            }

            float bonus = effectAggregator.GetExperienceBonus(skill);
            float multiplier = 1f + bonus;
            return multiplier < 0f ? 0f : multiplier;
        }

        public bool IsTaskUnlocked(TaskData task)
        {
            if (task == null)
                return false;

            return effectAggregator.IsTaskUnlocked(task);
        }

        public float GetTaskSpawnWeight(TaskData task)
        {
            if (task == null)
                return 0f;

            return effectAggregator.GetTaskWeight(task);
        }

        public MilestoneSetDefinition GetSetDefinition(MilestoneSet set)
        {
            if (set == MilestoneSet.None)
                return null;
            setLookup.TryGetValue(set, out var definition);
            return definition;
        }

        public IEnumerable<ActiveMilestoneInfo> EnumerateActiveMilestones()
        {
            foreach (var pair in milestoneStates)
            {
                var skill = pair.Key;
                foreach (var entry in pair.Value)
                {
                    var definition = entry.Key;
                    var state = entry.Value;
                    if (definition == null || state == null || !state.IsActive)
                        continue;

                    yield return new ActiveMilestoneInfo(skill, definition, state.TierIndex);
                }
            }
        }

        public IEnumerable<MilestoneSetSummary> EnumerateActiveSets()
        {
            foreach (var kvp in activeSetCounts)
            {
                if (!setLookup.TryGetValue(kvp.Key, out var def) || def == null)
                    continue;

                bool three = kvp.Value >= 3;
                bool six = kvp.Value >= 6;
                if (!three && !six)
                    continue;

                yield return new MilestoneSetSummary(def, kvp.Value, three, six);
            }
        }

        private void EvaluateMilestones(Skill skill, SkillProgress prog, bool raiseEvents)
        {
            if (skill == null)
                return;

            foreach (var definition in skill.milestones)
            {
                if (definition == null)
                    continue;

                var state = EnsureState(skill, definition);
                int previousTier = state.TierIndex;
                int newTier = definition.GetTierIndex(prog.Level);

                if (previousTier != newTier)
                {
                    state.TierIndex = newTier;

                    if (previousTier < 0 && newTier >= 0)
                    {
                        if (raiseEvents)
                            OnMilestoneUnlocked?.Invoke(skill, definition);
                    }
                    else if (newTier >= 0 && raiseEvents)
                    {
                        OnMilestoneTierChanged?.Invoke(skill, definition, newTier);
                    }
                }

                if (newTier < 0 && state.IsActive)
                {
                    state.IsActive = false;
                    if (raiseEvents)
                        OnMilestoneActivationChanged?.Invoke(skill, definition, false);
                }
            }
        }

        private void RebuildEffectCache()
        {
            effectAggregator.Reset();
            activeSetCounts.Clear();

            int previousActiveCount = activeSlotsUsed;
            activeSlotsUsed = 0;

            foreach (var pair in milestoneStates)
            {
                var skill = pair.Key;
                if (!progress.TryGetValue(skill, out var prog))
                    continue;

                foreach (var entry in pair.Value)
                {
                    var definition = entry.Key;
                    var state = entry.Value;
                    if (definition == null || state == null)
                        continue;

                    if (state.TierIndex < 0)
                    {
                        state.IsActive = false;
                        continue;
                    }

                    bool isActive = state.IsActive && definition.CanActivate;
                    if (isActive)
                    {
                        activeSlotsUsed++;
                        if (definition.Set != MilestoneSet.None)
                        {
                            activeSetCounts.TryGetValue(definition.Set, out var count);
                            activeSetCounts[definition.Set] = count + 1;
                        }
                    }

                    var effect = isActive ? definition.ActiveEffect : definition.PassiveEffect;
                    if (effect == null)
                        continue;

                    float magnitude = isActive
                        ? definition.GetActiveValue(state.TierIndex)
                        : definition.GetPassiveValue(state.TierIndex);

                    var source = isActive ? MilestoneEffectSource.Active : MilestoneEffectSource.Passive;
                    var context = new MilestoneEffectContext(this, skill, definition, source, effectAggregator);
                    effect.Apply(context, magnitude);
                }
            }

            foreach (var kvp in activeSetCounts)
            {
                if (!setLookup.TryGetValue(kvp.Key, out var setDefinition) || setDefinition == null)
                    continue;

                if (kvp.Value >= 3)
                    setDefinition.ApplyThreePiece(this, effectAggregator);
                if (kvp.Value >= 6)
                    setDefinition.ApplySixPiece(this, effectAggregator);
            }

            if (previousActiveCount != activeSlotsUsed)
                RaiseActiveSlotsChanged();

            OnMilestoneDataChanged?.Invoke();
        }

        private void EnforceActiveSlotLimits()
        {
            if (totalActiveSlots <= 0)
            {
                foreach (var skillStates in milestoneStates)
                {
                    foreach (var entry in skillStates.Value)
                    {
                        if (entry.Value.IsActive)
                        {
                            entry.Value.IsActive = false;
                            OnMilestoneActivationChanged?.Invoke(skillStates.Key, entry.Key, false);
                        }
                    }
                }
                return;
            }

            var activeList = new List<(Skill skill, MilestoneDefinition definition, MilestoneRuntimeState state)>();
            foreach (var skillStates in milestoneStates)
            {
                foreach (var entry in skillStates.Value)
                {
                    if (entry.Value.IsActive && entry.Value.TierIndex >= 0 && entry.Key != null)
                        activeList.Add((skillStates.Key, entry.Key, entry.Value));
                }
            }

            int allowed = Mathf.Min(totalActiveSlots, MaxActiveSlots);
            if (activeList.Count <= allowed)
                return;

            for (int i = allowed; i < activeList.Count; i++)
            {
                var entry = activeList[i];
                if (!entry.state.IsActive)
                    continue;

                entry.state.IsActive = false;
                OnMilestoneActivationChanged?.Invoke(entry.skill, entry.definition, false);
            }
        }

        private void RaiseActiveSlotsChanged()
        {
            OnActiveSlotsChanged?.Invoke(activeSlotsUsed, totalActiveSlots);
        }

        private void SaveState()
        {
            if (oracle == null)
                return;

            var dict = new Dictionary<string, Blindsided.SaveData.GameData.SkillProgress>();
            foreach (var skill in skills)
            {
                if (skill == null)
                    continue;
                if (!progress.TryGetValue(skill, out var prog))
                    continue;

                var record = new Blindsided.SaveData.GameData.SkillProgress
                {
                    Level = prog.Level,
                    CurrentXP = prog.CurrentXP,
                    Milestones = new List<Blindsided.SaveData.GameData.MilestoneProgressRecord>()
                };

                if (milestoneStates.TryGetValue(skill, out var stateDict))
                {
                    foreach (var entry in stateDict)
                    {
                        var definition = entry.Key;
                        var state = entry.Value;
                        if (definition == null || state == null)
                            continue;

                        record.Milestones.Add(new Blindsided.SaveData.GameData.MilestoneProgressRecord
                        {
                            Id = definition.Id,
                            TierIndex = state.TierIndex,
                            IsActive = state.IsActive
                        });
                    }
                }

                dict[skill.name] = record;
            }

            oracle.saveData.SkillData = dict;
        }

        private void LoadState()
        {
            if (oracle == null)
                return;

            oracle.saveData.SkillData ??= new Dictionary<string, Blindsided.SaveData.GameData.SkillProgress>();
            progress.Clear();
            milestoneStates.Clear();
            skillsAtActiveThreshold.Clear();

            foreach (var skill in skills)
            {
                if (skill == null)
                    continue;

                var prog = CreateDefaultProgress();
                var stateDict = new Dictionary<MilestoneDefinition, MilestoneRuntimeState>();
                milestoneStates[skill] = stateDict;

                if (oracle.saveData.SkillData.TryGetValue(skill.name, out var data))
                {
                    prog.Level = data.Level;
                    prog.CurrentXP = data.CurrentXP;

                    if (data.Milestones != null)
                    {
                        foreach (var saved in data.Milestones)
                        {
                            var definition = skill.milestones.FirstOrDefault(m => m != null && m.Id == saved.Id);
                            if (definition == null)
                                continue;

                            var state = EnsureState(skill, definition);
                            state.TierIndex = definition.TierMode == MilestoneTierMode.Manual
                                ? Mathf.Clamp(saved.TierIndex, -1, definition.ManualTiers.Count - 1)
                                : Mathf.Max(-1, saved.TierIndex);
                            state.IsActive = saved.IsActive && definition.CanActivate;
                        }
                    }
                }

                progress[skill] = prog;

                if (prog.Level >= ActiveSlotUnlockLevel)
                    skillsAtActiveThreshold.Add(skill);

                EvaluateMilestones(skill, prog, false);
            }

            totalActiveSlots = Mathf.Min(skillsAtActiveThreshold.Count, MaxActiveSlots);
            EnforceActiveSlotLimits();
            RebuildEffectCache();
            RaiseActiveSlotsChanged();
        }

        private SkillProgress CreateDefaultProgress()
        {
            return new SkillProgress
            {
                Level = 1,
                CurrentXP = 0f
            };
        }

        private MilestoneRuntimeState EnsureState(Skill skill, MilestoneDefinition definition)
        {
            if (!milestoneStates.TryGetValue(skill, out var dict))
            {
                dict = new Dictionary<MilestoneDefinition, MilestoneRuntimeState>();
                milestoneStates.Add(skill, dict);
            }

            if (!dict.TryGetValue(definition, out var state))
            {
                state = new MilestoneRuntimeState();
                dict.Add(definition, state);
            }

            return state;
        }

        private static float GetXpForLevel(Skill skill, int level)
        {
            if (skill == null)
                return 0f;
            return skill.xpForFirstLevel * Mathf.Pow(level, skill.xpLevelMultiplier);
        }

        [Serializable]
        public class SkillProgress
        {
            public int Level = 1;
            public float CurrentXP;
        }

        public class MilestoneRuntimeState
        {
            public int TierIndex = -1;
            public bool IsActive;
            public bool IsUnlocked => TierIndex >= 0;
        }

        public readonly struct ActiveMilestoneInfo
        {
            public readonly Skill Skill;
            public readonly MilestoneDefinition Definition;
            public readonly int TierIndex;

            public ActiveMilestoneInfo(Skill skill, MilestoneDefinition definition, int tierIndex)
            {
                Skill = skill;
                Definition = definition;
                TierIndex = tierIndex;
            }
        }

        public readonly struct MilestoneSetSummary
        {
            public readonly MilestoneSetDefinition Definition;
            public readonly int ActiveCount;
            public readonly bool ThreePieceActive;
            public readonly bool SixPieceActive;

            public MilestoneSetSummary(MilestoneSetDefinition definition, int activeCount, bool threePiece, bool sixPiece)
            {
                Definition = definition;
                ActiveCount = activeCount;
                ThreePieceActive = threePiece;
                SixPieceActive = sixPiece;
            }
        }
    }
}

