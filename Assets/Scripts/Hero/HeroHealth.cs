using TimelessEchoes.Stats;
using UnityEngine.Serialization;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Health component specifically for the hero. Handles defense and stat tracking.
    /// </summary>
    [RequireComponent(typeof(HeroController))]
    public class HeroHealth : HealthBase
    {
        public static HeroHealth Instance { get; private set; }
        private HeroController controller;
        public bool Immortal { get; set; }

        [Header("Defense Tuning")]
        [SerializeField]
        private TimelessEchoes.DefenseTuning defenseTuning = new TimelessEchoes.DefenseTuning
        {
            N = 60f
        };

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

        protected override float CalculateDamage(float fullDamage)
        {
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null)
            {
                // Apply simplified defense formula always
                var total = TimelessEchoes.Combat.ApplyDefense(fullDamage, controller.Defense, defenseTuning);
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

        public override void TakeDamage(float amount, float bonusDamage = 0f)
        {
            if (Immortal) return;
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null && controller.IsEcho && Instance != null && Instance != this)
            {
                Instance.TakeDamage(amount * 0.5f, bonusDamage);
                return;
            }
            base.TakeDamage(amount, bonusDamage);
        }

        /// <summary>
        /// Apply damage from an enemy, providing the enemy's level so defense scaling can be applied.
        /// </summary>
        public void TakeDamageFromEnemy(float amount, int enemyLevel, float bonusDamage = 0f)
        {
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null && controller.IsEcho && Instance != null && Instance != this)
            {
                // Echo forwards to main hero with the echo damage reduction
                Instance.TakeDamageFromEnemy(amount * 0.5f, enemyLevel, bonusDamage);
                return;
            }

            pendingAttackerLevel = Mathf.Max(0, enemyLevel);
            TakeDamage(amount, bonusDamage);
        }
    }
}