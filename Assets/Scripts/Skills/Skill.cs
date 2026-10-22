using System.Collections.Generic;
using Sirenix.OdinInspector;
using Blindsided.Utilities;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [System.Serializable]
    public class ResourceUnlockEntry
    {
        public TaskData task;
        public TimelessEchoes.Upgrades.Resource resource;
        public int requiredLevel;
        public bool useOverrideIcon;
        [ShowIf("useOverrideIcon")]
        public Sprite overrideIcon;
        public string description;

        public string DisplayDescription => string.IsNullOrEmpty(description) ? string.Empty :
            description.StartsWith("Unlocks ", System.StringComparison.Ordinal) && (resource || task)
                ? TimelessEchoes.UI.Toolkit.ToolkitLocalization.Text("skills.unlocks-resource", "Unlocks {0}",
                    TimelessEchoes.UI.Toolkit.ToolkitLocalization.Name(resource ? (UnityEngine.Object)resource : task))
                : description;
    }

    [ManageableData]
    [CreateAssetMenu(fileName = "Skill", menuName = "SO/Skill")]
    public class Skill : SerializedScriptableObject
    {
        public string skillName;
        public Sprite skillIcon;
        public float xpForFirstLevel = 10f;
        public float xpLevelMultiplier = 1.5f;
        public float taskSpeedPerLevel = 0.01f;
        public List<MilestoneDefinition> milestones = new();

        [TitleGroup("Resource Unlocks")]
        public List<ResourceUnlockEntry> resourceUnlocks = new();

        public int GetResourceUnlockLevel(TimelessEchoes.Upgrades.Resource resource)
        {
            var level = 0;
            if (!resource || resourceUnlocks == null) return level;
            foreach (var unlock in resourceUnlocks)
                if (unlock != null && unlock.resource == resource) level = Mathf.Max(level, unlock.requiredLevel);
            return level;
        }
    }
}
