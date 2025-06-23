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
            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        private void UpdateBar()
        {
            if (healthBar != null)
                healthBar.fillAmount = CurrentHealth / MaxHealth;
        }
    }
}
