using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Blindsided.Utilities.Pooling
{
    /// <summary>
    /// Generic pooling system based on Unity's ObjectPool.
    /// Use Get to retrieve instances and Release to return them.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<int, IObjectPool<GameObject>> pools = new();

        /// <summary>
        /// Create a pool for the given prefab and optionally prewarm it.
        /// </summary>
        public static void CreatePool(GameObject prefab, int initialSize = 0)
        {
            var pool = GetPool(prefab);
            for (int i = 0; i < initialSize; i++)
            {
                var obj = pool.Get();
                pool.Release(obj);
            }
        }

        /// <summary>
        /// Get an instance of the given prefab from its pool.
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            var pool = GetPool(prefab);
            var obj = pool.Get();
            var marker = obj.GetComponent<PooledObject>() ?? obj.AddComponent<PooledObject>();
            marker.pool = pool;
            return obj;
        }

        /// <summary>
        /// Release a pooled object back to its pool.
        /// </summary>
        public static void Release(GameObject obj)
        {
            var marker = obj.GetComponent<PooledObject>();
            if (marker != null && marker.pool != null)
                marker.pool.Release(obj);
            else
                Object.Destroy(obj);
        }

        private static IObjectPool<GameObject> GetPool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            if (!pools.TryGetValue(id, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    () => Object.Instantiate(prefab),
                    o => { if (o != null) o.SetActive(true); },
                    o => { if (o != null) o.SetActive(false); },
                    o => { if (o != null) Object.Destroy(o); });
                pools[id] = pool;
            }
            return pool;
        }
    }
}
