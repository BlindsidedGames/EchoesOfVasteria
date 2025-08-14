using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    [Serializable]
    public class RarityWeight
    {
        public RaritySO rarity;
        [MinValue(0f)] public float weight = 0f;
        [Tooltip("Additive weight per Ivan level (can be negative).")]
        public float weightPerLevel = 0f;
    }

    [CreateAssetMenu(fileName = "Core", menuName = "SO/Gear/Core")]
    public class CoreSO : ScriptableObject
    {
        [Range(0, 7)] public int tierIndex;
        [Title("Cost")] public Resource requiredIngot;
        [MinValue(0)] public int ingotCost = 1;

        [Title("Rarity Weights (manual)")]
        [Tooltip("Manual weights normalized at runtime to pick a rarity. Leave 0 to exclude a rarity.")]
        public List<RarityWeight> rarityWeights = new();

        [Title("Slot Weights (optional)")]
        [Tooltip("Optional per-slot weights; if empty, uniform distribution with smart protection.")]
        public List<string> slotNames = new();
        public List<float> slotWeights = new();

        [Title("Salvage Drops")]
        [Tooltip("Resources to award when salvaging an item crafted with this core. Similar format to enemy/task drops.")]
        public List<ResourceDrop> salvageDrops = new();

        [Title("Additional Salvage Slot Chances")]
        [Tooltip("Chance (0-1) for each additional salvage slot; evaluated sequentially after the first guaranteed slot.")]
        [MinValue(0f), MaxValue(1f)]
        public List<float> salvageAdditionalLootChances = new();

        public float GetRarityWeight(RaritySO rarity)
        {
            if (rarity == null) return 0f;
            foreach (var rw in rarityWeights)
                if (rw != null && rw.rarity == rarity)
                    return Mathf.Max(0f, rw.weight);
            return 0f;
        }

        public float GetRarityWeightPerLevel(RaritySO rarity)
        {
            if (rarity == null) return 0f;
            foreach (var rw in rarityWeights)
                if (rw != null && rw.rarity == rarity)
                    return rw.weightPerLevel;
            return 0f;
        }
    }
}


