// ============================================================
// DISPOSABLE MIGRATION TOOL - DELETE AFTER VALIDATION
// Step 1: Run "Generate Config from Quest Chains" to auto-populate config
// Step 2: Review/adjust levels in ResourceUnlockConfig.asset
// Step 3: Run "Apply Config to Assets" to write data to Skill/TaskData
// Step 4: Delete this script after validation
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Tasks;
using TimelessEchoes.Skills;

public class ResourceUnlockMigrationTool : EditorWindow
{
    private const string CONFIG_PATH = "Assets/Resources/ResourceUnlockConfig.asset";

    // Level scaling parameters - adjust as needed
    private const int BASE_LEVEL = 1;
    private const int LEVEL_INCREMENT = 5;
    private const float DISTANCE_TO_LEVEL_FACTOR = 0.02f; // minX * factor = level contribution

    [MenuItem("Tools/Migration/1. Generate Config from Quest Chains (DISPOSABLE)")]
    public static void GenerateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ResourceUnlockConfig>(CONFIG_PATH);
        if (config == null)
        {
            // Ensure the Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            config = ScriptableObject.CreateInstance<ResourceUnlockConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
        }

        Undo.RecordObject(config, "Generate Unlock Config");

        // Find all TaskData assets
        var taskGuids = AssetDatabase.FindAssets("t:TaskData");
        var allTasks = taskGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(t => t != null)
            .ToList();

        // Group by skill
        var tasksBySkill = allTasks
            .Where(t => t.associatedSkill != null)
            .GroupBy(t => t.associatedSkill.skillName?.ToLower() ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process each skill
        config.farmingUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("farming"));
        config.fishingUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("fishing"));
        config.miningUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("mining"));
        // Woodcutting skill is named "Logging" in the asset
        config.woodcuttingUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("logging") ?? tasksBySkill.GetValueOrDefault("woodcutting"));
        config.lootingUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("looting"));

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"Config generated at {CONFIG_PATH}. Review and adjust levels, then run Apply.");
    }

    private static List<ResourceUnlockConfig.UnlockMapping> BuildUnlockList(List<TaskData> tasks)
    {
        if (tasks == null || tasks.Count == 0)
            return new();

        // Sort by taskID to ensure proper ordering (e.g., medium trees before big trees)
        var sorted = tasks
            .OrderBy(t => t.taskID)
            .ToList();

        var result = new List<ResourceUnlockConfig.UnlockMapping>();
        int position = 0;

        foreach (var task in sorted)
        {
            // Calculate level: base + position increment + distance contribution
            int levelFromPosition = BASE_LEVEL + (position * LEVEL_INCREMENT);
            int levelFromDistance = Mathf.RoundToInt(task.minX * DISTANCE_TO_LEVEL_FACTOR);
            int finalLevel = Mathf.Max(1, levelFromPosition + levelFromDistance);

            result.Add(new ResourceUnlockConfig.UnlockMapping
            {
                task = task,
                requiredLevel = finalLevel,
                description = $"Unlocks {task.taskName}"
            });

            position++;
        }

        return result;
    }

    [MenuItem("Tools/Migration/2. Apply Config to Assets (DISPOSABLE)")]
    public static void ApplyConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ResourceUnlockConfig>(CONFIG_PATH);
        if (config == null)
        {
            Debug.LogError($"Config not found at {CONFIG_PATH}. Run Generate first.");
            return;
        }

        // Find all Skill assets
        var skillGuids = AssetDatabase.FindAssets("t:Skill");
        var skills = skillGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<Skill>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .ToList();

        int taskCount = 0;

        foreach (var skill in skills)
        {
            var unlocks = config.GetUnlocksForSkill(skill);
            if (unlocks == null || unlocks.Count == 0)
                continue;

            Undo.RecordObject(skill, "Apply Resource Unlocks");

            // Populate skill.resourceUnlocks
            skill.resourceUnlocks.Clear();
            foreach (var mapping in unlocks.OrderBy(m => m.requiredLevel))
            {
                if (mapping.task == null) continue;

                skill.resourceUnlocks.Add(new ResourceUnlockEntry
                {
                    task = mapping.task,
                    requiredLevel = mapping.requiredLevel,
                    description = mapping.description
                });

                // Also set requiredSkillLevel on the TaskData
                Undo.RecordObject(mapping.task, "Set Required Skill Level");
                mapping.task.requiredSkillLevel = mapping.requiredLevel;
                EditorUtility.SetDirty(mapping.task);
                taskCount++;
            }

            EditorUtility.SetDirty(skill);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Applied config to {skills.Count} skills, {taskCount} tasks.");
    }
}
#endif
