using System;
using System.Collections.Generic;
using Blindsided.SaveData;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Dictionary extension methods to reduce boilerplate for common patterns.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets an existing value or creates a new one using the factory function.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
        {
            if (!dict.TryGetValue(key, out var value))
                dict[key] = value = factory();
            return value;
        }

        /// <summary>
        /// Increments an integer value in the dictionary by the specified amount.
        /// </summary>
        public static void Increment(this Dictionary<string, int> dict, string key, int amount = 1)
        {
            dict.TryGetValue(key, out var current);
            dict[key] = current + amount;
        }

        /// <summary>
        /// Increments a double value in the dictionary by the specified amount.
        /// </summary>
        public static void IncrementDouble(this Dictionary<string, double> dict, string key, double amount)
        {
            dict.TryGetValue(key, out var current);
            dict[key] = current + amount;
        }

        /// <summary>
        /// Updates a StatAgg entry in the dictionary with a new value.
        /// </summary>
        public static void UpdateStatAgg(this Dictionary<string, GameData.ForgeStats.StatAgg> dict, string key, float value)
        {
            var agg = dict.GetOrCreate(key, () => new GameData.ForgeStats.StatAgg());
            agg.count++;
            agg.sum += value;
            if (value < agg.min) agg.min = value;
            if (value > agg.max) agg.max = value;
        }

        /// <summary>
        /// Updates a FloatAgg entry in the dictionary with a new value.
        /// </summary>
        public static void UpdateFloatAgg(this Dictionary<string, GameData.ForgeStats.FloatAgg> dict, string key, float value)
        {
            var agg = dict.GetOrCreate(key, () => new GameData.ForgeStats.FloatAgg());
            agg.count++;
            agg.sum += value;
        }

        /// <summary>
        /// Updates a nested StatAgg dictionary (e.g., StatRollsByRarity[rarity][statId]).
        /// </summary>
        public static void UpdateNestedStatAgg(
            this Dictionary<string, Dictionary<string, GameData.ForgeStats.StatAgg>> dict,
            string outerKey,
            string innerKey,
            float value)
        {
            var inner = dict.GetOrCreate(outerKey, () => new Dictionary<string, GameData.ForgeStats.StatAgg>());
            inner.UpdateStatAgg(innerKey, value);
        }

        /// <summary>
        /// Updates a nested int dictionary (e.g., RarityCountsByCore[core][rarity]).
        /// </summary>
        public static void IncrementNested(
            this Dictionary<string, Dictionary<string, int>> dict,
            string outerKey,
            string innerKey,
            int amount = 1)
        {
            var inner = dict.GetOrCreate(outerKey, () => new Dictionary<string, int>());
            inner.Increment(innerKey, amount);
        }

        /// <summary>
        /// Updates the highest value in a dictionary if the new value exceeds the current one.
        /// </summary>
        public static void UpdateMax(this Dictionary<string, float> dict, string key, float value)
        {
            if (!dict.TryGetValue(key, out var current) || value > current)
                dict[key] = value;
        }

        /// <summary>
        /// Updates the lowest value in a dictionary if the new value is less than the current one.
        /// </summary>
        public static void UpdateMin(this Dictionary<string, float> dict, string key, float value)
        {
            if (!dict.TryGetValue(key, out var current) || value < current)
                dict[key] = value;
        }
    }
}
