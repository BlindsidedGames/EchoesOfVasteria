using TimelessEchoes.Stats;
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

        protected override void Awake()
        {
            if (Instance != null && Instance != this) Destroy(Instance.gameObject);
            Instance = this;
            controller = GetComponent<HeroController>();
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        protected override float CalculateDamage(float fullDamage)
        {
            controller = controller != null ? controller : GetComponent<HeroController>();
            if (controller != null)
            {
                var min = fullDamage * 0.1f;
                return Mathf.Max(fullDamage - controller.Defense, min);
            }

            return fullDamage;
        }

        public override void TakeDamage(float amount, float bonusDamage = 0f)
        {
            if (Immortal)
                return;
            base.TakeDamage(amount, bonusDamage);
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
    }
}