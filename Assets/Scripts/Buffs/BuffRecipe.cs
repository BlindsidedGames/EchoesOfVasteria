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
        [Min(0)] public int echoCount;
        [Range(-100f, 100f)] public float moveSpeedPercent;
        [Range(-100f, 100f)] public float damagePercent;
        [Range(-100f, 100f)] public float defensePercent;
        [Range(-100f, 100f)] public float attackSpeedPercent;
        [Tooltip("Percent of damage returned as health while active.")]
        [Range(0f, 100f)] public float lifestealPercent;
        [Tooltip("Tasks complete instantly while active.")]
        public bool instantTasks;
        [Tooltip("If true, echoes spawned by this buff can fight enemies.")]
        public bool combatEnabled;
        [Tooltip("Percent of longest run distance this buff remains active. 0 = no distance limit")]
        [Range(0f,1f)] public float distancePercent;
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
