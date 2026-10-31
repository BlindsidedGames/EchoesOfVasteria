using System;
using System.Collections.Generic;
using Blindsided.Utilities;
using Blindsided.Utilities.Pooling;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.Buffs
{
    public partial class BuffManager
    {
        [Serializable]
        public sealed class BuffRunState
        {
            public List<BuffState> Active = new();
            public Dictionary<string, float> Cooldowns = new();
        }

        [Serializable]
        public sealed class BuffState
        {
            public string Recipe;
            public float Remaining;
            public float ExpireAtDistance;
            public int GatheringCompleted;
            public Dictionary<string, double> Bundle = new();
            // Ages survive a new Unity process; Time.time timestamps do not.
            public Dictionary<int, float> EchoCompletionAges = new();
        }

        public BuffRunState CaptureRunState()
        {
            var state = new BuffRunState();
            foreach (var buff in activeBuffs)
            {
                if (buff?.recipe == null || buff.recipe.IsRetired || !(buff.remaining > 0f)) continue;
                var saved = new BuffState
                {
                    Recipe = buff.recipe.name,
                    Remaining = buff.remaining,
                    ExpireAtDistance = buff.expireAtDistance,
                    GatheringCompleted = buff.gathering.completed
                };
                foreach (var pair in buff.gathering.bundle)
                    if (pair.Key != null && FinitePositive(pair.Value)) saved.Bundle[pair.Key.name] = pair.Value;
                foreach (var pair in buff.gathering.echoCompletions)
                {
                    var age = Time.time - pair.Value;
                    if (FiniteNonnegative(age) && age <= 10f) saved.EchoCompletionAges[pair.Key] = age;
                }
                state.Active.Add(saved);
            }
            foreach (var pair in cooldowns)
                if (pair.Key != null && !pair.Key.IsRetired && FinitePositive(pair.Value))
                    state.Cooldowns[pair.Key.name] = pair.Value;
            return state;
        }

        /// <summary>Recreates saved activations after the run's hero and task world are ready.</summary>
        public void RestoreRunState(BuffRunState state)
        {
            // Replacement is not expiry: do not defer old Echoes or start their cooldowns.
            foreach (var buff in activeBuffs)
                if (buff != null)
                    foreach (var echo in buff.echoes)
                        if (echo != null) PoolManager.Release(echo.gameObject);
            activeBuffs.Clear();
            cooldowns.Clear();

            var recipes = new Dictionary<string, BuffRecipe>(StringComparer.Ordinal);
            foreach (var recipe in Recipes)
                if (recipe != null && !recipe.IsRetired) recipes[recipe.name] = recipe;
            var resources = new Dictionary<string, Resource>(StringComparer.Ordinal);
            foreach (var resource in AssetCache.GetAll<Resource>())
                if (resource != null) resources.TryAdd(resource.name, resource);

            if (state?.Active != null)
                foreach (var saved in state.Active)
                {
                    if (saved == null || string.IsNullOrEmpty(saved.Recipe) ||
                        !recipes.TryGetValue(saved.Recipe, out var recipe) ||
                        activeBuffs.Exists(buff => buff.recipe == recipe)) continue;
                    var distanceBased = recipe.durationType == BuffDurationType.DistancePercent;
                    if (distanceBased ? !FinitePositive(saved.ExpireAtDistance) : !FinitePositive(saved.Remaining))
                        continue;
                    var buff = new ActiveBuff
                    {
                        recipe = recipe,
                        effects = recipe.GetAggregatedEffects(),
                        remaining = distanceBased ? float.PositiveInfinity : saved.Remaining,
                        expireAtDistance = distanceBased ? saved.ExpireAtDistance : float.PositiveInfinity
                    };
                    buff.gathering.completed = Mathf.Clamp(saved.GatheringCompleted, 0, 9);
                    if (saved.Bundle != null)
                        foreach (var pair in saved.Bundle)
                            if (pair.Key != null && resources.TryGetValue(pair.Key, out var resource) &&
                                FinitePositive(pair.Value)) buff.gathering.bundle[resource] = pair.Value;
                    if (saved.EchoCompletionAges != null)
                        foreach (var pair in saved.EchoCompletionAges)
                            if (FiniteNonnegative(pair.Value) && pair.Value <= 10f)
                                buff.gathering.echoCompletions[pair.Key] = Time.time - pair.Value;
                    activeBuffs.Add(buff);
                    var count = recipe.GetEchoCount();
                    if (recipe.echoConfig != null && count > 0)
                        foreach (var echo in EchoManager.SpawnEchoes(recipe.echoConfig, buff.remaining,
                                     null, false, count, excludeFromCap: true))
                            if (echo != null) buff.echoes.Add(echo);
                }
            if (state?.Cooldowns != null)
                foreach (var pair in state.Cooldowns)
                    if (pair.Key != null && recipes.TryGetValue(pair.Key, out var recipe) && FinitePositive(pair.Value))
                        cooldowns[recipe] = pair.Value;

            TaskWeightService.NotifyWeightsChanged();
            ApplyTimeScaleModifier();
            HeroStatSystem.MarkDirty(DirtyMask.Damage | DirtyMask.AttackRate | DirtyMask.CritChance |
                DirtyMask.CritDamage | DirtyMask.Move | DirtyMask.Defense | DirtyMask.Regen, DirtyReason.BuffsChanged);
            NotifyAutoBuffChanged();
        }

        private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        private static bool FinitePositive(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    }
}
