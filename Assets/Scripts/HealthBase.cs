using System;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Base component for objects that have health.
    /// Handles health values, healing and floating text display.
    /// </summary>
    public abstract class HealthBase : MonoBehaviour, IDamageable, IHasHealth
    {
        [SerializeField] protected int maxHealth = 10;
        [SerializeField] protected SlicedFilledImage healthBar;
        [SerializeField, Range(0f, 1f)] protected float minFillPercent = 0.05f;
        [SerializeField] protected HealthBarSpriteOption[] barSprites;
        
        // Set by TakeDamage when the incoming hit appears to be a critical strike
        protected bool lastHitWasCritical;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public virtual void TakeDamage(float amount, float bonusDamage = 0f)
        {
            if (CurrentHealth <= 0f) return;

            float total = CalculateDamage(amount + bonusDamage);
            CurrentHealth -= total;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            // Heuristic: consider it a crit if bonusDamage is at least the base portion
            lastHitWasCritical = bonusDamage >= amount - 0.0001f;

            if (Application.isPlaying && ShouldShowFloatingText())
                ShowFloatingText(total, bonusDamage);

            AfterDamage(total);

            if (CurrentHealth <= 0f)
                OnZeroHealth();
        }

        protected virtual float CalculateDamage(float fullDamage)
        {
            return fullDamage;
        }

        protected virtual void AfterDamage(float total) { }

        protected virtual void OnZeroHealth()
        {
            OnDeath?.Invoke();
        }

        public virtual void Heal(float amount)
        {
            if (amount <= 0f || CurrentHealth >= MaxHealth) return;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public float CurrentHealth { get; protected set; }
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        protected void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public SlicedFilledImage HealthBar
        {
            get => healthBar;
            set
            {
                healthBar = value;
                UpdateBar();
            }
        }

        public virtual void Init(int hp)
        {
            maxHealth = hp;
            CurrentHealth = hp;
            UpdateBar();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        protected virtual void UpdateBar()
        {
            if (healthBar == null) return;

            var percent = MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
            healthBar.fillAmount = Mathf.Max(percent, minFillPercent);

            if (barSprites != null && barSprites.Length > 0)
            {
                var chosen = healthBar.sprite;
                var best = -1f;
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

        protected void ShowFloatingText(float total, float bonusDamage)
        {
            string text = CalcUtils.FormatNumber(total);
            if (bonusDamage != 0f)
                text += $"<size=70%><color=#60C560>+{CalcUtils.FormatNumber(bonusDamage)}</color></size>";
            var size = GetFloatingTextSize() * (lastHitWasCritical ? 1.5f : 1f);
            if (lastHitWasCritical)
            {
                var critTag = TimelessEchoes.Upgrades.StatIconLookup.GetIconTag(TimelessEchoes.Upgrades.StatIconLookup.StatKey.CritChance);
                if (!string.IsNullOrEmpty(critTag))
                    text = $"{critTag} " + text;
            }
            FloatingText.SpawnDamageText(text, transform.position + Vector3.up, GetFloatingTextColor(),
                size, null, GetFloatingTextDuration());
        }

        protected abstract Color GetFloatingTextColor();
        protected abstract float GetFloatingTextSize();
        protected abstract float GetFloatingTextDuration();
        protected virtual bool ShouldShowFloatingText() => true;

        [Serializable]
        public struct HealthBarSpriteOption
        {
            public Sprite sprite;
            [Range(0f, 1f)] public float minPercent;
        }
    }
}
