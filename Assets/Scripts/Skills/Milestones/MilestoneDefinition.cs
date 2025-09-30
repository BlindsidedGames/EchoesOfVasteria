using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
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
        [SerializeField] [EnumToggleButtons] private MilestoneTierMode tierMode = MilestoneTierMode.Manual;
        [SerializeField, ShowIf(nameof(tierMode), MilestoneTierMode.Manual)] private List<MilestoneTier> tiers = new();
        [SerializeField, ShowIf(nameof(tierMode), MilestoneTierMode.Infinite)] private MilestoneInfiniteTier infiniteTier = new();

        public string Id => string.IsNullOrEmpty(milestoneId) ? name : milestoneId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public MilestoneSet Set => set;
        public Sprite SetIcon => setIcon;
        public bool CanActivate => canActivate && activeEffect != null;
        public MilestoneEffectDefinition PassiveEffect => passiveEffect;
        public MilestoneEffectDefinition ActiveEffect => activeEffect;
        public MilestoneTierMode TierMode => tierMode;
        public IReadOnlyList<MilestoneTier> ManualTiers => tiers;
        public MilestoneInfiniteTier InfiniteTier => infiniteTier;

        public int UnlockLevel
        {
            get
            {
                return tierMode switch
                {
                    MilestoneTierMode.Manual => tiers.Count > 0 ? tiers[0].requiredLevel : int.MaxValue,
                    MilestoneTierMode.Infinite => infiniteTier != null ? infiniteTier.requiredLevel : int.MaxValue,
                    _ => int.MaxValue
                };
            }
        }

        public int GetTierIndex(int currentLevel)
        {
            return tierMode switch
            {
                MilestoneTierMode.Manual => GetManualTierIndex(currentLevel),
                MilestoneTierMode.Infinite => GetInfiniteTierIndex(currentLevel),
                _ => -1
            };
        }

        private int GetManualTierIndex(int currentLevel)
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

        private int GetInfiniteTierIndex(int currentLevel)
        {
            if (infiniteTier == null)
                return -1;

            int startLevel = Mathf.Max(1, infiniteTier.requiredLevel);
            if (currentLevel < startLevel)
                return -1;

            int interval = Mathf.Max(1, infiniteTier.levelsPerIncrease);
            return (currentLevel - startLevel) / interval;
        }

        public float GetPassiveValue(int tierIndex)
        {
            return tierMode switch
            {
                MilestoneTierMode.Manual => GetManualPassiveValue(tierIndex),
                MilestoneTierMode.Infinite => GetInfinitePassiveValue(tierIndex),
                _ => 0f
            };
        }

        private float GetManualPassiveValue(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= tiers.Count)
                return 0f;
            return tiers[tierIndex].passiveValue;
        }

        private float GetInfinitePassiveValue(int tierIndex)
        {
            if (infiniteTier == null || tierIndex < 0)
                return 0f;
            return infiniteTier.passiveBaseValue + infiniteTier.passiveIncrement * tierIndex;
        }

        public float GetActiveValue(int tierIndex)
        {
            return tierMode switch
            {
                MilestoneTierMode.Manual => GetManualActiveValue(tierIndex),
                MilestoneTierMode.Infinite => GetInfiniteActiveValue(tierIndex),
                _ => 0f
            };
        }

        private float GetManualActiveValue(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= tiers.Count)
                return 0f;
            return tiers[tierIndex].activeValue;
        }

        private float GetInfiniteActiveValue(int tierIndex)
        {
            if (infiniteTier == null || tierIndex < 0)
                return 0f;
            return infiniteTier.activeBaseValue + infiniteTier.activeIncrement * tierIndex;
        }

        public int TierCount => tierMode == MilestoneTierMode.Manual ? tiers.Count : -1;
        public bool IsInfinite => tierMode == MilestoneTierMode.Infinite;

        public string GetPassiveDescriptionForTier(int tierIndex, string skillName)
        {
            if (passiveEffect == null)
                return string.Empty;

            if (tierMode == MilestoneTierMode.Manual)
            {
                if (tiers.Count == 0)
                    return string.Empty;
                tierIndex = Mathf.Clamp(tierIndex, 0, tiers.Count - 1);
            }
            else
            {
                if (tierIndex < 0)
                    tierIndex = 0;
            }

            return passiveEffect.GetDescription(GetPassiveValue(tierIndex), skillName, false);
        }

        public string GetActiveDescriptionForTier(int tierIndex, string skillName)
        {
            if (activeEffect == null)
                return string.Empty;

            if (tierMode == MilestoneTierMode.Manual)
            {
                if (tiers.Count == 0)
                    return string.Empty;
                tierIndex = Mathf.Clamp(tierIndex, 0, tiers.Count - 1);
            }
            else
            {
                if (tierIndex < 0)
                    tierIndex = 0;
            }

            return activeEffect.GetDescription(GetActiveValue(tierIndex), skillName, true);
        }

        public int GetNextTierLevel(int currentLevel)
        {
            return tierMode switch
            {
                MilestoneTierMode.Manual => GetNextManualTierLevel(currentLevel),
                MilestoneTierMode.Infinite => GetNextInfiniteTierLevel(currentLevel),
                _ => -1
            };
        }

        private int GetNextManualTierLevel(int currentLevel)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].requiredLevel > currentLevel)
                    return tiers[i].requiredLevel;
            }
            return -1;
        }

        private int GetNextInfiniteTierLevel(int currentLevel)
        {
            if (infiniteTier == null)
                return -1;

            int startLevel = Mathf.Max(1, infiniteTier.requiredLevel);
            int interval = Mathf.Max(1, infiniteTier.levelsPerIncrease);

            if (currentLevel < startLevel)
                return startLevel;

            int tierIndex = (currentLevel - startLevel) / interval;
            return startLevel + (tierIndex + 1) * interval;
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
        [Button(ButtonSizes.Medium)]
        private void GenerateNewMilestoneId()
        {
            Undo.RecordObject(this, "Generate Milestone ID");
            milestoneId = Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(milestoneId))
            {
                milestoneId = Guid.NewGuid().ToString("N");
                EditorUtility.SetDirty(this);
            }

            if (tierMode == MilestoneTierMode.Manual)
                tiers.Sort((a, b) => a.requiredLevel.CompareTo(b.requiredLevel));
        }
#endif
    }

    public enum MilestoneTierMode
    {
        Manual,
        Infinite
    }

    [Serializable]
    public class MilestoneTier
    {
        public int requiredLevel = 1;
        public float passiveValue = 0f;
        public float activeValue = 0f;
    }

    [Serializable]
    public class MilestoneInfiniteTier
    {
        [Min(1)] public int requiredLevel = 1;
        [Min(1)] public int levelsPerIncrease = 5;
        public float passiveBaseValue = 0f;
        public float passiveIncrement = 0f;
        public float activeBaseValue = 0f;
        public float activeIncrement = 0f;
    }
}

