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
        public enum Condition { QuestComplete, QuestIncomplete, QuestInProgress, NpcMet, NpcNotMet, InTown, InRun }
        public Condition condition;
        public QuestData quest;
        public string npcId;

        public bool IsSatisfied()
        {
            switch (condition)
            {
                case Condition.QuestComplete: return quest && QuestUtils.QuestCompleted(quest);
                case Condition.QuestIncomplete: return !quest || !QuestUtils.QuestCompleted(quest);
                case Condition.QuestInProgress:
                    return quest && QuestManager.Instance != null && QuestManager.Instance.IsQuestInProgress(quest.questId);
                case Condition.NpcMet: return !string.IsNullOrEmpty(npcId) && StaticReferences.CompletedNpcTasks.Contains(npcId);
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
