using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Redirects damage taken by an echo to the main hero's health component.
    ///     This keeps echoes feeling like extensions of the hero without introducing a separate health pool.
    /// </summary>
    public class EchoHealthProxy : MonoBehaviour, IDamageable, IHasHealth
    {
        /// <summary>
        ///     Current hero health value. Returns zero when the hero component is unavailable (e.g. during scene loads).
        /// </summary>
        public float CurrentHealth => HeroHealth.Instance != null ? HeroHealth.Instance.CurrentHealth : 0f;

        /// <summary>
        ///     Maximum hero health value. Mirrors <see cref="HeroHealth" /> so UI elements remain consistent.
        /// </summary>
        public float MaxHealth => HeroHealth.Instance != null ? HeroHealth.Instance.MaxHealth : 0f;

        /// <summary>
        ///     Forwards incoming damage to <see cref="HeroHealth" />. Echoes do not absorb damage independently.
        /// </summary>
        public void TakeDamage(float amount, float bonusDamage = 0f, bool isCritical = false)
        {
            HeroHealth.Instance?.TakeDamage(amount, bonusDamage, isCritical);
        }
    }
}
