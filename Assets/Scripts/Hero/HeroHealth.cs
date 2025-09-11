using TimelessEchoes.Stats;
using UnityEngine.Serialization;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Health component specifically for the hero. Handles defense and stat tracking.
    /// </summary>
    public class HeroHealth : HealthBase
    {
        public static HeroHealth Instance { get; private set; }
        private HeroController controller;
        public bool Immortal { get; set; }

        // Defense tuning removed; hero now uses the global default defined in Combat

        // When a projectile from an enemy hits, it sets this before calling TakeDamage
        // so CalculateDamage can apply the scaling defense formula.
        private int pendingAttackerLevel = -1;

        protected override void Awake()
        {
            controller = GetComponent<HeroController>();
            if (!controller || !controller.IsEcho)
            {
                if (Instance != null && Instance != this) Destroy(Instance.gameObject);
                Instance = this;
                base.Awake();
            }
            else
            {
                healthBar = null;
                if (Instance != null)
                    CurrentHealth = Instance.CurrentHealth;
            }
        }

        private void OnDestroy()
        {
            if (!controller || !controller.IsEcho)
            {
                if (Instance == this)
                    Instance = null;
            }
        }

        /// <summary>
        /// Refreshes the health UI/bar without generating floating text or altering health.
        /// </summary>
        public void RefreshUI()
        {
            UpdateBar();
            RaiseHealthChanged();
        }

        /// <summary>
        /// Sets the current health and updates UI without invoking damage/heal side effects
        /// like floating text, crit flags, or death triggers.
        /// </summary>
        /// <param name="value">New current health value (clamped to [0, MaxHealth]).</param>
        public void SetCurrentHealthSilently(float value)
        {
            var clamped = Mathf.Clamp(value, 0f, MaxHealth);
            CurrentHealth = clamped;
            UpdateBar();
            RaiseHealthChanged();
        }

        /// <summary>
        /// Applies a change to MaxHealth while adjusting CurrentHealth according to rules:
        /// - If new max is higher, increase current by the delta (up to new max).
        /// - If new max is lower, do not reduce current unless it exceeds the new max
        ///   (i.e., clamp down only if above new max). Optionally enforces a minimum max of 1.
        /// This does not trigger damage/death side effects.
        /// </summary>
        /// <param name="newMax">New maximum health value.</param>
        /// <param name="enforceFloor">If true, enforce a minimum MaxHealth of 1.</param>
        public void ApplyMaxHealthChange(int newMax, bool enforceFloor = true)
        {
            var targetMax = enforceFloor ? Mathf.Max(1, newMax) : Mathf.Max(0, newMax);
            var oldMax = Mathf.RoundToInt(MaxHealth);
            var current = CurrentHealth;

            float newCurrent;
            if (targetMax > oldMax)
            {
                var delta = targetMax - oldMax;
                newCurrent = Mathf.Min(current + delta, targetMax);
            }
            else
            {
                newCurrent = Mathf.Min(current, targetMax);
            }

            maxHealth = targetMax;
            CurrentHealth = Mathf.Clamp(newCurrent, 0f, targetMax);
            UpdateBar();
            RaiseHealthChanged();
        }

        protected override float CalculateDamage(float fullDamage)
        {
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null)
            {
                // Apply simplified defense formula using Combat's default tuning (single source of truth)
                var total = TimelessEchoes.Combat.ApplyDefense(fullDamage, controller.Defense);
                pendingAttackerLevel = -1;
                return total;
            }

            return fullDamage;
        }

        protected override void AfterDamage(float total)
        {
            var tracker = GameplayStatTracker.Instance ??
                          FindFirstObjectByType<GameplayStatTracker>();
            tracker?.AddDamageTaken(total);
        }

        protected override Color GetFloatingTextColor()
        {
            ColorUtility.TryParseHtmlString("#C56260", out var red);
            return red;
        }

        protected override float GetFloatingTextSize()
        {
            return 6f;
        }

        protected override float GetFloatingTextDuration()
        {
            return Blindsided.SaveData.StaticReferences.PlayerDamageTextDuration;
        }

        protected override bool ShouldShowFloatingText()
        {
            return Blindsided.SaveData.StaticReferences.PlayerFloatingDamage;
        }

        public override void TakeDamage(float amount, float bonusDamage = 0f, bool isCritical = false)
        {
            if (Immortal) return;
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null && controller.IsEcho && Instance != null && Instance != this)
            {
                Instance.TakeDamage(amount * 0.5f, bonusDamage, isCritical);
                return;
            }
            base.TakeDamage(amount, bonusDamage, isCritical);
        }

        /// <summary>
        /// Apply damage from an enemy, providing the enemy's level so defense scaling can be applied.
        /// </summary>
        public void TakeDamageFromEnemy(float amount, int enemyLevel, float bonusDamage = 0f, bool isCritical = false)
        {
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null && controller.IsEcho && Instance != null && Instance != this)
            {
                // Echo forwards to main hero with the echo damage reduction
                Instance.TakeDamageFromEnemy(amount * 0.5f, enemyLevel, bonusDamage, isCritical);
                return;
            }

            pendingAttackerLevel = Mathf.Max(0, enemyLevel);
            TakeDamage(amount, bonusDamage, isCritical);
        }
    }
}
