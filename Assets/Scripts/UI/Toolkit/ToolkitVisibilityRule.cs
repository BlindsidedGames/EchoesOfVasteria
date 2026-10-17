using System;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Presentation gates use progression data, never hidden legacy UI objects.</summary>
    [Serializable]
    public sealed class ToolkitVisibilityRule
    {
        public enum Condition { QuestComplete, QuestIncomplete, QuestInProgress, NpcMet, NpcNotMet, InTown, InRun, Never, ReturnedFromRiver, SkillLevelled, ReturnedFromFarmlands, HubAvailable }
        public Condition condition;
        public QuestData quest;
        public string npcId;

        public static bool HasLevelledSkill()
        {
            var data = Blindsided.Oracle.oracle?.saveData;
            if (data == null) return false;
            var skills = Skills.SkillController.Instance;
            if (skills != null)
                foreach (var skill in skills.EnumerateSkills())
                    if (skills.GetLevel(skill, data) > 1) return true;
            if (data.SkillData != null)
                foreach (var progress in data.SkillData.Values)
                    if (progress != null && progress.Level > 1) return true;
            return false;
        }

        public bool IsSatisfied()
        {
            switch (condition)
            {
                case Condition.HubAvailable: return HasLevelledSkill() || QuestUtils.QuestCompleted("The names Gill") || QuestUtils.QuestCompleted("Mildred");
                case Condition.ReturnedFromFarmlands: return Blindsided.Oracle.oracle?.saveData?.General.ReturnedFromFarmlands == true;
                case Condition.SkillLevelled: return HasLevelledSkill();
                case Condition.ReturnedFromRiver: return Blindsided.Oracle.oracle?.saveData?.General.ReturnedFromRiver == true;
                case Condition.QuestComplete: return quest && QuestUtils.QuestCompleted(quest);
                case Condition.QuestIncomplete: return !quest || !QuestUtils.QuestCompleted(quest);
                case Condition.QuestInProgress:
                    return quest && QuestManager.Instance != null && QuestManager.Instance.IsQuestInProgress(quest.questId);
                case Condition.NpcMet:
                    // A retired introduction can preserve access for older saves.
                    return (!string.IsNullOrEmpty(npcId) && StaticReferences.CompletedNpcTasks.Contains(npcId)) ||
                           (quest && QuestUtils.QuestCompleted(quest));
                case Condition.NpcNotMet: return string.IsNullOrEmpty(npcId) || !StaticReferences.CompletedNpcTasks.Contains(npcId);
                case Condition.InTown: return GameManager.Instance == null || GameManager.Instance.CurrentMap == null;
                case Condition.InRun: return GameManager.Instance != null && GameManager.Instance.CurrentMap != null;
                default: return false;
            }
        }

        public static bool All(ToolkitVisibilityRule[] rules)
        {
            if (rules == null) return true;
            foreach (var rule in rules) if (rule != null && !rule.IsSatisfied()) return false;
            return true;
        }
    }
}
