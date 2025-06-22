using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Interface for any component that exposes a health value.
    /// </summary>
    public interface IHasHealth
    {
        /// <summary>
        /// Current health of the object.
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Maximum health of the object.
        /// </summary>
        float MaxHealth { get; }
    }
}

