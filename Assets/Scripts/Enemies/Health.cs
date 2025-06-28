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

        private void Awake()
        {
            CurrentHealth = maxHealth;
            UpdateBar();
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

            // Show floating damage text
            ColorUtility.TryParseHtmlString("#C56260", out var red);
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            var isHero = GetComponent<HeroController>() != null;
            var colour = isHero ? red : orange;
            var fontSize = isHero ? 6f : 8f;
            FloatingText.Spawn(CalcUtils.FormatNumber(amount), transform.position + Vector3.up, colour, fontSize);
            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();

                // Automatically remove enemies when their health reaches zero
                if (GetComponent<Enemy>() != null)
                    Destroy(gameObject);
            }
        }

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;

        public event Action OnDeath;

        public void Init(int hp)
        {
            maxHealth = hp;
            CurrentHealth = hp;
            UpdateBar();
        }

        private void UpdateBar()
        {
            if (healthBar != null)
                healthBar.fillAmount = CurrentHealth / MaxHealth;
        }
    }
}