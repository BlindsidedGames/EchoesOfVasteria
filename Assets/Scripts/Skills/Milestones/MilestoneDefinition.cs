using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "MilestoneDefinition", menuName = "SO/Milestones/Milestone Definition")]
    public class MilestoneDefinition : ScriptableObject
    {
        [SerializeField] [Tooltip("Unique identifier used for save data.")]
        private string milestoneId = string.Empty;
        [SerializeField] private string displayName;
        [SerializeField] private MilestoneSet set = MilestoneSet.None;
        [SerializeField] private Sprite setIcon;
        [SerializeField] private bool canActivate = true;
        [SerializeField] private MilestoneEffectDefinition passiveEffect;
        [SerializeField] private MilestoneEffectDefinition activeEffect;
        [SerializeField] private List<MilestoneTier> tiers = new();

        public string Id => string.IsNullOrEmpty(milestoneId) ? name : milestoneId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public MilestoneSet Set => set;
        public Sprite SetIcon => setIcon;
        public bool CanActivate => canActivate && activeEffect != null;
        public IReadOnlyList<MilestoneTier> Tiers => tiers;
        public MilestoneEffectDefinition PassiveEffect => passiveEffect;
        public MilestoneEffectDefinition ActiveEffect => activeEffect;

        public int UnlockLevel => tiers.Count > 0 ? tiers[0].requiredLevel : int.MaxValue;

        public int GetTierIndex(int currentLevel)
        {
            int index = -1;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (currentLevel >= tiers[i].requiredLevel)
                    index = i;
                else
                    break;
            }
            return index;
        }

        public float GetPassiveValue(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= tiers.Count)
                return 0f;
            return tiers[tierIndex].passiveValue;
        }

        public float GetActiveValue(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= tiers.Count)
                return 0f;
            return tiers[tierIndex].activeValue;
        }

        public int TierCount => tiers.Count;

        public string GetPassiveDescriptionForTier(int tierIndex, string skillName)
        {
            if (passiveEffect == null || tiers.Count == 0)
                return string.Empty;
            tierIndex = Mathf.Clamp(tierIndex, 0, tiers.Count - 1);
            return passiveEffect.GetDescription(GetPassiveValue(tierIndex), skillName, false);
        }

        public string GetActiveDescriptionForTier(int tierIndex, string skillName)
        {
            if (activeEffect == null || tiers.Count == 0)
                return string.Empty;
            tierIndex = Mathf.Clamp(tierIndex, 0, tiers.Count - 1);
            return activeEffect.GetDescription(GetActiveValue(tierIndex), skillName, true);
        }

        public int GetNextTierLevel(int currentLevel)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].requiredLevel > currentLevel)
                    return tiers[i].requiredLevel;
            }
            return -1;
        }

        public string GetPassiveDescription(int skillLevel, string skillName)
        {
            int tierIndex = GetTierIndex(skillLevel);
            if (tierIndex < 0 || passiveEffect == null)
                return string.Empty;
            return passiveEffect.GetDescription(GetPassiveValue(tierIndex), skillName, false);
        }

        public string GetActiveDescription(int skillLevel, string skillName)
        {
            int tierIndex = GetTierIndex(skillLevel);
            if (tierIndex < 0 || activeEffect == null)
                return string.Empty;
            return activeEffect.GetDescription(GetActiveValue(tierIndex), skillName, true);
        }

        public bool HasActiveEffect => activeEffect != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(milestoneId))
            {
                milestoneId = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }

            tiers.Sort((a, b) => a.requiredLevel.CompareTo(b.requiredLevel));
        }
#endif
    }

    [Serializable]
    public class MilestoneTier
    {
        public int requiredLevel = 1;
        public float passiveValue = 0f;
        public float activeValue = 0f;
    }
}
