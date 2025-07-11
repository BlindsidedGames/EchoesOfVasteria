using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
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

        [SerializeField] private List<BuffRecipe> allRecipes = new();

        private readonly List<ActiveBuff> activeBuffs = new();
        private readonly List<BuffRecipe> slotAssignments = new(new BuffRecipe[5]);
        private bool ticking = true;

        public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;
        public IEnumerable<BuffRecipe> Recipes =>
            allRecipes?.Count > 0 ? allRecipes : Resources.LoadAll<BuffRecipe>("");

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

            LoadSlots();
            OnLoadData += LoadSlots;
        }

        private void Start() {}

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
                    activeBuffs.RemoveAt(i);
                    if (buff.recipe != null)
                        TELogger.Log($"Buff {buff.recipe.name} expired", TELogCategory.Buff, this);
                }
            }
        }

        public bool CanPurchase(BuffRecipe recipe)
        {
            if (recipe == null) return false;
            foreach (var req in recipe.requirements)
                if (resourceManager != null &&
                    resourceManager.GetAmount(req.resource) < req.amount)
                    return false;
            return true;
        }

        public bool PurchaseBuff(BuffRecipe recipe)
        {
            if (!CanPurchase(recipe)) return false;

            foreach (var req in recipe.requirements)
                resourceManager?.Spend(req.resource, req.amount);

            var buff = activeBuffs.Find(b => b.recipe == recipe);
            if (buff == null)
            {
                buff = new ActiveBuff { recipe = recipe, remaining = recipe.baseDuration };
                activeBuffs.Add(buff);
                TELogger.Log($"Buff {recipe.name} added", TELogCategory.Buff, this);
            }
            else
            {
                buff.remaining += recipe.baseDuration;
                TELogger.Log($"Buff {recipe.name} extended", TELogCategory.Buff, this);
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

        private void LoadSlots()
        {
            if (oracle == null) return;
            oracle.saveData.BuffSlots ??= new List<string>(new string[5]);
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

        public void AssignBuff(int slot, BuffRecipe recipe)
        {
            if (slot < 0 || slot >= slotAssignments.Count) return;

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
            if (slot < 0 || slot >= slotAssignments.Count) return false;
            var recipe = slotAssignments[slot];
            return recipe != null && PurchaseBuff(recipe);
        }

        public BuffRecipe GetAssigned(int slot)
        {
            return slot >= 0 && slot < slotAssignments.Count ? slotAssignments[slot] : null;
        }

        public void ClearActiveBuffs()
        {
            activeBuffs.Clear();
        }


        [Serializable]
        public class ActiveBuff
        {
            public BuffRecipe recipe;
            public float remaining;
        }
    }
}