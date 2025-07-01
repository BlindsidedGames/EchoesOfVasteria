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
        public Sprite buffIcon;
        [Min(0f)] public float baseDuration = 30f;
        [Range(-100f, 100f)] public float moveSpeedPercent;
        [Range(-100f, 100f)] public float damagePercent;
        [Range(-100f, 100f)] public float defensePercent;
        [Range(-100f, 100f)] public float attackSpeedPercent;
        public List<ResourceRequirement> requirements = new();
    }

    [Serializable]
    public class ResourceRequirement
    {
        public Resource resource;
        public int amount;
    }
}
