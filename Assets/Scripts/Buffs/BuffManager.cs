using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Buffs
{
    /// <summary>
    /// Manages active buffs and persists them across scenes.
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private AnimationCurve diminishingCurve =
            AnimationCurve.Linear(0f, 1f, 60f, 0f);

        private readonly List<ActiveBuff> activeBuffs = new();
        private bool ticking = true;

        public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();

            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        private void Update()
        {
            if (ticking)
                TickBuffs(Time.deltaTime);
        }

        /// <summary>Pauses ticking of buff timers.</summary>
        public void Pause() => ticking = false;

        /// <summary>Resumes ticking of buff timers.</summary>
        public void Resume() => ticking = true;

        private void TickBuffs(float delta)
        {
            if (delta <= 0f) return;
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.remaining -= delta;
                if (buff.remaining <= 0f)
                    activeBuffs.RemoveAt(i);
            }
        }

        public bool CanPurchase(BuffRecipe recipe)
        {
            if (recipe == null) return false;
            foreach (var req in recipe.requirements)
            {
                if (resourceManager != null &&
                    resourceManager.GetAmount(req.resource) < req.amount)
                    return false;
            }
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
            }
            else
            {
                float extra = recipe.baseDuration;
                if (diminishingCurve != null)
                    extra *= diminishingCurve.Evaluate(buff.remaining);
                buff.remaining += extra;
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
                float percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.moveSpeedPercent;
                return 1f + percent / 100f;
            }
        }

        public float DamageMultiplier
        {
            get
            {
                float percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.damagePercent;
                return 1f + percent / 100f;
            }
        }

        public float DefenseMultiplier
        {
            get
            {
                float percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.defensePercent;
                return 1f + percent / 100f;
            }
        }

        public float AttackSpeedMultiplier
        {
            get
            {
                float percent = 0f;
                foreach (var b in activeBuffs)
                    percent += b.recipe.attackSpeedPercent;
                return 1f + percent / 100f;
            }
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, float>();
            foreach (var buff in activeBuffs)
            {
                if (buff.recipe != null && buff.remaining > 0f)
                    dict[buff.recipe.name] = buff.remaining;
            }
            oracle.saveData.ActiveBuffs = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.ActiveBuffs ??= new Dictionary<string, float>();
            activeBuffs.Clear();
            foreach (var recipe in Resources.LoadAll<BuffRecipe>(""))
            {
                if (recipe == null) continue;
                if (oracle.saveData.ActiveBuffs.TryGetValue(recipe.name, out var remain) && remain > 0f)
                    activeBuffs.Add(new ActiveBuff { recipe = recipe, remaining = remain });
            }
        }

        [System.Serializable]
        public class ActiveBuff
        {
            public BuffRecipe recipe;
            public float remaining;
        }
    }
}

