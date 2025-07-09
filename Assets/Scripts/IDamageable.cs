using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple interface for objects that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage to the object.
        /// </summary>
        /// <param name="amount">Base damage dealt.</param>
        /// <param name="bonusDamage">Additional bonus damage displayed separately.</param>
        void TakeDamage(float amount, float bonusDamage = 0f);
    }
}
