# Plan: Migrate Resource Unlocks from Quests to Skills

## Git Workflow

```
1. Create feature branch
   git checkout -b feature/skill-based-resource-unlocks

2. After Step 1-2 (Data Models):
   git add Assets/Scripts/Upgrades/ResourceDrop.cs Assets/Scripts/Tasks/TaskData.cs Assets/Scripts/Skills/Skill.cs
   git commit -m "Add requiredSkillLevel field and ResourceUnlockEntry to data models"

3. After Step 3-5 (Runtime Logic):
   git add Assets/Scripts/Upgrades/DropResolver.cs Assets/Scripts/Tasks/ProceduralTaskGenerator.cs Assets/Scripts/Tasks/ResourceGeneratingTask.cs
   git commit -m "Replace quest-based unlock checks with skill level checks"

4. After Step 6 (UI):
   git add Assets/Scripts/Skills/MilestoneBonusUI.cs
   git commit -m "Display resource unlock entries in skill panel UI"

5. After Step 8 (Migration Tooling):
   git add Assets/Scripts/Skills/ResourceUnlockConfig.cs Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs
   git commit -m "Add disposable migration tooling for resource unlock data"

6. After running migration and reviewing config:
   git add Assets/Resources/ResourceUnlockConfig.asset Assets/Scriptables/Skills/*.asset Assets/Resources/Tasks/**/*.asset
   git commit -m "Populate resource unlock levels from quest chain analysis"

7. After validation - cleanup commit:
   git rm Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs Assets/Scripts/Skills/ResourceUnlockConfig.cs Assets/Resources/ResourceUnlockConfig.asset
   git commit -m "Remove disposable migration tooling after validation"

8. Final cleanup (remove requiredQuest fields):
   git add Assets/Scripts/Upgrades/ResourceDrop.cs Assets/Scripts/Tasks/TaskData.cs
   git commit -m "Remove legacy requiredQuest fields"

9. Merge to main:
   git checkout main
   git merge feature/skill-based-resource-unlocks
```

---

## Overview
Migrate resource unlocks from quest-based gating to skill-level-based gating. Resource unlock entries appear in the skill panel as informational items showing what unlocks at each level.

**Note:** This is a clean break - no backward compatibility. Old saves will use a legacy Steam branch.

---

## Design Decisions

### Resource Unlocks: Level Field on TaskData/ResourceDrop
- Add `requiredSkillLevel` integer field directly to `TaskData` and `ResourceDrop`
- Remove `requiredQuest` field entirely (clean break)
- Resources unlock when skill reaches the specified level

### UI Display: Informational Entries (NOT milestone assets)
- Add `ResourceUnlockEntry` list to `Skill` ScriptableObject for UI display metadata
- **Auto-populated via editor script** - derives levels from quest chain + minX distance
- Entries show in skill panel mixed with milestones, sorted by level
- **Uses same prefab** (`MilestoneEntryUIReferences`) - activates existing `taskImage` field, hides toggle
- These are purely informational - the actual unlock logic reads `requiredSkillLevel` from TaskData/ResourceDrop

### No Double Resource Milestone
- Existing boosted resource chance system is sufficient
- No new milestone assets needed

---

## Files to Modify

### Core Data Models
| File | Change |
|------|--------|
| [ResourceDrop.cs](Assets/Scripts/Upgrades/ResourceDrop.cs) | Add `requiredSkillLevel`, remove `requiredQuest` |
| [TaskData.cs](Assets/Scripts/Tasks/TaskData.cs) | Add `requiredSkillLevel`, remove `requiredQuest` |
| [Skill.cs](Assets/Scripts/Skills/Skill.cs) | Add `ResourceUnlockEntry` list for UI display |

### Runtime Logic
| File | Change |
|------|--------|
| [DropResolver.cs](Assets/Scripts/Upgrades/DropResolver.cs) | Add `associatedSkill` parameter, skill level check |
| [ProceduralTaskGenerator.cs](Assets/Scripts/Tasks/ProceduralTaskGenerator.cs) | Replace quest check with skill level check |
| [ResourceGeneratingTask.cs](Assets/Scripts/Tasks/ResourceGeneratingTask.cs) | Pass `associatedSkill` to `RollDrops()` |

### UI
| File | Change |
|------|--------|
| [MilestoneBonusUI.cs](Assets/Scripts/Skills/MilestoneBonusUI.cs) | Display resource unlock entries using `taskImage` |

### Quest Assets
| Location | Change |
|----------|--------|
| Assets/Resources/Quests/ | Update unlock quests to grant skill XP instead |

### Migration Tooling (DISPOSABLE)
| File | Purpose |
|------|---------|
| [ResourceUnlockConfig.cs](Assets/Scripts/Skills/ResourceUnlockConfig.cs) | ScriptableObject for editable level mapping |
| [ResourceUnlockConfig.asset](Assets/Resources/ResourceUnlockConfig.asset) | Created by migration tool |
| [ResourceUnlockMigrationTool.cs](Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs) | Editor script to generate/apply config |

---

## Implementation Steps

### Step 1: Update Data Models

**ResourceDrop.cs** - Replace quest field with level field:
```csharp
// REMOVE: public QuestData requiredQuest;

// ADD:
[Tooltip("Skill level required for this drop (0 = no requirement)")]
public int requiredSkillLevel;
```

**TaskData.cs** - Replace quest field with level field:
```csharp
// REMOVE: public QuestData requiredQuest;

// ADD:
[TitleGroup("Spawn Range")]
[Tooltip("Skill level required for this task (0 = no requirement)")]
public int requiredSkillLevel;
```

### Step 2: Add Resource Unlock Metadata to Skill

**Skill.cs** - Add for UI display:
```csharp
[System.Serializable]
public class ResourceUnlockEntry
{
    public TaskData task;
    public int requiredLevel;
    public Sprite overrideIcon;  // Optional, falls back to task.taskIcon
    public string description;   // Brief description shown in UI
}

[TitleGroup("Resource Unlocks")]
public List<ResourceUnlockEntry> resourceUnlocks = new();
```

**Auto-populated via editor script** (see Step 8 for migration tooling)

### Step 3: Update DropResolver

**DropResolver.cs** - Modify `RollDrops` signature and logic:
```csharp
public static List<DropResult> RollDrops(
    IEnumerable<ResourceDrop> drops,
    IList<float> additionalLootChances,
    float worldX,
    Skill associatedSkill = null,  // NEW PARAMETER
    bool ignoreSkillLevel = false,  // RENAMED from ignoreQuest
    Func<float> rand = null)
{
    // In the filtering loop, replace quest check with:
    if (!ignoreSkillLevel && !IsDropUnlocked(drop, associatedSkill)) continue;
}

private static bool IsDropUnlocked(ResourceDrop drop, Skill skill)
{
    if (drop.requiredSkillLevel <= 0)
        return true;  // No requirement

    if (skill == null)
        return false;

    var controller = SkillController.Instance;
    if (controller == null)
        return false;

    int level = controller.GetProgress(skill)?.Level ?? 1;
    return level >= drop.requiredSkillLevel;
}
```

Also remove the `using TimelessEchoes.Quests;` import and `QuestUtils` reference.

### Step 4: Update ProceduralTaskGenerator

Add helper method:
```csharp
private bool IsTaskUnlocked(TaskData task)
{
    if (task == null) return false;
    if (task.requiredSkillLevel <= 0) return true;  // No requirement
    if (task.associatedSkill == null) return false;

    var controller = SkillController.Instance;
    if (controller == null) return false;

    int level = controller.GetProgress(task.associatedSkill)?.Level ?? 1;
    return level >= task.requiredSkillLevel;
}
```

Replace quest check in `PickTaskFromCategory()`:
```csharp
// REMOVE: if (t.requiredQuest != null && !QuestCompleted(t.requiredQuest.questId)) return false;
// ADD:
if (!IsTaskUnlocked(t)) return false;
```

Remove quest-related imports if no longer needed.

### Step 5: Update ResourceGeneratingTask

Pass skill to `RollDrops`:
```csharp
var results = DropResolver.RollDrops(
    taskData.resourceDrops,
    taskData.additionalLootChances,
    worldX,
    associatedSkill  // NEW
);
```

### Step 6: Update MilestoneBonusUI for Resource Unlock Display

Milestones and resource unlocks are **mixed together and sorted by required level** so players see unified progression.

**Uses same prefab:** The existing `MilestoneEntryUIReferences` prefab is used for both. Resource unlock entries:
- Activate `taskImageObject` and set `taskImage` sprite (currently unused fields)
- Hide `toggleButton`, `toggleImage`, `activeText` (not applicable to unlocks)
- Use `passiveText` for the brief description

In `PopulateMilestones()`, replace the current milestone-only loop with a unified approach:
```csharp
// Combine milestones and resource unlocks, sorted by required level
var milestoneEntries = skill.milestones
    .Where(m => m != null)
    .Select(m => (Level: m.UnlockLevel, IsMilestone: true, Milestone: m, Unlock: (Skill.ResourceUnlockEntry)null));

var unlockEntries = (skill.resourceUnlocks ?? new List<Skill.ResourceUnlockEntry>())
    .Where(u => u != null && u.task != null)
    .Select(u => (Level: u.requiredLevel, IsMilestone: false, Milestone: (MilestoneDefinition)null, Unlock: u));

var allEntries = milestoneEntries.Concat(unlockEntries)
    .OrderBy(e => e.Level)
    .ToList();

foreach (var item in allEntries)
{
    var entry = Instantiate(entryPrefab, entryParent);
    var refs = entry.GetComponent<MilestoneEntryUIReferences>();
    if (refs == null) continue;

    if (item.IsMilestone)
    {
        var binding = new EntryBinding(skill, item.Milestone, refs, entry.GetComponent<Image>() ?? refs.ToggleImage);
        bindings.Add(binding);
        ConfigureEntry(binding);
    }
    else
    {
        ConfigureResourceUnlockEntry(refs, item.Unlock, currentLevel);
    }
}
```

Add new method:
```csharp
private void ConfigureResourceUnlockEntry(MilestoneEntryUIReferences refs,
    Skill.ResourceUnlockEntry unlock, int currentLevel)
{
    bool isUnlocked = currentLevel >= unlock.requiredLevel;

    // Show task icon
    if (refs.TaskImageObject != null)
        refs.TaskImageObject.SetActive(true);
    if (refs.TaskImage != null)
    {
        refs.TaskImage.sprite = unlock.overrideIcon ?? unlock.task?.taskIcon;
        refs.TaskImage.color = isUnlocked ? unlockedColor : lockedColor;
    }

    // Title - shows what unlocks
    string name = unlock.task?.taskName ?? "Resource";
    refs.NameText?.SetText(isUnlocked
        ? $"{name} Unlocked"
        : $"{name} | Unlocks at level {unlock.requiredLevel}");

    // Brief description instead of passive/active text
    if (refs.PassiveText != null)
    {
        refs.PassiveText.text = !string.IsNullOrEmpty(unlock.description)
            ? unlock.description
            : string.Empty;
    }

    // Hide toggle and other milestone-specific elements
    if (refs.ActiveText != null) refs.ActiveText.gameObject.SetActive(false);
    if (refs.ToggleButton != null) refs.ToggleButton.gameObject.SetActive(false);
    if (refs.ToggleImage != null) refs.ToggleImage.gameObject.SetActive(false);
    if (refs.SetText != null) refs.SetText.text = string.Empty;
    if (refs.SetIcon != null) refs.SetIcon.enabled = false;
}
```

### Step 7: Update Quest Assets

For quests that previously unlocked resources:
- Change reward from "unlock" to `SkillExperience` (grants XP to the relevant skill)
- The XP amount should help players reach unlock levels naturally through questing

### Step 8: Create Migration Editor Script (DISPOSABLE)

Create `Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs`:

**Two-part approach:**

**Part A: Create `Assets/Scripts/Skills/ResourceUnlockConfig.cs`** (ScriptableObject for editable level mapping)

```csharp
// Assets/Scripts/Skills/ResourceUnlockConfig.cs
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Tasks;
using Sirenix.OdinInspector;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "ResourceUnlockConfig", menuName = "SO/Resource Unlock Config")]
    public class ResourceUnlockConfig : ScriptableObject
    {
        [System.Serializable]
        public class UnlockMapping
        {
            public TaskData task;
            public int requiredLevel;
            [TextArea] public string description;
        }

        [ListDrawerSettings(ShowFoldout = true)]
        public List<UnlockMapping> farmingUnlocks = new();

        [ListDrawerSettings(ShowFoldout = true)]
        public List<UnlockMapping> fishingUnlocks = new();

        [ListDrawerSettings(ShowFoldout = true)]
        public List<UnlockMapping> miningUnlocks = new();

        [ListDrawerSettings(ShowFoldout = true)]
        public List<UnlockMapping> woodcuttingUnlocks = new();

        [ListDrawerSettings(ShowFoldout = true)]
        public List<UnlockMapping> lootingUnlocks = new();

        public List<UnlockMapping> GetUnlocksForSkill(Skill skill)
        {
            if (skill == null) return new();

            return skill.skillName?.ToLower() switch
            {
                "farming" => farmingUnlocks,
                "fishing" => fishingUnlocks,
                "mining" => miningUnlocks,
                "woodcutting" => woodcuttingUnlocks,
                "looting" => lootingUnlocks,
                _ => new()
            };
        }
    }
}
```

**Part B: Create `Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs`** (DISPOSABLE)

```csharp
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
using TimelessEchoes.Quests;

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
            config = ScriptableObject.CreateInstance<ResourceUnlockConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
        }

        Undo.RecordObject(config, "Generate Unlock Config");

        // Find all TaskData with requiredQuest
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
        config.woodcuttingUnlocks = BuildUnlockList(tasksBySkill.GetValueOrDefault("woodcutting"));
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

        // Sort by: quest chain order first, then minX distance
        var sorted = tasks
            .OrderBy(t => GetQuestChainOrder(t))
            .ThenBy(t => t.minX)
            .ToList();

        var result = new List<ResourceUnlockConfig.UnlockMapping>();
        int chainPosition = 0;

        foreach (var task in sorted)
        {
            // Calculate level: base + chain position increment + distance contribution
            int levelFromChain = BASE_LEVEL + (chainPosition * LEVEL_INCREMENT);
            int levelFromDistance = Mathf.RoundToInt(task.minX * DISTANCE_TO_LEVEL_FACTOR);
            int finalLevel = Mathf.Max(1, levelFromChain + levelFromDistance);

            result.Add(new ResourceUnlockConfig.UnlockMapping
            {
                task = task,
                requiredLevel = finalLevel,
                description = $"Unlocks {task.taskName}"
            });

            // Only increment chain position for quest-gated tasks
            if (task.requiredQuest != null)
                chainPosition++;
        }

        return result;
    }

    private static int GetQuestChainOrder(TaskData task)
    {
        if (task.requiredQuest == null)
            return 0; // No quest = available from start

        // Walk up quest chain to count depth
        int depth = 1;
        var quest = task.requiredQuest;
        while (quest.requiredQuests != null && quest.requiredQuests.Count > 0)
        {
            depth++;
            quest = quest.requiredQuests[0]; // Follow first prerequisite
        }
        return depth;
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

                skill.resourceUnlocks.Add(new Skill.ResourceUnlockEntry
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
```

**Usage:**
1. Run code changes first (Steps 1-6)
2. Run `Tools > Migration > 1. Generate Config from Quest Chains` - creates editable config
3. Open `Assets/Resources/ResourceUnlockConfig.asset` and **review/adjust levels**
4. Run `Tools > Migration > 2. Apply Config to Assets` - writes to Skill/TaskData
5. Repeat steps 3-4 as needed for balancing
6. **Delete migration tool and config after final validation**

---

## Verification

1. **Run migration tool:** Execute `Tools > Migration > 1. Generate Config` then `2. Apply Config`
2. **Inspect data:** Check Skill assets have `resourceUnlocks` populated, TaskData has `requiredSkillLevel` set
3. **Fresh save:** Start new game, verify resources are locked by skill level
4. **Level up test:** Gain skill levels, verify resources unlock at correct thresholds
5. **UI test:** Open skill panel, verify resource unlock entries show with icons mixed among milestones
6. **Task spawning:** Verify locked tasks don't appear, unlocked tasks do
7. **Drop filtering:** Verify locked resource drops don't occur, unlocked ones do

---

## Final Cleanup (after validation)

1. Delete `Assets/Scripts/Editor/ResourceUnlockMigrationTool.cs`
2. Delete `Assets/Scripts/Skills/ResourceUnlockConfig.cs`
3. Delete `Assets/Resources/ResourceUnlockConfig.asset`
4. Remove `requiredQuest` field from `TaskData.cs` and `ResourceDrop.cs` (now unused)
5. Remove quest-related imports from `DropResolver.cs` and `ProceduralTaskGenerator.cs`

---

## Notes

- The existing `TaskWeightService` toggle system remains unchanged (doubles spawn weight)
- Resource unlock entries are informational only - no toggle, no active/passive effects
- This is a clean break from old saves - no migration needed
- Migration tool is disposable - delete after validating the populated data
