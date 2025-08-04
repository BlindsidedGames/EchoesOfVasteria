using System.Collections.Generic;
using System.Linq;
using QFSW.QC;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Skills;
using TimelessEchoes.NPC;
using TimelessEchoes.Quests;
using Blindsided.SaveData;
using Blindsided;
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

        [Command("set-all-resources", "Set all resources to the specified amount")]
        public static void SetAllResources(double amount)
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;

            var existing = oracle.saveData.Resources;
            var dict = new Dictionary<string, GameData.ResourceEntry>();
            foreach (var res in Resources.LoadAll<Resource>(string.Empty))
            {
                if (res == null) continue;
                GameData.ResourceEntry oldEntry = null;
                existing?.TryGetValue(res.name, out oldEntry);
                dict[res.name] = new GameData.ResourceEntry
                {
                    Earned = true,
                    Amount = amount,
                    BestPerMinute = oldEntry?.BestPerMinute ?? 0
                };
            }

            oracle.saveData.Resources = dict;
            Blindsided.EventHandler.LoadData();
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

        [Command("unlock-witch", "Unlock the witch NPC")]
        public static void UnlockWitch()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.CompletedNpcTasks ??= new HashSet<string>();
            if (!oracle.saveData.CompletedNpcTasks.Contains("Witch1"))
                oracle.saveData.CompletedNpcTasks.Add("Witch1");

            var qm = Object.FindFirstObjectByType<QuestManager>();
            qm?.OnNpcMet("Witch1");
            NpcObjectStateController.Instance?.UpdateObjectStates();
        }

        [Command("complete-mildred", "Complete the Mildred quest")]
        public static void CompleteMildredQuest()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            if (!oracle.saveData.Quests.TryGetValue("Mildred", out var rec))
                rec = new GameData.QuestRecord();
            rec.Completed = true;
            oracle.saveData.Quests["Mildred"] = rec;

            EventHandler.QuestHandin("Mildred");
            NpcObjectStateController.Instance?.UpdateObjectStates();
        }

        [Command("wipe-quests", "Clear all quest progress")]
        public static void WipeQuests()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.Quests = new Dictionary<string, GameData.QuestRecord>();
            Blindsided.EventHandler.LoadData();
        }

        [Command("wipe-resources", "Remove all stored resources")]
        public static void WipeResources()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.Resources = new Dictionary<string, GameData.ResourceEntry>();
            oracle.saveData.ResourceStats = new Dictionary<string, GameData.ResourceRecord>();
            Blindsided.EventHandler.LoadData();
        }

        [Command("wipe-upgrades", "Reset all upgrade levels")]
        public static void WipeUpgrades()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.UpgradeLevels = new Dictionary<string, int>();
            Blindsided.EventHandler.LoadData();
        }

        [Command("wipe-skills", "Reset all skill progress")]
        public static void WipeSkills()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.SkillData = new Dictionary<string, GameData.SkillProgress>();
            Blindsided.EventHandler.LoadData();
        }

        [Command("wipe-npc", "Reset NPC tasks and generation data")]
        public static void WipeNpc()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.CompletedNpcTasks = new HashSet<string>();
            oracle.saveData.Disciples = new Dictionary<string, GameData.DiscipleGenerationRecord>();
            Blindsided.EventHandler.LoadData();
            NpcObjectStateController.Instance?.UpdateObjectStates();
        }

        [Command("wipe-disciples", "Clear all disciple generation data")]
        public static void WipeDisciples()
        {
            var oracle = Blindsided.Oracle.oracle;
            if (oracle == null) return;
            oracle.saveData.Disciples = new Dictionary<string, GameData.DiscipleGenerationRecord>();
            oracle.saveData.DisciplePercent = 0.1f;
            Blindsided.EventHandler.LoadData();
        }

    }
}
