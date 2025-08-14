using System;
using System.Collections.Generic;
using UnityEngine;
using static TimelessEchoes.Quests.QuestUtils;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Utility for rolling weighted ResourceDrop tables with optional extra slots.
    /// Handles world position and quest requirements.
    /// </summary>
    public static class DropResolver
    {
        public struct DropResult
        {
            public Resource resource;
            public int count;
        }

        /// <summary>
        /// Rolls from the provided drops, returning results with amounts calculated
        /// using the same biased range logic as runtime systems.
        /// </summary>
        /// <param name="drops">Potential drops to choose from.</param>
        /// <param name="additionalLootChances">Sequential extra slot chances after the first guaranteed roll (0-1 values).</param>
        /// <param name="worldX">World position used for min/max filters.</param>
        /// <param name="ignoreQuest">If true, required quest checks are skipped.</param>
        /// <param name="rand">Optional random generator; defaults to UnityEngine.Random.value.</param>
        public static List<DropResult> RollDrops(IEnumerable<ResourceDrop> drops, IList<float> additionalLootChances, float worldX, bool ignoreQuest = false, Func<float> rand = null)
        {
            float Rand() => rand != null ? rand() : UnityEngine.Random.value;

            var results = new List<DropResult>();
            if (drops == null) return results;

            var available = new List<ResourceDrop>();
            foreach (var drop in drops)
            {
                if (drop == null || drop.resource == null) continue;
                if (drop.weight <= 0f) continue;
                if (!ignoreQuest && drop.requiredQuest != null && !QuestCompleted(drop.requiredQuest.questId)) continue;
                if (worldX < drop.minX || worldX > drop.maxX) continue;
                available.Add(drop);
            }

            if (available.Count == 0) return results;

            ResourceDrop ChooseWeighted(List<ResourceDrop> pool)
            {
                float total = 0f;
                foreach (var d in pool) total += Mathf.Max(0f, d.weight);
                float roll = Rand() * total;
                foreach (var d in pool)
                {
                    roll -= Mathf.Max(0f, d.weight);
                    if (roll <= 0f) return d;
                }
                return pool[pool.Count - 1];
            }

            int RollAmount(ResourceDrop drop)
            {
                int min = Mathf.Max(0, drop.dropRange.x);
                int max = Mathf.Max(min, drop.dropRange.y);
                float t = Rand();
                t *= t; // bias towards lower values
                return Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
            }

            void AddResult(ResourceDrop drop)
            {
                int amt = RollAmount(drop);
                if (amt > 0)
                    results.Add(new DropResult { resource = drop.resource, count = amt });
            }

            var selected = ChooseWeighted(available);
            available.Remove(selected);
            AddResult(selected);

            if (additionalLootChances != null)
            {
                foreach (var chance in additionalLootChances)
                {
                    if (available.Count == 0) break;
                    if (Rand() > Mathf.Clamp01(chance)) break;
                    selected = ChooseWeighted(available);
                    available.Remove(selected);
                    AddResult(selected);
                }
            }

            return results;
        }
    }
}
