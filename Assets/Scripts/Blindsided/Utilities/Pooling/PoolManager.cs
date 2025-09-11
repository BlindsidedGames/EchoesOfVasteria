using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Playables;

namespace Blindsided.Utilities.Pooling
{
    /// <summary>
    /// Generic pooling system based on Unity's ObjectPool.
    /// Use Get to retrieve instances and Release to return them.
    /// </summary>
    public static class PoolManager
    {
        private static Transform poolRoot;
        private static bool isQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Application.quitting += OnApplicationQuitting;
        }
        
        // Ensure any PlayableGraphs created by components like PlayableDirector
        // are explicitly destroyed before destroying pooled objects to avoid
        // "PlayableGraph was not destroyed" warnings during teardown.
        private static void CleanupPlayables(GameObject o)
        {
            if (o == null) return;
            // Clean up directors on this object and its children
            var directors = o.GetComponentsInChildren<PlayableDirector>(true);
            for (int i = 0; i < directors.Length; i++)
            {
                var d = directors[i];
                // Guard against invalid or already-disposed graphs
                if (d != null)
                {
                    var graph = d.playableGraph;
                    if (graph.IsValid())
                    {
                        // Stop playback and destroy the graph
                        d.Stop();
                        graph.Destroy();
                    }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Ensure clean state after domain reloads in the Editor
            poolRoot = null;
            isQuitting = false;
            poolsByPrefabId.Clear();
            poolsByName.Clear();
        }

        private static void OnApplicationQuitting()
        {
            isQuitting = true;
            // Ensure pooled inactive objects run their destroy callbacks so
            // any PlayableGraphs are torn down before process exit.
            if (poolsByPrefabId.Count > 0)
            {
                foreach (var kv in poolsByPrefabId)
                {
                    kv.Value.pool?.Clear();
                }
            }
            if (poolsByName.Count > 0)
            {
                foreach (var kv in poolsByName)
                {
                    kv.Value.pool?.Clear();
                }
            }
        }
        private static Transform PoolRoot
        {
            get
            {
                // Never create or fetch the pool root while not playing or during application quit
                if (!Application.isPlaying || isQuitting)
                {
                    return null;
                }

                if (poolRoot == null)
                {
                    var existing = GameObject.Find("PooledObjectsRoot");
                    if (existing == null)
                    {
                        existing = new GameObject("PooledObjectsRoot");
                    }
                    // Ensure the root is not tied to the active scene to avoid cleanup warnings.
                    Object.DontDestroyOnLoad(existing);
                    existing.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
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
            // Defensive: if a destroyed instance slipped into the pool (e.g.,
            // someone called Destroy instead of PoolManager.Release), obj can be null.
            // In that case, recreate an instance so callers don't NRE, and log a hint.
            if (obj == null)
            {
                Debug.LogError($"Pool '{info.key}' returned a destroyed instance. " +
                               "Ensure pooled objects are returned via PoolManager.Release, not Destroy().");
                obj = Object.Instantiate(prefab);
                info.total++;
            }
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
            // During teardown or when not playing, do not reparent or pool — just destroy.
            if (!Application.isPlaying || isQuitting)
            {
                Object.Destroy(obj);
                return;
            }
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

                            // Re-enable disabled task components when retrieved from pool so
                            // TaskController can pick them up again on rebuild.
                            // Include children because tasks may live on nested objects in prefabs
                            var components = o.GetComponentsInChildren<MonoBehaviour>(true);
                            for (int i = 0; i < components.Length; i++)
                            {
                                var mb = components[i];
                                if (mb is TimelessEchoes.Tasks.ITask && mb is Behaviour beh && !beh.enabled)
                                {
                                    beh.enabled = true;
                                }
                            }
                        }
                    },
                    actionOnRelease: o =>
                    {
                        if (o != null)
                        {
                            // Stop and destroy any playable graphs before pooling to prevent leaks
                            CleanupPlayables(o);
                            // Reparent to a global pool root outside of runtime map (if available)
                            var root = PoolRoot;
                            if (root != null)
                                o.transform.SetParent(root, false);
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
                            // Proactively tear down playable graphs to prevent leak warnings
                            CleanupPlayables(o);
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

                            // Re-enable disabled task components when retrieved from pool.
                            var components = o.GetComponents<MonoBehaviour>();
                            for (int i = 0; i < components.Length; i++)
                            {
                                var mb = components[i];
                                if (mb is TimelessEchoes.Tasks.ITask && mb is Behaviour beh && !beh.enabled)
                                {
                                    beh.enabled = true;
                                }
                            }
                        }
                    },
                    actionOnRelease: o =>
                    {
                        if (o != null)
                        {
                            // Stop and destroy any playable graphs before pooling to prevent leaks
                            CleanupPlayables(o);
                            var root = PoolRoot;
                            if (root != null)
                                o.transform.SetParent(root, false);
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
                            // Proactively tear down playable graphs to prevent leak warnings
                            CleanupPlayables(o);
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
