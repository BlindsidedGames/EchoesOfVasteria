using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Handles skill experience and levels.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class SkillController : MonoBehaviour
    {
        public static SkillController Instance { get; private set; }
        [SerializeField]
        private Skill combatSkill;

        public Skill CombatSkill => combatSkill;

        [Serializable]
        public class SkillProgress
        {
            public int Level;
            public float CurrentXP;
            public HashSet<string> Milestones = new();
        }

        [SerializeField] private List<Skill> skills = new();

        private readonly Dictionary<Skill, SkillProgress> progress = new();

        public event Action<Skill, float, float> OnExperienceGained;
        public event Action<Skill, int> OnLevelUp;
        public event Action<Skill, MilestoneBonus> OnMilestoneUnlocked;

        private void Awake()
        {
            Instance = this;
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

        public SkillProgress GetProgress(Skill skill)
        {
            if (skill == null) return null;
            progress.TryGetValue(skill, out var prog);
            return prog;
        }

        public void AddExperience(Skill skill, float xpAmount)
        {
            if (skill == null || xpAmount <= 0f) return;
            if (!progress.TryGetValue(skill, out var prog))
            {
                prog = new SkillProgress { Level = 1, CurrentXP = 0f };
                progress[skill] = prog;
            }

            prog.CurrentXP += xpAmount;

            var currentLevel = prog.Level;
            float xpNeeded = skill.xpForFirstLevel * Mathf.Pow(currentLevel, skill.xpLevelMultiplier);
            var leveled = false;
            while (prog.CurrentXP >= xpNeeded)
            {
                prog.CurrentXP -= xpNeeded;
                prog.Level++;
                OnLevelUp?.Invoke(skill, prog.Level);
                leveled = true;
                currentLevel = prog.Level;
                xpNeeded = skill.xpForFirstLevel * Mathf.Pow(currentLevel, skill.xpLevelMultiplier);
            }

            if (leveled)
                CheckMilestones(skill, prog);

            OnExperienceGained?.Invoke(skill, prog.CurrentXP, xpNeeded);
        }

        private void CheckMilestones(Skill skill, SkillProgress prog)
        {
            foreach (var m in skill.milestones)
            {
                if (prog.Level >= m.levelRequirement && !prog.Milestones.Contains(m.bonusID))
                {
                    prog.Milestones.Add(m.bonusID);
                    OnMilestoneUnlocked?.Invoke(skill, m);
                }
            }
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, Blindsided.SaveData.GameData.SkillProgress>();
            foreach (var pair in progress)
            {
                if (pair.Key != null)
                    dict[pair.Key.name] = new Blindsided.SaveData.GameData.SkillProgress
                    {
                        Level = pair.Value.Level,
                        CurrentXP = pair.Value.CurrentXP,
                        Milestones = pair.Value.Milestones.ToList()
                    };
            }
            oracle.saveData.SkillData = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.SkillData ??= new Dictionary<string, Blindsided.SaveData.GameData.SkillProgress>();
            progress.Clear();
            foreach (var skill in skills)
            {
                if (skill == null) continue;
                if (oracle.saveData.SkillData.TryGetValue(skill.name, out var data))
                {
                    progress[skill] = new SkillProgress
                    {
                        Level = data.Level,
                        CurrentXP = data.CurrentXP,
                        Milestones = new HashSet<string>(data.Milestones ?? new List<string>())
                    };
                }
                else
                {
                    progress[skill] = new SkillProgress { Level = 1, CurrentXP = 0f };
                }

                // Ensure milestones are reevaluated on load so previously earned
                // bonuses are applied even if they weren't saved.
                CheckMilestones(skill, progress[skill]);
            }
        }

        public bool IsMilestoneUnlocked(Skill skill, MilestoneBonus milestone)
        {
            if (skill == null || milestone == null) return false;
            if (!progress.TryGetValue(skill, out var prog)) return false;
            return prog.Milestones.Contains(milestone.bonusID);
        }

        public bool RollForEffect(Skill skill, MilestoneType type)
        {
            if (type == MilestoneType.DoubleResources || type == MilestoneType.DoubleXP)
                return GetEffectMultiplier(skill, type) > 1;

            if (skill == null) return false;
            if (!progress.TryGetValue(skill, out var prog)) return false;
            foreach (var id in prog.Milestones)
            {
                var m = skill.milestones.Find(ms => ms.bonusID == id);
                if (m != null && m.type == type && UnityEngine.Random.value <= m.chance)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the multiplier for effects that stack beyond a single double.
        /// </summary>
        /// <remarks>
        /// The total chance from unlocked milestones may exceed 100%. Each full
        /// 100% guarantees an additional doubling, and any remaining fraction
        /// is rolled for one more doubling.
        /// </remarks>
        /// <param name="skill">Skill associated with the effect.</param>
        /// <param name="type">Milestone type to evaluate.</param>
        /// <returns>The integer multiplier to apply to the reward.</returns>
        public int GetEffectMultiplier(Skill skill, MilestoneType type)
        {
            if (skill == null) return 1;
            if (!progress.TryGetValue(skill, out var prog)) return 1;

            float totalChance = 0f;
            foreach (var id in prog.Milestones)
            {
                var m = skill.milestones.Find(ms => ms.bonusID == id);
                if (m != null && m.type == type)
                    totalChance += m.chance;
            }

            int multiplier = 1;
            while (totalChance >= 1f)
            {
                multiplier++;
                totalChance -= 1f;
            }

            if (UnityEngine.Random.value <= totalChance)
                multiplier++;

            return multiplier;
        }

        public float GetFlatStatBonus(TimelessEchoes.Upgrades.StatUpgrade upgrade)
        {
            float total = 0f;
            if (upgrade == null) return total;
            foreach (var pair in progress)
            {
                foreach (var id in pair.Value.Milestones)
                {
                    var m = pair.Key.milestones.Find(ms => ms.bonusID == id);
                    if (m != null && m.type == MilestoneType.StatIncrease && !m.percentBonus && m.statUpgrade == upgrade)
                        total += m.statAmount;
                }
            }
            return total;
        }

        public float GetPercentStatBonus(TimelessEchoes.Upgrades.StatUpgrade upgrade)
        {
            float total = 0f;
            if (upgrade == null) return total;
            foreach (var pair in progress)
            {
                foreach (var id in pair.Value.Milestones)
                {
                    var m = pair.Key.milestones.Find(ms => ms.bonusID == id);
                    if (m != null && m.type == MilestoneType.StatIncrease && m.percentBonus && m.statUpgrade == upgrade)
                        total += m.statAmount;
                }
            }
            return total;
        }

        public float GetTaskSpeedMultiplier(Skill skill)
        {
            if (skill == null) return 1f;
            if (!progress.TryGetValue(skill, out var prog))
                return 1f;

            return 1f + prog.Level * skill.taskSpeedPerLevel;
        }
    }
}
