using System.Collections.Generic;
using Blindsided.SaveData;
using static Blindsided.Oracle;

namespace TimelessEchoes.Quests
{
    /// <summary>
    /// Utility helpers for working with quest completion state.
    /// </summary>
    public static class QuestUtils
    {
        /// <summary>
        /// Returns true if the quest with the given id has been completed.
        /// </summary>
        public static bool QuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(questId, out var rec) && rec.Completed;
        }

        /// <summary>
        /// Returns true if the given quest has been completed.
        /// </summary>
        public static bool QuestCompleted(QuestData quest)
        {
            return QuestCompleted(quest != null ? quest.questId : null);
        }
    }
}
