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
        private static Transform poolRoot;
        private static Transform PoolRoot
        {
            get
            {
                if (poolRoot == null)
                {
                    var existing = GameObject.Find("PooledObjectsRoot");
                    if (existing == null)
                    {
                        existing = new GameObject("PooledObjectsRoot");
                        Object.DontDestroyOnLoad(existing);
                    }
                    poolRoot = existing.transform;
                }
                return poolRoot;
            }
        }
        private class PoolInfo
        {
            public IObjectPool<GameObject> pool;
            public int total;
            public int active;
            public int inactive;
            public string key; // prefab name or named pool key
        }

        private static readonly Dictionary<int, PoolInfo> poolsByPrefabId = new();
        private static readonly Dictionary<string, PoolInfo> poolsByName = new();
        private const int InactiveWarningThreshold = 100;

        /// <summary>
        /// Create a pool for the given prefab and optionally prewarm it.
        /// </summary>
        public static void CreatePool(GameObject prefab, int initialSize = 0)
        {
            var info = GetOrCreatePrefabPool(prefab);
            for (int i = 0; i < initialSize; i++)
            {
                var obj = info.pool.Get();
                info.pool.Release(obj);
            }
        }

        /// <summary>
        /// Get an instance of the given prefab from its pool.
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            var info = GetOrCreatePrefabPool(prefab);
            var obj = info.pool.Get();
            var marker = obj.GetComponent<PooledObject>() ?? obj.AddComponent<PooledObject>();
            marker.pool = info.pool;
            marker.inPool = false;
            return obj;
        }

        /// <summary>
        /// Get or create a pooled empty GameObject by name (for segment roots, etc.).
        /// The returned object will be active. Rename/set parent as needed by caller.
        /// </summary>
        public static GameObject GetNamed(string name)
        {
            var info = GetOrCreateNamedPool(name);
            var obj = info.pool.Get();
            obj.name = name;
            var marker = obj.GetComponent<PooledObject>() ?? obj.AddComponent<PooledObject>();
            marker.pool = info.pool;
            return obj;
        }

        /// <summary>
        /// Release a pooled object back to its pool.
        /// </summary>
        public static void Release(GameObject obj)
        {
            if (obj == null) return;
            var marker = obj.GetComponent<PooledObject>();
            if (marker != null && marker.pool != null)
            {
                // Avoid double-release
                if (marker.inPool)
                    return;
                marker.pool.Release(obj);
                marker.inPool = true;
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        private static PoolInfo GetOrCreatePrefabPool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            if (!poolsByPrefabId.TryGetValue(id, out var info))
            {
                info = new PoolInfo { key = prefab.name };
                info.pool = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        var o = Object.Instantiate(prefab);
                        info.total++;
                        return o;
                    },
                    actionOnGet: o =>
                    {
                        if (o != null)
                        {
                            o.SetActive(true);
                            info.active++;
                            if (info.inactive > 0) info.inactive--;
                            var m = o.GetComponent<PooledObject>();
                            if (m != null) m.inPool = false;
                        }
                    },
                    actionOnRelease: o =>
                    {
                        if (o != null)
                        {
                            // Reparent to a global pool root outside of runtime map
                            o.transform.SetParent(PoolRoot, false);
                            o.SetActive(false);
                            info.inactive++;
                            if (info.active > 0) info.active--;
                            if (info.inactive >= InactiveWarningThreshold)
                                Debug.LogWarning($"Pool '{info.key}' has {info.inactive} inactive instances.");
                            var m = o.GetComponent<PooledObject>();
                            if (m != null) m.inPool = true;
                        }
                    },
                    actionOnDestroy: o =>
                    {
                        if (o != null)
                        {
                            Object.Destroy(o);
                            if (info.total > 0) info.total--;
                        }
                    });
                poolsByPrefabId[id] = info;
            }
            return info;
        }

        private static PoolInfo GetOrCreateNamedPool(string name)
        {
            if (!poolsByName.TryGetValue(name, out var info))
            {
                info = new PoolInfo { key = name };
                info.pool = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        var go = new GameObject(name);
                        info.total++;
                        return go;
                    },
                    actionOnGet: o =>
                    {
                        if (o != null)
                        {
                            o.SetActive(true);
                            info.active++;
                            if (info.inactive > 0) info.inactive--;
                            var m = o.GetComponent<PooledObject>();
                            if (m != null) m.inPool = false;
                        }
                    },
                    actionOnRelease: o =>
                    {
                        if (o != null)
                        {
                            o.transform.SetParent(PoolRoot, false);
                            o.SetActive(false);
                            info.inactive++;
                            if (info.active > 0) info.active--;
                            if (info.inactive >= InactiveWarningThreshold)
                                Debug.LogWarning($"Pool '{info.key}' has {info.inactive} inactive instances.");
                            var m = o.GetComponent<PooledObject>();
                            if (m != null) m.inPool = true;
                        }
                    },
                    actionOnDestroy: o =>
                    {
                        if (o != null)
                        {
                            Object.Destroy(o);
                            if (info.total > 0) info.total--;
                        }
                    });
                poolsByName[name] = info;
            }
            return info;
        }
    }
}
