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
                "woodcutting" or "logging" => woodcuttingUnlocks,
                "looting" => lootingUnlocks,
                _ => new()
            };
        }
    }
}
