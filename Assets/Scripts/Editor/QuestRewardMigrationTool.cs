// ============================================================
// DISPOSABLE MIGRATION TOOL - DELETE AFTER VALIDATION
// Adds SkillExperience rewards to unlock quests based on
// the requiredSkillLevel from the corresponding TaskData.
// XP = requiredSkillLevel * 200
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;

public class QuestRewardMigrationTool : EditorWindow
{
    private const float XP_MULTIPLIER = 200f;

    // Manual mappings for quest names that don't match task names exactly
    private static readonly Dictionary<string, string> QuestToTaskMapping = new()
    {
        // Spelling differences
        { "chilli", "chillie" },
        { "pumpking", "pumking" },
        { "watermelone", "wartermelone" },
        // Ores have " Ore" suffix
        { "copium", "copium ore" },
        { "erif", "erif ore" },
        { "idle", "idle ore" },
        { "lirium", "lirium ore" },
        { "vastium", "vastium ore" },
        { "dlog", "dlog ore" },
        { "nori", "nori ore" },
        { "eznorb", "eznorb ore" },
    };

    [MenuItem("Tools/Migration/3. Update Unlock Quests to XP (DISPOSABLE)")]
    public static void UpdateUnlockQuests()
    {
        // Find all QuestData assets
        var questGuids = AssetDatabase.FindAssets("t:QuestData");
        var allQuests = questGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(q => q != null)
            .ToList();

        // Find all TaskData assets for lookup
        var taskGuids = AssetDatabase.FindAssets("t:TaskData");
        var allTasks = taskGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(t => t != null)
            .ToDictionary(t => t.taskName.ToLower(), t => t);

        int updatedCount = 0;
        int skippedCount = 0;
        var log = new List<string>();

        foreach (var quest in allQuests)
        {
            // Only process "Unlock" quests
            if (!quest.name.StartsWith("Unlock ", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Extract the task name from quest name (e.g., "Unlock Carrot" -> "Carrot")
            string questKey = quest.name.Substring("Unlock ".Length).Trim().ToLower();

            // Apply manual mapping if exists
            string taskName = QuestToTaskMapping.TryGetValue(questKey, out var mapped) ? mapped : questKey;

            // Try to find matching TaskData
            if (!allTasks.TryGetValue(taskName, out var taskData))
            {
                log.Add($"SKIP: {quest.name} - No matching TaskData found for '{taskName}'");
                skippedCount++;
                continue;
            }

            // Check if task has a skill level requirement
            if (taskData.requiredSkillLevel <= 0)
            {
                log.Add($"SKIP: {quest.name} - TaskData has no requiredSkillLevel");
                skippedCount++;
                continue;
            }

            // Check if task has an associated skill
            if (taskData.associatedSkill == null)
            {
                log.Add($"SKIP: {quest.name} - TaskData has no associatedSkill");
                skippedCount++;
                continue;
            }

            // Check if quest already has a SkillExperience reward
            bool hasSkillXpReward = quest.rewards.Any(r => r.type == QuestData.RewardType.SkillExperience);
            if (hasSkillXpReward)
            {
                log.Add($"SKIP: {quest.name} - Already has SkillExperience reward");
                skippedCount++;
                continue;
            }

            // Calculate XP
            float xpAmount = taskData.requiredSkillLevel * XP_MULTIPLIER;

            // Add the reward
            Undo.RecordObject(quest, "Add SkillExperience Reward");

            var newReward = new QuestData.Reward
            {
                type = QuestData.RewardType.SkillExperience,
                skill = taskData.associatedSkill,
                experience = xpAmount
            };

            var rewardsList = new List<QuestData.Reward>(quest.rewards) { newReward };
            quest.rewards = rewardsList;

            EditorUtility.SetDirty(quest);
            log.Add($"UPDATED: {quest.name} - Added {xpAmount} XP to {taskData.associatedSkill.skillName}");
            updatedCount++;
        }

        AssetDatabase.SaveAssets();

        // Log results
        Debug.Log($"=== Quest Reward Migration Complete ===");
        Debug.Log($"Updated: {updatedCount} quests");
        Debug.Log($"Skipped: {skippedCount} quests");
        Debug.Log("--- Details ---");
        foreach (var line in log)
            Debug.Log(line);
    }
}
#endif
