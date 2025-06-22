using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Interface for tracking active units and querying by layer.
    /// </summary>
    public interface ITargetRegistry
    {
        void Register(Transform target);
        void Unregister(Transform target);

        /// <summary>
        /// Find the closest unit to the given position matching the layer mask.
        /// </summary>
        Transform FindClosest(Vector3 position, LayerMask mask, Transform ignore = null);

        /// <summary>
        /// Enumerate all registered units matching the layer mask.
        /// </summary>
        IEnumerable<Transform> GetTargets(LayerMask mask);
    }
}
