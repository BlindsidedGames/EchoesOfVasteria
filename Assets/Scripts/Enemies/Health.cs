using System;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;

namespace TimelessEchoes.Enemies
{
    public class Health : MonoBehaviour, IDamageable, IHasHealth
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private SlicedFilledImage healthBar;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;

        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            UpdateBar();
        }

        public void Init(int hp)
        {
            maxHealth = hp;
            CurrentHealth = hp;
            UpdateBar();
        }

        public void TakeDamage(float amount)
        {
            if (CurrentHealth <= 0f) return;
            CurrentHealth -= amount;
            UpdateBar();

            // Show floating damage text
            ColorUtility.TryParseHtmlString("#C56260", out var red);
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            bool isHero = GetComponent<TimelessEchoes.Hero.HeroController>() != null;
            var colour = isHero ? orange : red;
            TimelessEchoes.FloatingText.Spawn(Mathf.RoundToInt(amount).ToString(), transform.position + Vector3.up, colour);
            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();

                // Automatically remove enemies when their health reaches zero
                if (GetComponent<Enemy>() != null)
                    Destroy(gameObject);
            }
        }

        private void UpdateBar()
        {
            if (healthBar != null)
                healthBar.fillAmount = CurrentHealth / MaxHealth;
        }
    }
}
