using System;
using System.Collections.Generic;
using TimelessEchoes.Skills;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Utility for rolling weighted ResourceDrop tables with optional extra slots.
    /// Handles world position and skill level requirements.
    /// </summary>
    public static class DropResolver
    {
        public struct DropResult
        {
            public Resource resource;
            public int count;
        }

        // Scratch lists to avoid allocations in hot path
        [ThreadStatic] private static List<DropResult> _scratchResults;
        [ThreadStatic] private static List<ResourceDrop> _scratchAvailable;

        /// <summary>
        /// Rolls from the provided drops, returning results with amounts calculated
        /// using the same biased range logic as runtime systems.
        /// </summary>
        /// <param name="drops">Potential drops to choose from.</param>
        /// <param name="additionalLootChances">Sequential extra slot chances after the first guaranteed roll (0-1 values).</param>
        /// <param name="worldX">World position used for min/max filters.</param>
        /// <param name="associatedSkill">Skill used for unlock level checks.</param>
        /// <param name="ignoreSkillLevel">If true, required skill level checks are skipped.</param>
        /// <param name="rand">Optional random generator; defaults to UnityEngine.Random.value.</param>
        public static List<DropResult> RollDrops(
            IEnumerable<ResourceDrop> drops,
            IList<float> additionalLootChances,
            float worldX,
            Skill associatedSkill = null,
            bool ignoreSkillLevel = false,
            Func<float> rand = null)
        {
            // Initialize scratch lists on first use
            _scratchResults ??= new List<DropResult>(8);
            _scratchAvailable ??= new List<ResourceDrop>(16);
            _scratchResults.Clear();
            _scratchAvailable.Clear();

            if (drops == null) return _scratchResults;

            foreach (var drop in drops)
            {
                if (drop == null || drop.resource == null) continue;
                if (drop.weight <= 0f) continue;
                if (!ignoreSkillLevel && !IsDropUnlocked(drop, associatedSkill)) continue;
                if (worldX < drop.minX || worldX > drop.maxX) continue;
                _scratchAvailable.Add(drop);
            }

            if (_scratchAvailable.Count == 0) return _scratchResults;

            float Rand() => rand != null ? rand() : UnityEngine.Random.value;

            int ChooseWeightedIndex(List<ResourceDrop> pool)
            {
                float total = 0f;
                for (int i = 0; i < pool.Count; i++)
                    total += Mathf.Max(0f, pool[i].weight);
                float roll = Rand() * total;
                for (int i = 0; i < pool.Count; i++)
                {
                    roll -= Mathf.Max(0f, pool[i].weight);
                    if (roll <= 0f) return i;
                }
                return pool.Count - 1;
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
                    _scratchResults.Add(new DropResult { resource = drop.resource, count = amt });
            }

            int selectedIdx = ChooseWeightedIndex(_scratchAvailable);
            var selected = _scratchAvailable[selectedIdx];
            _scratchAvailable.RemoveAt(selectedIdx);
            AddResult(selected);

            if (additionalLootChances != null)
            {
                foreach (var chance in additionalLootChances)
                {
                    if (_scratchAvailable.Count == 0) break;
                    if (Rand() > Mathf.Clamp01(chance)) break;
                    selectedIdx = ChooseWeightedIndex(_scratchAvailable);
                    selected = _scratchAvailable[selectedIdx];
                    _scratchAvailable.RemoveAt(selectedIdx);
                    AddResult(selected);
                }
            }

            return _scratchResults;
        }

        private static bool IsDropUnlocked(ResourceDrop drop, Skill skill)
        {
            if (drop.requiredSkillLevel <= 0)
                return true;  // No requirement

            if (skill == null)
                return false;

            var controller = SkillController.Instance;
            if (controller == null)
                return false;

            int level = controller.GetProgress(skill)?.Level ?? 1;
            return level >= drop.requiredSkillLevel;
        }
    }
}
