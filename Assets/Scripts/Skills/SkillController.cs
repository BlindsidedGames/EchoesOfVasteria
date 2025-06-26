using System;
using System.Collections.Generic;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Handles skill experience and levels.
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        [Serializable]
        public class SkillProgress
        {
            public int Level;
            public float CurrentXP;
        }

        [SerializeField] private List<Skill> skills = new();

        private readonly Dictionary<Skill, SkillProgress> progress = new();

        public event Action<Skill, float, float> OnExperienceGained;
        public event Action<Skill, int> OnLevelUp;

        private void Awake()
        {
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
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
            while (prog.CurrentXP >= xpNeeded)
            {
                prog.CurrentXP -= xpNeeded;
                prog.Level++;
                OnLevelUp?.Invoke(skill, prog.Level);
                currentLevel = prog.Level;
                xpNeeded = skill.xpForFirstLevel * Mathf.Pow(currentLevel, skill.xpLevelMultiplier);
            }

            OnExperienceGained?.Invoke(skill, prog.CurrentXP, xpNeeded);
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
                        CurrentXP = pair.Value.CurrentXP
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
                    progress[skill] = new SkillProgress { Level = data.Level, CurrentXP = data.CurrentXP };
                }
                else
                {
                    progress[skill] = new SkillProgress { Level = 1, CurrentXP = 0f };
                }
            }
        }
    }
}
