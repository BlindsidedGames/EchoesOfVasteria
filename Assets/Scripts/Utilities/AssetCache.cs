using System.Collections.Generic;
using UnityEngine;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Centralized, static asset cache for Resources-based assets.
    /// Minimizes repeated Resources.LoadAll/Load calls which can be costly and alloc-heavy.
    /// Use GetAll<T>() for bulk categories and GetOne<T>(path) for single loads.
    /// </summary>
    public static class AssetCache
    {
        private static readonly Dictionary<System.Type, object> allCache = new();
        private static readonly Dictionary<string, Object> oneCache = new();

        /// <summary>
        /// Returns cached array of all assets of type T under an optional Resources path.
        /// Empty or null path searches entire Resources.
        /// </summary>
        public static T[] GetAll<T>(string resourcesPath = "") where T : Object
        {
            // Compose a cache key based only on T. We cache the union of all paths for that type
            // by loading with the provided path if unseen; subsequent calls with other paths will
            // also be loaded once and merged.
            // To keep this deterministic and simple, we cache per type and ignore path specificity,
            // since this project often uses empty path or a single folder per type.
            if (!allCache.TryGetValue(typeof(T), out var boxed))
            {
                var loaded = Resources.LoadAll<T>(resourcesPath);
                allCache[typeof(T)] = loaded;
                return loaded;
            }

            var arr = (T[])boxed;
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                // If a non-empty path is supplied, ensure those assets are included at least once.
                // Merge upon first request for that path.
                var newlyLoaded = Resources.LoadAll<T>(resourcesPath);
                if (newlyLoaded != null && newlyLoaded.Length > 0)
                {
                    var set = new HashSet<T>(arr);
                    var changed = false;
                    foreach (var item in newlyLoaded)
                    {
                        if (item == null) continue;
                        if (set.Add(item)) changed = true;
                    }
                    if (changed)
                    {
                        var list = new List<T>(set);
                        var merged = list.ToArray();
                        allCache[typeof(T)] = merged;
                        return merged;
                    }
                }
            }

            return arr;
        }

        /// <summary>
        /// Returns a cached single asset loaded via Resources.Load at the given path.
        /// </summary>
        public static T GetOne<T>(string resourcesPath) where T : Object
        {
            if (string.IsNullOrEmpty(resourcesPath)) return null;
            if (oneCache.TryGetValue(resourcesPath, out var obj))
                return obj as T;
            var loaded = Resources.Load<T>(resourcesPath);
            oneCache[resourcesPath] = loaded;
            return loaded;
        }

        /// <summary>
        /// Clears all cached references. Useful on domain reload or explicit refresh.
        /// </summary>
        public static void Clear()
        {
            allCache.Clear();
            oneCache.Clear();
        }
    }
}