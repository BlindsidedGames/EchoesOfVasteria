using UnityEngine;

namespace AutoBattler
{
    /// <summary>
    /// Simple interface for objects that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
