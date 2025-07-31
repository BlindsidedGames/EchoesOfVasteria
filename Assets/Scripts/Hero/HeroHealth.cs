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
            controller = GetComponent<HeroController>();
            if (!controller || !controller.IsEcho)
            {
                if (Instance != null && Instance != this) Destroy(Instance.gameObject);
                Instance = this;
            }
            base.Awake();
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
                var min = fullDamage * 0.1f;
                return Mathf.Max(fullDamage - controller.Defense, min);
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
    }
}