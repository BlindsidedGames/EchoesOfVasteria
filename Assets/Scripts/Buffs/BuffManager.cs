using System;
using System.Collections;
using System.Collections.Generic;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;
using static Blindsided.SaveData.StaticReferences;
using Resources = UnityEngine.Resources;

namespace TimelessEchoes.Buffs
{
    /// <summary>
    ///     Manages active buffs and persists them across scenes.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        private BuffRecipe[] cachedRecipes;

        private readonly List<ActiveBuff> activeBuffs = new();
        private readonly List<BuffRecipe> slotAssignments = new(new BuffRecipe[5]);
        private readonly List<bool> autoCastSlots = new(new bool[5]);


        public int UnlockedSlots
        {
            get
            {
                if (oracle != null)
                    return Mathf.Clamp(oracle.saveData.UnlockedBuffSlots, 1, slotAssignments.Count);
                return 1;
            }
        }

        public int UnlockedAutoSlots
        {
            get
            {
                if (oracle != null)
                    return Mathf.Clamp(oracle.saveData.UnlockedAutoBuffSlots, 0, autoCastSlots.Count);
                return 0;
            }
        }

        private bool ticking = true;

        public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

        public bool AnySlotAutoBuffing
        {
            get
            {
                foreach (var flag in autoCastSlots)
                    if (flag)
                        return true;
                return false;
            }
        }

        public bool InstantTaskBuffActive => GetEffectTotal(BuffEffectType.InstantTasks) > 0f;

        public IEnumerable<BuffRecipe> Recipes
        {
            get
            {
                if (cachedRecipes == null || cachedRecipes.Length == 0)
                    cachedRecipes = Resources.LoadAll<BuffRecipe>("Buffs");
                return cachedRecipes;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            OnLoadData += LoadSlots;
        }

        private void Start()
        {
            StartCoroutine(DelayedLoad());
        }

        private IEnumerator DelayedLoad()
        {
            yield return null;
            LoadSlots();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnLoadData -= LoadSlots;
        }

        /// <summary>
        ///     Update all buff timers.
        /// </summary>
        /// <param name="delta">Time elapsed since last tick.</param>
        public void Tick(float delta)
        {
            if (ticking)
                TickBuffs(delta);

            AutoCastBuffs();
        }

        /// <summary>Pauses ticking of buff timers.</summary>
        public void Pause()
        {
            ticking = false;
        }

        /// <summary>Resumes ticking of buff timers.</summary>
        public void Resume()
        {
            ticking = true;
        }

        private void TickBuffs(float delta)
        {
            if (delta <= 0f) return;
            for (var i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.remaining -= delta;
                if (buff.remaining <= 0f) RemoveBuffAt(i);
            }
        }

        public bool CanActivate(BuffRecipe recipe)
        {
            if (recipe == null) return false;
            if (oracle != null)
            {
                oracle.saveData.BuffLevels ??= new Dictionary<string, int>();
                if (!oracle.saveData.BuffLevels.TryGetValue(recipe.name, out var lvl) || lvl <= 0)
                    return false;
            }
            if (!recipe.IsUnlocked()) return false;
            if (GetRemaining(recipe) > 0f) return false;

            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            if (recipe.durationType == BuffDurationType.Distance && tracker != null)
            {
                var expireDist = tracker.LongestRun * recipe.durationMagnitude;
                if (tracker.CurrentRunDistance >= expireDist)
                    return false;
            }

            return true;
        }

        public bool PurchaseBuff(BuffRecipe recipe)
        {
            if (!CanActivate(recipe)) return false;

            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            tracker?.AddBuffCast();

            if (activeBuffs.Exists(b => b.recipe == recipe))
                return false;

            var durationMag = recipe.durationMagnitude;
            var echoCount = recipe.echoSpawnConfig != null ? recipe.echoSpawnConfig.echoCount : 0;
            foreach (var eff in recipe.GetActiveEffects())
            {
                switch (eff.type)
                {
                    case BuffEffectType.Duration:
                        durationMag += eff.magnitude;
                        break;
                    case BuffEffectType.EchoCount:
                        echoCount += Mathf.RoundToInt(eff.magnitude);
                        break;
                }
            }

            float expireDist = float.PositiveInfinity;
            float remaining = float.PositiveInfinity;
            if (recipe.durationType == BuffDurationType.Distance && tracker != null)
                expireDist = tracker.LongestRun * durationMag;
            else
                remaining = durationMag;

            var buff = new ActiveBuff
            {
                recipe = recipe,
                remaining = remaining,
                expireAtDistance = expireDist
            };
            activeBuffs.Add(buff);
            Log($"Buff {recipe.name} added", TELogCategory.Buff, this);

            if (recipe.echoSpawnConfig != null && echoCount > 0)
            {
                var needed = echoCount - buff.echoes.Count;
                if (needed > 0)
                {
                    var spawned = EchoManager.SpawnEchoes(recipe.echoSpawnConfig, float.PositiveInfinity,
                        null, false, needed);
                    foreach (var c in spawned)
                        if (c != null)
                            buff.echoes.Add(c);
                }
            }

            return true;
        }

        public float GetRemaining(BuffRecipe recipe)
        {
            var buff = activeBuffs.Find(b => b.recipe == recipe);
            return buff != null ? buff.remaining : 0f;
        }

        private float GetEffectTotal(BuffEffectType type)
        {
            float total = 0f;
            foreach (var b in activeBuffs)
            {
                if (b.recipe == null) continue;
                foreach (var eff in b.recipe.GetActiveEffects())
                    if (eff.type == type)
                        total += eff.magnitude;
            }
            return total;
        }

        public float MoveSpeedMultiplier => 1f + GetEffectTotal(BuffEffectType.MoveSpeed) / 100f;

        public float DamageMultiplier => 1f + GetEffectTotal(BuffEffectType.Damage) / 100f;

        public float DefenseMultiplier => 1f + GetEffectTotal(BuffEffectType.Defense) / 100f;

        public float AttackSpeedMultiplier => 1f + GetEffectTotal(BuffEffectType.AttackSpeed) / 100f;

        public float TaskSpeedMultiplier => 1f + GetEffectTotal(BuffEffectType.TaskSpeed) / 100f;

        public float LifestealPercent => GetEffectTotal(BuffEffectType.Lifesteal);

        private void LoadSlots()
        {
            if (oracle == null) return;
            oracle.saveData.BuffSlots ??= new List<string>();
            while (oracle.saveData.BuffSlots.Count < slotAssignments.Count)
                oracle.saveData.BuffSlots.Add(null);
            oracle.saveData.AutoBuffSlots ??= new List<bool>();
            while (oracle.saveData.AutoBuffSlots.Count < autoCastSlots.Count)
                oracle.saveData.AutoBuffSlots.Add(false);
            oracle.saveData.BuffLevels ??= new Dictionary<string, int>();
            for (var i = 0; i < slotAssignments.Count; i++)
            {
                var name = oracle.saveData.BuffSlots[i];
                slotAssignments[i] = null;
                if (string.IsNullOrEmpty(name)) continue;
                foreach (var rec in Recipes)
                    if (rec != null && rec.name == name)
                    {
                        slotAssignments[i] = rec;
                        break;
                    }
                autoCastSlots[i] = oracle.saveData.AutoBuffSlots[i];
            }
        }

        public bool IsSlotUnlocked(int slot)
        {
            return slot >= 0 && slot < UnlockedSlots;
        }

        public void AssignBuff(int slot, BuffRecipe recipe)
        {
            if (!IsSlotUnlocked(slot)) return;

            if (recipe != null)
                for (var i = 0; i < slotAssignments.Count; i++)
                    if (slotAssignments[i] == recipe)
                    {
                        slotAssignments[i] = null;
                        if (oracle != null && oracle.saveData.BuffSlots != null)
                            oracle.saveData.BuffSlots[i] = null;
                    }

            slotAssignments[slot] = recipe;

            if (oracle != null && oracle.saveData.BuffSlots != null)
                oracle.saveData.BuffSlots[slot] = recipe ? recipe.name : null;
        }

        public bool ActivateSlot(int slot)
        {
            if (!IsSlotUnlocked(slot)) return false;
            var recipe = slotAssignments[slot];
            return recipe != null && CanActivate(recipe) && PurchaseBuff(recipe);
        }

        public void UnlockSlots(int count)
        {
            if (oracle == null || count <= 0) return;
            var newCount = Mathf.Clamp(oracle.saveData.UnlockedBuffSlots + count, 1, slotAssignments.Count);
            oracle.saveData.UnlockedBuffSlots = newCount;
        }

        public void UnlockAutoSlots(int count)
        {
            if (oracle == null || count <= 0) return;
            var newCount = Mathf.Clamp(oracle.saveData.UnlockedAutoBuffSlots + count, 0, autoCastSlots.Count);
            oracle.saveData.UnlockedAutoBuffSlots = newCount;
        }

        public bool IsAutoSlotUnlocked(int slot)
        {
            return slot >= 0 && slot < UnlockedAutoSlots;
        }

        public bool IsSlotAutoCasting(int slot)
        {
            return slot >= 0 && slot < autoCastSlots.Count && autoCastSlots[slot];
        }

        public void ToggleSlotAutoCast(int slot)
        {
            if (!IsAutoSlotUnlocked(slot)) return;
            if (slot < 0 || slot >= autoCastSlots.Count) return;
            autoCastSlots[slot] = !autoCastSlots[slot];
            if (oracle != null && oracle.saveData.AutoBuffSlots != null)
                SetAutoBuffSlot(slot, autoCastSlots[slot]);
        }

        public BuffRecipe GetAssigned(int slot)
        {
            return slot >= 0 && slot < slotAssignments.Count ? slotAssignments[slot] : null;
        }

        public void ClearActiveBuffs()
        {
            foreach (var buff in activeBuffs)
                DestroyEchoes(buff);
            activeBuffs.Clear();
        }

        public void UpdateDistance(float heroX)
        {
            for (var i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (heroX >= buff.expireAtDistance) RemoveBuffAt(i);
            }
        }

        private void RemoveBuffAt(int index)
        {
            if (index < 0 || index >= activeBuffs.Count) return;
            var buff = activeBuffs[index];
            DestroyEchoes(buff);
            activeBuffs.RemoveAt(index);
            if (buff.recipe != null)
                Log($"Buff {buff.recipe.name} expired", TELogCategory.Buff, this);
        }

        private void DestroyEchoes(ActiveBuff buff)
        {
            if (buff == null || buff.echoes == null) return;
            foreach (var c in buff.echoes)
                if (c != null)
                    Destroy(c.gameObject);
            buff.echoes.Clear();
        }

        private void AutoCastBuffs()
        {
            for (var i = 0; i < slotAssignments.Count && i < UnlockedSlots && i < autoCastSlots.Count; i++)
            {
                if (!autoCastSlots[i]) continue;
                var recipe = slotAssignments[i];
                if (recipe == null) continue;
                if (CanActivate(recipe))
                    PurchaseBuff(recipe);
            }
        }


        [Serializable]
        public class ActiveBuff
        {
            public BuffRecipe recipe;
            public float remaining;
            public float expireAtDistance = float.PositiveInfinity;
            public List<HeroController> echoes = new();
        }
    }
}