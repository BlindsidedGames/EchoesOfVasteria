using System;
using Blindsided.Utilities;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Enemies
{
    public class Health : MonoBehaviour, IDamageable, IHasHealth
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private SlicedFilledImage healthBar;
        [SerializeField, Range(0f, 1f)] private float minFillPercent = 0.05f;
        [SerializeField] private HealthBarSpriteOption[] barSprites;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (CurrentHealth <= 0f) return;
            var hero = GetComponent<HeroController>();
            if (hero != null)
            {
                float min = amount * 0.1f;
                amount = Mathf.Max(amount - hero.Defense, min);
            }
            CurrentHealth -= amount;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            // Show floating damage text
            ColorUtility.TryParseHtmlString("#C56260", out var red);
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            var isHero = GetComponent<HeroController>() != null;
            var colour = isHero ? red : orange;
            var fontSize = isHero ? 6f : 8f;
            if (Application.isPlaying)
            {
                FloatingText.Spawn(CalcUtils.FormatNumber(amount), transform.position + Vector3.up, colour, fontSize);
            }
            if (isHero)
            {
                var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
                tracker?.AddDamageTaken(amount);
            }
            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();

                // Automatically remove enemies when their health reaches zero
                if (GetComponent<Enemy>() != null)
                    Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || CurrentHealth >= MaxHealth) return;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        public SlicedFilledImage HealthBar
        {
            get => healthBar;
            set
            {
                healthBar = value;
                UpdateBar();
            }
        }

        public void Init(int hp)
        {
            maxHealth = hp;
            CurrentHealth = hp;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void UpdateBar()
        {
            if (healthBar == null) return;

            float percent = MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
            healthBar.fillAmount = Mathf.Max(percent, minFillPercent);

            if (barSprites != null && barSprites.Length > 0)
            {
                Sprite chosen = healthBar.sprite;
                float best = -1f;
                foreach (var opt in barSprites)
                {
                    if (opt.sprite == null) continue;
                    if (percent >= opt.minPercent && opt.minPercent > best)
                    {
                        chosen = opt.sprite;
                        best = opt.minPercent;
                    }
                }

                if (chosen != null)
                    healthBar.sprite = chosen;
            }
        }

        [Serializable]
        public struct HealthBarSpriteOption
        {
            public Sprite sprite;
            [Range(0f, 1f)] public float minPercent;
        }
    }
}
