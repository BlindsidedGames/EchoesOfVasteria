using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Hero;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Buffs
{
    /// <summary>
    ///     Manages active buffs and persists them across scenes.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        private ResourceManager resourceManager;

        private BuffRecipe[] cachedRecipes;

        private readonly List<ActiveBuff> activeBuffs = new();
        private readonly List<BuffRecipe> slotAssignments = new(new BuffRecipe[5]);

        private HeroController SpawnEcho(bool combat)
        {
            var hero = Hero.HeroController.Instance;
            if (hero == null)
                return null;

            Hero.HeroController.PrepareForEcho();
            var obj = Instantiate(hero.gameObject, hero.transform.position, hero.transform.rotation, hero.transform.parent);
            var echo = obj.GetComponent<Hero.HeroController>();
            if (echo != null)
            {
                var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers)
                {
                    var c = r.color;
                    c.a = 0.7f;
                    r.color = c;
                }

                var hp = echo.GetComponent<Hero.HeroHealth>();
                if (hp != null)
                    Object.Destroy(hp);
                echo.gameObject.AddComponent<Hero.EchoHealthProxy>();
                echo.AllowAttacks = combat;
            }

            return echo;
        }

        public int UnlockedSlots
        {
            get
            {
                if (oracle != null)
                    return Mathf.Clamp(oracle.saveData.UnlockedBuffSlots, 1, slotAssignments.Count);
                return 1;
            }
        }
        private bool ticking = true;

        public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;
        public bool InstantTaskBuffActive
        {
            get
            {
                foreach (var b in activeBuffs)
                    if (b.recipe != null && b.recipe.instantTasks)
                        return true;
                return false;
            }
        }
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

            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);

            OnLoadData += LoadSlots;
        }

        private void Start()
        {
            StartCoroutine(DelayedLoad());
        }

        private System.Collections.IEnumerator DelayedLoad()
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
                if (buff.remaining <= 0f)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        private void EnsureResourceManager()
        {
            if (resourceManager == null)
                resourceManager = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
        }

        public bool CanPurchase(BuffRecipe recipe)
        {
            EnsureResourceManager();
            if (recipe == null || resourceManager == null) return false;
            foreach (var req in recipe.requirements)
                if (resourceManager.GetAmount(req.resource) < req.amount)
                    return false;
            return true;
        }

        public bool PurchaseBuff(BuffRecipe recipe)
        {
            EnsureResourceManager();
            if (!CanPurchase(recipe)) return false;

            foreach (var req in recipe.requirements)
                resourceManager?.Spend(req.resource, req.amount);

            var buff = activeBuffs.Find(b => b.recipe == recipe);
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance ??
                          FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            float expireDist = float.PositiveInfinity;
            if (recipe.distancePercent > 0f && tracker != null)
                expireDist = tracker.LongestRun * recipe.distancePercent;
            if (buff == null)
            {
                buff = new ActiveBuff { recipe = recipe, remaining = recipe.baseDuration, expireAtDistance = expireDist };
                activeBuffs.Add(buff);
                TELogger.Log($"Buff {recipe.name} added", TELogCategory.Buff, this);
            }
            else
            {
                buff.remaining += recipe.baseDuration;
                if (expireDist > buff.expireAtDistance)
                    buff.expireAtDistance = expireDist;
                TELogger.Log($"Buff {recipe.name} extended", TELogCategory.Buff, this);
            }

            if (recipe.echoCount > 0)
            {
                int needed = recipe.echoCount - buff.echoes.Count;
                for (int i = 0; i < needed; i++)
                {
                    var c = SpawnEcho(recipe.combatEnabled);
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

        public float MoveSpeedMultiplier
        {
            get
            {
                var percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.moveSpeedPercent;
                return 1f + percent / 100f;
            }
        }

        public float DamageMultiplier
        {
            get
            {
                var percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.damagePercent;
                return 1f + percent / 100f;
            }
        }

        public float DefenseMultiplier
        {
            get
            {
                var percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.defensePercent;
                return 1f + percent / 100f;
            }
        }

        public float AttackSpeedMultiplier
        {
            get
            {
                var percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.attackSpeedPercent;
                return 1f + percent / 100f;
            }
        }

        public float LifestealPercent
        {
            get
            {
                float percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.lifestealPercent;
                return percent;
            }
        }

        private void LoadSlots()
        {
            if (oracle == null) return;
            oracle.saveData.BuffSlots ??= new List<string>();
            while (oracle.saveData.BuffSlots.Count < slotAssignments.Count)
                oracle.saveData.BuffSlots.Add(null);
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
            {
                for (var i = 0; i < slotAssignments.Count; i++)
                {
                    if (slotAssignments[i] == recipe)
                    {
                        slotAssignments[i] = null;
                        if (oracle != null && oracle.saveData.BuffSlots != null)
                            oracle.saveData.BuffSlots[i] = null;
                    }
                }
            }

            slotAssignments[slot] = recipe;

            if (oracle != null && oracle.saveData.BuffSlots != null)
                oracle.saveData.BuffSlots[slot] = recipe ? recipe.name : null;
        }

        public bool ActivateSlot(int slot)
        {
            if (!IsSlotUnlocked(slot)) return false;
            var recipe = slotAssignments[slot];
            return recipe != null && PurchaseBuff(recipe);
        }

        public void UnlockSlots(int count)
        {
            if (oracle == null || count <= 0) return;
            int newCount = Mathf.Clamp(oracle.saveData.UnlockedBuffSlots + count, 1, slotAssignments.Count);
            oracle.saveData.UnlockedBuffSlots = newCount;
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
                if (heroX >= buff.expireAtDistance)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        private void RemoveBuffAt(int index)
        {
            if (index < 0 || index >= activeBuffs.Count) return;
            var buff = activeBuffs[index];
            DestroyEchoes(buff);
            activeBuffs.RemoveAt(index);
            if (buff.recipe != null)
                TELogger.Log($"Buff {buff.recipe.name} expired", TELogCategory.Buff, this);
        }

        private void DestroyEchoes(ActiveBuff buff)
        {
            if (buff == null || buff.echoes == null) return;
            foreach (var c in buff.echoes)
                if (c != null)
                    Destroy(c.gameObject);
            buff.echoes.Clear();
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