using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using System.Reflection;
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
                    foreach (var m in buff.mirrors)
                        if (m != null)
                            Destroy(m.gameObject);
                    buff.mirrors.Clear();
                    activeBuffs.RemoveAt(i);
                    if (buff.recipe != null)
                        TELogger.Log($"Buff {buff.recipe.name} expired", TELogCategory.Buff, this);
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
                SpawnMirrors(buff);
                activeBuffs.Add(buff);
                TELogger.Log($"Buff {recipe.name} added", TELogCategory.Buff, this);
            }
            else
            {
                buff.remaining += recipe.baseDuration;
                if (expireDist > buff.expireAtDistance)
                    buff.expireAtDistance = expireDist;
                TELogger.Log($"Buff {recipe.name} extended", TELogCategory.Buff, this);
                SpawnMirrors(buff);
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

        private void SpawnMirrors(ActiveBuff buff)
        {
            if (buff == null || buff.recipe == null || buff.recipe.mirrorHeroes <= 0)
                return;

            var mainHero = Hero.HeroController.Instance ?? FindFirstObjectByType<Hero.HeroController>();
            if (mainHero == null)
                return;

            var mainController = mainHero.GetComponentInParent<Tasks.TaskController>();
            if (mainController == null)
                return;

            while (buff.mirrors.Count < buff.recipe.mirrorHeroes)
            {
                var mirrorObj = new GameObject("MirrorHero");
                mirrorObj.transform.SetParent(mainController.transform, false);
                var heroClone = Instantiate(mainHero.gameObject, mainHero.transform.position, Quaternion.identity, mirrorObj.transform);
                var heroComp = heroClone.GetComponent<Hero.HeroController>();
                heroComp?.MarkAsMirror();

                var tc = mirrorObj.AddComponent<Tasks.TaskController>();
                tc.hero = heroComp;
                tc.maxBacktrackDistance = mainController.maxBacktrackDistance;
                tc.backtrackingAdditionalWeight = mainController.backtrackingAdditionalWeight;
                var fEnemy = typeof(Tasks.TaskController).GetField("enemyMask", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fEnemy != null)
                    fEnemy.SetValue(tc, fEnemy.GetValue(mainController));
                var fPath = typeof(Tasks.TaskController).GetField("astarPath", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fPath != null)
                    fPath.SetValue(tc, mainController.Pathfinder);
                var fCam = typeof(Tasks.TaskController).GetField("mapCamera", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fCam != null)
                    fCam.SetValue(tc, mainController.MapCamera);
                foreach (var obj in mainController.TaskObjects)
                    tc.AddTaskObject(obj);
                tc.ResetTasks();
                buff.mirrors.Add(heroComp);
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
            foreach (var b in activeBuffs)
            {
                if (b.mirrors != null)
                {
                    foreach (var m in b.mirrors)
                        if (m != null)
                            Destroy(m.gameObject);
                    b.mirrors.Clear();
                }
            }
            activeBuffs.Clear();
        }

        public void UpdateDistance(float heroX)
        {
            for (var i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (heroX >= buff.expireAtDistance)
                {
                    foreach (var m in buff.mirrors)
                        if (m != null)
                            Destroy(m.gameObject);
                    buff.mirrors.Clear();
                    activeBuffs.RemoveAt(i);
                    if (buff.recipe != null)
                        TELogger.Log($"Buff {buff.recipe.name} expired", TELogCategory.Buff, this);
                }
            }
        }


        [Serializable]
        public class ActiveBuff
        {
            public BuffRecipe recipe;
            public float remaining;
            public float expireAtDistance = float.PositiveInfinity;
            public List<Hero.HeroController> mirrors = new();
        }
    }
}