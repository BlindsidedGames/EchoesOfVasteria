using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Redirects damage taken by an echo to the main hero's health component.
    /// </summary>
    public class EchoHealthProxy : MonoBehaviour, IDamageable, IHasHealth
    {
        public float CurrentHealth => HeroHealth.Instance != null ? HeroHealth.Instance.CurrentHealth : 0f;
        public float MaxHealth => HeroHealth.Instance != null ? HeroHealth.Instance.MaxHealth : 0f;

        public void TakeDamage(float amount, float bonusDamage = 0f, bool isCritical = false)
        {
            HeroHealth.Instance?.TakeDamage(amount, bonusDamage, isCritical);
        }
    }
}
