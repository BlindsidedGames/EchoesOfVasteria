using System.Collections.Generic;
using System.Linq;
using QFSW.QC;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Skills;
using Blindsided.SaveData;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes
{
    /// <summary>
    /// Console commands for debugging resource, stat and skill progression.
    /// </summary>
    public static class ConsoleCommands
    {
        [Command("add-resource", "Add an amount of a resource by name")]
        public static void AddResource(string resourceName, double amount)
        {
            var manager = ResourceManager.Instance;
            if (manager == null)
            {
                TELogger.Log("ResourceManager missing", TELogCategory.Resource);
                return;
            }

            var res = Resources.LoadAll<Resource>(string.Empty).FirstOrDefault(r => r.name == resourceName);
            if (res != null)
            {
                manager.Add(res, amount);
            }
        }

        [Command("set-stat-level", "Set the level of a stat upgrade")]
        public static void SetStatLevel(string upgradeName, int level)
        {
            var upgrade = Resources.FindObjectsOfTypeAll<StatUpgrade>().FirstOrDefault(u => u.name == upgradeName);
            if (upgrade == null) return;
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.UpgradeLevels ??= new Dictionary<string, int>();
            oracle.saveData.UpgradeLevels[upgrade.name] = level;
            Blindsided.EventHandler.LoadData();
        }

        [Command("set-skill-level", "Set the level of a skill")]
        public static void SetSkillLevel(string skillName, int level)
        {
            var skill = Resources.FindObjectsOfTypeAll<Skill>().FirstOrDefault(s => s.name == skillName);
            if (skill == null) return;
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            if (!oracle.saveData.SkillData.TryGetValue(skill.name, out var prog))
                prog = new GameData.SkillProgress();
            prog.Level = level;
            prog.CurrentXP = 0f;
            oracle.saveData.SkillData[skill.name] = prog;
            Blindsided.EventHandler.LoadData();
        }
    }
}
