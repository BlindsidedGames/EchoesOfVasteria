using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Buffs
{
    [ManageableData]
    [CreateAssetMenu(fileName = "BuffRecipe", menuName = "SO/Buff Recipe")]
    public class BuffRecipe : ScriptableObject
    {
        [Tooltip("Display name for this buff. If empty the asset name will be used.")]
        public string title;

        [TextArea]
        public string description;

        public Sprite buffIcon;
        [Min(0f)] public float baseDuration = 30f;
        [Range(-100f, 100f)] public float moveSpeedPercent;
        [Range(-100f, 100f)] public float damagePercent;
        [Range(-100f, 100f)] public float defensePercent;
        [Range(-100f, 100f)] public float attackSpeedPercent;
        public List<ResourceRequirement> requirements = new();

        public string Title => string.IsNullOrEmpty(title) ? name : title;
    }

    [Serializable]
    public class ResourceRequirement
    {
        public Resource resource;
        public int amount;
    }
}
