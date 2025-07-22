using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Buffs
{
    [ManageableData]
    [CreateAssetMenu(fileName = "BuffRecipe", menuName = "SO/Buff Recipe")]
    public class BuffRecipe : ScriptableObject
    {
        [TitleGroup("General")]
        [Tooltip("Display name for this buff. If empty the asset name will be used.")]
        public string title;

        [TitleGroup("General")]
        [TextArea]
        public string description;

        [TitleGroup("General")]
        public Sprite buffIcon;

        [TitleGroup("General")]
        [MinValue(0f)]
        public float baseDuration = 30f;

        [TitleGroup("General")]
        [Tooltip("Percent of longest run distance this buff remains active. 0 = no distance limit")]
        [Range(0f,1f)]
        public float distancePercent;

        [TitleGroup("General")]
        [SerializeField]
        public TimelessEchoes.EchoSpawnConfig echoSpawnConfig;

        [TitleGroup("Effects")]
        [Range(-100f, 100f)]
        public float moveSpeedPercent;

        [TitleGroup("Effects")]
        [Range(-100f, 100f)]
        public float damagePercent;

        [TitleGroup("Effects")]
        [Range(-100f, 100f)]
        public float defensePercent;

        [TitleGroup("Effects")]
        [Range(-100f, 100f)]
        public float attackSpeedPercent;

        [TitleGroup("Effects")]
        [Range(-100f, 100f)]
        public float taskSpeedPercent;

        [TitleGroup("Effects")]
        [Tooltip("Percent of damage returned as health while active.")]
        [Range(0f, 100f)]
        public float lifestealPercent;

        [TitleGroup("Effects")]
        [Tooltip("Tasks complete instantly while active.")]
        public bool instantTasks;

        [TitleGroup("Requirements")]
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
