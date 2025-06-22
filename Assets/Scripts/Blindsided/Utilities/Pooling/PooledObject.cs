using UnityEngine;
using UnityEngine.Pool;

namespace Blindsided.Utilities.Pooling
{
    /// <summary>
    /// Marks an object as pooled and stores the pool reference
    /// so it can be released back to it.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        internal IObjectPool<GameObject> pool;
    }
}
